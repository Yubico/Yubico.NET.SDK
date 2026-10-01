// Shared by crap.cs and complexity.cs through #:include. See TOOLCHAIN.md.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// The subset of a per-method row needed for module aggregation, sourced either from a
/// freshly computed <see cref="CrapRow"/> or from a loaded <c>--json</c> baseline report.
/// </summary>
sealed record MethodSample
{
    /// <summary>Repo-relative path with forward slashes, e.g. "src/Piv/src/PivSession.cs".</summary>
    public required string File { get; init; }
    public required double Crap { get; init; }
    public required int Cognitive { get; init; }
    public required double Coverage { get; init; }
}

sealed record ModuleStats
{
    public required int MethodCount { get; init; }
    public required double TotalCrap { get; init; }
    public required int CountCrapAtLeast8 { get; init; }
    public required int CountCognitiveOver15 { get; init; }

    /// <summary>Mean of per-method coverage across the module, as a percentage (0-100).</summary>
    public required double MeanCoveragePercent { get; init; }

    /// <summary>
    /// Stands in for a module absent from one side of a --baseline comparison. A module that
    /// disappeared (or has not yet appeared) is not skipped: it is compared against zero.
    /// </summary>
    public static readonly ModuleStats Zero = new()
    {
        MethodCount = 0,
        TotalCrap = 0,
        CountCrapAtLeast8 = 0,
        CountCognitiveOver15 = 0,
        MeanCoveragePercent = 0,
    };
}

/// <summary>
/// Groups methods into shipping SDK modules and aggregates CRAP/coverage per module.
/// </summary>
static class ModuleAggregator
{
    static readonly Regex ModulePattern = new("^src/([^/]+)/src/", RegexOptions.Compiled);

    /// <summary>
    /// Applet module display order. Core always leads; anything not listed here (including
    /// Cli.Commands and Cli.Shared) sorts alphabetically after this list.
    /// </summary>
    static readonly string[] AppletOrder =
    [
        "Management", "Piv", "Fido2", "WebAuthn", "Oath", "YubiOtp", "OpenPgp", "SecurityDomain", "YubiHsm",
    ];

