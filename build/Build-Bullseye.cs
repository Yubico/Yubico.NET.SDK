#!/usr/bin/env dotnet run

#:package Bullseye@4.2.0
#:package SimpleExec@11.0.0
#:package System.CommandLine@2.0.0-beta4.22272.1

using System.CommandLine;
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

async Task ExecuteBuild(
    string? version,
    string feedName,
    string? feedPath,
    bool includeDocs,
    bool dryRun,
    bool clean)
{
    string repositoryRoot = GetRepositoryRoot();
    const string solutionFile = "Yubico.NET.SDK.sln";
    string solutionFilePath = Path.Combine(repositoryRoot, solutionFile);

    Target("clean", () =>
    {
        if (!clean)
        {
            Console.WriteLine("Skipping clean step as per configuration.");
            return;
        }

        Console.WriteLine("Cleaning old packages...");
        string[] packagePatterns =
        [
            "Yubico.Core/src/bin/Release/*.nupkg",
                "Yubico.YubiKey/src/bin/Release/*.nupkg"
        ];

        foreach (string pattern in packagePatterns)
        {
            string fullPattern = Path.Combine(repositoryRoot, pattern);
            string dir = Path.GetDirectoryName(fullPattern)!;
            string filePattern = Path.GetFileName(fullPattern);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (string file in Directory.GetFiles(dir, filePattern))
            {
                File.Delete(file);
            }
        }
    });

    Target("restore", () =>
    {
        Run("dotnet", $"restore \"{solutionFilePath}\"");
    });

    Target("build", DependsOn("clean", "restore"), () =>
    {
        Run("dotnet", $"build \"{solutionFilePath}\" --no-restore --configuration Release");
    });

    Target("test", DependsOn("build"), () =>
    {
        string yubiKeyTests = Path.Combine(repositoryRoot, "Yubico.YubiKey", "tests", "unit", "Yubico.YubiKey.UnitTests.csproj");
        string coreTests = Path.Combine(repositoryRoot, "Yubico.Core", "tests", "Yubico.Core.UnitTests.csproj");

        Run("dotnet", $"test \"{yubiKeyTests}\" --no-build --configuration Release");
        Run("dotnet", $"test \"{coreTests}\" --no-build --configuration Release");
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

    Target("setup-feed", async () =>
    {
        if (string.IsNullOrWhiteSpace(feedPath))
        {
            throw new ArgumentException(
                "Feed path must be specified for local NuGet feed." +
                " Use --nuget-feed-path to set a custom path.", nameof(feedPath));
        }

        var (standardOutput, _) = await ReadAsync("dotnet", "nuget list source");
        if (standardOutput.Contains(feedName))
        {
            Console.WriteLine($"✓ NuGet source '{feedName}' is already configured.");
            return;
        }

        if (!Directory.Exists(feedPath))
        {
            Console.WriteLine($"Creating directory: {feedPath}");
            _ = Directory.CreateDirectory(feedPath!);
        }

        await RunAsync("dotnet", $"nuget add source \"{feedPath}\" --name \"{feedName}\"");
        Console.WriteLine($"✓ Local NuGet feed '{feedName}' created at '{feedPath}'.");
    });

    Target("publish", DependsOn("pack", "setup-feed"), async () =>
    {
        string[] packagePatterns =
        [
            "Yubico.Core/src/bin/Release/*.nupkg",
            "Yubico.YubiKey/src/bin/Release/*.nupkg"
        ];

        var packageFilePaths = packagePatterns
            .SelectMany(pattern =>
            {
                string fullPattern = Path.Combine(repositoryRoot, pattern);
                string dir = Path.GetDirectoryName(fullPattern)!;
                string filePattern = Path.GetFileName(fullPattern);

                if (!Directory.Exists(dir))
                {
                    return Array.Empty<string>();
                }

                return Directory.GetFiles(dir, filePattern);
            })
            .ToList();

        if (dryRun)
        {
            Console.WriteLine("DRY RUN: The following packages would be published:");
            foreach (string? pkg in packageFilePaths)
            {
                Console.WriteLine($" - {Path.GetFileName(pkg)}");
            }
            return;
        }

        foreach (string? pkg in packageFilePaths)
        {
            await RunAsync("dotnet", $"nuget push \"{pkg}\" --source \"{feedName}\" --skip-duplicate");
        }
    });

    Target("default", DependsOn("test", "publish"));

    await RunTargetsAndExitAsync(["default"]);
}
;

string GetRepositoryRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository root.");
}
