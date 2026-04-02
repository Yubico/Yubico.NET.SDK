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
 *   build      - Build the solution (restores only if needed)
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
 *   --integration                  Include integration tests (requires --project, unit tests only by default)
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
 * TEST TRAIT FILTERS:
 *   Tests are categorized with traits. Use --filter to include/exclude:
 *
 *   Categories:
 *     RequiresHardware      - Tests needing physical YubiKey connected
 *     RequiresUserPresence  - Tests needing user to insert/remove/touch device
 *     Slow                  - Tests taking >5 seconds
 *     Integration           - Tests exercising multiple components
 *
 *   Filter Examples:
 *     --filter "Category!=RequiresUserPresence"     Skip user presence tests (for CI/agents)
 *     --filter "Category!=RequiresHardware"         Skip hardware tests (unit tests only)
 *     --filter "Category!=Slow"                     Skip slow tests
 *     --filter "Category!=RequiresHardware&Category!=RequiresUserPresence&Category!=Slow"
 *                                                   Run only fast unit tests
 *
 *   AI AGENTS: Always exclude RequiresUserPresence tests (cannot insert/remove devices).
 *
 * XUNIT V2 VS V3 TEST RUNNER DETECTION:
 *   This script automatically detects which test runner each project uses:
 *
 *   - xUnit v3 (Microsoft.Testing.Platform): Projects with
 *     <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
 *     These use: dotnet run --project <proj> -- --filter-method/--filter-trait "..."
 *     VSTest --filter expressions are auto-translated to xUnit v3 native options
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
var includeIntegration = HasFlag("--integration");

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

var testProjects = includeIntegration
    ? [..unitTestProjects, ..integrationTestProjects]
    : unitTestProjects;

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

Target("restore", () =>
{
    PrintHeader("Restoring dependencies");
    Run("dotnet", $"restore {solutionFile}");
    PrintInfo("Dependencies restored");
});

