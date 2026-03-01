using System.ComponentModel;
using System.Text.Json;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli;

sealed class CtorsCommand : AsyncCommand<CtorsCommand.Settings>
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public sealed class Settings : BaseSettings
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
        var assemblies = await FetchAssembliesAsync(settings, ct);

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

    static async Task<IReadOnlyList<string>> FetchAssembliesAsync(Settings settings, CancellationToken ct)
    {
        var downloader = new PackageDownloader();

        if (settings.Json)
            return await downloader.DownloadAsync(settings.Package, settings.Version, ct);

        IReadOnlyList<string> assemblies = [];
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"Downloading [bold]{settings.Package} {settings.Version}[/]...",
                async _ => assemblies = await downloader.DownloadAsync(settings.Package, settings.Version, ct));

        return assemblies;
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
        Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
    }

    static void PrintFormatted(IReadOnlyList<ConstructorSignature> ctors, Settings settings)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(settings.TypeName)}[/] — {ctors.Count} public constructor(s)\n");

        foreach (var ctor in ctors)
        {
            var paramStr = string.Join(", ",
                ctor.Parameters.Select(p => $"[cyan]{Markup.Escape(p.TypeName)}[/] {Markup.Escape(p.Name)}"));
            AnsiConsole.MarkupLine($"  .ctor({paramStr})");
        }

        if (ctors.Count == 0)
            AnsiConsole.MarkupLine("  [yellow](no public constructors)[/]");
    }

    static void PrintError(string message, bool json)
    {
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(new { error = message }, JsonOptions));
        else
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
    }
}
