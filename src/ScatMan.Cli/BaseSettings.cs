using System.ComponentModel;
using Spectre.Console.Cli;

namespace ScatMan.Cli;

class BaseSettings : CommandSettings
{
    [CommandOption("--json")]
    [Description("Output results as JSON instead of formatted text")]
    public bool Json { get; init; }
}