    /// <summary>
    /// Extracts the module name from a repo-relative file path, or null if the path is not
    /// shipping production code (tests, examples, and anything outside src/&lt;module&gt;/src/).
    /// </summary>
    public static string? ModuleOf(string file)
    {
        var match = ModulePattern.Match(file.Replace('\\', '/'));
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>Orders module names: Core, then the known applet order, then the rest alphabetically.</summary>
    public static List<string> OrderModules(IEnumerable<string> modules)
    {
        var remaining = new HashSet<string>(modules, StringComparer.Ordinal);
        var ordered = new List<string>();

        if (remaining.Remove("Core"))
            ordered.Add("Core");

        foreach (var known in AppletOrder)
        {
            if (remaining.Remove(known))
                ordered.Add(known);
        }

        ordered.AddRange(remaining.OrderBy(m => m, StringComparer.Ordinal));
        return ordered;
    }

    public static Dictionary<string, ModuleStats> Aggregate(IEnumerable<MethodSample> samples)
    {
        var byModule = new Dictionary<string, List<MethodSample>>(StringComparer.Ordinal);

        foreach (var sample in samples)
        {
            var module = ModuleOf(sample.File);
            if (module is null)
                continue;

            if (!byModule.TryGetValue(module, out var list))
                byModule[module] = list = [];

            list.Add(sample);
        }

        return byModule.ToDictionary(
            kv => kv.Key,
            kv => new ModuleStats
            {
                MethodCount = kv.Value.Count,
                TotalCrap = kv.Value.Sum(s => s.Crap),
                CountCrapAtLeast8 = kv.Value.Count(s => s.Crap >= 8),
                CountCognitiveOver15 = kv.Value.Count(s => s.Cognitive > 15),
                MeanCoveragePercent = kv.Value.Average(s => s.Coverage) * 100,
            },
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Sums module aggregates into a single TOTAL row. Coverage is deliberately not
    /// re-derived here: a single mean-of-means across modules of very different sizes would
    /// misrepresent overall coverage, so the report leaves that cell blank.
    /// </summary>
    public static ModuleStats Total(IReadOnlyDictionary<string, ModuleStats> byModule)
    {
        if (byModule.Count == 0)
            return ModuleStats.Zero;

        return new ModuleStats
        {
            MethodCount = byModule.Values.Sum(s => s.MethodCount),
            TotalCrap = byModule.Values.Sum(s => s.TotalCrap),
            CountCrapAtLeast8 = byModule.Values.Sum(s => s.CountCrapAtLeast8),
            CountCognitiveOver15 = byModule.Values.Sum(s => s.CountCognitiveOver15),
            MeanCoveragePercent = 0,
        };
    }
}

/// <summary>Loads the <c>methods</c> array of a previous <c>--json</c> report.</summary>
static class BaselineReport
{
    public static List<MethodSample>? Load(string repoRoot, string path)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(repoRoot, path);
        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine($"error: --baseline file not found: {fullPath}");
            return null;
        }

        try
        {
            using var stream = File.OpenRead(fullPath);
            using var doc = JsonDocument.Parse(stream);

            if (!doc.RootElement.TryGetProperty("methods", out var methodsElement)
                || methodsElement.ValueKind != JsonValueKind.Array)
            {
                Console.Error.WriteLine($"error: --baseline file has no 'methods' array: {fullPath}");
                return null;
            }

            var samples = new List<MethodSample>();
            foreach (var entry in methodsElement.EnumerateArray())
            {
                var file = entry.TryGetProperty("file", out var fileProp) ? fileProp.GetString() : null;
                if (string.IsNullOrEmpty(file))
                    continue;

                var crap = entry.TryGetProperty("crap", out var crapProp) ? crapProp.GetDouble() : 0;
                var cognitive = entry.TryGetProperty("cognitive", out var cognitiveProp) ? cognitiveProp.GetInt32() : 0;
                var coverage = entry.TryGetProperty("coverage", out var coverageProp) ? coverageProp.GetDouble() : 0;

                samples.Add(new MethodSample
                {
                    File = file.Replace('\\', '/'),
                    Crap = crap,
                    Cognitive = cognitive,
                    Coverage = coverage,
                });
            }

            return samples;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"error: could not parse --baseline JSON '{fullPath}': {ex.Message}");
            return null;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"error: could not read --baseline file '{fullPath}': {ex.Message}");
            return null;
        }
    }
}

/// <summary>Shared numeric formatting for the module report, so deltas read the same in both renderers.</summary>
static class Fmt
{
    public static string WholeNumber(double value) =>
        Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

    /// <summary>Whole number with thousands separators, e.g. "25,375".</summary>
    public static string Grouped(double value) =>
        Math.Round(value, MidpointRounding.AwayFromZero).ToString("#,0", CultureInfo.InvariantCulture);

    /// <summary>"." when the absolute change is under half a CRAP point; otherwise a signed whole number.</summary>
    public static string SignedCrapDelta(double? delta, bool grouped = false)
    {
        if (delta is null)
            return string.Empty;

        if (Math.Abs(delta.Value) < 0.5)
            return ".";

        var rounded = Math.Round(delta.Value, MidpointRounding.AwayFromZero);
        var text = grouped ? Grouped(rounded) : WholeNumber(rounded);
        return rounded > 0 ? $"+{text}" : text;
    }

    /// <summary>"." when the absolute change is under half a percentage point; otherwise signed "pp".</summary>
    public static string SignedCoverageDeltaPp(double? deltaPp)
    {
        if (deltaPp is null)
            return string.Empty;

        if (Math.Abs(deltaPp.Value) < 0.5)
            return ".";

        var sign = deltaPp.Value > 0 ? "+" : "-";
        return $"{sign}{Math.Abs(deltaPp.Value).ToString("F1", CultureInfo.InvariantCulture)}pp";
    }
}

static class CrapVerdict
{
    public static string Render(double totalCrapDelta)
    {
        if (Math.Abs(totalCrapDelta) < 0.5)
            return "**CRAP unchanged.**";

        var magnitude = Fmt.WholeNumber(Math.Abs(totalCrapDelta));
        return totalCrapDelta > 0
            ? $"**CRAP increased by {magnitude}.**"
            : $"**CRAP decreased by {magnitude}.**";
    }
}

