// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Text.RegularExpressions;
using static Output;

/// <summary>
/// An expected failure: bad usage, a failed gate, failed tests. Reported as its message alone; any other
/// exception is a toolchain bug and keeps its stack trace.
/// </summary>
sealed class ToolchainException(string message) : Exception(message);

enum Outcome
{
    Passed,
    Failed,

    /// <summary>The project ran but executed no test: the filter matched nothing, or every test was skipped.</summary>
    NoTestsExecuted,
}

/// <summary>What happened to one test project in a test or coverage run.</summary>
sealed record ProjectResult(string Project, Outcome Outcome, string? Reason = null)
{
    public TestCounts? Counts { get; init; }
    public IReadOnlyList<FailedTest> FailedTests { get; init; } = [];

    /// <summary>The project's full runner output, when it was saved.</summary>
    public string? LogPath { get; init; }
}

/// <summary>A failed test and the first lines of its failure message.</summary>
sealed record FailedTest(string Name, string Message)
{
    /// <summary>The fully qualified method name, without theory arguments: usable in --filter.</summary>
    public string Method => Name.Split('(', 2)[0].Trim();

    // xUnit v3 on MTP:   "failed Ns.Class.Method(x: 3) (0ms)" (or "(1s 20ms)") then 2-space-indented message lines, then "    at ..." frames.
    static readonly Regex s_mtpHeader = new(@"^failed (?<name>.+) \(\d[\d a-z.]*\)$", RegexOptions.Compiled);

    // xUnit v2 on VSTest: "  Failed Ns.Class.Method [2 ms]", "  Error Message:", message lines, "  Stack Trace:".
    static readonly Regex s_vsTestHeader = new(@"^\s{2}Failed (?<name>.+?) \[[^\]]*\]$", RegexOptions.Compiled);

    const int MessageLines = 3;

    public static IReadOnlyList<FailedTest> Parse(string output)
    {
        var lines = output.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var failures = new List<FailedTest>();

        for (var i = 0; i < lines.Length; i++)
        {
            var mtp = s_mtpHeader.Match(lines[i]);
            if (mtp.Success)
            {
                var message = lines.Skip(i + 1)
                    .TakeWhile(l => l.StartsWith("  ", StringComparison.Ordinal) && !l.TrimStart().StartsWith("at ", StringComparison.Ordinal));
                failures.Add(new FailedTest(mtp.Groups["name"].Value, Summarise(message)));
                continue;
            }

            var vsTest = s_vsTestHeader.Match(lines[i]);
            if (vsTest.Success)
            {
                var message = lines.Skip(i + 1)
                    .SkipWhile(l => l.Trim() == "Error Message:")
                    .TakeWhile(l => l.Trim() != "Stack Trace:" && !s_vsTestHeader.IsMatch(l));
                failures.Add(new FailedTest(vsTest.Groups["name"].Value, Summarise(message)));
            }
        }

        return failures;
    }

    // The runner wraps exceptions in its own type; the inner type and message are what matter.
    static string Summarise(IEnumerable<string> messageLines) =>
        string.Join(" | ", messageLines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(l => Regex.Replace(l, @"^Xunit\.Runner\.\S+Exception: ", ""))
            .Take(MessageLines));
}

static class Gates
{
    /// <summary>Prints each failure and fails the target when there is at least one.</summary>
    public static void FailIfAny(IReadOnlyCollection<string> failures, string check)
    {
        if (failures.Count == 0)
            return;

        foreach (var failure in failures)
            PrintColored($"  ✗ {failure}", ConsoleColor.Red);

        throw new ToolchainException($"{check} failed with {failures.Count} issue(s)");
    }

    /// <summary>
    /// Fails the target when a project failed, or when <paramref name="requireExecution"/> is set and
    /// no project executed a test. A run that executed nothing is not a pass.
    /// </summary>
    public static void EnsureTestsRan(IReadOnlyList<ProjectResult> results, string kind, bool requireExecution = true)
    {
        var failCount = results.Count(r => r.Outcome == Outcome.Failed);
        if (failCount > 0)
            throw new ToolchainException($"{failCount} {kind} project(s) failed");

        if (requireExecution && results.Count > 0 && results.All(r => r.Outcome == Outcome.NoTestsExecuted))
            throw new ToolchainException($"No {kind}s were executed (no matching tests, or every test was skipped)");
    }
}

/// <summary>Per-test totals from a runner summary. Executed is passed + failed.</summary>
sealed record TestCounts(int Executed, int Skipped, int Failed)
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
            var failed = int.Parse(vsTest.Groups["failed"].Value);
            return new TestCounts(
                failed + int.Parse(vsTest.Groups["passed"].Value),
                int.Parse(vsTest.Groups["skipped"].Value),
                failed);
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
            return new TestCounts(
                passed + succeeded + failedUpper + failedLower,
                skippedUpper + skippedLower,
                failedUpper + failedLower);
        }

        return null;
    }
}
