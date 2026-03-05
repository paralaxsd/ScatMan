using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.IO.Compression;

namespace ScatMan.Core;

/// <summary>
/// Downloads a NuGet package and its dependencies and returns assembly paths for inspection.
/// </summary>
public sealed class PackageDownloader(string? cacheRoot = null)
{
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

        if (!Directory.Exists(packageDir))
            await DownloadAndExtractAsync(packageId, NuGetVersion.Parse(version), packageDir, ct);

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

        var nupkgPath = Path.Combine(destDir, $"{packageId}.nupkg");
        await using (var stream = File.Create(nupkgPath))
            await resource.CopyNupkgToStreamAsync(
                packageId, version, stream, new SourceCacheContext(), NullLogger.Instance, ct);

        await ZipFile.ExtractToDirectoryAsync(nupkgPath, destDir, overwriteFiles: true, cancellationToken: ct);
        File.Delete(nupkgPath);
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
        var bestDirs = allDirs.Where(d => BaseRank(d) == bestRank);

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
