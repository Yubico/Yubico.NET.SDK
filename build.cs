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
 *   dotnet build.cs -- [target] [options]   (use -- if options conflict with dotnet)
 *
 * NOTE: Use -- separator when passing --help or if options aren't working:
 *   dotnet build.cs -- --help               (--help requires --)
 *   dotnet build.cs -- build --project Piv  (when in doubt, use --)
 *
 * TARGETS:
 *   clean      - Remove artifacts directory
 *   restore    - Restore NuGet dependencies
 *   build      - Build the solution (or specific project with --project)
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
 *   --filter <expression>          Test filter expression (e.g., "FullyQualifiedName~MyTest")
 *   --project <name>               Build/test specific project only (partial match)
 *
 * EXAMPLES:
 *   dotnet build.cs build
 *   dotnet build.cs build --project Piv
 *   dotnet build.cs test
 *   dotnet build.cs test --filter "FullyQualifiedName~MyTestClass"
 *   dotnet build.cs test --project Piv --filter "Method~Sign"
 *   dotnet build.cs coverage
 *   dotnet build.cs publish --package-version 1.0.0-preview.1
 *   dotnet build.cs -- --help
 *
 * XUNIT V2 VS V3 TEST RUNNER DETECTION:
 *   This script automatically detects which test runner each project uses:
 *
 *   - xUnit v3 (Microsoft.Testing.Platform): Projects with
 *     <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
 *     These use: dotnet run --project <proj> -- --filter "..."
 *
 *   - xUnit v2 (traditional): Projects without that setting
 *     These use: dotnet test <proj> --filter "..."
 *
 *   IMPORTANT: Always use "dotnet build.cs test" instead of invoking dotnet test
 *   directly. The build script handles this detection automatically, preventing
 *   failures from using the wrong command syntax for each test project.
 *
 * See BUILD.md for full documentation.
 */

using System;
using System.Collections.Generic;
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
var testFilter = GetArgument("--filter");
var testProject = GetArgument("--project");

// Dynamically discover projects using glob patterns

// Projects to pack - all Yubico.YubiKit.*/src/*.csproj
var packableProjects = Directory.GetFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
    .Where(p => p.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}") &&
                p.Contains("Yubico.YubiKit."))
    .Select(p => Path.GetRelativePath(repoRoot, p))
    .OrderBy(p => p)
    .ToArray();

// Unit test projects - all Yubico.YubiKit.*.UnitTests/*.csproj
var unitTestProjects = Directory.GetFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
    .Where(p => p.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}") &&
                p.Contains(".UnitTests") &&
                p.Contains("Yubico.YubiKit."))
    .Select(p => Path.GetRelativePath(repoRoot, p))
    .OrderBy(p => p)
    .ToArray();

// Integration test projects - all Yubico.YubiKit.*.IntegrationTests/*.csproj
var integrationTestProjects = Directory.GetFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
    .Where(p => p.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}") &&
                p.Contains(".IntegrationTests") &&
                p.Contains("Yubico.YubiKit."))
    .Select(p => Path.GetRelativePath(repoRoot, p))
    .OrderBy(p => p)
    .ToArray();

var testProjects = unitTestProjects
    .Concat(integrationTestProjects)
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
    PrintHeader("Building");
    
    if (!string.IsNullOrEmpty(testProject))
    {
        // Build specific project(s) matching the filter
        var matchingProjects = packableProjects
            .Where(p => Path.GetFileNameWithoutExtension(p)
                .Contains(testProject, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (matchingProjects.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ No projects match '{testProject}'");
            Console.ResetColor();
            Console.WriteLine("Available projects:");
            foreach (var proj in packableProjects)
                Console.WriteLine($"  - {Path.GetFileNameWithoutExtension(proj)}");
            return;
        }
        
        foreach (var project in matchingProjects)
        {
            var projectName = Path.GetFileNameWithoutExtension(project);
            Console.WriteLine($"Building: {projectName}");
            Run("dotnet", $"build {project} -c {configuration} --no-restore");
        }
        PrintInfo($"Built {matchingProjects.Count} project(s) matching '{testProject}'");
    }
    else
    {
        // Build entire solution
        Run("dotnet", $"build {solutionFile} -c {configuration} --no-restore");
        PrintInfo($"Built {solutionFile} in {configuration} configuration");
    }
});

