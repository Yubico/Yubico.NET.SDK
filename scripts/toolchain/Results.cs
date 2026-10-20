// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Text.RegularExpressions;
using static Output;

enum Outcome
{
    Passed,
    Failed,

    /// <summary>The project ran but executed no test: the filter matched nothing, or every test was skipped.</summary>
    NoTestsExecuted,
}

/// <summary>What happened to one test project in a test or coverage run.</summary>
sealed record ProjectResult(string Project, Outcome Outcome, string? Reason = null);

static class Gates
{
    /// <summary>Prints each failure and fails the target when there is at least one.</summary>
    public static void FailIfAny(IReadOnlyCollection<string> failures, string check)
    {
        if (failures.Count == 0)
            return;

        foreach (var failure in failures)
            PrintColored($"  ✗ {failure}", ConsoleColor.Red);

        throw new InvalidOperationException($"{check} failed with {failures.Count} issue(s)");
    }

    /// <summary>
    /// Fails the target when a project failed, or when <paramref name="requireExecution"/> is set and
    /// no project executed a test. A run that executed nothing is not a pass.
    /// </summary>
    public static void EnsureTestsRan(IReadOnlyList<ProjectResult> results, string kind, bool requireExecution = true)
    {
        var failCount = results.Count(r => r.Outcome == Outcome.Failed);
        if (failCount > 0)
            throw new InvalidOperationException($"{failCount} {kind} project(s) failed");

        if (requireExecution && results.Count > 0 && results.All(r => r.Outcome == Outcome.NoTestsExecuted))
            throw new InvalidOperationException($"No {kind}s were executed (no matching tests, or every test was skipped)");
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
