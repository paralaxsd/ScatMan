using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScatMan.Mcp.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

builder.Services
    .AddMcpServer(options => {
        options.ServerInfo = new()
        {
            Name = "ScatMan - NuGet API Explorer", 
            Version = ThisAssembly.AssemblyInformationalVersion
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(ScatManTools).Assembly);

await builder.Build().RunAsync();
