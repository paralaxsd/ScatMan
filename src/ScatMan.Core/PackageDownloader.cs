using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.IO.Compression;
using System.Net;

namespace ScatMan.Core;

/// <summary>
/// Downloads a NuGet package and its dependencies and returns assembly paths for inspection.
/// </summary>
public sealed class PackageDownloader(string? cacheRoot = null)
{
    const string CacheCompletionMarker = ".scatman.complete";
    const string FlatContainerBaseUrl = "https://api.nuget.org/v3-flatcontainer";

    static readonly HttpClient Http = new();

    readonly string _cacheRoot = cacheRoot
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".scatman", "cache");

    /// <summary>
    /// Downloads package assets and resolves transitive dependencies.
    /// </summary>
    /// <param name="packageId">NuGet package ID.</param>
    /// <param name="version">Exact package version.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Assembly paths from the selected target framework of each package.</returns>
    public async Task<IReadOnlyList<string>> DownloadAsync(
        string packageId, string version, CancellationToken ct = default)
    {
        var paths   = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await CollectAsync(packageId, version, paths, visited, ct);

        return paths;
    }

    async Task CollectAsync(
        string packageId, string version,
        List<string> paths, HashSet<string> visited, CancellationToken ct)
    {
        if (!visited.Add($"{packageId}/{version}")) return;

        var packageDir = Path.Combine(_cacheRoot, packageId.ToLowerInvariant(), version);

        if (!IsCacheEntryComplete(packageDir))
        {
            TryDeleteDirectory(packageDir);
            await DownloadAndExtractAsync(packageId, NuGetVersion.Parse(version), packageDir, ct);
        }

        paths.AddRange(SelectAssemblies(packageDir));

        foreach (var (depId, depVer) in ReadDependencies(packageDir))
            await CollectAsync(depId, depVer, paths, visited, ct);
    }

    async Task DownloadAndExtractAsync(
        string packageId, NuGetVersion version, string destDir, CancellationToken ct)
    {
        var repo     = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var resource = await repo.GetResourceAsync<FindPackageByIdResource>(ct);

        Directory.CreateDirectory(destDir);

        var tempNupkgPath = Path.Combine(
            Path.GetTempPath(),
            $"{packageId}.{version}.{Guid.NewGuid():N}.nupkg");

        try
        {
            bool found;
            await using (var stream = File.Create(tempNupkgPath))
                found = await resource.CopyNupkgToStreamAsync(
                    packageId,
                    version,
                    stream,
                    new SourceCacheContext(),
                    NullLogger.Instance,
                    ct);

            if (!found)
            {
                var downloaded = await DownloadFromFlatContainerAsync(packageId, version, tempNupkgPath, ct);
                if (!downloaded)
                    throw new IOException($"Package '{packageId} {version}' is not available on NuGet.");
            }

            if (!File.Exists(tempNupkgPath) || new FileInfo(tempNupkgPath).Length == 0)
                throw new IOException($"Downloaded package '{packageId} {version}' is empty.");

            await ZipFile.ExtractToDirectoryAsync(
                tempNupkgPath,
                destDir,
                overwriteFiles: true,
                cancellationToken: ct);

            File.WriteAllText(Path.Combine(destDir, CacheCompletionMarker), "ok");
        }
        catch
        {
            TryDeleteDirectory(destDir);
            throw;
        }
        finally
        {
            try { if (File.Exists(tempNupkgPath)) File.Delete(tempNupkgPath); }
            catch { /* ignored */ }
        }
    }

    static async Task<bool> DownloadFromFlatContainerAsync(
        string packageId,
        NuGetVersion version,
        string destinationPath,
        CancellationToken ct)
    {
        var id = packageId.ToLowerInvariant();
        var normalized = version.ToNormalizedString().ToLowerInvariant();
        var url = $"{FlatContainerBaseUrl}/{id}/{normalized}/{id}.{normalized}.nupkg";

        using var response = await Http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(ct);
        await using var target = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await source.CopyToAsync(target, ct);
        return true;
    }

    static bool IsCacheEntryComplete(string packageDir)
    {
        if (!Directory.Exists(packageDir))
            return false;

        if (File.Exists(Path.Combine(packageDir, CacheCompletionMarker)))
            return true;

        var hasNuspec = Directory.GetFiles(packageDir, "*.nuspec", SearchOption.TopDirectoryOnly).Length > 0;
        var hasDll = Directory.GetFiles(packageDir, "*.dll", SearchOption.AllDirectories).Length > 0;
        return hasNuspec && hasDll;
    }

    static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // ignored; a later extract attempt will report actionable errors
        }
    }

    static IEnumerable<(string Id, string Version)> ReadDependencies(string packageDir)
    {
        var nuspecPath = Directory.GetFiles(packageDir, "*.nuspec").FirstOrDefault();
        if (nuspecPath is null) yield break;

        NuspecReader nuspec;
        try   { nuspec = new NuspecReader(nuspecPath); }
        catch { yield break; }

        var group = nuspec.GetDependencyGroups()
            .OrderByDescending(g => RankTfm(SafeShortName(g)))
            .FirstOrDefault();

        if (group is null) yield break;

        foreach (var pkg in group.Packages)
        {
            var resolvedVersion = (pkg.VersionRange.MinVersion ?? pkg.VersionRange.MaxVersion)
                ?.ToNormalizedString();

            if (resolvedVersion is not null)
                yield return (pkg.Id, resolvedVersion);
        }
    }

    static string SafeShortName(PackageDependencyGroup g)
    {
        try   { return g.TargetFramework.GetShortFolderName() ?? ""; }
        catch { return ""; }
    }

    static IReadOnlyList<string> SelectAssemblies(string packageDir)
    {
        var libDir = Path.Combine(packageDir, "lib");
        if (!Directory.Exists(libDir)) return [];

        var allDirs = Directory.GetDirectories(libDir);
        if (allDirs.Length == 0) return [];

        // Strip platform suffix before ranking so net10.0-android ranks same as net10.0.
        static int BaseRank(string dir)
        {
            var tfm  = Path.GetFileName(dir);
            var dash = tfm.IndexOf('-');
            return RankTfm(dash < 0 ? tfm : tfm[..dash]);
        }

        var bestRank = allDirs.Max(BaseRank);
        var bestDirs = allDirs.Where(d => BaseRank(d) == bestRank).ToArray();

        // Deduplicate by filename; platform-specific overrides portable so we include
        // platform extension methods while avoiding duplicate assembly loads in MLC.
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dll in bestDirs.Where(d => !Path.GetFileName(d).Contains('-'))
                                    .SelectMany(d => Directory.GetFiles(d, "*.dll")))
            byName[Path.GetFileName(dll)] = dll;

        foreach (var dll in bestDirs.Where(d => Path.GetFileName(d).Contains('-'))
                                    .SelectMany(d => Directory.GetFiles(d, "*.dll")))
            byName[Path.GetFileName(dll)] = dll;

        return [.. byName.Values];
    }

    static int RankTfm(string tfm) => tfm.ToLowerInvariant() switch
    {
        var t when t.StartsWith("net10.") => 100,
        var t when t.StartsWith("net9.")  => 90,
        var t when t.StartsWith("net8.")  => 80,
        var t when t.StartsWith("net7.")  => 70,
        var t when t.StartsWith("net6.")  => 60,
        var t when t.StartsWith("net5.")  => 50,
        "netstandard2.1"                  => 40,
        "netstandard2.0"                  => 30,
        var t when t.StartsWith("netstandard") => 20,
        var t when t.StartsWith("net4")   => 10,
        _                                 => 0
    };
}
