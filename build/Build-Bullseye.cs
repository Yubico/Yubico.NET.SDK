#!/usr/bin/env dotnet run

#:package Bullseye@4.2.0
#:package SimpleExec@11.0.0
#:package System.CommandLine@2.0.0-beta4.22272.1

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;
using Bullseye;
using static Bullseye.Targets;
using static SimpleExec.Command;

var versionOption = new Option<string?>("--package-version", "The version to assign to the built NuGet packages.");
var feedNameOption = new Option<string>("--nuget-feed-name", () => "Yubico.NET.SDK-LocalNuGet", "The name of the NuGet feed to publish to.");
var feedPathOption = new Option<string>("--nuget-feed-path", () => Path.Combine(Path.GetTempPath(), "Yubico.NET.SDK-LocalNuGet"), "The path to the local NuGet feed.");
var includeDocsOption = new Option<bool>("--include-docs", () => false, "Whether to include documentation in the package.");
var dryRunOption = new Option<bool>("--dry-run", () => false, "Whether to perform a dry run without making any changes.");
var cleanOption = new Option<bool>("--clean", () => false, "Whether to clean the output directory before building.");
var rootCommand = new RootCommand("Builds the Yubico.NET.SDK project locally or in CI.")
{
    versionOption, feedNameOption, feedPathOption, includeDocsOption, dryRunOption, cleanOption
};

rootCommand.SetHandler(async ctx =>
{
    string? version = ctx.ParseResult.GetValueForOption(versionOption);
    string feedName = ctx.ParseResult.GetValueForOption(feedNameOption)!;
    string? feedPath = ctx.ParseResult.GetValueForOption(feedPathOption);
    bool includeDocs = ctx.ParseResult.GetValueForOption(includeDocsOption);
    bool dryRun = ctx.ParseResult.GetValueForOption(dryRunOption);
    bool clean = ctx.ParseResult.GetValueForOption(cleanOption);
    await ExecuteBuild(version, feedName, feedPath, includeDocs, dryRun, clean);
});

return await rootCommand.InvokeAsync(args);
async Task ExecuteBuild(string? version, string feedName, string? feedPath, bool includeDocs, bool dryRun, bool clean)
{
    const string solutionFile = "Yubico.NET.SDK.sln";
    string solutionFilePath = Path.Combine(Utils_GetRepositoryRoot(), solutionFile);
    Target("restore", () =>
    {
        Run("dotnet", $"restore \"{solutionFilePath}\"");
    });

    Target("build", DependsOn("restore"), () =>
    {
        Run("dotnet", $"build \"{solutionFilePath}\" --no-restore --configuration Release");
    });

    Target("test", DependsOn("build"), () =>
    {
        Run("dotnet", $"test \"{solutionFilePath}\" --no-build --configuration Release");
    });

    Target("pack", DependsOn("build"), () =>
    {
        if (!string.IsNullOrEmpty(version))
        {
            Run("dotnet", $"pack \"{solutionFilePath}\" --no-build --configuration Release /p:Version={version}");
        }
        else
        {
            Run("dotnet", $"pack \"{solutionFilePath}\" --no-build --configuration Release");
        }
    });

    Target("publish", DependsOn("pack"), async () =>
    {
        var packages = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.nupkg", SearchOption.AllDirectories)
            .Where(p => p.Contains("bin/Release"));

        if (dryRun)
        {
            Console.WriteLine("DRY RUN: The following packages would be published:");
            foreach (var pkg in packages)
            {
                Console.WriteLine($" - {Path.GetFileName(pkg)}");
            }
            return;
        }

        foreach (var pkg in packages)
        {
            await RunAsync("dotnet", $"nuget push \"{pkg}\" --source \"{feedName}\" --skip-duplicate");
        }
    });

    Target("default", DependsOn("test", "publish"));

    await RunTargetsAndExitAsync(["default"]);
};

string Utils_GetRepositoryRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository root.");
}
