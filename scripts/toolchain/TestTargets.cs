// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Diagnostics;
using System.Text.RegularExpressions;
using static Bullseye.Targets;
using static Output;

static class TestTargets
{
    public static void Register(ToolchainContext ctx)
    {
        Target("test", DependsOn("test-infrastructure-qa"), () =>
        {
            // --integration requires --project to prevent accidentally running all integration tests
            if (ctx.IncludeIntegration && string.IsNullOrEmpty(ctx.ProjectFilter))
            {
                PrintColored("Error: --integration requires --project to specify which module to test.", ConsoleColor.Red);
                Console.WriteLine("Example: dotnet toolchain.cs -- test --integration --project Piv");
                Console.WriteLine("\nAvailable integration test projects:");
                PrintProjectList(ctx.IntegrationTestProjects);
                throw new InvalidOperationException("--integration requires --project");
            }

            PrintHeader(ctx.IncludeIntegration ? "Running unit + integration tests" : "Running unit tests");

            var projectsToTest = ProjectDiscovery.FilterToProject(ctx.TestProjectInfos, ctx.ProjectFilter);
            if (projectsToTest is null)
                return;

            var results = RunTestProjects(ctx, ctx.TestFilter, projectsToTest);
            PrintTestSummary(results, "TEST");

            var failCount = results.Count(r => !r.Passed);
            if (failCount > 0)
                throw new InvalidOperationException($"{failCount} test project(s) failed");

            if (results.Count > 0 && results.All(r => r.Skipped))
                throw new InvalidOperationException("No tests were executed (no matching tests, or every test was skipped)");
        });

        Target("resilience", () =>
        {
            PrintHeader("Running fast runtime resilience gates");

            if (!ctx.FastMode)
            {
                throw new InvalidOperationException("The resilience target currently requires --fast; only fast no-hardware mode is implemented.");
            }

            var resilienceFilter = string.IsNullOrEmpty(ctx.TestFilter)
                ? "Category=RuntimeResilience"
                : $"{ctx.TestFilter}&Category=RuntimeResilience";

            var projectsToTest = ProjectDiscovery.FilterToProject(ctx.TestProjectInfos, "Core");
            if (projectsToTest is null)
                return;

            var stopwatch = Stopwatch.StartNew();
            var results = RunTestProjects(ctx, resilienceFilter, projectsToTest);
            stopwatch.Stop();

            PrintTestSummary(results, "RUNTIME RESILIENCE");
            PrintInfo("Evidence: Core unit tests with Category=RuntimeResilience");
            PrintInfo($"Elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s");

            var failCount = results.Count(r => !r.Passed);
            if (failCount > 0)
                throw new InvalidOperationException($"{failCount} runtime resilience test project(s) failed");

            if (results.Count > 0 && results.All(r => r.Skipped))
                throw new InvalidOperationException("No runtime resilience tests matched the specified filter");
        });
    }

