using System.IO.Compression;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace ScatMan.Core;

public sealed class PackageDownloader(string? cacheRoot = null)
{
    readonly string _cacheRoot = cacheRoot
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".scatman", "cache");

    public async Task<IReadOnlyList<string>> DownloadAsync(
        string packageId, string version, CancellationToken ct = default)
    {
        var packageVersion = NuGetVersion.Parse(version);
        var packageDir = Path.Combine(_cacheRoot, packageId.ToLowerInvariant(), version);

        if (!Directory.Exists(packageDir))
            await DownloadAndExtractAsync(packageId, packageVersion, packageDir, ct);

        return SelectAssemblies(packageDir);
    }

    async Task DownloadAndExtractAsync(
        string packageId, NuGetVersion version, string destDir, CancellationToken ct)
    {
        var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var resource = await repo.GetResourceAsync<FindPackageByIdResource>(ct);

        Directory.CreateDirectory(destDir);

        var nupkgPath = Path.Combine(destDir, $"{packageId}.nupkg");
        using (var stream = File.Create(nupkgPath))
            await resource.CopyNupkgToStreamAsync(
                packageId, version, stream, new SourceCacheContext(), NullLogger.Instance, ct);

        ZipFile.ExtractToDirectory(nupkgPath, destDir, overwriteFiles: true);
        File.Delete(nupkgPath);
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
