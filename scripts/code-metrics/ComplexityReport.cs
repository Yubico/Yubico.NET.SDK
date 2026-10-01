// Shared by complexity.cs and crap.cs (self-check fixtures) through #:include. See TOOLCHAIN.md.

using System.Text;

/// <summary>What a complexity report needs to know about the run that produced it.</summary>
/// <param name="RepoRoot">Absolute repo root, for repo-relative paths.</param>
/// <param name="MaxCyclomatic">Flag when cyclomatic complexity exceeds this.</param>
/// <param name="MaxCognitive">Flag when cognitive complexity exceeds this.</param>
/// <param name="Top">Rows shown in a table.</param>
/// <param name="LinkBase">
/// Optional URL prefix for source links, e.g. "https://github.com/o/r/blob/&lt;sha&gt;/".
/// </param>
sealed record ComplexityReportSettings(string RepoRoot, int MaxCyclomatic, int MaxCognitive, int Top, string? LinkBase = null);

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
/// <remarks>
/// Laid out for a reviewer: the verdict is the heading, methods that need action come first,
/// each score shows where it came from, and the rules of the check sit in a footer.
/// </remarks>
static class MarkdownReport
{
    const string Justify = "<why this complexity is warranted>";

    public static string Render(ComplexityReportSettings settings, MetricScope scope, List<MethodResult> results)
    {
        var findings = ComplexityFindings.Ordered(results);
        var action = findings.Where(f => f.NeedsAction).ToList();
        var debt = findings.Count - action.Count;

        var sb = new StringBuilder();
        sb.AppendLine($"### Complexity: {Headline(scope, results.Count, findings.Count, action.Count, debt)}");
        sb.AppendLine();

        if (findings.Count > 0)
        {
            AppendTable(sb, settings, scope, findings);
            sb.AppendLine();
        }

        if (scope.IsChanged && action.Count > 0)
        {
            sb.AppendLine(action.Count == 1
                ? "Simplify it, or add this line to the commit message with a real reason:"
                : "Simplify each one, or add these lines to the commit message with a real reason:");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var finding in action)
                sb.AppendLine($"Complexity-Justification: {finding.ShortName}: {Justify}");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (scope.IsChanged && debt > 0)
        {
            var debtStatus = findings.First(f => !f.NeedsAction).Status?.ToString().ToLowerInvariant();
            sb.AppendLine(debt == 1
                ? $"The {debtStatus} method is existing debt this change touched: simplifying it is welcome but optional."
                : "The unchanged and improved methods are existing debt this change touched: simplifying them is welcome but optional.");
            sb.AppendLine();
        }

        sb.AppendLine($"<sub>{Footer(settings, scope, results.Count)}</sub>");
        return sb.ToString().TrimEnd();
    }

    static string Headline(MetricScope scope, int checkedCount, int findingCount, int actionCount, int debt)
    {
        if (checkedCount == 0)
            return scope.IsChanged ? "no shipping C# methods changed" : "no shipping C# methods in scope";

        if (findingCount == 0)
            return scope.IsChanged ? "no changed method exceeds a limit" : "no method exceeds a limit";

        if (!scope.IsChanged)
            return $"{Count(findingCount)} {(findingCount == 1 ? "exceeds" : "exceed")} a limit";

        if (actionCount == 0)
            return $"no new or worse methods ({debt} existing debt)";

        return $"{Count(actionCount)} {(actionCount == 1 ? "needs" : "need")} action";
    }

    static void AppendTable(StringBuilder sb, ComplexityReportSettings settings, MetricScope scope, List<MethodResult> findings)
    {
        var withStatus = scope.IsChanged;
        sb.AppendLine(withStatus ? "| Status | Method | Cyclomatic | Cognitive |" : "| Method | Cyclomatic | Cognitive |");
        sb.AppendLine(withStatus ? "|---|---|---:|---:|" : "|---|---:|---:|");

        foreach (var finding in findings.Take(settings.Top))
        {
            var m = finding.Method;
            var cyclomatic = Score(finding.Base?.Cyclomatic, m.Cyclomatic, settings.MaxCyclomatic);
            var cognitive = Score(finding.Base?.Cognitive, m.Cognitive, settings.MaxCognitive);
            var method = MethodCell(settings, finding);

            if (!withStatus)
            {
                sb.AppendLine($"| {method} | {cyclomatic} | {cognitive} |");
                continue;
            }

            var status = finding.Status?.ToString().ToLowerInvariant() ?? "";
            sb.AppendLine($"| {(finding.NeedsAction ? $"**{status}**" : status)} | {method} | {cyclomatic} | {cognitive} |");
        }

        if (findings.Count > settings.Top)
        {
            sb.AppendLine();
            sb.AppendLine($"… and {findings.Count - settings.Top} more.");
        }
    }

    /// <summary>"12 → **14**" when the score moved, "**17**" when it did not; bold means over the limit.</summary>
    static string Score(int? before, int now, int limit)
    {
        var current = now > limit ? $"**{now}**" : now.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return before is { } b && b != now ? $"{b} → {current}" : current;
    }

    static string MethodCell(ComplexityReportSettings settings, MethodResult finding)
    {
        var m = finding.Method;
        var path = Repo.Relative(settings.RepoRoot, m.FilePath);
        var name = $"`{finding.ShortName}`";

        if (settings.LinkBase is { } linkBase)
        {
            var encoded = string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
            name = $"[{name}]({linkBase.TrimEnd('/')}/{encoded}#L{m.StartLine}-L{m.EndLine})";
        }

        return $"{name}<br><sub>{path}:{m.StartLine}-{m.EndLine}</sub>";
    }

    static string Footer(ComplexityReportSettings settings, MetricScope scope, int checkedCount)
    {
        var what = scope.IsChanged ? $"Changed methods vs `{scope.BaseRef}`" : "Methods";
        var where = scope.Modules.Count switch
        {
            0 => scope.IsChanged ? "" : " in the shipping SDK",
            1 => $" in module {scope.Modules[0]}",
            _ => $" in modules {string.Join(", ", scope.Modules)}",
        };

        return $"{what}{where} ({checkedCount} checked) · limits: cyclomatic {settings.MaxCyclomatic}, " +
               $"cognitive {settings.MaxCognitive} · **bold** is over the limit · source only, no coverage";
    }

    static string Count(int n) => n == 1 ? "1 method" : $"{n} methods";
}