sealed record ModuleRow
{
    public required string Name { get; init; }
    public required bool Bold { get; init; }
    public required bool IsTotal { get; init; }
    public required int Methods { get; init; }
    public required double Crap { get; init; }
    public required double? CrapDelta { get; init; }
    public required int CrapAtLeast8 { get; init; }
    public required int CognitiveOver15 { get; init; }

    /// <summary>Null for the TOTAL row, which leaves coverage blank rather than a misleading mean-of-means.</summary>
    public required double? CoveragePercent { get; init; }
    public required double? CoverageDeltaPp { get; init; }
}

/// <summary>
/// Builds the module-aggregate report used by --markdown and/or --baseline. Compares module
/// aggregates, not individual methods, so renamed methods and modules that appear or
/// disappear between the two sides are handled by treating the missing side as zero.
/// </summary>
static class ModuleReportBuilder
{
    public static (string Text, double TotalCrapDelta) Build(
        IReadOnlyDictionary<string, ModuleStats> head,
        IReadOnlyDictionary<string, ModuleStats>? baseline,
        bool markdown)
    {
        var moduleNames = ModuleAggregator.OrderModules(
            head.Keys.Concat(baseline?.Keys ?? Enumerable.Empty<string>()));

        var rows = new List<ModuleRow>();
        foreach (var name in moduleNames)
        {
            var headStats = head.TryGetValue(name, out var h) ? h : ModuleStats.Zero;
            ModuleStats? baseStats = baseline is null
                ? null
                : baseline.TryGetValue(name, out var b) ? b : ModuleStats.Zero;

            rows.Add(BuildRow(name, headStats, baseStats, bold: name == "Core", isTotal: false));
        }

        var headTotal = ModuleAggregator.Total(head);
        var baseTotal = baseline is null ? null : ModuleAggregator.Total(baseline);
        rows.Add(BuildRow("TOTAL", headTotal, baseTotal, bold: true, isTotal: true));

        var totalCrapDelta = baseline is null ? 0 : headTotal.TotalCrap - baseTotal!.TotalCrap;
        var hasBaseline = baseline is not null;

        var text = markdown
            ? RenderMarkdown(rows, hasBaseline, totalCrapDelta)
            : RenderConsole(rows, hasBaseline, totalCrapDelta);

        return (text, totalCrapDelta);
    }

    static ModuleRow BuildRow(string name, ModuleStats head, ModuleStats? baseline, bool bold, bool isTotal)
    {
        double? crapDelta = baseline is null ? null : head.TotalCrap - baseline.TotalCrap;
        double? coveragePercent = isTotal ? null : head.MeanCoveragePercent;
        double? coverageDelta = isTotal || baseline is null ? null : head.MeanCoveragePercent - baseline.MeanCoveragePercent;

        return new ModuleRow
        {
            Name = name,
            Bold = bold,
            IsTotal = isTotal,
            Methods = head.MethodCount,
            Crap = head.TotalCrap,
            CrapDelta = crapDelta,
            CrapAtLeast8 = head.CountCrapAtLeast8,
            CognitiveOver15 = head.CountCognitiveOver15,
            CoveragePercent = coveragePercent,
            CoverageDeltaPp = coverageDelta,
        };
    }

