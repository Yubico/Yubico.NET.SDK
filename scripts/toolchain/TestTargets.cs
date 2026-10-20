// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Diagnostics;
using System.Text.RegularExpressions;
using static Output;

static class TestTargets
{
    public static void Register(ToolchainContext ctx)
    {
        TargetRegistry.Define("test", "Run unit tests (plus integration tests with --integration) and summarise per project", ["test-infrastructure-qa"], () =>
        {
            // --integration requires --project to prevent accidentally running all integration tests
            if (ctx.IncludeIntegration && string.IsNullOrEmpty(ctx.ProjectFilter))
            {
                throw new ToolchainException(
                    "--integration requires --project, e.g. `dotnet toolchain.cs -- test --integration --project Piv`. " +
                    $"Integration test projects: {string.Join(", ", ctx.IntegrationTestProjects.Select(ProjectDiscovery.ShortName))}");
            }

            PrintHeader(ctx.IncludeIntegration ? "Running unit + integration tests" : "Running unit tests");

            var projectsToTest = ProjectDiscovery.MatchTestProjects(ctx.TestProjects, ctx.ProjectFilter);
            var results = RunTestProjects(ctx.TestFilter, projectsToTest);
            PrintTestSummary(results, "TEST");
            Gates.EnsureTestsRan(results, "test");
        });

        TargetRegistry.Define("resilience", "Run the fast no-hardware runtime resilience gates (requires --fast)", () =>
        {
            PrintHeader("Running fast runtime resilience gates");

            if (!ctx.FastMode)
                throw new ToolchainException("The resilience target requires --fast (only the fast no-hardware mode exists): `dotnet toolchain.cs -- resilience --fast`");

            var projectsToTest = ProjectDiscovery.MatchTestProjects(ctx.TestProjects, "Core");

            var stopwatch = Stopwatch.StartNew();
            var results = RunTestProjects(Traits.And(ctx.TestFilter, Traits.Is(Traits.RuntimeResilience)), projectsToTest);
            stopwatch.Stop();

            PrintTestSummary(results, "RUNTIME RESILIENCE");
            PrintInfo($"Evidence: Core unit tests with {Traits.Is(Traits.RuntimeResilience)}");
            PrintInfo($"Elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s");
            Gates.EnsureTestsRan(results, "runtime resilience test");
        });
    }

    static List<ProjectResult> RunTestProjects(string? testFilter, IEnumerable<TestProject> projects)
    {
        var results = new List<ProjectResult>();
        foreach (var project in projects)
        {
            var result = RunTestProject(testFilter, project);
            RunLog.Record(result);
            results.Add(result);
        }

        return results;
    }

    static ProjectResult RunTestProject(string? testFilter, TestProject project)
    {
        var projectName = ProjectDiscovery.ShortName(project.Path);
        PrintProjectHeader("Testing", projectName);

        string? command;
        try
        {
            command = BuildTestCommand(project, testFilter);
        }
        catch (Exception ex) when (ex is ArgumentException or ToolchainException)
        {
            // A bad --filter, or test discovery that failed: say what was wrong.
            PrintOutcome(Outcome.Failed, ex.Message);
            return new ProjectResult(projectName, Outcome.Failed, ex.Message);
        }

        if (command is null)
        {
            PrintOutcome(Outcome.NoTestsExecuted, "no test matches --filter");
            return new ProjectResult(projectName, Outcome.NoTestsExecuted, "no test matches --filter");
        }

        var run = Exec.Run("dotnet", command, Exec.Display.ErrorsOnly, $"test-{projectName}");
        var counts = TestCounts.Parse(run.Output);
        var failedTests = FailedTest.Parse(run.Output);
        var result = Classify(projectName, run.ExitCode, counts, failedTests) with
        {
            Counts = counts,
            FailedTests = failedTests,
            LogPath = run.LogPath,
        };

        PrintOutcome(result.Outcome, result.Reason);
        return result;
    }

