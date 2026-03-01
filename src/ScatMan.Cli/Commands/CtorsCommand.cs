using System.ComponentModel;
using System.Text.Json;
using ScatMan.Cli;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli.Commands;

sealed class CtorsCommand : AsyncCommand<CtorsCommand.Settings>
{
    public sealed class Settings : PackageSettings
    {
        [CommandArgument(2, "<typeName>")]
        [Description("Full or simple type name")]
        public string TypeName { get; init; } = "";
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        var assemblies = await settings.FetchAssembliesAsync(ct);

        try
        {
            var ctors = new TypeInspector().GetConstructors(assemblies, settings.TypeName);

            if (settings.Json) PrintJson(ctors, settings);
            else PrintFormatted(ctors, settings);
        }
        catch (TypeNotFoundException ex)
        {
            PrintError(ex.Message, settings.Json);
            return 1;
        }

        return 0;
    }

    static void PrintJson(IReadOnlyList<ConstructorSignature> ctors, Settings settings)
    {
        var result = new
        {
            package = settings.Package,
            version = settings.Version,
            typeName = settings.TypeName,
            constructors = ctors.Select(c => new
            {
                parameters = c.Parameters.Select(p => new { p.Name, p.TypeName })
            })
        };
        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(IReadOnlyList<ConstructorSignature> ctors, Settings settings)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(settings.TypeName)}[/] — {ctors.Count} public constructor(s)\n");

        var width = AnsiConsole.Profile.Width;
        foreach (var ctor in ctors)
        {
            var flat = string.Join(", ", ctor.Parameters.Select(p => $"{p.TypeName} {p.Name}"));
            if (2 + 6 + flat.Length + 1 <= width || !ctor.Parameters.Any())
            {
                var markup = string.Join(", ", ctor.Parameters
                    .Select(p => $"[cyan]{Markup.Escape(p.TypeName)}[/] {Markup.Escape(p.Name)}"));
                AnsiConsole.MarkupLine($"  .ctor({markup})");
            }
            else
            {
                AnsiConsole.MarkupLine("  .ctor(");
                foreach (var (p, last) in ctor.Parameters.Select((p, i) => (p, i == ctor.Parameters.Count - 1)))
                    AnsiConsole.MarkupLine(
                        $"    [cyan]{Markup.Escape(p.TypeName)}[/] {Markup.Escape(p.Name)}{(last ? ")" : ",")}");
            }
        }

        if (ctors.Count == 0)
            AnsiConsole.MarkupLine("  [yellow](no public constructors)[/]");
    }

    static void PrintError(string message, bool json)
    {
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(new { error = message }, BaseSettings.JsonOptions));
        else
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
    }
}
