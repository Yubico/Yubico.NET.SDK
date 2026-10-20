// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

static class Output
{
    public static void PrintActiveDocumentationFiles(string[] files)
    {
        foreach (var file in files)
            Console.WriteLine(file.Replace(Path.DirectorySeparatorChar, '/'));
    }

    // Every status line starts with one of these fixed markers, so a reader (or grep) can classify a
    // line without colour: "▶ RUN" a command is starting, "✓ PASS" / "✗ FAIL" / "○ NO-TESTS" a project's
    // outcome, "! WARN" something that did not stop the run, and a bare "✓" a completed step.
    public const string RunMarker = "▶ RUN";
    public const string PassMarker = "✓ PASS";
    public const string FailMarker = "✗ FAIL";
    public const string NoTestsMarker = "○ NO-TESTS";
    public const string WarnMarker = "! WARN";

    static readonly bool s_noColor = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

    public static void PrintInfo(string message) => Console.WriteLine($"✓ {message}");
    public static void PrintHeader(string message) => Console.WriteLine($"\n=== {message} ===\n");
    public static void PrintRun(string command) => Console.WriteLine($"{RunMarker} {command}");
    public static void PrintWarn(string message) => PrintColored($"{WarnMarker} {message}", ConsoleColor.Yellow);

    /// <summary>A project's outcome, inside the section that already names the project.</summary>
    public static void PrintOutcome(Outcome outcome, string? detail)
    {
        var suffix = string.IsNullOrEmpty(detail) ? "" : $" ({detail})";
        PrintColored($"{Marker(outcome)}{suffix}", Colour(outcome));
    }