    static List<(string Project, bool Passed, string? Error, bool Skipped)> RunTestProjects(
        ToolchainContext ctx,
        string? testFilter,
        IEnumerable<(string ProjectPath, bool UsesTestingPlatformRunner)> projects)
    {
        var results = new List<(string Project, bool Passed, string? Error, bool Skipped)>();

        foreach (var (projectPath, usesTestingPlatformRunner) in projects)
        {
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            PrintProjectHeader("Testing", projectName);

            try
            {
                string command;
                if (usesTestingPlatformRunner)
                {
                    // xUnit v3 MTP runner uses native filter options (--filter-method, --filter-trait, etc.)
                    // not VSTest's --filter syntax
                    command = $"run --project {projectPath} -c {ctx.Configuration}";
                    if (!string.IsNullOrEmpty(testFilter))
                    {
                        var (mtpFilter, hasPositiveFilters, positiveMtpFilterArgs) = TranslateToMtpFilter(testFilter);
                        // Preflight only the inclusion filter. Exclusions still apply to the real run.
                        if (hasPositiveFilters && !MtpFilterHasMatches(ctx, projectPath, positiveMtpFilterArgs))
                        {
                            results.Add((projectName, true, "No matching tests", true));
                            PrintColored($"○ {projectName} - No matching tests", ConsoleColor.Yellow);
                            continue;
                        }

                        command += $" -- {mtpFilter}";
                    }
                }
                else
                {
                    // xUnit v2 runs under VSTest, which takes --filter directly.
                    command = $"test {projectPath} -c {ctx.Configuration} --logger \"console;verbosity=normal\"";
                    if (!string.IsNullOrEmpty(testFilter))
                    {
                        var vsTestFilter = TranslateToVsTestFilter(testFilter);

                        // Same preflight the MTP path gets. Without it a filter matching nothing makes
                        // VSTest print "No test matches..." and still exit 0, which this method would
                        // report as "All tests passed" — a green result from a run that never happened.
                        if (!VsTestFilterHasMatches(ctx, projectPath, vsTestFilter))
                        {
                            results.Add((projectName, true, "No matching tests", true));
                            PrintColored($"○ {projectName} - No matching tests", ConsoleColor.Yellow);
                            continue;
                        }

                        command += $" --filter \"{vsTestFilter}\"";
                    }
                }

                var counts = DotnetProcess.RunTestAndReadCounts(command);

                // A run where every test was skipped (typically [SkippableTheory] + no matching YubiKey) exits 0.
                // That is not a pass: report it as skipped, like a filter that matched nothing.
                if (counts is { Executed: 0, Skipped: > 0 })
                {
                    var reason = $"all {counts.Skipped} tests skipped";
                    results.Add((projectName, true, reason, true));
                    PrintColored($"○ {projectName} - No tests executed ({reason})", ConsoleColor.Yellow);
                    continue;
                }

                results.Add((projectName, true, null, false));
                PrintColored(
                    counts is null
                        ? $"✓ {projectName} - All tests passed"
                        : $"✓ {projectName} - All tests passed ({counts.Executed} executed, {counts.Skipped} skipped)",
                    ConsoleColor.Green);
            }
            catch (ArgumentException ex)
            {
                // A bad --filter is a usage error, not a test failure: say what was wrong.
                results.Add((projectName, false, ex.Message, false));
                PrintColored($"✗ {projectName} - {ex.Message}", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                results.Add((projectName, false, ex.Message, false));
                PrintColored($"✗ {projectName} - Tests failed", ConsoleColor.Red);
            }
        }

        return results;
    }

    // Translates a VSTest-style --filter expression to xUnit v3 MTP native filter arguments.
    // Supports: FullyQualifiedName~X, Method~X, Category!=X, Category=X, and '&' compounds.
    // Returns (filter args string, whether any positive/inclusion filters are present).
    // Translates a VSTest-style --filter expression to xUnit v3 Microsoft.Testing.Platform options.
    //
    // Supported shapes: `A&B&C` (every term must match) and `A|B|C` (any term matches). MTP has no
    // grouping operator, so an expression that mixes `&` and `|` is rejected with a clear message
    // instead of silently matching the wrong set. Repeated options of one kind are ORed by xUnit
    // (`--filter-method X --filter-method Y`), which is what `|` maps onto; `&` maps onto options of
    // different kinds, or onto a single `--filter-method` with both wildcards when both terms are
    // method patterns (xUnit ANDs distinct option kinds, so two method terms cannot be ANDed).
    static (string Args, bool HasPositiveFilters, string[] PositiveArgs) TranslateToMtpFilter(string vstestFilter)
    {
        if (vstestFilter.Contains('|'))
        {
            if (vstestFilter.Contains('&'))
                throw new ArgumentException(
                    "--filter cannot mix '|' and '&' for xUnit v3 projects: use either \"A|B\" (any) or \"A&B\" (all).");

            var alternatives = vstestFilter.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var translated = alternatives.Select(TranslateToMtpFilter).ToArray();
            var kinds = translated.SelectMany(t => t.PositiveArgs.Where(a => a.StartsWith("--filter", StringComparison.Ordinal))).Distinct().ToArray();
            if (kinds.Length > 1)
                throw new ArgumentException(
                    $"--filter '|' alternatives must be of one kind for xUnit v3 projects (got {string.Join(", ", kinds)}); e.g. \"FullyQualifiedName~A|FullyQualifiedName~B\".");

            return (
                string.Join(" ", translated.Select(t => t.Args)),
                translated.Any(t => t.HasPositiveFilters),
                [.. translated.SelectMany(t => t.PositiveArgs)]);
        }

        var parts = vstestFilter.Split('&');
        var mtpArgs = new List<string>();
        var positiveArgs = new List<string>();
        var hasPositiveFilters = false;

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
                hasPositiveFilters = true;
                var segments = trimmed.Split('~', 2);
                var property = segments[0].Trim();
                var value = segments[1].Trim();

                var filterOption = property switch
                {
                    "ClassName" or "Namespace" => "--filter-class",
                    _ => "--filter-method"
                };
                var filterValue = $"*{value}*";

                mtpArgs.Add($"{filterOption} \"{filterValue}\"");
                positiveArgs.Add(filterOption);
                positiveArgs.Add(filterValue);
            }
            // Category=Value → --filter-trait "Category=Value"
            else if (trimmed.Contains('='))
            {
                hasPositiveFilters = true;
                var segments = trimmed.Split('=', 2);
                var property = segments[0].Trim();
                var value = segments[1].Trim();

                if (property is "FullyQualifiedName" or "Name" or "Method")
                {
                    mtpArgs.Add($"--filter-method \"{value}\"");
                    positiveArgs.Add("--filter-method");
                    positiveArgs.Add(value);
                }
                else
                {
                    mtpArgs.Add($"--filter-trait \"{property}={value}\"");
                    positiveArgs.Add("--filter-trait");
                    positiveArgs.Add($"{property}={value}");
                }
            }
            else
            {
                hasPositiveFilters = true;
                // Unrecognized pattern — pass as-is to --filter-method with wildcards
                var filterValue = $"*{trimmed}*";
                mtpArgs.Add($"--filter-method \"{filterValue}\"");
                positiveArgs.Add("--filter-method");
                positiveArgs.Add(filterValue);
            }
        }

