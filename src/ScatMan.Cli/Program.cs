using ScatMan.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("scatman");

    config.AddCommand<CtorsCommand>("ctors")
        .WithDescription("List constructors of a type in a NuGet package.")
        .WithExample(["ctors", "NAudio.Wasapi", "2.2.1", "NAudio.CoreAudioApi.WasapiCapture"]);

    config.AddCommand<MembersCommand>("members")
        .WithDescription("List public members of a type in a NuGet package.")
        .WithExample(["members", "NAudio.Wasapi", "2.2.1", "NAudio.CoreAudioApi.WasapiCapture"]);

    config.AddCommand<TypesCommand>("types")
        .WithDescription("List public types in a NuGet package.")
        .WithExample(["types", "NAudio.Wasapi", "2.2.1"])
        .WithExample(["types", "NAudio.Wasapi", "2.2.1", "--namespace", "NAudio.CoreAudioApi"]);

    config.AddCommand<VersionsCommand>("versions")
        .WithDescription("List available versions of a NuGet package.")
        .WithExample(["versions", "NAudio.Lame"])
        .WithExample(["versions", "LiveChartsCore.SkiaSharpView.Maui", "--pre"]);
});

return await app.RunAsync(args);