Target("build", () =>
{
    PrintHeader("Building");

    if (!string.IsNullOrEmpty(testProject))
    {
        var matchingProjects = packableProjects
            .Where(p => Path.GetFileNameWithoutExtension(p)
                .Contains(testProject, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingProjects.Count == 0)
        {
            PrintNoProjectsFound(testProject, packableProjects);
            return;
        }

        foreach (var project in matchingProjects)
        {
            Console.WriteLine($"Building: {Path.GetFileNameWithoutExtension(project)}");
            Run("dotnet", $"build {project} -c {configuration}");
        }

        PrintInfo($"Built {matchingProjects.Count} project(s) matching '{testProject}'");
    }
    else
    {
        Run("dotnet", $"build {solutionFile} -c {configuration}");
        PrintInfo($"Built {solutionFile} in {configuration} configuration");
    }
});

Target("test", () =>
{
    // --integration requires --project to prevent accidentally running all integration tests
    if (includeIntegration && string.IsNullOrEmpty(testProject))
    {
        PrintColored("Error: --integration requires --project to specify which module to test.", ConsoleColor.Red);
        Console.WriteLine("Example: dotnet build.cs test --integration --project Piv");
        Console.WriteLine("\nAvailable integration test projects:");
        PrintProjectList(integrationTestProjects);
        throw new InvalidOperationException("--integration requires --project");
    }

    PrintHeader(includeIntegration ? "Running unit + integration tests" : "Running unit tests");

    var projectsToTest = FilterToProject(testProjectInfos, testProject);
    if (projectsToTest is null)
        return;

    var results = RunTestProjects(projectsToTest);
    PrintTestSummary(results, "TEST");

    var failCount = results.Count(r => !r.Passed);
    if (failCount > 0)
        throw new InvalidOperationException($"{failCount} test project(s) failed");
});

Target("coverage", () =>
{
    PrintHeader("Running tests with coverage");

    // NOTE: Coverage uses dotnet test directly and only runs xUnit v2 unit test projects.
    // Projects using Microsoft.Testing.Platform (UseMicrosoftTestingPlatformRunner=true)
    // require different tooling for coverage collection and are excluded here.
    var coverageResultsDir = Path.Combine(artifactsDir, "coverage");
    Directory.CreateDirectory(coverageResultsDir);

    var results = new List<(string Project, bool Passed, string? Error)>();

    foreach (var project in unitTestProjects)
    {
        var projectName = Path.GetFileNameWithoutExtension(project);
        Console.WriteLine($"\n{'='} Running coverage for: {projectName} {'='}");

        try
        {
            Run("dotnet", $"test {project} -c {configuration} --settings coverlet.runsettings.xml --collect:\"XPlat Code Coverage\" --results-directory {coverageResultsDir}");
            results.Add((projectName, true, null));
            PrintColored($"✓ {projectName} - Coverage collected", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            results.Add((projectName, false, ex.Message));
            PrintColored($"✗ {projectName} - Coverage collection failed", ConsoleColor.Red);
        }
    }

    PrintTestSummary(results, "COVERAGE");

    var coverageFiles = Directory.GetFiles(coverageResultsDir, "coverage.cobertura.xml", SearchOption.AllDirectories);
    if (coverageFiles.Length > 0)
    {
        PrintColored("\nCoverage reports generated:", ConsoleColor.Cyan);
        foreach (var file in coverageFiles)
            Console.WriteLine($"  {file}");
    }

    PrintInfo($"Coverage results saved to {coverageResultsDir}");

    var failCount = results.Count(r => !r.Passed);
    if (failCount > 0)
        throw new InvalidOperationException($"{failCount} test project(s) failed during coverage collection");
});

Target("pack", DependsOn("build"), () =>
{
    PrintHeader("Creating NuGet packages");

    Directory.CreateDirectory(packagesDir);

    var versionArg = string.IsNullOrEmpty(packageVersion) ? "" : $"/p:Version={packageVersion}";
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
        Console.WriteLine($"\n(Dry run - no packages were actually published)");
});

Target("default", DependsOn("test", "publish"));

// Handle --help before Bullseye processes args
if (args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return;
}

// Run Bullseye — strip all custom args so Bullseye only sees target names and its own flags
var bullseyeArgs = FilterBullseyeArgs(args,
    optionsWithValues: ["--project", "--filter", "--package-version", "--nuget-feed-name", "--nuget-feed-path"],
    flags: ["--integration", "--include-docs", "--dry-run", "--clean"]);
await RunTargetsAndExitAsync(bullseyeArgs);

// ─── Helper functions ──────────────────────────────────────────────────────────

string GetRepoRoot()
{
    var current = Directory.GetCurrentDirectory();
    while (current is not null && !Directory.Exists(Path.Combine(current, ".git")))
        current = Directory.GetParent(current)?.FullName;
    return current ?? Directory.GetCurrentDirectory();
}

// Use the captured top-level `args`, not Environment.GetCommandLineArgs() which includes the host path.
string? GetArgument(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index < args.Length - 1 ? args[index + 1] : null;
}

bool HasFlag(string name) => args.Contains(name);

void PrintInfo(string message) => Console.WriteLine($"✓ {message}");
void PrintHeader(string message) => Console.WriteLine($"\n=== {message} ===\n");

void PrintColored(string message, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ResetColor();
}

void PrintProjectList(string[] projects)
{
    foreach (var p in projects)
        Console.WriteLine($"  - {Path.GetFileNameWithoutExtension(p)}");
}

void PrintNoProjectsFound(string filter, string[] available)
{
    PrintColored($"⚠ No projects match '{filter}'", ConsoleColor.Yellow);
    Console.WriteLine("Available projects:");
    PrintProjectList(available);
}

// Returns null when no projects matched and an error was already printed (caller should return early).
List<(string ProjectPath, bool UsesTestingPlatformRunner)>? FilterToProject(
    (string ProjectPath, bool UsesTestingPlatformRunner)[] projectInfos,
    string? filter)
{
    if (string.IsNullOrEmpty(filter))
        return [..projectInfos];

    var matched = projectInfos
        .Where(p => Path.GetFileNameWithoutExtension(p.ProjectPath)
            .Contains(filter, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (matched.Count > 0)
        return matched;

    PrintColored($"⚠ No test projects match '{filter}'", ConsoleColor.Yellow);
    Console.WriteLine("Available test projects:");
    PrintProjectList(projectInfos.Select(p => p.ProjectPath).ToArray());
    return null;
}

List<(string Project, bool Passed, string? Error)> RunTestProjects(
    IEnumerable<(string ProjectPath, bool UsesTestingPlatformRunner)> projects)
{
    var results = new List<(string Project, bool Passed, string? Error)>();

    foreach (var (projectPath, usesTestingPlatformRunner) in projects)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        Console.WriteLine($"\n{'='} Testing: {projectName} {'='}");

        try
        {
            string command;
            if (usesTestingPlatformRunner)
            {
                // xUnit v3 MTP runner uses native filter options (--filter-method, --filter-trait, etc.)
                // not VSTest's --filter syntax
                command = $"run --project {projectPath} -c {configuration}";
                if (!string.IsNullOrEmpty(testFilter))
                {
                    var mtpFilter = TranslateToMtpFilter(testFilter);
                    // --minimum-expected-tests 0 prevents failure when no tests match the filter
                    command += $" -- --minimum-expected-tests 0 {mtpFilter}";
                }
            }
            else
            {
                // xUnit v2 uses --filter directly
                command = $"test {projectPath} -c {configuration} --logger \"console;verbosity=normal\"";
                if (!string.IsNullOrEmpty(testFilter))
                    command += $" --filter \"{testFilter}\"";
            }

            Run("dotnet", command);
            results.Add((projectName, true, null));
            PrintColored($"✓ {projectName} - All tests passed", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            results.Add((projectName, false, ex.Message));
            PrintColored($"✗ {projectName} - Tests failed", ConsoleColor.Red);
        }
    }

    return results;
}

void PrintTestSummary(List<(string Project, bool Passed, string? Error)> results, string label)
{
    var separator = new string('=', 60);
    Console.WriteLine($"\n{separator}");
    Console.WriteLine($"{label} SUMMARY");
    Console.WriteLine(separator);

    foreach (var (project, passed, error) in results)
    {
        if (passed)
        {
            PrintColored($"  ✓ {project}", ConsoleColor.Green);
        }
        else
        {
            PrintColored($"  ✗ {project}", ConsoleColor.Red);
            if (!string.IsNullOrEmpty(error) && error.Contains("Test Run Aborted"))
                Console.WriteLine("    (Test run aborted - check for initialization errors)");
        }
    }

    var passedCount = results.Count(r => r.Passed);
    var failedCount = results.Count(r => !r.Passed);

    Console.WriteLine(separator);
    Console.ForegroundColor = passedCount > 0 ? ConsoleColor.Green : ConsoleColor.Gray;
    Console.Write($"Passed: {passedCount}");
    Console.ResetColor();
    Console.Write(" | ");
    Console.ForegroundColor = failedCount > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
    Console.Write($"Failed: {failedCount}");
    Console.ResetColor();
    Console.WriteLine($" | Total: {results.Count}");
    Console.WriteLine(separator);
}

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
  build      - Build the solution (restores only if needed)
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
  --integration                  Include integration tests (requires --project)
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

    PrintColored($"Discovered {packableProjects.Length} packable projects:", ConsoleColor.Cyan);
    PrintProjectList(packableProjects);

    PrintColored($"\nDiscovered {testProjects.Length} test projects:", ConsoleColor.Cyan);
    PrintProjectList(testProjects);

    Console.WriteLine("\nSee BUILD.md for full documentation.");
}

static bool UsesMicrosoftTestingPlatformRunner(string repoRoot, string projectPath)
{
    var fullPath = Path.Combine(repoRoot, projectPath);
    return File.Exists(fullPath) &&
           File.ReadAllText(fullPath).Contains(
               "<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>",
               StringComparison.OrdinalIgnoreCase);
}

// Translates a VSTest-style --filter expression to xUnit v3 MTP native filter arguments.
// Supports: FullyQualifiedName~X, Method~X, Category!=X, Category=X, and '&' compounds.
static string TranslateToMtpFilter(string vstestFilter)
{
    var parts = vstestFilter.Split('&');
    var mtpArgs = new List<string>();

    foreach (var part in parts)
    {
        var trimmed = part.Trim();

        // Category!=Value → --filter-not-trait "Category=Value"
        if (trimmed.Contains("!="))
        {
            var segments = trimmed.Split("!=", 2);
            mtpArgs.Add($"--filter-not-trait \"{segments[0].Trim()}={segments[1].Trim()}\"");
        }
        // FullyQualifiedName~Value or Name~Value or Method~Value → --filter-method "*Value*"
        else if (trimmed.Contains('~'))
        {
            var segments = trimmed.Split('~', 2);
            var property = segments[0].Trim();
            var value = segments[1].Trim();

            mtpArgs.Add(property switch
            {
                "ClassName" or "Namespace" => $"--filter-class \"*{value}*\"",
                _ => $"--filter-method \"*{value}*\""
            });
        }
        // Category=Value → --filter-trait "Category=Value"
        else if (trimmed.Contains('='))
        {
            var segments = trimmed.Split('=', 2);
            var property = segments[0].Trim();
            var value = segments[1].Trim();

            if (property is "FullyQualifiedName" or "Name" or "Method")
                mtpArgs.Add($"--filter-method \"{value}\"");
            else
                mtpArgs.Add($"--filter-trait \"{property}={value}\"");
        }
        else
        {
            // Unrecognized pattern — pass as-is to --filter-method with wildcards
            mtpArgs.Add($"--filter-method \"*{trimmed}*\"");
        }
    }

    return string.Join(" ", mtpArgs);
}

string[] FilterBullseyeArgs(string[] args, string[] optionsWithValues, string[] flags)
{
    var valueOptions = new HashSet<string>(optionsWithValues, StringComparer.OrdinalIgnoreCase);
    var flagOptions = new HashSet<string>(flags, StringComparer.OrdinalIgnoreCase);
    var filtered = new List<string>();

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        if (flagOptions.Contains(arg))
            continue;

        if (valueOptions.Contains(arg))
        {
            if (i + 1 < args.Length)
                i++;
            continue;
        }

        filtered.Add(arg);
    }

    return [..filtered];
}
