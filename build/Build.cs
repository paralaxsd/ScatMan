using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Serilog;
using System;
using System.IO;
using System.Linq;
using Fallout.Solutions;
using static Fallout.Common.Tools.DotNet.DotNetTasks;
// ReSharper disable UnusedMember.Local
// ReSharper disable AllUnderscoreLocalParameterName

[GitHubActions("test",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push, GitHubActionsTrigger.WorkflowDispatch],
    FetchDepth = 0,  // full history required for Nerdbank.GitVersioning
    InvokedTargets = [nameof(Test)], CacheKeyFiles = [])]
class Build : FalloutBuild
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    [Parameter("Build configuration — Debug (local) or Release (CI)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("NuGet API key for publishing"), Secret]
    readonly string? NuGetApiKey;

    [Solution("ScatMan.slnx", GenerateProjects = true)] readonly Solution Solution = null!;

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    AbsolutePath ArtifactsDir => RootDirectory / "artifacts";
    AbsolutePath CoverageDir => RootDirectory / "coverage";
    AbsolutePath CliProject    => Solution._1_src.ScatMan_Cli.Path;
    AbsolutePath McpProject    => Solution._1_src.ScatMan_Mcp.Path;
    AbsolutePath CoreProject => Solution._1_src.ScatMan_Core.Path;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() => ArtifactsDir.CreateOrCleanDirectory());

    Target Restore => _ => _
        .Executes(() => DotNetRestore(s => s
            .SetProjectFile(Solution)));

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(s => s
            .SetProjectFile(Solution)
            .SetConfiguration(Configuration)
            .EnableNoRestore()));
    
    Target Test => _ => _
        .Description("Build and run all test projects discovered under tests/")
        .Produces(CoverageDir / "report")
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProjects = Solution.GetAllProjects("*Tests");
            DotNetTest(s => s
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .AddLoggers("GitHubActions")
                .SetResultsDirectory(CoverageDir)
                .AddProcessAdditionalArguments("--collect:\"XPlat Code Coverage\"")
                .CombineWith(testProjects, (cs, project) => cs
                    .SetProjectFile(project)));

            PublishCoverageReport();
        });

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
    void PublishCoverageReport()
    {
        var coberturaFiles = CoverageDir.GlobFiles("**/coverage.cobertura.xml");
        if (coberturaFiles.Count == 0)
        {
            Log.Warning("No Cobertura coverage files found — skipping report generation.");
            return;
        }

        DotNet("tool restore");

        var reportDir = CoverageDir / "report";
        var reports = string.Join(";", coberturaFiles.Select(f => f.ToString()));
        DotNet($"tool run reportgenerator -- -reports:\"{reports}\" -targetdir:\"{reportDir}\" -reporttypes:MarkdownSummaryGithub;Html");

        var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (summaryFile is { } && File.Exists(reportDir / "SummaryGithub.md"))
            File.AppendAllText(summaryFile, "\n\n" + File.ReadAllText(reportDir / "SummaryGithub.md"));
    }

    public static int Main() => Execute<Build>(x => x.Compile);
}