Target("test", DependsOn("build"), () =>
{
    PrintHeader("Running unit tests");

    var testResults = new List<(string Project, bool Passed, string? Error)>();

    // Filter to specific project if --project specified
    var projectsToTest = testProjectInfos.AsEnumerable();
    if (!string.IsNullOrEmpty(testProject))
    {
        projectsToTest = projectsToTest.Where(p => 
            Path.GetFileNameWithoutExtension(p.ProjectPath)
                .Contains(testProject, StringComparison.OrdinalIgnoreCase));
        
        if (!projectsToTest.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ No test projects match '{testProject}'");
            Console.ResetColor();
            Console.WriteLine("Available test projects:");
            foreach (var p in testProjectInfos)
                Console.WriteLine($"  - {Path.GetFileNameWithoutExtension(p.ProjectPath)}");
            return;
        }
    }

    foreach (var projectInfo in projectsToTest)
    {
        var project = projectInfo.ProjectPath;
        var projectName = Path.GetFileNameWithoutExtension(project);
        Console.WriteLine($"\n{'='} Testing: {projectName} {'='}");

        try
        {
            string command;
            if (projectInfo.UsesTestingPlatformRunner)
            {
                // Microsoft.Testing.Platform uses -- to pass filter
                command = $"run --project {project} -c {configuration} --no-build";
                if (!string.IsNullOrEmpty(testFilter))
                    command += $" -- --filter \"{testFilter}\"";
            }
            else
            {
                // xUnit/MSTest use --filter directly
                command = $"test {project} -c {configuration} --no-build --logger \"console;verbosity=normal\"";
                if (!string.IsNullOrEmpty(testFilter))
                    command += $" --filter \"{testFilter}\"";
            }

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
            Console.WriteLine($"✗ {projectName} - Tests failed");;
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

    foreach (var project in unitTestProjects)
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
var bullseyeArgs = FilterBullseyeArgs(args, "--project", "--filter");
await RunTargetsAndExitAsync(bullseyeArgs);

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
  dotnet build.cs -- [target] [options]   (use -- if options conflict with dotnet)

NOTE: The -- separator passes arguments to the script instead of dotnet:
  dotnet build.cs -- --help               Required for --help
  dotnet build.cs -- build --project Piv  Use when in doubt

TARGETS:
  clean      - Remove artifacts directory
  restore    - Restore NuGet dependencies
  build      - Build the solution (or specific project with --project)
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
  --filter <expression>          Test filter expression (e.g., ""FullyQualifiedName~MyTest"")
  --project <name>               Build/test specific project only (partial match)
  -h, --help                     Show this help message

EXAMPLES:
  dotnet build.cs build
  dotnet build.cs build --project Piv
  dotnet build.cs test
  dotnet build.cs test --filter ""FullyQualifiedName~MyTestClass""
  dotnet build.cs test --project Piv --filter ""Method~Sign""
  dotnet build.cs coverage
  dotnet build.cs publish --package-version 1.0.0-preview.1
  dotnet build.cs -- --help

FILTER SYNTAX (for --filter):
  FullyQualifiedName~MyClass     Tests containing 'MyClass' in full name
  Name=MyTestMethod              Exact test method name
  ClassName~Integration          Classes containing 'Integration'
  Name!=SkipMe                   Exclude tests named 'SkipMe'
  Category=Unit                  Tests with [Trait(""Category"", ""Unit"")]
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

string[] FilterBullseyeArgs(string[] args, params string[] optionNames)
{
    var options = new HashSet<string>(optionNames, StringComparer.OrdinalIgnoreCase);
    var filtered = new List<string>();

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        if (options.Contains(arg))
        {
            if (i + 1 < args.Length)
            {
                i++;
            }

            continue;
        }

        filtered.Add(arg);
    }

    return filtered.ToArray();
}
