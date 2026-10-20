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

    public static void PrintProjectList(IEnumerable<string> projects)
    {
        foreach (var p in projects)
            Console.WriteLine($"  - {Path.GetFileNameWithoutExtension(p)}");
    }

    public static void PrintProjectHeader(string label, string projectName) =>
        Console.WriteLine($"\n=== {label}: {projectName} ===");

    public static void PrintTestSummary(IReadOnlyList<ProjectResult> results, string label)
    {
        var separator = new string('=', 60);
        Console.WriteLine($"\n{separator}");
        Console.WriteLine($"{label} SUMMARY");
        Console.WriteLine(separator);

        foreach (var (project, outcome, reason) in results)
        {
            switch (outcome)
            {
                case Outcome.NoTestsExecuted:
                    PrintColored($"  ○ {project} ({reason ?? "no matching tests"})", ConsoleColor.Yellow);
                    break;
                case Outcome.Passed:
                    PrintColored($"  ✓ {project}", ConsoleColor.Green);
                    break;
                default:
                    PrintColored($"  ✗ {project}", ConsoleColor.Red);
                    if (!string.IsNullOrEmpty(reason) && reason.Contains("Test Run Aborted"))
                        Console.WriteLine("    (Test run aborted - check for initialization errors)");
                    break;
            }
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
            """);

        PrintColored($"Discovered {ctx.PackableProjects.Length} packable projects:", ConsoleColor.Cyan);
        PrintProjectList(ctx.PackableProjects);

        PrintColored($"\nDiscovered {ctx.TestProjects.Length} test projects:", ConsoleColor.Cyan);
        PrintProjectList(ctx.TestProjects.Select(p => p.Path));

        Console.WriteLine("\nSee TOOLCHAIN.md for full documentation.");
    }
}
