using System.ComponentModel;
using System.Text.Json;
using ScatMan.Cli;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli.Commands;

sealed class SearchCommand : AsyncCommand<SearchCommand.Settings>
{
    public sealed class Settings : PackageSettings
    {
        [CommandArgument(2, "<query>")]
        [Description("Case-insensitive substring to search for in type and member names")]
        public string Query { get; init; } = "";

        [CommandOption("-n|--namespace")]
        [Description("Restrict search to this namespace")]
        public string? Namespace { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        var assemblies = await settings.FetchAssembliesAsync(ct);
        var hits       = new TypeInspector().Search(assemblies, settings.Query, settings.Namespace);

        if (settings.Json) PrintJson(hits, settings);
        else PrintFormatted(hits, settings);

        return 0;
    }

    static void PrintJson(SearchHits hits, Settings settings)
    {
        var result = new
        {
            package        = settings.Package,
            version        = settings.Version,
            query          = settings.Query,
            @namespace     = settings.Namespace,
            matchingTypes  = hits.Types.Select(t => new { t.FullName, t.Kind, t.Summary }),
            matchingMembers = hits.Members.Select(h => new
            {
                h.TypeName,
                h.Member.Kind,
                h.Member.Signature,
                h.Member.Summary
            })
        };
        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(SearchHits hits, Settings settings)
    {
        var nsLabel = settings.Namespace is { } ns ? $" [[{Markup.Escape(ns)}]]" : "";
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(settings.Package)} {settings.Version}[/]{nsLabel} — search [italic]\"{Markup.Escape(settings.Query)}\"[/]\n");

        AnsiConsole.MarkupLine($"[bold]Types ({hits.Types.Count})[/]");
        if (hits.Types.Count == 0)
            AnsiConsole.MarkupLine("  [yellow](none)[/]");
        else
            foreach (var t in hits.Types)
            {
                AnsiConsole.MarkupLine($"  [cyan]{t.Kind.PadRight(9)}[/]  {Markup.Escape(t.FullName)}");

                if (t.Summary is { Length: > 0 } summary)
                    AnsiConsole.MarkupLine($"             [grey]{Markup.Escape(summary)}[/]");
            }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Members ({hits.Members.Count})[/]");

        if (hits.Members.Count == 0)
        {
            AnsiConsole.MarkupLine("  [yellow](none)[/]");
            return;
        }

        foreach (var group in hits.Members.GroupBy(h => h.TypeName).OrderBy(g => g.Key))
        {
            AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(group.Key)}[/]");
            foreach (var h in group)
            {
                AnsiConsole.MarkupLine($"    [cyan]{h.Member.Kind.PadRight(11)}[/]  {Markup.Escape(h.Member.Signature)}");

                if (h.Member.Summary is { Length: > 0 } summary)
                    AnsiConsole.MarkupLine($"                 [grey]{Markup.Escape(summary)}[/]");
            }
        }
    }
}
