using System.ComponentModel;
using System.Text.Json;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli.Commands;

sealed class VersionsCommand : AsyncCommand<VersionsCommand.Settings>
{
    public sealed class Settings : BaseSettings
    {
        [CommandArgument(0, "<package>")]
        [Description("NuGet package ID")]
        public string Package { get; init; } = "";

        [CommandOption("-s|--source")]
        [Description("Package source name or URL. Defaults to nuget.org.")]
        public string? Source { get; init; }

        [CommandOption("--pre")]
        [Description("Include prerelease versions")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool Pre { get; init; }

        [CommandOption("--head <n>")]
        [Description("Show only the N most recent versions")]
        public int? Head { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context, Settings settings, CancellationToken ct)
    {
        var sourceUrl = PackageSourceResolver.ResolveSourceUrl(settings.Source);
        var client = new NuGetRegistrationClient();

        IReadOnlyList<PackageVersionInfo> allVersions = [];
        try
        {
            if (settings.Json)
                allVersions = await client.GetVersionsAsync(settings.Package, sourceUrl, ct);
            else
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync(
                        $"Fetching versions for [bold]{settings.Package}[/]...",
                        async _ => allVersions = await client.GetVersionsAsync(settings.Package, sourceUrl, ct));
        }
        catch (PackageNotFoundException ex)
        {
            return PrintError(ex.Message, settings.Json);
        }

        IReadOnlyList<PackageVersionInfo> versions = settings.Pre
            ? allVersions
            : allVersions.Where(v => !v.IsPrerelease).ToList();

        var totalCount = versions.Count;
        if (settings.Head is { } n)
            versions = [.. versions.Take(n)];

        if (settings.Json)
            PrintJson(versions, totalCount, settings);
        else
            PrintFormatted(versions, totalCount, settings);

        return 0;
    }

    static void PrintJson(IReadOnlyList<PackageVersionInfo> versions, int totalCount, Settings settings) =>
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            package = settings.Package,
            total = totalCount,
            versions = versions.Select(v => new
            {
                version = v.Version,
                published = v.Published.ToString("yyyy-MM-dd"),
                isPrerelease = v.IsPrerelease
            })
        }, BaseSettings.JsonOptions));

    static void PrintFormatted(IReadOnlyList<PackageVersionInfo> versions, int totalCount, Settings settings)
    {
        var countLabel = versions.Count < totalCount
            ? $"{versions.Count} of {totalCount}"
            : $"{versions.Count}";
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(settings.Package)}[/] — {countLabel} version(s)\n");

        if (settings.Pre)
        {
            var pre = versions.Where(v => v.IsPrerelease).ToList();
            AnsiConsole.MarkupLine("[yellow]prerelease[/]");
            if (pre.Count == 0)
                AnsiConsole.MarkupLine("  [dim](none)[/]");
            else
                foreach (var v in pre)
                    AnsiConsole.MarkupLine(
                        $"  [cyan]{Markup.Escape(v.Version)}[/]    {v.Published:yyyy-MM-dd}");

            AnsiConsole.WriteLine();
        }

        var stable = versions.Where(v => !v.IsPrerelease).ToList();
        AnsiConsole.MarkupLine("[green]stable[/]");
        if (stable.Count == 0)
            AnsiConsole.MarkupLine("  [dim](none)[/]");
        else
            foreach (var v in stable)
                AnsiConsole.MarkupLine(
                    $"  [cyan]{Markup.Escape(v.Version)}[/]    {v.Published:yyyy-MM-dd}");
    }

    static int PrintError(string message, bool json)
    {
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(new { error = message }, BaseSettings.JsonOptions));
        else
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
        return 1;
    }
}
