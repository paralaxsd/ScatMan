using System.ComponentModel;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli;

sealed class CtorsCommand : AsyncCommand<CtorsCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<package>")]
        [Description("NuGet package ID")]
        public string Package { get; init; } = "";

        [CommandArgument(1, "<version>")]
        [Description("Package version")]
        public string Version { get; init; } = "";

        [CommandArgument(2, "<typeName>")]
        [Description("Full or simple type name")]
        public string TypeName { get; init; } = "";
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        IReadOnlyList<string> assemblies = [];

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"Downloading [bold]{settings.Package} {settings.Version}[/]...",
                async _ => assemblies = await new PackageDownloader()
                    .DownloadAsync(settings.Package, settings.Version, ct));

        var inspector = new TypeInspector();

        try
        {
            var ctors = inspector.GetConstructors(assemblies, settings.TypeName);
            var escaped = Markup.Escape(settings.TypeName);

            AnsiConsole.MarkupLine($"[bold]{escaped}[/] — {ctors.Count} public constructor(s)\n");

            foreach (var ctor in ctors)
            {
                var paramStr = string.Join(", ",
                    ctor.Parameters.Select(p => $"[cyan]{Markup.Escape(p.TypeName)}[/] {Markup.Escape(p.Name)}"));

                AnsiConsole.MarkupLine($"  .ctor({paramStr})");
            }

            if (ctors.Count == 0)
                AnsiConsole.MarkupLine("  [yellow](no public constructors)[/]");
        }
        catch (TypeNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        return 0;
    }
}