        return (string.Join(" ", mtpArgs), hasPositiveFilters, [.. positiveArgs]);
    }

    // Normalises a --filter expression for VSTest (xUnit v2, the integration projects).
    //
    // VSTest only understands FullyQualifiedName, DisplayName and trait properties. `Method` is an
    // MSTest/NUnit property, so `Method~Sign` silently matches nothing here even though the MTP path
    // translates it happily — the exact asymmetry that made documented commands no-op on the hardware
    // lanes. Map it onto the closest portable equivalent so one documented syntax works everywhere.
    //
    // FullyQualifiedName covers namespace.class.method, so the rewrite is slightly wider than a
    // method-only match: `Method~Sign` will also match a class named SignatureTests. That fails safe
    // (more tests, never fewer), and VsTestFilterHasMatches still catches a filter that matches none.
    static string TranslateToVsTestFilter(string vstestFilter) =>
        Regex.Replace(vstestFilter, @"\b(Method|Name)\s*~", "FullyQualifiedName~");

    // True when the filter selects at least one test in the project.
    //
    // Discovery-only, so it never runs a ceremony or touches hardware.
    static bool VsTestFilterHasMatches(ToolchainContext ctx, string projectPath, string vsTestFilter)
    {
        var (exitCode, output) = DotnetProcess.RunAndCapture(
        [
            "test", projectPath, "-c", ctx.Configuration, "--list-tests", "--filter", vsTestFilter
        ]);

        // VSTest 18.x reports zero discovery matches with this text and still exits 0.
        if (output.Contains("No test matches the given testcase filter", StringComparison.OrdinalIgnoreCase))
            return false;

        if (exitCode != 0)
            throw new InvalidOperationException($"Failed to list tests for {projectPath}: {output}");

        return true;
    }

    static bool MtpFilterHasMatches(ToolchainContext ctx, string projectPath, string[] positiveMtpFilterArgs)
    {
        var args = new List<string>
        {
            "run",
            "--project",
            projectPath,
            "-c",
            ctx.Configuration,
            "--",
            "--list-tests"
        };
        args.AddRange(positiveMtpFilterArgs);

        var (exitCode, output) = DotnetProcess.RunAndCapture(args);
        // xUnit v3 MTP 3.0.0 reports zero discovery matches with this summary text.
        if (output.Contains("found 0 test(s)", StringComparison.OrdinalIgnoreCase))
            return false;

        if (exitCode != 0)
            throw new InvalidOperationException($"Failed to list tests for {projectPath}: {output}");

        return true;
    }
}

/// <summary>Per-test totals from a runner summary. Executed is passed + failed.</summary>
sealed record TestCounts(int Executed, int Skipped)
{
    // VSTest: "Passed!  - Failed:     0, Passed:    83, Skipped:    12, Total:    95, Duration: ..."
    static readonly Regex s_vsTest = new(
        @"Failed:\s*(?<failed>\d+),\s*Passed:\s*(?<passed>\d+),\s*Skipped:\s*(?<skipped>\d+),\s*Total:\s*(?<total>\d+)",
        RegexOptions.Compiled);

    // VSTest console logger at verbosity=normal: "Total tests: 95" then indented "Passed: 83" / "Failed: 0" / "Skipped: 12"
    // (a zero line is omitted). Microsoft.Testing.Platform (xUnit v3): "total:", "failed:", "succeeded:", "skipped:" lines.
    static readonly Regex s_lines = new(
        @"^\s*(?<key>Total tests|total|Passed|Failed|Skipped|failed|succeeded|skipped):\s*(?<value>\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public static TestCounts? Parse(string output)
    {
        var vsTest = s_vsTest.Matches(output).LastOrDefault();
        if (vsTest is not null)
        {
            return new TestCounts(
                int.Parse(vsTest.Groups["failed"].Value) + int.Parse(vsTest.Groups["passed"].Value),
                int.Parse(vsTest.Groups["skipped"].Value));
        }

        var lines = s_lines.Matches(output)
            .GroupBy(m => m.Groups["key"].Value, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => int.Parse(g.Last().Groups["value"].Value), StringComparer.Ordinal);
        if (lines.ContainsKey("Total tests") || lines.ContainsKey("total"))
        {
            lines.TryGetValue("Passed", out var passed);
            lines.TryGetValue("succeeded", out var succeeded);
            lines.TryGetValue("Failed", out var failedUpper);
            lines.TryGetValue("failed", out var failedLower);
            lines.TryGetValue("Skipped", out var skippedUpper);
            lines.TryGetValue("skipped", out var skippedLower);
            return new TestCounts(passed + succeeded + failedUpper + failedLower, skippedUpper + skippedLower);
        }

        return null;
    }
}