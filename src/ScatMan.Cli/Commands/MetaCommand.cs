using System.Globalization;
using System.Text.Json;
using ScatMan.Core;
using Spectre.Console;
using Spectre.Console.Cli;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ScatMan.Cli.Commands;

public sealed class MetaCommand : Command<MetaCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--json")]
        public bool Json { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var metaInfo = MetaInfoFactory.Create("ScatMan.Cli");

        if (settings.Json)
        {
            var json = JsonSerializer.Serialize(metaInfo, new JsonSerializerOptions { WriteIndented = true });
            AnsiConsole.WriteLine(json);
        }
        else
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Property").AddColumn("Value");

            table.AddRow("NuGet Package", metaInfo.NugetPackageName);
            table.AddRow("Version", metaInfo.Version);
            table.AddRow("Configuration", metaInfo.Configuration);
            table.AddRow("Commit Date", metaInfo.CommitDate.ToLocalTime().ToString(CultureInfo.CurrentCulture));
            table.AddRow("Is Public", metaInfo.IsPublic.ToString());
            table.AddRow("OS", metaInfo.OS);
            table.AddRow(".NET Version", metaInfo.DotNetVersion);

            AnsiConsole.Write(table);
        }

        return 0;
    }
}