    static ProjectResult Classify(string projectName, int exitCode, TestCounts? counts, IReadOnlyList<FailedTest> failedTests)
    {
        if (exitCode != 0)
        {
            var reason = failedTests.Count > 0 || counts is { Failed: > 0 }
                ? $"{Math.Max(failedTests.Count, counts?.Failed ?? 0)} test(s) failed"
                : $"test run exited with code {exitCode} before reporting results (build error or crash; see output)";
            return new ProjectResult(projectName, Outcome.Failed, reason);
        }

        // A run where every test was skipped (typically [SkippableTheory] + no matching YubiKey) exits 0.
        // That is not a pass: report it like a filter that matched nothing.
        if (counts is { Executed: 0, Skipped: > 0 })
            return new ProjectResult(projectName, Outcome.NoTestsExecuted, $"all {counts.Skipped} tests skipped");

        return new ProjectResult(projectName, Outcome.Passed, counts is null ? null : $"{counts.Executed} executed, {counts.Skipped} skipped");
    }

    /// <summary>
    /// The dotnet arguments that run <paramref name="project"/> with <paramref name="testFilter"/> translated
    /// for its runner, or null when the filter selects no test in it (checked by discovery, without running).
    /// </summary>
    static string? BuildTestCommand(TestProject project, string? testFilter)
    {
        if (project.UsesTestingPlatformRunner)
        {
            // xUnit v3 MTP runner uses native filter options (--filter-method, --filter-trait, etc.)
            // not VSTest's --filter syntax
            var command = $"run --project {project.Path} -c {Conventions.Configuration}";
            if (string.IsNullOrEmpty(testFilter))
                return command;

            var (mtpFilter, hasPositiveFilters, positiveMtpFilterArgs) = TranslateToMtpFilter(testFilter);
            // Preflight only the inclusion filter. Exclusions still apply to the real run.
            if (hasPositiveFilters && !MtpFilterHasMatches(project.Path, positiveMtpFilterArgs))
                return null;

            return $"{command} -- {mtpFilter}";
        }

        // xUnit v2 runs under VSTest, which takes --filter directly.
        var vsTestCommand = $"test {project.Path} -c {Conventions.Configuration} --logger \"console;verbosity=normal\"";
        if (string.IsNullOrEmpty(testFilter))
            return vsTestCommand;

        var vsTestFilter = TranslateToVsTestFilter(testFilter);

        // Same preflight the MTP path gets. Without it a filter matching nothing makes
        // VSTest print "No test matches..." and still exit 0, which would be reported
        // as "All tests passed" — a green result from a run that never happened.
        if (!VsTestFilterHasMatches(project.Path, vsTestFilter))
            return null;

        return $"{vsTestCommand} --filter \"{vsTestFilter}\"";
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

    static bool VsTestFilterHasMatches(string projectPath, string vsTestFilter) =>
        FilterHasMatches(
            projectPath,
            ["test", projectPath, "-c", Conventions.Configuration, "--list-tests", "--filter", vsTestFilter],
            RunnerMarkers.VsTestNoMatches);

    static bool MtpFilterHasMatches(string projectPath, string[] positiveMtpFilterArgs) =>
        FilterHasMatches(
            projectPath,
            ["run", "--project", projectPath, "-c", Conventions.Configuration, "--", "--list-tests", .. positiveMtpFilterArgs],
            RunnerMarkers.MtpNoMatches);

    // True when the filter selects at least one test in the project. Discovery-only, so it never runs a
    // ceremony or touches hardware. Both runners exit 0 when nothing matches, so the marker text decides.
    static bool FilterHasMatches(string projectPath, IReadOnlyList<string> listTestsArguments, string noMatchesMarker)
    {
        var (exitCode, output) = Dotnet.Capture(listTestsArguments);
        if (output.Contains(noMatchesMarker, StringComparison.OrdinalIgnoreCase))
            return false;

        if (exitCode != 0)
            throw new ToolchainException($"Listing tests failed (exit code {exitCode}):\n{output}");

        return true;
    }
}
