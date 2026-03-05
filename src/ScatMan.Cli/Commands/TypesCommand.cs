using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;
using TypeDescriptor = ScatMan.Core.TypeDescriptor;
// ReSharper disable All

namespace ScatMan.Cli.Commands;

sealed class TypesCommand : AsyncCommand<TypesCommand.Settings>
{
    public sealed class Settings : PackageSettings
    {
        [CommandOption("-n|--namespace")]
        [Description("Namespace filter: exact or glob pattern (Microsoft.Extensions.FileSystemGlobbing)")]
        public string? Namespace { get; init; }

        [CommandOption("-f|--filter")]
        [Description("Type-name filter: glob pattern; plain text stays case-insensitive substring")]
        public string? Filter { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        var (assemblies, resolvedVersion) = await settings.FetchAssembliesAsync(ct);
        var types = new TypeInspector().GetTypes(assemblies, settings.Namespace);

        if (settings.Filter is { } f)
            types = [.. types.Where(t => TypeFilterMatches(t.Name, f))];

        if (settings.Json) PrintJson(types, settings, resolvedVersion);
        else PrintFormatted(types, settings, resolvedVersion);

        return 0;
    }

    static void PrintJson(
        IReadOnlyList<TypeDescriptor> types,
        Settings settings,
        string resolvedVersion)
    {
        var result = new TypesResult(
            settings.Package,
            resolvedVersion,
            settings.Namespace,
            settings.Filter,
            [.. types.Select(t => new TypeResult(t.FullName, t.Kind, t.Summary))]);

        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(
        IReadOnlyList<TypeDescriptor> types,
        Settings settings,
        string resolvedVersion)
    {
        var nsLabel = settings.Namespace is { } ns ? $" [[{Markup.Escape(ns)}]]" : "";
        var filterLabel = settings.Filter is { } f ? $" ~[italic]{Markup.Escape(f)}[/]" : "";
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(settings.Package)} {resolvedVersion}[/]{nsLabel}{filterLabel} — {types.Count} public type(s)\n");

        foreach (var group in types.GroupBy(t => t.Namespace).OrderBy(g => g.Key))
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(group.Key)}[/]");

            foreach (var t in group)
            {
                AnsiConsole.MarkupLine($"  [cyan]{t.Kind.PadRight(9)}[/]  {Markup.Escape(t.Name)}");

                if (t.Summary is { Length: > 0 } summary)
                    AnsiConsole.MarkupLine($"             [grey]{Markup.Escape(summary)}[/]");
            }

            AnsiConsole.WriteLine();
        }

        if (types.Count == 0)
            AnsiConsole.MarkupLine("  [yellow](no public types found)[/]");
    }

    static bool TypeFilterMatches(string typeName, string filter) =>
        PatternFilters.MatchesSubstringOrGlob(typeName, filter);
}
