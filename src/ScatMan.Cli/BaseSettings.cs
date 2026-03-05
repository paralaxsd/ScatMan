using System.ComponentModel;
using System.Text.Json;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ScatMan.Cli;

class BaseSettings : CommandSettings
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [CommandOption("--json")]
    [Description("Output results as JSON instead of formatted text")]
    public bool Json { get; init; }
}

class PackageSettings : BaseSettings
{
    [CommandArgument(0, "<package>")]
    [Description("NuGet package ID")]
    public string Package { get; init; } = "";

    [CommandArgument(1, "<version>")]
    [Description("Package version, or alias: latest / latest-pre")]
    public string Version { get; init; } = "";

    internal async Task<(IReadOnlyList<string> Assemblies, string ResolvedVersion)> FetchAssembliesAsync(
        CancellationToken ct)
    {
        var downloader = new PackageDownloader();
        var resolvedVersion = await ResolveVersionAsync(ct);

        if (Json)
            return (await downloader.DownloadAsync(Package, resolvedVersion, ct), resolvedVersion);

        var versionLabel = resolvedVersion == Version
            ? resolvedVersion
            : $"{Version} -> {resolvedVersion}";

        IReadOnlyList<string> assemblies = [];
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"Downloading [bold]{Package} {versionLabel}[/]...",
                async _ => assemblies = await downloader.DownloadAsync(Package, resolvedVersion, ct));

        return (assemblies, resolvedVersion);
    }

    async Task<string> ResolveVersionAsync(CancellationToken ct) => 
        await PackageVersionResolver.ResolveAsync(Package, Version, ct);
}
