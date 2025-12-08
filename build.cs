#!/usr/bin/env dotnet run

#:package Bullseye
#:package SimpleExec

/*
 * Yubico.YubiKit Build Script
 * ============================
 *
 * .NET 10 build automation script using Bullseye task runner.
 *
 * USAGE:
 *   dotnet build.cs [target] [options]
 *
 * TARGETS:
 *   clean      - Remove artifacts directory
 *   restore    - Restore NuGet dependencies
 *   build      - Build the solution
 *   test       - Run unit tests with summary output
 *   coverage   - Run tests with code coverage
 *   pack       - Create NuGet packages
 *   setup-feed - Configure local NuGet feed
 *   publish    - Publish packages to local feed
 *   default    - Run tests and publish
 *
 * OPTIONS:
 *   --package-version <version>    Override NuGet package version
 *   --nuget-feed-name <name>       NuGet feed name (default: Yubico.YubiKit-LocalNuGet)
 *   --nuget-feed-path <path>       NuGet feed path (default: artifacts/nuget-feed)
 *   --include-docs                 Include XML documentation in packages
 *   --dry-run                      Show what would be published without publishing
 *   --clean                        Run dotnet clean before build
 *
 * EXAMPLES:
 *   dotnet build.cs build
 *   dotnet build.cs test
 *   dotnet build.cs coverage
 *   dotnet build.cs publish --package-version 1.0.0-preview.1
 *   dotnet build.cs publish --dry-run
 *
 * See BUILD.md for full documentation.
 */

using static Bullseye.Targets;
using static SimpleExec.Command;

// Configuration
var repoRoot = GetRepoRoot();
var solutionFile = "Yubico.YubiKit.sln";
var configuration = "Release";
var packageVersion = GetArgument("--package-version");
var nugetFeedName = GetArgument("--nuget-feed-name") ?? "Yubico.YubiKit-LocalNuGet";
var nugetFeedPath = GetArgument("--nuget-feed-path") ?? Path.Combine(repoRoot, "artifacts", "nuget-feed");
var includeDocs = HasFlag("--include-docs");
var dryRun = HasFlag("--dry-run");
var shouldClean = HasFlag("--clean");

// Dynamically discover projects using glob patterns

// Projects to pack - all Yubico.YubiKit.*/src/*.csproj
var packableProjects = Directory.GetFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
    .Where(p => p.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}") &&
                p.Contains("Yubico.YubiKit."))
    .Select(p => Path.GetRelativePath(repoRoot, p))
    .OrderBy(p => p)
    .ToArray();

// Test projects - all Yubico.YubiKit.*.UnitTests/*.csproj
var testProjects = Directory.GetFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
    .Where(p => p.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}") &&
                p.Contains(".UnitTests") &&
                p.Contains("Yubico.YubiKit."))
    .Select(p => Path.GetRelativePath(repoRoot, p))
    .OrderBy(p => p)
    .ToArray();

var testProjectInfos = testProjects
    .Select(p => (ProjectPath: p, UsesTestingPlatformRunner: UsesMicrosoftTestingPlatformRunner(repoRoot, p)))
    .ToArray();

var artifactsDir = Path.Combine(repoRoot, "artifacts");
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

    var testResults = new List<(string Project, bool Passed, string? Error)>();

    foreach (var projectInfo in testProjectInfos)
    {
        var project = projectInfo.ProjectPath;
        var projectName = Path.GetFileNameWithoutExtension(project);
        Console.WriteLine($"\n{'='} Testing: {projectName} {'='}");

        try
        {
            var command = projectInfo.UsesTestingPlatformRunner
                ? $"run --project {project} -c {configuration} --no-build"
                : $"test {project} -c {configuration} --no-build --logger \"console;verbosity=normal\"";

            Run("dotnet", command);
            testResults.Add((projectName, true, null));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {projectName} - All tests passed");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            testResults.Add((projectName, false, ex.Message));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {projectName} - Tests failed");
            Console.ResetColor();
        }
    }

    // Print summary
    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine("TEST SUMMARY");
    Console.WriteLine(new string('=', 60));

    var passed = testResults.Count(r => r.Passed);
    var failed = testResults.Count(r => !r.Passed);

    foreach (var (project, success, error) in testResults)
    {
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {project}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {project}");
            if (!string.IsNullOrEmpty(error) && error.Contains("Test Run Aborted"))
            {
                Console.WriteLine($"    (Test run aborted - check for initialization errors)");
            }
        }
        Console.ResetColor();
    }

    Console.WriteLine(new string('=', 60));
    Console.ForegroundColor = passed > 0 ? ConsoleColor.Green : ConsoleColor.Gray;
    Console.Write($"Passed: {passed}");
    Console.ResetColor();
    Console.Write(" | ");
    Console.ForegroundColor = failed > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
    Console.Write($"Failed: {failed}");
    Console.ResetColor();
    Console.Write($" | Total: {testResults.Count}\n");
    Console.WriteLine(new string('=', 60));

    if (failed > 0)
    {
        throw new InvalidOperationException($"{failed} test project(s) failed");
    }
});

