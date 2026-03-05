using System.ComponentModel;
using System.Text.Json;
using ScatMan.Cli;
using ScatMan.Core;
using TypeDescriptor = ScatMan.Core.TypeDescriptor;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli.Commands;

sealed class TypesCommand : AsyncCommand<TypesCommand.Settings>
{
    public sealed class Settings : PackageSettings
    {
        [CommandOption("-n|--namespace")]
        [Description("Filter by namespace")]
        public string? Namespace { get; init; }

        [CommandOption("-f|--filter")]
        [Description("Case-insensitive substring filter on type name")]
        public string? Filter { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        var (assemblies, resolvedVersion) = await settings.FetchAssembliesAsync(ct);
        var types      = new TypeInspector().GetTypes(assemblies, settings.Namespace);

        if (settings.Filter is { } f)
            types = [.. types.Where(t => t.Name.Contains(f, StringComparison.OrdinalIgnoreCase))];

        if (settings.Json) PrintJson(types, settings, resolvedVersion);
        else PrintFormatted(types, settings, resolvedVersion);

        return 0;
    }

    static void PrintJson(
        IReadOnlyList<TypeDescriptor> types,
        Settings settings,
        string resolvedVersion)
    {
        var result = new
        {
            package    = settings.Package,
            version    = resolvedVersion,
            @namespace = settings.Namespace,
            filter     = settings.Filter,
            types      = types.Select(t => new { t.FullName, t.Kind, t.Summary })
        };
        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(
        IReadOnlyList<TypeDescriptor> types,
        Settings settings,
        string resolvedVersion)
    {
        var nsLabel     = settings.Namespace is { } ns ? $" [[{Markup.Escape(ns)}]]" : "";
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
}
