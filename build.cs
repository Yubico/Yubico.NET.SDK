#!/usr/bin/env dotnet run

#:package Bullseye
#:package SimpleExec

using static Bullseye.Targets;
using static SimpleExec.Command;

// Configuration
var solutionFile = "Yubico.YubiKit.sln";
var configuration = "Release";
var packageVersion = GetArgument("--package-version");
var nugetFeedName = GetArgument("--nuget-feed-name") ?? "Yubico.YubiKit-LocalNuGet";
var nugetFeedPath = GetArgument("--nuget-feed-path") ?? Path.Combine(GetRepoRoot(), "artifacts", "nuget-feed");
var includeDocs = HasFlag("--include-docs");
var dryRun = HasFlag("--dry-run");
var shouldClean = HasFlag("--clean");

// Projects to pack
var packableProjects = new[]
{
    "Yubico.YubiKit.Core/src/Yubico.YubiKit.Core.csproj",
    "Yubico.YubiKit.Management/src/Yubico.YubiKit.Management.csproj"
};

// Test projects
var testProjects = new[]
{
    "Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj",
    "Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.UnitTests/Yubico.YubiKit.Management.UnitTests.csproj"
};

var artifactsDir = Path.Combine(GetRepoRoot(), "artifacts");
var packagesDir = Path.Combine(artifactsDir, "packages");

// Define Bullseye targets
Target("clean", () =>
{
    PrintHeader("Cleaning");

    if (Directory.Exists(artifactsDir))
    {
        Directory.Delete(artifactsDir, recursive: true);
        PrintInfo($"Deleted {artifactsDir}");
    }

    if (shouldClean)
    {
        Run("dotnet", $"clean {solutionFile} -c {configuration}");
        PrintInfo("Cleaned solution");
    }
});

Target("restore", DependsOn("clean"), () =>
{
    PrintHeader("Restoring dependencies");
    Run("dotnet", $"restore {solutionFile}");
    PrintInfo("Dependencies restored");
});

Target("build", DependsOn("restore"), () =>
{
    PrintHeader("Building solution");
    Run("dotnet", $"build {solutionFile} -c {configuration} --no-restore");
    PrintInfo($"Built {solutionFile} in {configuration} configuration");
});

Target("test", DependsOn("build"), () =>
{
    PrintHeader("Running unit tests");

    foreach (var project in testProjects)
    {
        Console.WriteLine($"\nTesting: {Path.GetFileNameWithoutExtension(project)}");
        Run("dotnet", $"test {project} -c {configuration} --no-build --verbosity normal");
        PrintInfo($"Tests passed for {Path.GetFileNameWithoutExtension(project)}");
    }
});

Target("pack", DependsOn("build"), () =>
{
    PrintHeader("Creating NuGet packages");

    Directory.CreateDirectory(packagesDir);

    var versionArg = string.IsNullOrEmpty(packageVersion)
        ? ""
        : $"/p:Version={packageVersion}";

    var docsArg = includeDocs ? "" : "/p:GenerateDocumentationFile=false";

    foreach (var project in packableProjects)
    {
        Console.WriteLine($"\nPacking: {Path.GetFileNameWithoutExtension(project)}");
        Run("dotnet", $"pack {project} -c {configuration} --no-build -o {packagesDir} {versionArg} {docsArg}");
        PrintInfo($"Packed {Path.GetFileNameWithoutExtension(project)}");
    }

    var packages = Directory.GetFiles(packagesDir, "*.nupkg");
    PrintInfo($"Created {packages.Length} package(s) in {packagesDir}");
});

Target("setup-feed", async () =>
{
    PrintHeader("Setting up local NuGet feed");

    Directory.CreateDirectory(nugetFeedPath);

    try
    {
        var result = await ReadAsync("dotnet", "nuget list source");

        if (!result.StandardOutput.Contains(nugetFeedName))
        {
            Run("dotnet", $"nuget add source {nugetFeedPath} -n {nugetFeedName}");
            PrintInfo($"Added NuGet source: {nugetFeedName}");
        }
        else
        {
            PrintInfo($"NuGet source already exists: {nugetFeedName}");
        }
    }
    catch
    {
        Run("dotnet", $"nuget add source {nugetFeedPath} -n {nugetFeedName}");
        PrintInfo($"Added NuGet source: {nugetFeedName}");
    }
});

Target("publish", DependsOn("pack", "setup-feed"), () =>
{
    PrintHeader(dryRun ? "Dry run - packages to publish" : "Publishing packages");

    var packages = Directory.GetFiles(packagesDir, "*.nupkg");

    if (packages.Length == 0)
    {
        Console.WriteLine("No packages found to publish");
        return;
    }

    foreach (var package in packages)
    {
        var packageName = Path.GetFileName(package);

        if (dryRun)
        {
            Console.WriteLine($"  Would publish: {packageName}");
        }
        else
        {
            Console.WriteLine($"\nPublishing: {packageName}");
            Run("dotnet", $"nuget push {package} -s {nugetFeedName} --skip-duplicate");
            PrintInfo($"Published {packageName}");
        }
    }

    if (dryRun)
    {
        Console.WriteLine($"\n(Dry run - no packages were actually published)");
    }
});

Target("default", DependsOn("test", "publish"));

// Run Bullseye
await RunTargetsAndExitAsync(args);

// Helper functions
string GetRepoRoot()
{
    var current = Directory.GetCurrentDirectory();
    while (current is not null && !Directory.Exists(Path.Combine(current, ".git")))
    {
        current = Directory.GetParent(current)?.FullName;
    }
    return current ?? Directory.GetCurrentDirectory();
}

string? GetArgument(string name)
{
    var args = Environment.GetCommandLineArgs();
    var index = Array.IndexOf(args, name);
    return index >= 0 && index < args.Length - 1 ? args[index + 1] : null;
}

bool HasFlag(string name)
{
    return Environment.GetCommandLineArgs().Contains(name);
}

void PrintInfo(string message) => Console.WriteLine($"✓ {message}");
void PrintHeader(string message) => Console.WriteLine($"\n=== {message} ===\n");