    /// <summary>
    /// The pull request comment section. CRAP is background here, not a verdict: it rises
    /// with any new code, so the summary is neutral and only modules that moved are listed.
    /// </summary>
    static string RenderMarkdown(List<ModuleRow> rows, bool hasBaseline, double totalCrapDelta)
    {
        var modules = rows.Where(r => !r.IsTotal).ToList();
        var total = rows.Single(r => r.IsTotal);

        var sb = new StringBuilder();
        sb.AppendLine("<!-- yubikit-crap-report -->");
        sb.AppendLine("### Coverage and CRAP (background)");
        sb.AppendLine();

        var shown = hasBaseline ? modules.Where(Moved).ToList() : modules;
        sb.AppendLine(Summary(total, modules.Count, shown.Count, hasBaseline, totalCrapDelta));
        sb.AppendLine();

        if (shown.Count > 0)
        {
            sb.AppendLine(hasBaseline
                ? "| Module | Methods | CRAP | Δ CRAP | CRAP ≥ 8 | Coverage | Δ coverage |"
                : "| Module | Methods | CRAP | CRAP ≥ 8 | Coverage |");
            sb.AppendLine(hasBaseline
                ? "|---|---:|---:|---:|---:|---:|---:|"
                : "|---|---:|---:|---:|---:|");

            foreach (var row in shown.Append(total))
            {
                var name = row.IsTotal ? "**Total**" : row.Name;
                var coverage = row.CoveragePercent is { } c ? $"{c.ToString("F1", CultureInfo.InvariantCulture)}%" : "";

                sb.AppendLine(hasBaseline
                    ? $"| {name} | {Fmt.Grouped(row.Methods)} | {Fmt.Grouped(row.Crap)} | {Dash(Fmt.SignedCrapDelta(row.CrapDelta, grouped: true))} | {Fmt.Grouped(row.CrapAtLeast8)} | {coverage} | {Dash(Fmt.SignedCoverageDeltaPp(row.CoverageDeltaPp))} |"
                    : $"| {name} | {Fmt.Grouped(row.Methods)} | {Fmt.Grouped(row.Crap)} | {Fmt.Grouped(row.CrapAtLeast8)} | {coverage} |");
            }

            sb.AppendLine();
        }

        sb.AppendLine(
            "<sub>Per method, CRAP = cyclomatic² × (1 − coverage)³ + cyclomatic, summed per module. " +
            "It rises with any new code, so read it as background on test coverage, not as a target. " +
            "Coverage is the mean across a module's methods; the total leaves it blank.</sub>");

        return sb.ToString().TrimEnd();
    }

    static bool Moved(ModuleRow row) =>
        row.CrapDelta is { } crap && Math.Abs(crap) >= 0.5
        || row.CoverageDeltaPp is { } coverage && Math.Abs(coverage) >= 0.5;

    static string Summary(ModuleRow total, int moduleCount, int movedCount, bool hasBaseline, double totalCrapDelta)
    {
        var crap = Fmt.Grouped(total.Crap);
        if (!hasBaseline)
            return $"Total CRAP {crap} across {moduleCount} {(moduleCount == 1 ? "module" : "modules")}.";

        if (movedCount == 0)
            return $"Total CRAP {crap}, unchanged from the base. No module moved.";

        var delta = Fmt.SignedCrapDelta(totalCrapDelta, grouped: true);
        var change = delta == "." ? "unchanged from the base" : $"{delta} from the base";
        return $"Total CRAP {crap}, {change}. {movedCount} of {moduleCount} modules moved:";
    }

    /// <summary>"." reads as a typo in a table; an en dash reads as "no change".</summary>
    static string Dash(string delta) => delta == "." ? "–" : delta;

    static string RenderConsole(List<ModuleRow> rows, bool hasBaseline, double totalCrapDelta)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("Coverage and CRAP by module");
        sb.AppendLine(new string('=', 96));
        sb.AppendLine(hasBaseline
            ? $"{"module",-16}{"methods",8}{"CRAP",8}{"dCRAP",9}{">=8",6}{"cog>15",8}{"coverage",10}{"dcov",9}"
            : $"{"module",-16}{"methods",8}{"CRAP",8}{">=8",6}{"cog>15",8}{"coverage",10}");
        sb.AppendLine(new string('-', 96));

        foreach (var row in rows)
        {
            var crap = Fmt.WholeNumber(row.Crap);
            var coverage = row.CoveragePercent is null
                ? ""
                : $"{row.CoveragePercent.Value.ToString("F1", CultureInfo.InvariantCulture)}%";

            if (hasBaseline)
            {
                var crapDelta = Fmt.SignedCrapDelta(row.CrapDelta);
                var coverageDelta = Fmt.SignedCoverageDeltaPp(row.CoverageDeltaPp);
                sb.AppendLine(
                    $"{row.Name,-16}{row.Methods,8}{crap,8}{crapDelta,9}{row.CrapAtLeast8,6}{row.CognitiveOver15,8}{coverage,10}{coverageDelta,9}");
            }
            else
            {
                sb.AppendLine($"{row.Name,-16}{row.Methods,8}{crap,8}{row.CrapAtLeast8,6}{row.CognitiveOver15,8}{coverage,10}");
            }
        }

        if (hasBaseline)
        {
            sb.AppendLine();
            sb.AppendLine(CrapVerdict.Render(totalCrapDelta).Replace("**", ""));
        }

        return sb.ToString().TrimEnd();
    }
}
