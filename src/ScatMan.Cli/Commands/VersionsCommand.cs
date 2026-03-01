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

        [CommandOption("--pre")]
        [Description("Include prerelease versions")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool Pre { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context, Settings settings, CancellationToken ct)
    {
        var client = new NuGetRegistrationClient();

        IReadOnlyList<PackageVersionInfo> allVersions = [];
        try
        {
            if (settings.Json)
                allVersions = await client.GetVersionsAsync(settings.Package, ct);
            else
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync(
                        $"Fetching versions for [bold]{settings.Package}[/]...",
                        async _ => allVersions = await client.GetVersionsAsync(settings.Package, ct));
        }
        catch (PackageNotFoundException ex)
        {
            return PrintError(ex.Message, settings.Json);
        }

        var versions = settings.Pre
            ? allVersions
            : allVersions.Where(v => !v.IsPrerelease).ToList();

        if (settings.Json)
            PrintJson(versions, settings);
        else
            PrintFormatted(versions, allVersions, settings);

        return 0;
    }

    static void PrintJson(IReadOnlyList<PackageVersionInfo> versions, Settings settings) =>
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            package = settings.Package,
            versions = versions.Select(v => new
            {
                version = v.Version,
                published = v.Published.ToString("yyyy-MM-dd"),
                isPrerelease = v.IsPrerelease
            })
        }, BaseSettings.JsonOptions));

    static void PrintFormatted(
        IReadOnlyList<PackageVersionInfo> versions,
        IReadOnlyList<PackageVersionInfo> allVersions,
        Settings settings)
    {
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(settings.Package)}[/] — {versions.Count} version(s)\n");

        if (settings.Pre)
        {
            var pre = allVersions.Where(v => v.IsPrerelease).ToList();
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
