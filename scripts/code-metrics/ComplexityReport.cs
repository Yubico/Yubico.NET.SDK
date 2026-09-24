// Shared by complexity.cs and crap.cs (self-check fixtures) through #:include. See TOOLCHAIN.md.

using System.Text;

/// <summary>What a complexity report needs to know about the run that produced it.</summary>
sealed record ComplexityReportSettings(string RepoRoot, int MaxCyclomatic, int MaxCognitive, int Top);

sealed record MethodResult
{
    public required SourceMethod Method { get; init; }
    public required bool Exceeds { get; init; }

    /// <summary>Null outside a changed scope, and for methods within thresholds.</summary>
    public ChangeStatus? Status { get; init; }
    public SourceMethod? Base { get; init; }

    /// <summary>
    /// New and worse findings need action. In a full scan there is no base to compare with,
    /// so every finding does.
    /// </summary>
    public bool NeedsAction => Exceeds && Status is null or ChangeStatus.New or ChangeStatus.Worse;

    /// <summary>Type and member without the namespace, for the justification line.</summary>
    public string ShortName => $"{Method.NestedTypeName}.{Method.MethodName}";
}

static class ComplexityFindings
{
    /// <summary>Findings with those that need action first, then the hardest to read.</summary>
    public static List<MethodResult> Ordered(List<MethodResult> results) =>
        [.. results
            .Where(r => r.Exceeds)
            .OrderByDescending(r => r.NeedsAction)
            .ThenByDescending(r => r.Method.Cognitive)
            .ThenByDescending(r => r.Method.Cyclomatic)];
}

/// <summary>
/// The pull request comment section: the same findings as the console report, as GitHub markdown.
/// </summary>
static class MarkdownReport
{
    const string Justify = "<why this complexity is warranted>";

    public static string Render(ComplexityReportSettings options, MetricScope scope, List<MethodResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### Complexity");
        sb.AppendLine();
        sb.AppendLine(Intro(options, scope));
        sb.AppendLine();

        var findings = ComplexityFindings.Ordered(results);

        if (results.Count == 0)
        {
            sb.AppendLine(scope.IsChanged ? "No shipping C# methods changed." : "No shipping C# methods in scope.");
            return sb.ToString().TrimEnd();
        }

        if (findings.Count == 0)
        {
            sb.AppendLine($"No {(scope.IsChanged ? "changed " : "")}method exceeds a threshold ({Count(results.Count)} checked).");
            return sb.ToString().TrimEnd();
        }

        AppendTable(sb, options, scope, findings);
        sb.AppendLine();
        AppendSummary(sb, scope, findings);
        return sb.ToString().TrimEnd();
    }

    static string Intro(ComplexityReportSettings options, MetricScope scope)
    {
        var what = scope.IsChanged
            ? $"Methods changed vs `{scope.BaseRef}`"
            : "Methods";
        var where = scope.Modules.Count switch
        {
            0 => scope.IsChanged ? "" : " in the shipping SDK",
            1 => $" in module {scope.Modules[0]}",
            _ => $" in modules {string.Join(", ", scope.Modules)}",
        };

        return $"{what}{where} with cyclomatic complexity above {options.MaxCyclomatic} " +
               $"or cognitive complexity above {options.MaxCognitive}. Source only; coverage plays no part.";
    }

    static void AppendTable(StringBuilder sb, ComplexityReportSettings options, MetricScope scope, List<MethodResult> findings)
    {
        var withStatus = scope.IsChanged;
        sb.AppendLine(withStatus ? "| cc | cog | status | method | location |" : "| cc | cog | method | location |");
        sb.AppendLine(withStatus ? "|---:|---:|---|---|---|" : "|---:|---:|---|---|");

        foreach (var finding in findings.Take(options.Top))
        {
            var m = finding.Method;
            var location = $"`{Repo.Relative(options.RepoRoot, m.FilePath)}:{m.StartLine}-{m.EndLine}`";
            var method = $"`{finding.ShortName}`";

            if (!withStatus)
            {
                sb.AppendLine($"| {m.Cyclomatic} | {m.Cognitive} | {method} | {location} |");
                continue;
            }

            var status = finding.Status?.ToString().ToLowerInvariant() ?? "";
            if (finding.Status is ChangeStatus.Worse or ChangeStatus.Improved && finding.Base is { } b)
                status += $" (was {b.Cyclomatic}/{b.Cognitive})";
            if (finding.NeedsAction)
                status = $"**{status}**";

            sb.AppendLine($"| {m.Cyclomatic} | {m.Cognitive} | {status} | {method} | {location} |");
        }

        if (findings.Count > options.Top)
        {
            sb.AppendLine();
            sb.AppendLine($"… and {findings.Count - options.Top} more.");
        }
    }

    static void AppendSummary(StringBuilder sb, MetricScope scope, List<MethodResult> findings)
    {
        if (!scope.IsChanged)
        {
            sb.AppendLine($"{Count(findings.Count)} {(findings.Count == 1 ? "exceeds" : "exceed")} a threshold.");
            return;
        }

        var action = findings.Where(f => f.NeedsAction).ToList();
        var debt = findings.Count - action.Count;

        if (action.Count == 0)
        {
            sb.AppendLine(
                $"No new or worse methods. {debt} touched {(debt == 1 ? "method is" : "methods are")} existing debt; " +
                $"simplifying {(debt == 1 ? "it" : "them")} is welcome but optional.");
            return;
        }

        sb.Append($"**{Count(action.Count)} {(action.Count == 1 ? "needs" : "need")} action** (new or worse)");
        sb.AppendLine(debt == 0 ? "." : $"; {debt} {(debt == 1 ? "is" : "are")} existing debt.");
        sb.AppendLine("For each, simplify it or add to the commit message:");
        sb.AppendLine();
        sb.AppendLine("```");
        foreach (var finding in action)
            sb.AppendLine($"Complexity-Justification: {finding.ShortName}: {Justify}");
        sb.AppendLine("```");
    }

    static string Count(int n) => n == 1 ? "1 method" : $"{n} methods";
}
