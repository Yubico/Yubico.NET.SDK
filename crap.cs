#!/usr/bin/env dotnet run

#:package Microsoft.CodeAnalysis.CSharp

#:include scripts/code-metrics/Repo.cs
#:include scripts/code-metrics/Scope.cs
#:include scripts/code-metrics/SourceMethods.cs
#:include scripts/code-metrics/CyclomaticComplexity.cs
#:include scripts/code-metrics/CognitiveComplexity.cs
#:include scripts/code-metrics/Coverage.cs
#:include scripts/code-metrics/ModuleReport.cs
#:include scripts/code-metrics/BaseComparison.cs
#:include scripts/code-metrics/ComplexityReport.cs
#:include scripts/code-metrics/SelfCheck.cs

/*
 * Yubico.YubiKit CRAP Metrics Script
 * ===================================
 *
 * Computes the CRAP (Change Risk Anti-Patterns) metric per method:
 *
 *     CRAP(m) = cc(m)^2 * (1 - cov(m))^3 + cc(m)
 *
 * WHY THIS SCRIPT EXISTS
 * ----------------------
 * ReportGenerator already reports a "crap score" from the Cobertura files that
 * `dotnet toolchain.cs coverage` produces, but it reads coverlet's `complexity`
 * attribute, which is NOT cyclomatic complexity. coverlet computes it as
 * `Math.Max(1, branches.Count)` over recorded IL branch outcomes, so a single
 * `if` contributes 2. Measured against source-level cyclomatic complexity on
 * this repo, the ratio ranges from 0.5x to 6x with no stable multiplier, and it
 * tracks Roslyn codegen rather than the source, so an SDK upgrade can move the
 * numbers with no source change.
 *
 * This script therefore computes cyclomatic complexity from the syntax tree and
 * takes only coverage from the Cobertura reports.
 *
 * USAGE:
 *   dotnet crap.cs [options]
 *
 * OPTIONS:
 *   --coverage <dir>    Directory searched recursively for coverage.cobertura.xml.
 *                       Default: artifacts/coverage
 *   --source <dir>      Source root to analyze. Repeatable. Default: src
 *   --min-crap <n>      Only report methods at or above this CRAP score. Default: 8
 *   --min-cognitive <n> Cognitive-complexity threshold used to separate genuinely
 *                       hard code from large-but-flat code. Default: 15 (Sonar's own).
 *   --top <n>           Rows in the console table. Default: 25
 *   --json <path>       Write the full ranked result as JSON.
 *   --no-conditional-access
 *                       Exclude `?.` / `?[]` from cyclomatic complexity.
 *   --baseline <path>   Load a previous `--json` report and diff module aggregates
 *                       against it (module grouping, not per-method).
 *   --markdown          Emit a GitHub-flavoured markdown module report instead of the
 *                       console top-N method table. Combines with --baseline to add
 *                       delta columns; the CI PR-comment case is
 *                       `--baseline old.json --markdown`.
 *   --fail-on-crap-increase
 *                       Exit 1 if total CRAP increased versus --baseline. Off by
 *                       default; requires --baseline to have any effect.
 *   --self-check        Run the built-in golden fixtures and exit.
 *
 * SCOPE (shared with complexity.cs; coverage is still correlated repo-wide):
 *   --module <Name>     Only report src/<Name>/src/. Repeatable.
 *   --changed           Only report methods overlapping lines changed vs HEAD
 *                       (staged, unstaged, and untracked files).
 *   --base <ref>        Compare with <ref> instead of HEAD. Implies --changed.
 *                       --changed cannot be combined with --baseline.
 *
 * EXIT CODES:
 *   0  success
 *   1  usage / IO error, a self-check fixture failed, or (with
 *      --fail-on-crap-increase) total CRAP increased versus --baseline
 *   2  coverage could not be reconciled with the source tree
 *
 * See TOOLCHAIN.md and docs/TESTING.md.
 */

using System.Globalization;
using System.Text.Json;

var options = CrapOptions.Parse(args);
if (options is null)
    return 1;

if (options.SelfCheck)
    return SelfCheck.Run(options.RepoRoot);

return CrapAnalysis.Run(options);

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------

sealed record CrapOptions
{
    public required string RepoRoot { get; init; }
    public required IReadOnlyList<string> SourceRoots { get; init; }
    public required string CoverageGlob { get; init; }
    public double MinCrap { get; init; } = 8;