Target("coverage", DependsOn("build"), () =>
{
    PrintHeader("Running tests with coverage");

    var coverageResultsDir = Path.Combine(artifactsDir, "coverage");
    Directory.CreateDirectory(coverageResultsDir);

    var testResults = new List<(string Project, bool Passed)>();

    foreach (var project in testProjects)
    {
        var projectName = Path.GetFileNameWithoutExtension(project);
        Console.WriteLine($"\n{'='} Running coverage for: {projectName} {'='}");

        try
        {
            Run("dotnet", $"test {project} -c {configuration} --no-build --settings coverlet.runsettings.xml --collect:\"XPlat Code Coverage\" --results-directory {coverageResultsDir}");
            testResults.Add((projectName, true));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {projectName} - Coverage collected");
            Console.ResetColor();
        }
        catch (Exception)
        {
            testResults.Add((projectName, false));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {projectName} - Coverage collection failed");
            Console.ResetColor();
        }
    }

    // Print summary
    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine("COVERAGE SUMMARY");
    Console.WriteLine(new string('=', 60));

    var passed = testResults.Count(r => r.Passed);
    var failed = testResults.Count(r => !r.Passed);

    foreach (var (project, success) in testResults)
    {
        Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"  {(success ? "✓" : "✗")} {project}");
        Console.ResetColor();
    }

    Console.WriteLine(new string('=', 60));
    Console.ForegroundColor = passed > 0 ? ConsoleColor.Green : ConsoleColor.Gray;
    Console.Write($"Collected: {passed}");
    Console.ResetColor();
    Console.Write(" | ");
    Console.ForegroundColor = failed > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
    Console.Write($"Failed: {failed}");
    Console.ResetColor();
    Console.Write($" | Total: {testResults.Count}\n");
    Console.WriteLine(new string('=', 60));

    // Find coverage files
    var coverageFiles = Directory.GetFiles(coverageResultsDir, "coverage.cobertura.xml", SearchOption.AllDirectories);
    if (coverageFiles.Length > 0)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nCoverage reports generated:");
        foreach (var file in coverageFiles)
        {
            Console.WriteLine($"  {file}");
        }
        Console.ResetColor();
    }

    PrintInfo($"Coverage results saved to {coverageResultsDir}");

    if (failed > 0)
    {
        throw new InvalidOperationException($"{failed} test project(s) failed during coverage collection");
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

// Handle --help before Bullseye processes args
if (args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return;
}

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

void PrintHelp()
{
    Console.WriteLine(@"
Yubico.YubiKit Build Script
============================

.NET 10 build automation script using Bullseye task runner.

USAGE:
  dotnet build.cs [target] [options]
  dotnet build.cs -- --help    (use -- separator for help)

TARGETS:
  clean      - Remove artifacts directory
  restore    - Restore NuGet dependencies
  build      - Build the solution
  test       - Run unit tests with summary output
  coverage   - Run tests with code coverage
  pack       - Create NuGet packages
  setup-feed - Configure local NuGet feed
  publish    - Publish packages to local feed
  default    - Run tests and publish

OPTIONS:
  --package-version <version>    Override NuGet package version
  --nuget-feed-name <name>       NuGet feed name (default: Yubico.YubiKit-LocalNuGet)
  --nuget-feed-path <path>       NuGet feed path (default: artifacts/nuget-feed)
  --include-docs                 Include XML documentation in packages
  --dry-run                      Show what would be published without publishing
  --clean                        Run dotnet clean before build
  -h, --help                     Show this help message

EXAMPLES:
  dotnet build.cs build
  dotnet build.cs test
  dotnet build.cs coverage
  dotnet build.cs publish --package-version 1.0.0-preview.1
  dotnet build.cs publish --dry-run
");

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Discovered {packableProjects.Length} packable projects:");
    Console.ResetColor();
    foreach (var proj in packableProjects)
    {
        Console.WriteLine($"  • {Path.GetFileNameWithoutExtension(proj)}");
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\nDiscovered {testProjects.Length} test projects:");
    Console.ResetColor();
    foreach (var proj in testProjects)
    {
        Console.WriteLine($"  • {Path.GetFileNameWithoutExtension(proj)}");
    }

    Console.WriteLine("\nSee BUILD.md for full documentation.");
}

static bool UsesMicrosoftTestingPlatformRunner(string repoRoot, string projectPath)
{
    var fullPath = Path.Combine(repoRoot, projectPath);
    if (!File.Exists(fullPath))
    {
        return false;
    }

    var contents = File.ReadAllText(fullPath);
    return contents.Contains(
        "<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>",
        StringComparison.OrdinalIgnoreCase);
}

