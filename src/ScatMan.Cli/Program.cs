using ScatMan.Cli;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("scatman");
    config.AddCommand<CtorsCommand>("ctors")
        .WithDescription("List constructors of a type in a NuGet package.")
        .WithExample(["ctors", "NAudio.Wasapi", "2.2.1", "NAudio.CoreAudioApi.WasapiCapture"]);
});

return await app.RunAsync(args);
