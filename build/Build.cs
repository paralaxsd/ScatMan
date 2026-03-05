using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using System.Linq;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
// ReSharper disable UnusedMember.Local
// ReSharper disable AllUnderscoreLocalParameterName

[GitHubActions("ci",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push, GitHubActionsTrigger.WorkflowDispatch],
    FetchDepth = 0,  // full history required for Nerdbank.GitVersioning
    InvokedTargets = [nameof(Compile)])]
class Build : NukeBuild
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    [Parameter("Build configuration — Debug (local) or Release (CI)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("NuGet API key for publishing"), Secret]
    readonly string? NuGetApiKey;

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    AbsolutePath ArtifactsDir => RootDirectory / "artifacts";
    AbsolutePath SolutionFile  => RootDirectory / "ScatMan.slnx";
    AbsolutePath CliProject    => RootDirectory / "src" / "ScatMan.Cli" / "ScatMan.Cli.csproj";
    AbsolutePath McpProject    => RootDirectory / "src" / "ScatMan.Mcp" / "ScatMan.Mcp.csproj";
    AbsolutePath CoreProject => RootDirectory / "src" / "ScatMan.Core" / "ScatMan.Core.csproj";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() => ArtifactsDir.CreateOrCleanDirectory());

    Target Restore => _ => _
        .Executes(() => DotNetRestore(s => s
            .SetProjectFile(SolutionFile)));

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(s => s
            .SetProjectFile(SolutionFile)
            .SetConfiguration(Configuration)
            .EnableNoRestore()));

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            ArtifactsDir.CreateOrCleanDirectory();
            foreach (var project in new[] { CliProject, McpProject, CoreProject })
                DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .SetOutputDirectory(ArtifactsDir));
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            var nonSymbolNugetPackages = ArtifactsDir.GetFiles("*.nupkg")
                .Where(f => !f.Name.EndsWith(".symbols.nupkg"));

            foreach (var pkg in nonSymbolNugetPackages)
            {
                DotNetNuGetPush(s => s
                    .SetTargetPath(pkg)
                    .SetSource("https://api.nuget.org/v3/index.json")
                    .SetApiKey(NuGetApiKey)
                    .AddProcessAdditionalArguments("--skip-duplicate"));
            }
        });

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public static int Main() => Execute<Build>(x => x.Compile);
}
