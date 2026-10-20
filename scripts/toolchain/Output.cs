// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

static class Output
{
    public static void PrintActiveDocumentationFiles(string[] files)
    {
        foreach (var file in files)
            Console.WriteLine(file.Replace(Path.DirectorySeparatorChar, '/'));
    }

    public static void PrintInfo(string message) => Console.WriteLine($"✓ {message}");
    public static void PrintHeader(string message) => Console.WriteLine($"\n=== {message} ===\n");

    public static void PrintColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void PrintProjectList(string[] projects)
    {
        foreach (var p in projects)
            Console.WriteLine($"  - {Path.GetFileNameWithoutExtension(p)}");
    }

    public static void PrintNoProjectsFound(string filter, string[] available)
    {
        PrintColored($"⚠ No projects match '{filter}'", ConsoleColor.Yellow);
        Console.WriteLine("Available projects:");
        PrintProjectList(available);
    }

    public static void PrintProjectHeader(string label, string projectName) =>
        Console.WriteLine($"\n=== {label}: {projectName} ===");

    public static void PrintTestSummary(List<(string Project, bool Passed, string? Error, bool Skipped)> results, string label)
    {
        var separator = new string('=', 60);
        Console.WriteLine($"\n{separator}");
        Console.WriteLine($"{label} SUMMARY");
        Console.WriteLine(separator);

        foreach (var (project, passed, error, skipped) in results)
        {
            if (skipped)
            {
                PrintColored($"  ○ {project} ({error ?? "no matching tests"})", ConsoleColor.Yellow);
            }
            else if (passed)
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

        var skippedCount = results.Count(r => r.Skipped);
        var passedCount = results.Count(r => r.Passed && !r.Skipped);
        var failedCount = results.Count(r => !r.Passed);

        // These count test PROJECTS. The per-test totals are in each runner's own summary above
        // ("total:/failed:" for xUnit v3, "Total tests:/Passed:" for VSTest), which this line used to mimic.
        Console.WriteLine(separator);
        Console.Write("Test projects: ");
        Console.ForegroundColor = passedCount > 0 ? ConsoleColor.Green : ConsoleColor.Gray;
        Console.Write($"{passedCount} passed");
        Console.ResetColor();
        Console.Write(", ");
        Console.ForegroundColor = failedCount > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
        Console.Write($"{failedCount} failed");
        Console.ResetColor();
        Console.Write(", ");
        Console.ForegroundColor = skippedCount > 0 ? ConsoleColor.Yellow : ConsoleColor.Gray;
        Console.Write($"{skippedCount} with no tests executed");
        Console.ResetColor();
        Console.WriteLine($" ({results.Count} total)");
        Console.WriteLine(separator);
    }

    public static void PrintHelp(string[] packableProjects, string[] testProjects)
    {
        Console.WriteLine(@"
Yubico.YubiKit Toolchain Script
================================

.NET 10 build automation script using Bullseye task runner.

USAGE:
  dotnet toolchain.cs [target]
  dotnet toolchain.cs -- [target] [options]   (required before script long options)

 NOTE: The -- separator passes arguments to the script instead of dotnet.
       It is required before every script long option, including --project:
   dotnet toolchain.cs -- --help
   dotnet toolchain.cs -- build --project Piv

TARGETS:
  clean          - Remove artifacts directory
  restore        - Restore NuGet dependencies
  build          - Build the solution (restores only if needed)
  test           - Run unit tests with summary output
  resilience     - Run fast no-hardware runtime resilience gates
  test-infrastructure-qa - Validate hardware-test declaration invariants
  benchmark      - Run performance benchmarks (manual, not CI)
  docs-qa        - Validate active documentation hygiene
  docs-list-active - Print the exact active documentation set used by docs-qa
  docs-inventory - Generate report-only active documentation inventory
  docs-architecture - Validate architecture diagram evidence map + image freshness
  coverage       - Run tests with code coverage
  crap           - Rank methods by CRAP score from collected coverage (requires: coverage)
  complexity     - Flag complex methods in your uncommitted changes (source only, no coverage)
  pack           - Create NuGet packages
  setup-feed     - Configure local NuGet feed
  publish        - Publish packages to local feed
  publish-remote - Push packages to a remote NuGet feed (e.g. GitHub Packages)
  default        - Run tests and publish

OPTIONS:
  --package-version <version>    Override NuGet package version
  --nuget-feed-name <name>       NuGet feed name (default: Yubico.YubiKit-LocalNuGet)
  --nuget-feed-path <path>       NuGet feed path (default: artifacts/nuget-feed)
  --nuget-feed-url <url>         Remote NuGet feed URL (required for publish-remote)
  --nuget-api-key <key>          API key for remote NuGet feed (required for publish-remote)
  --include-docs                 Include XML documentation in packages
  --dry-run                      Show what would be published without publishing
  --clean                        Run dotnet clean before build
  --filter <expression>          Test filter expression (e.g., ""FullyQualifiedName~MyTest"")
  --project <name>               Build/test specific project only (partial match)
  --integration                  Include integration tests (requires --project)
  --smoke                        Smoke test mode: skip Slow and RequiresUserPresence tests
  --fast                         Required fast mode for resilience gates
  --benchmark-args <args>         Arguments passed to BenchmarkDotNet
  --crap-args <args>             Arguments passed to crap.cs (e.g. ""--module Piv --top 50"")
  --complexity-args <args>       Arguments passed to complexity.cs (e.g. ""--module Piv"")
  -h, --help                     Show this help message

EXAMPLES:
  dotnet toolchain.cs build
  dotnet toolchain.cs -- build --project Piv
  dotnet toolchain.cs test
  dotnet toolchain.cs docs-qa
  dotnet toolchain.cs -- docs-inventory
  dotnet toolchain.cs -- test --filter ""FullyQualifiedName~MyTestClass""
  dotnet toolchain.cs -- test --project Piv --filter ""Method~Sign""
  dotnet toolchain.cs -- resilience --fast
  dotnet toolchain.cs -- benchmark --benchmark-args ""--list flat""
  dotnet toolchain.cs -- test --integration --project Piv --smoke
  dotnet toolchain.cs coverage
  dotnet toolchain.cs complexity
  dotnet toolchain.cs -- complexity --complexity-args ""--module Piv""
  dotnet toolchain.cs -- publish --package-version 1.0.0-preview.1
  dotnet toolchain.cs -- publish-remote --nuget-feed-url https://nuget.pkg.github.com/Yubico/index.json --nuget-api-key $TOKEN
  dotnet toolchain.cs -- publish-remote --dry-run --nuget-feed-url https://nuget.pkg.github.com/Yubico/index.json --nuget-api-key fake
  dotnet toolchain.cs -- --help

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

        Console.WriteLine("\nSee TOOLCHAIN.md for full documentation.");
    }
}
