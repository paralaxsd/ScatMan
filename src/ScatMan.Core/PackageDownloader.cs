using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.IO.Compression;

namespace ScatMan.Core;

public sealed class PackageDownloader(string? cacheRoot = null)
{
    readonly string _cacheRoot = cacheRoot
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".scatman", "cache");

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
        if (!Directory.Exists(libDir))
            return [];

        var best = Directory.GetDirectories(libDir)
            .OrderByDescending(d => RankTfm(Path.GetFileName(d)))
            .FirstOrDefault();

        return best is null ? [] : [.. Directory.GetFiles(best, "*.dll")];
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
