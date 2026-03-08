using System.Text.Json;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli.Commands;

sealed class SourcesCommand : AsyncCommand<BaseSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, BaseSettings settings, CancellationToken ct)
    {
        var sources = PackageSourceResolver.GetSources();

        if (settings.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(
                new { sources = sources.Select(s => new { name = s.Name, url = s.Url }) },
                BaseSettings.JsonOptions));
        }
        else
        {
            if (sources.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No package sources configured.[/]");
                return 0;
            }

            var table = new Table
            {
                Title = new TableTitle("[bold]Configured Package Sources[/]"),
                Border = TableBorder.Rounded
            };

            table.AddColumn(new TableColumn("[bold]Name[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]URL[/]").LeftAligned());

            foreach (var (name, url) in sources)
                table.AddRow(name, url);

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        return 0;
    }
}