    public static void PrintColored(string message, ConsoleColor color)
    {
        // Console colours are dropped automatically when output is redirected; NO_COLOR also turns them off.
        if (!s_noColor)
            Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void PrintProjectList(IEnumerable<string> projects)
    {
        foreach (var p in projects)
            Console.WriteLine($"  - {ProjectDiscovery.ShortName(p)}");
    }

    public static void PrintProjectHeader(string label, string projectName) =>
        Console.WriteLine($"\n=== {label}: {projectName} ===");

    static string Marker(Outcome outcome) => outcome switch
    {
        Outcome.Passed => PassMarker,
        Outcome.Failed => FailMarker,
        _ => NoTestsMarker,
    };

    static ConsoleColor Colour(Outcome outcome) => outcome switch
    {
        Outcome.Passed => ConsoleColor.Green,
        Outcome.Failed => ConsoleColor.Red,
        _ => ConsoleColor.Yellow,
    };

    public static void PrintTestSummary(IReadOnlyList<ProjectResult> results, string label)
    {
        var separator = new string('=', 60);
        Console.WriteLine($"\n{separator}");
        Console.WriteLine($"{label} SUMMARY");
        Console.WriteLine(separator);

        var nameWidth = results.Count == 0 ? 0 : results.Max(r => r.Project.Length) + 2;
        var markerWidth = new[] { PassMarker, FailMarker, NoTestsMarker }.Max(m => m.Length) + 2;
        foreach (var (project, outcome, reason) in results)
        {
            var detail = string.IsNullOrEmpty(reason) ? "" : $"  {reason}";
            PrintColored($"  {Marker(outcome).PadRight(markerWidth)}{project.PadRight(nameWidth)}{detail}".TrimEnd(), Colour(outcome));
            if (outcome == Outcome.Failed && reason?.Contains("Test Run Aborted") == true)
                Console.WriteLine("    (Test run aborted - check for initialization errors)");
        }

        var skippedCount = results.Count(r => r.Outcome == Outcome.NoTestsExecuted);
        var passedCount = results.Count(r => r.Outcome == Outcome.Passed);
        var failedCount = results.Count(r => r.Outcome == Outcome.Failed);

        // These count test PROJECTS. The per-test totals are in each runner's own summary above
        // ("total:/failed:" for xUnit v3, "Total tests:/Passed:" for VSTest), which this line used to mimic.
        Console.WriteLine(separator);
        Console.Write("Test projects: ");
        WriteCount($"{passedCount} passed", passedCount > 0 ? ConsoleColor.Green : ConsoleColor.Gray);
        Console.Write(", ");
        WriteCount($"{failedCount} failed", failedCount > 0 ? ConsoleColor.Red : ConsoleColor.Gray);
        Console.Write(", ");
        WriteCount($"{skippedCount} with no tests executed", skippedCount > 0 ? ConsoleColor.Yellow : ConsoleColor.Gray);
        Console.WriteLine($" ({results.Count} total)");
        Console.WriteLine(separator);

        static void WriteCount(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }
    }

    public static void PrintHelp(ToolchainContext ctx)
    {
        Console.WriteLine("""

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
            """);

        var nameWidth = TargetRegistry.All.Max(t => t.Name.Length) + 2;
        foreach (var target in TargetRegistry.All)
        {
            var dependsOn = target.DependsOn.Length > 0 ? $" (runs first: {string.Join(", ", target.DependsOn)})" : "";
            Console.WriteLine($"  {target.Name.PadRight(nameWidth)}{target.Description}{dependsOn}");
        }

        Console.WriteLine("\nOPTIONS:");
        var optionRows = Options.All
            .Select(o => (Usage: o.Value is null ? o.Name : $"{o.Name} {o.Value}", o.Description))
            .Append((Usage: "-h, --help", Description: "Show this help message"))
            .ToArray();
        var usageWidth = optionRows.Max(r => r.Usage.Length) + 2;
        foreach (var (usage, description) in optionRows)
            Console.WriteLine($"  {usage.PadRight(usageWidth)}{description}");

        Console.WriteLine("""

            EXAMPLES:
              dotnet toolchain.cs build
              dotnet toolchain.cs -- build --project Piv
              dotnet toolchain.cs test
              dotnet toolchain.cs -- test --project Piv --filter "Method~Sign"
              dotnet toolchain.cs -- test --project WebAuthn.UnitTests --filter "FullyQualifiedName~YESDK1619|FullyQualifiedName~YESDK1613"
              dotnet toolchain.cs -- test --integration --project Piv --smoke
              dotnet toolchain.cs -- resilience --fast
              dotnet toolchain.cs docs-qa
              dotnet toolchain.cs coverage
              dotnet toolchain.cs -- complexity --complexity-args "--module Piv"
              dotnet toolchain.cs -- benchmark --benchmark-args "--list flat"
              dotnet toolchain.cs -- publish --package-version 1.0.0-preview.1
              dotnet toolchain.cs -- publish-remote --dry-run --nuget-feed-url https://nuget.pkg.github.com/Yubico/index.json --nuget-api-key fake

            FILTER SYNTAX (for --filter):
              FullyQualifiedName~MyClass     Tests containing 'MyClass' in full name
              Method~Sign                    Test methods containing 'Sign'
              Name=MyTestMethod              Exact test method name
              ClassName~Integration          Classes containing 'Integration'
              Category=Slow                  Tests with [Trait("Category", "Slow")]
              Category!=RequiresHardware     Exclude tests with that trait
              A&B                            All terms must match
              A|B                            Any term matches (alternatives of one kind; xUnit v3
                                             projects reject mixing & and |)

              Categories: RequiresHardware, RequiresUserPresence, Slow, Integration, RuntimeResilience.
              AI agents: always exclude RequiresUserPresence (cannot insert, remove or touch a device).

            TEST RUNNERS:
              The script detects each test project's runner: xUnit v3 projects (UseMicrosoftTestingPlatformRunner)
              run with `dotnet run` and xUnit v3 filter options, xUnit v2 projects with `dotnet test --filter`.
              --filter is translated for both, so always test through this script, not `dotnet test`.

              To run a unit project by hand, use `dotnet run --project <proj>` or its apphost, not
              `dotnet <proj>.dll`: Core's probes re-launch the test host from the entry assembly and fail
              spuriously under `dotnet X.dll`.

            READING THE OUTPUT (AI agents and CI):
              The run ends with a RESULT block. Read it, not the lines above it:
                RESULT: PASSED | FAILED   the outcome; the exit code is 0 only for PASSED
                ✓/✗ <target>              each target that ran, with its error
                Projects: ...; tests: ... project and per-test totals
                Warnings: <n>             compiler/MSBuild warnings per step, by code (not shown above)
                Failed: <project>         each failing project, its failing tests and messages
                Next: <command>           re-runs only what failed
                Run log: <path>           where everything is saved
              While running, lines start with fixed markers: "▶ RUN" (a command starts, with
              "output: <log>" for its full output), "✓ PASS", "✗ FAIL", "○ NO-TESTS" (nothing executed:
              the filter matched nothing or every test was skipped; this fails the test target),
              "! WARN".
              Build and test output is not shown (only error lines); use --verbose to stream it.
              Project names omit the Yubico.YubiKit. prefix.

            RUN LOGS:
              Every run that executes a target is saved under artifacts/toolchain/runs/<time>-<targets>/:
              run.log (all console output), one log per build/test step, and result.json (the RESULT
              block as JSON). artifacts/toolchain/latest/ is the newest run (latest.txt names it where
              symlinks are unavailable). The 5 newest runs are kept; `clean` does not remove them.
            """);

        PrintColored($"Discovered {ctx.PackableProjects.Length} packable projects:", ConsoleColor.Cyan);
        PrintProjectList(ctx.PackableProjects);

        PrintColored($"\nDiscovered {ctx.TestProjects.Length} test projects:", ConsoleColor.Cyan);
        PrintProjectList(ctx.TestProjects.Select(p => p.Path));

        Console.WriteLine("\nSee TOOLCHAIN.md for full documentation.");
    }
}