    /// <summary>Sonar's own default threshold for rule S3776 is 15.</summary>
    public int MinCognitive { get; init; } = 15;
    public int Top { get; init; } = 25;
    public string? JsonPath { get; init; }
    public bool CountConditionalAccess { get; init; } = true;
    public bool SelfCheck { get; init; }

    /// <summary>Path to a previous `--json` report to diff module aggregates against.</summary>
    public string? BaselinePath { get; init; }

    /// <summary>Emit a GitHub-flavoured markdown module report instead of the console table.</summary>
    public bool Markdown { get; init; }

    /// <summary>Exit 1 if total CRAP increased versus --baseline. No effect without --baseline.</summary>
    public bool FailOnCrapIncrease { get; init; }

    /// <summary>--module, --changed, and --base. Filters what is reported, not what is correlated.</summary>
    public required ScopeArgs Scope { get; init; }

    public static CrapOptions? Parse(string[] args)
    {
        var repoRoot = Repo.FindRoot();
        if (repoRoot is null)
        {
            Console.Error.WriteLine("error: could not locate repo root (no toolchain.cs found in any parent directory)");
            return null;
        }

        var sources = new List<string>();
        string? coverageGlob = null;
        double minCrap = 8;
        var minCognitive = 15;
        var top = 25;
        string? json = null;
        var countConditionalAccess = true;
        var selfCheck = false;
        string? baseline = null;
        var markdown = false;
        var failOnCrapIncrease = false;
        var scope = new ScopeArgs();

        for (var i = 0; i < args.Length; i++)
        {
            if (scope.TryConsume(args, ref i))
                continue;

            switch (args[i])
            {
                case "--source" when i + 1 < args.Length:
                    sources.Add(args[++i]);
                    break;
                case "--coverage" when i + 1 < args.Length:
                    coverageGlob = args[++i];
                    break;
                case "--min-crap" when i + 1 < args.Length:
                    if (!double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out minCrap))
                    {
                        Console.Error.WriteLine($"error: --min-crap expects a number, got '{args[i]}'");
                        return null;
                    }

                    break;
                case "--min-cognitive" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out minCognitive))
                    {
                        Console.Error.WriteLine($"error: --min-cognitive expects an integer, got '{args[i]}'");
                        return null;
                    }

                    break;
                case "--top" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out top))
                    {
                        Console.Error.WriteLine($"error: --top expects an integer, got '{args[i]}'");
                        return null;
                    }

                    break;
                case "--json" when i + 1 < args.Length:
                    json = args[++i];
                    break;
                case "--no-conditional-access":
                    countConditionalAccess = false;
                    break;
                case "--baseline" when i + 1 < args.Length:
                    baseline = args[++i];
                    break;
                case "--markdown":
                    markdown = true;
                    break;
                case "--fail-on-crap-increase":
                    failOnCrapIncrease = true;
                    break;
                case "--self-check":
                    selfCheck = true;
                    break;
                case "--help" or "-h":
                    PrintUsage();
                    return null;
                default:
                    Console.Error.WriteLine($"error: unrecognized argument '{args[i]}' (try --help)");
                    return null;
            }
        }

        if (sources.Count == 0)
            sources.Add("src");

        // A module delta over a partial method set compares different populations.
        if (scope.Changed && baseline is not null)
        {
            Console.Error.WriteLine("error: --changed/--base cannot be combined with --baseline");
            return null;
        }

        return new CrapOptions
        {
            RepoRoot = repoRoot,
            SourceRoots = sources,
            CoverageGlob = coverageGlob ?? Path.Combine("artifacts", "coverage"),
            MinCrap = minCrap,
            MinCognitive = minCognitive,
            Top = top,
            JsonPath = json,
            CountConditionalAccess = countConditionalAccess,
            SelfCheck = selfCheck,
            BaselinePath = baseline,
            Markdown = markdown,
            FailOnCrapIncrease = failOnCrapIncrease,
            Scope = scope,
        };
    }

    static void PrintUsage() =>
        Console.WriteLine("""
            dotnet crap.cs [options]

              --coverage <dir>           Directory searched for coverage.cobertura.xml (default: artifacts/coverage)
              --source <dir>             Source root to analyze, repeatable (default: src)
              --min-crap <n>             Minimum CRAP score to report (default: 8)
              --min-cognitive <n>        Cognitive-complexity threshold for the risk column (default: 15)
              --top <n>                  Rows in the console table (default: 25)
              --json <path>              Write full ranked result as JSON
              --no-conditional-access    Exclude ?. and ?[] from cyclomatic complexity
              --baseline <path>          Diff module aggregates against a previous --json report
              --markdown                 Emit a markdown module report instead of the console table
              --fail-on-crap-increase    Exit 1 if total CRAP increased versus --baseline (default: off)
              --self-check               Run built-in golden fixtures and exit
            """ + "\n" + ScopeArgs.Usage);
}

