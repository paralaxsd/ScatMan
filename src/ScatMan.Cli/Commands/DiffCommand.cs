using System.ComponentModel;
using System.Text.Json;
using ScatMan.Cli;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli.Commands;

sealed class DiffCommand : AsyncCommand<DiffCommand.Settings>
{
    public sealed class Settings : BaseSettings
    {
        [CommandArgument(0, "<package>")]
        [Description("NuGet package ID")]
        public string Package { get; init; } = "";

        [CommandArgument(1, "<version1>")]
        [Description("First version (or alias: latest / latest-pre)")]
        public string Version1 { get; init; } = "";

        [CommandArgument(2, "<version2>")]
        [Description("Second version (or alias: latest / latest-pre)")]
        public string Version2 { get; init; } = "";

        [CommandOption("-t|--type")]
        [Description("Restrict diff to a single type (full or simple name)")]
        public string? TypeName { get; init; }

        [CommandOption("-s|--source")]
        [Description("Package source name or URL. Defaults to nuget.org.")]
        public string? Source { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        var sourceUrl = PackageSourceResolver.ResolveSourceUrl(settings.Source);
        var downloader = new PackageDownloader(sourceUrl: sourceUrl);

        string resolved1, resolved2;
        IReadOnlyList<string> assemblies1 = [], assemblies2 = [];

        try
        {
            resolved1 = await PackageVersionResolver.ResolveAsync(settings.Package, settings.Version1, sourceUrl, ct);
            resolved2 = await PackageVersionResolver.ResolveAsync(settings.Package, settings.Version2, sourceUrl, ct);
        }
        catch (PackageNotFoundException ex)
        {
            PrintError(ex.Message, settings.Json);
            return 1;
        }

        if (settings.Json)
        {
            assemblies1 = await downloader.DownloadAsync(settings.Package, resolved1, ct);
            assemblies2 = await downloader.DownloadAsync(settings.Package, resolved2, ct);
        }
        else
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(
                    $"Downloading [bold]{settings.Package} {resolved1}[/] and [bold]{resolved2}[/]...",
                    async _ =>
                    {
                        assemblies1 = await downloader.DownloadAsync(settings.Package, resolved1, ct);
                        assemblies2 = await downloader.DownloadAsync(settings.Package, resolved2, ct);
                    });
        }

        var diff = new ApiDiffer().Diff(
            settings.Package,
            resolved1, assemblies1,
            resolved2, assemblies2,
            settings.TypeName);

        if (settings.Json) PrintJson(diff, settings);
        else PrintFormatted(diff, settings);

        return 0;
    }

    static void PrintJson(ApiDiff diff, Settings settings)
    {
        var result = new DiffResult(
            diff.Package,
            diff.Version1,
            diff.Version2,
            settings.TypeName,
            diff.AddedTypes,
            diff.RemovedTypes,
            [.. diff.ChangedTypes.Select(t => new DiffTypeResult(
                t.TypeFullName,
                [.. t.Added.Select(m => new DiffMemberResult(m.Kind, m.Name, m.Signature))],
                [.. t.Removed.Select(m => new DiffMemberResult(m.Kind, m.Name, m.Signature))]))]);

        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(ApiDiff diff, Settings settings)
    {
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(diff.Package)}[/] — [grey]{Markup.Escape(diff.Version1)}[/] → [bold]{Markup.Escape(diff.Version2)}[/]\n");

        if (diff.AddedTypes.Count == 0 && diff.RemovedTypes.Count == 0 && diff.ChangedTypes.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No API changes detected.[/]");
            return;
        }

        foreach (var typeName in diff.RemovedTypes)
            AnsiConsole.MarkupLine($"[red]- TYPE  {Markup.Escape(typeName)}[/]");

        foreach (var typeName in diff.AddedTypes)
            AnsiConsole.MarkupLine($"[green]+ TYPE  {Markup.Escape(typeName)}[/]");

        if (diff.AddedTypes.Count > 0 || diff.RemovedTypes.Count > 0)
            AnsiConsole.WriteLine();

        foreach (var typeDiff in diff.ChangedTypes)
        {
            AnsiConsole.MarkupLine($"[bold]{Markup.Escape(typeDiff.TypeFullName)}[/]");

            foreach (var m in typeDiff.Removed)
                AnsiConsole.MarkupLine($"  [red]-[/] [grey]{Markup.Escape(m.Kind)}[/]  {Markup.Escape(m.Signature)}");

            foreach (var m in typeDiff.Added)
                AnsiConsole.MarkupLine($"  [green]+[/] [grey]{Markup.Escape(m.Kind)}[/]  {Markup.Escape(m.Signature)}");

            AnsiConsole.WriteLine();
        }
    }

    static void PrintError(string message, bool json)
    {
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(new { error = message }, BaseSettings.JsonOptions));
        else
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
    }
}
