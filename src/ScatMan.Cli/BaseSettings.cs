using System.ComponentModel;
using System.Text.Json;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;

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
    [Description("Package version")]
    public string Version { get; init; } = "";

    internal async Task<IReadOnlyList<string>> FetchAssembliesAsync(CancellationToken ct)
    {
        var downloader = new PackageDownloader();

        if (Json)
            return await downloader.DownloadAsync(Package, Version, ct);

        IReadOnlyList<string> assemblies = [];
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"Downloading [bold]{Package} {Version}[/]...",
                async _ => assemblies = await downloader.DownloadAsync(Package, Version, ct));

        return assemblies;
    }
}