static class CrapAnalysis
{
    public static int Run(CrapOptions options)
    {
        var methods = ShippingSource.LoadMethods(options.RepoRoot, options.SourceRoots, options.CountConditionalAccess);
        if (methods.Count == 0)
        {
            Console.Error.WriteLine("error: no source methods found; check --source");
            return 1;
        }

        var reports = CoverageData.DiscoverReports(options.RepoRoot, options.CoverageGlob);
        if (reports.Count == 0)
        {
            Console.Error.WriteLine(
                $"error: no coverage.cobertura.xml under '{options.CoverageGlob}'. " +
                "Run 'dotnet toolchain.cs coverage' first.");
            return 1;
        }

        var scope = MetricScope.Resolve(options.RepoRoot, options.Scope);
        if (scope is null)
            return 1;

        // Correlate against every method so the stale-coverage check below keeps judging the
        // whole report; the scope only narrows what is reported.
        var hits = CoverageData.LoadCoverage(reports);
        var rows = CoverageData.Correlate(methods, hits, out var unmatchedCoverage, out var uninstrumented);

        if (scope.IsRestricted)
        {
            // Every method is either a row or uninstrumented, so the scoped count follows.
            var scopedMethodCount = methods.Count(scope.Includes);
            rows = [.. rows.Where(r => scope.Includes(r.Method))];
            uninstrumented = scopedMethodCount - rows.Count;
            methods = [.. methods.Where(scope.Includes)];
        }

        var ranked = rows.OrderByDescending(r => r.Crap).ToList();

        Dictionary<string, ModuleStats>? baselineByModule = null;
        if (options.BaselinePath is not null)
        {
            var baselineSamples = BaselineReport.Load(options.RepoRoot, options.BaselinePath);
            if (baselineSamples is null)
                return 1;

            // --changed is rejected with --baseline, so only a module filter can apply here.
            baselineByModule = ModuleAggregator.Aggregate(baselineSamples.Where(s => scope.IncludesRelative(s.File)));
        }

        var crapIncreased = false;

        if (options.Markdown || baselineByModule is not null)
        {
            var headByModule = ModuleAggregator.Aggregate(ToSamples(options, rows));
            var (text, totalCrapDelta) = ModuleReportBuilder.Build(headByModule, baselineByModule, options.Markdown);
            Console.WriteLine(text);
            crapIncreased = totalCrapDelta > 0.5;
        }
        else
        {
            Report(options, scope, ranked, methods.Count, reports.Count, unmatchedCoverage, uninstrumented);
        }

        if (options.JsonPath is not null)
            WriteJson(options, ranked, methods.Count, reports.Count, unmatchedCoverage, uninstrumented);

        // Coverage that cannot be tied back to a source method means the two halves of the
        // formula disagree about the code under analysis. Reporting a number anyway is how
        // other CRAP tools silently produce wrong answers, so this is a hard failure.
        var orphanRatio = hits.Count == 0 ? 1.0 : (double)unmatchedCoverage / hits.Count;
        if (orphanRatio > 0.10)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"error: {unmatchedCoverage} of {hits.Count} covered lines ({orphanRatio:P1}) " +
                "could not be matched to a source method. Coverage is likely stale — re-run " +
                "'dotnet toolchain.cs coverage'. Refusing to report CRAP scores built on it.");
            return 2;
        }

        if (options.FailOnCrapIncrease && crapIncreased)
            return 1;

        return 0;
    }

    /// <summary>Projects freshly computed rows into the shape the module aggregator shares with --baseline.</summary>
    static List<MethodSample> ToSamples(CrapOptions options, IEnumerable<CrapRow> rows) =>
        rows.Select(r => new MethodSample
        {
            File = Path.GetRelativePath(options.RepoRoot, r.Method.FilePath).Replace('\\', '/'),
            Crap = r.Crap,
            Cognitive = r.Method.Cognitive,
            Coverage = r.Coverage,
        }).ToList();

    static void Report(
        CrapOptions options,
        MetricScope scope,
        List<CrapRow> ranked,
        int methodCount,
        int reportCount,
        int unmatchedCoverage,
        int uninstrumented)
    {
        var rows = ranked;
        var flagged = ranked.Where(r => r.Crap >= options.MinCrap).ToList();

        Console.WriteLine();
        Console.WriteLine("CRAP report (source-level cyclomatic complexity)");
        Console.WriteLine(new string('=', 96));
        if (scope.IsRestricted)
            Console.WriteLine($"  scope               {scope.Describe()}");
        Console.WriteLine($"  coverage reports    {reportCount}");
        Console.WriteLine($"  source methods      {methodCount}");
        var neverObserved = rows.Count(r => r.NeverObserved);
        Console.WriteLine($"  with coverage data  {rows.Count - neverObserved}");
        Console.WriteLine($"  never exercised     {neverObserved}  (implemented but absent from every report; scored 0%)");
        Console.WriteLine($"  not measurable      {uninstrumented}  (abstract, interface, extern, or auto-property)");
        Console.WriteLine($"  unmatched lines     {unmatchedCoverage}");
        Console.WriteLine($"  CRAP >= {options.MinCrap,-11:0.##}{flagged.Count}");
        Console.WriteLine($"  of those, cognitive > {options.MinCognitive,-4}{flagged.Count(r => r.Method.Cognitive > options.MinCognitive)}");
        Console.WriteLine($"  conditional access  {(options.CountConditionalAccess ? "counted" : "not counted")}");
        Console.WriteLine();

        if (flagged.Count > 0)
        {
            Console.WriteLine($"{"CRAP",10}  {"cc",4}  {"cog",4}  {"cov",7}  method");
            Console.WriteLine(new string('-', 96));
            foreach (var row in flagged.Take(options.Top))
            {
                var coverage = row.NeverObserved ? "  n/a" : row.Coverage.ToString("P1");
                Console.WriteLine(
                    $"{row.Crap,10:F1}  {row.Method.Cyclomatic,4}  {row.Method.Cognitive,4}  {coverage,7}  {row.Method.Display}");
            }

            if (flagged.Count > options.Top)
                Console.WriteLine($"... and {flagged.Count - options.Top} more (raise --top or use --json)");
        }
    }

    // Written with Utf8JsonWriter rather than JsonSerializer: the repo enables the trim and
    // AOT analyzers as errors, and reflection-based serialization of anonymous types trips
    // IL2026/IL3050.
    static void WriteJson(
        CrapOptions options,
        List<CrapRow> ranked,
        int methodCount,
        int reportCount,
        int unmatchedCoverage,
        int uninstrumented)
    {
        var path = Path.IsPathRooted(options.JsonPath!)
            ? options.JsonPath!
            : Path.Combine(options.RepoRoot, options.JsonPath!);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("generatedOn", DateTimeOffset.UtcNow);
        writer.WriteString("complexityMetric", "source-cyclomatic");
        writer.WriteBoolean("countsConditionalAccess", options.CountConditionalAccess);
        writer.WriteNumber("reportCount", reportCount);
        writer.WriteNumber("methodCount", methodCount);
        writer.WriteNumber("methodsWithCoverage", ranked.Count(r => !r.NeverObserved));
        writer.WriteNumber("neverExercised", ranked.Count(r => r.NeverObserved));
        writer.WriteNumber("uninstrumented", uninstrumented);
        writer.WriteNumber("unmatchedCoverage", unmatchedCoverage);

        writer.WriteStartArray("methods");
        foreach (var row in ranked)
        {
            writer.WriteStartObject();
            writer.WriteString("type", row.Method.TypeName);
            writer.WriteString("method", row.Method.MethodName);
            writer.WriteString("file", Path.GetRelativePath(options.RepoRoot, row.Method.FilePath));
            writer.WriteNumber("startLine", row.Method.StartLine);
            writer.WriteNumber("endLine", row.Method.EndLine);
            writer.WriteNumber("cyclomatic", row.Method.Cyclomatic);
            writer.WriteNumber("cognitive", row.Method.Cognitive);
            writer.WriteNumber("coveredLines", row.CoveredLines);
            writer.WriteNumber("totalLines", row.TotalLines);
            writer.WriteNumber("coverage", Math.Round(row.Coverage, 4));
            writer.WriteBoolean("neverObserved", row.NeverObserved);
            writer.WriteNumber("crap", Math.Round(row.Crap, 2));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        Console.WriteLine();
        Console.WriteLine($"JSON written to {path}");
    }
}
