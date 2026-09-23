#!/usr/bin/env dotnet run

#:package Microsoft.CodeAnalysis.CSharp

#:include scripts/code-metrics/Repo.cs
#:include scripts/code-metrics/Scope.cs
#:include scripts/code-metrics/SourceMethods.cs
#:include scripts/code-metrics/CyclomaticComplexity.cs
#:include scripts/code-metrics/CognitiveComplexity.cs
#:include scripts/code-metrics/BaseComparison.cs

/*
 * Yubico.YubiKit Complexity Check
 * ===============================
 *
 * Flags methods whose cyclomatic complexity (how many paths) or cognitive
 * complexity (how hard the control flow is to follow) exceeds a threshold.
 * Everything is measured from source; no tests or coverage are involved, so it
 * runs in seconds. It is the soft pre-commit gate described in CLAUDE.md.
 *
 * USAGE:
 *   dotnet complexity.cs [options]
 *
 * With no scope option it checks the methods you changed: lines changed in the
 * working tree (staged and unstaged) versus HEAD, plus untracked files. Each
 * flagged method is compared with the base version of its file and marked
 * new, worse, unchanged, or improved. New and worse methods need action.
 *
 * OPTIONS:
 *   --changed               Changed-lines scope (the default unless --all or --module)
 *   --base <ref>            Compare with <ref> instead of HEAD. Implies --changed.
 *   --module <Name>         Only src/<Name>/src/. Repeatable. Without --changed this
 *                           scans the whole module.
 *   --all                   Scan the whole shipping SDK.
 *   --max-cyclomatic <n>    Flag when cyclomatic complexity exceeds n. Default: 10
 *   --max-cognitive <n>     Flag when cognitive complexity exceeds n. Default: 20
 *   --top <n>               Rows in the console table. Default: 25
 *   --json <path>           Write every in-scope method as JSON.
 *   --fail-on-findings      Exit 3 when a finding needs action (new or worse in a
 *                           changed scope; any finding in a full scan).
 *
 * EXIT CODES:
 *   0  success, whether or not methods were flagged
 *   1  usage, IO, or git error
 *   3  --fail-on-findings was given and a finding needs action
 *
 * See TOOLCHAIN.md.
 */

using System.Globalization;
using System.Text.Json;

var options = ComplexityOptions.Parse(args);
if (options is null)
    return 1;

return ComplexityCheck.Run(options);

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------

sealed record ComplexityOptions
{
    public required string RepoRoot { get; init; }
    public required ScopeArgs Scope { get; init; }
    public int MaxCyclomatic { get; init; } = 10;
    public int MaxCognitive { get; init; } = 20;
    public int Top { get; init; } = 25;
    public string? JsonPath { get; init; }
    public bool FailOnFindings { get; init; }

    public static ComplexityOptions? Parse(string[] args)
    {
        var repoRoot = Repo.FindRoot();
        if (repoRoot is null)
        {
            Console.Error.WriteLine("error: could not locate repo root (no toolchain.cs found in any parent directory)");
            return null;
        }

        var scope = new ScopeArgs();
        var all = false;
        var maxCyclomatic = 10;
        var maxCognitive = 20;
        var top = 25;
        string? json = null;
        var failOnFindings = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (scope.TryConsume(args, ref i))
                continue;

            switch (args[i])
            {
                case "--all":
                    all = true;
                    break;
                case "--max-cyclomatic" when i + 1 < args.Length:
                    if (!TryParseCount(args[++i], "--max-cyclomatic", out maxCyclomatic))
                        return null;
                    break;
                case "--max-cognitive" when i + 1 < args.Length:
                    if (!TryParseCount(args[++i], "--max-cognitive", out maxCognitive))
                        return null;
                    break;
                case "--top" when i + 1 < args.Length:
                    if (!TryParseCount(args[++i], "--top", out top))
                        return null;
                    break;
                case "--json" when i + 1 < args.Length:
                    json = args[++i];
                    break;
                case "--fail-on-findings":
                    failOnFindings = true;
                    break;
                case "--help" or "-h":
                    PrintUsage();
                    return null;
                default:
                    Console.Error.WriteLine($"error: unrecognized argument '{args[i]}' (try --help)");
                    return null;
            }
        }

        if (all && (scope.Changed || scope.Modules.Count > 0))
        {
            Console.Error.WriteLine("error: --all cannot be combined with --module, --changed, or --base");
            return null;
        }

        // The pre-commit gate is the common case, so an unscoped run checks your changes.
        if (!all && scope.Modules.Count == 0)
            scope.Changed = true;

        return new ComplexityOptions
        {
            RepoRoot = repoRoot,
            Scope = scope,
            MaxCyclomatic = maxCyclomatic,
            MaxCognitive = maxCognitive,
            Top = top,
            JsonPath = json,
            FailOnFindings = failOnFindings,
        };
    }

    static bool TryParseCount(string value, string option, out int result)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= 0)
            return true;

        Console.Error.WriteLine($"error: {option} expects a non-negative integer, got '{value}'");
        return false;
    }

    static void PrintUsage() =>
        Console.WriteLine("""
            dotnet complexity.cs [options]

            With no scope option, checks methods overlapping your uncommitted changes vs HEAD.

              --all                      Scan the whole shipping SDK
              --max-cyclomatic <n>       Flag when cyclomatic complexity exceeds n (default: 10)
              --max-cognitive <n>        Flag when cognitive complexity exceeds n (default: 20)
              --top <n>                  Rows in the console table (default: 25)
              --json <path>              Write every in-scope method as JSON
              --fail-on-findings         Exit 3 when a finding needs action
            """ + "\n" + ScopeArgs.Usage);
}

// ---------------------------------------------------------------------------
// Check
// ---------------------------------------------------------------------------

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

static class ComplexityCheck
{
    public static int Run(ComplexityOptions options)
    {
        var scope = MetricScope.Resolve(options.RepoRoot, options.Scope);
        if (scope is null)
            return 1;

        var files = ShippingSource.EnumerateFiles(options.RepoRoot, ["src"]).Where(scope.IncludesFile).ToList();

        var methods = files
            .SelectMany(f => MethodExtractor.Extract(f, File.ReadAllText(f), countConditionalAccess: true))
            .Where(m => m.HasImplementation && scope.Includes(m))
            .ToList();

        Dictionary<SourceMethod, SourceMethod>? baseMethods = null;
        if (scope.IsChanged)
        {
            baseMethods = LoadBaseVersions(options, scope, methods);
            if (baseMethods is null)
                return 1;
        }

        var results = methods.Select(m =>
        {
            var exceeds = m.Cyclomatic > options.MaxCyclomatic || m.Cognitive > options.MaxCognitive;
            if (!exceeds || baseMethods is null)
                return new MethodResult { Method = m, Exceeds = exceeds };

            var baseline = baseMethods.TryGetValue(m, out var b) ? b : null;
            return new MethodResult { Method = m, Exceeds = true, Status = BaseComparison.Classify(m, baseline), Base = baseline };
        }).ToList();

        Report(options, scope, files.Count, results);

        if (options.JsonPath is not null)
            WriteJson(options, scope, results);

        return options.FailOnFindings && results.Any(r => r.NeedsAction) ? 3 : 0;
    }

    /// <summary>
    /// Maps each flagged method to the same member in the base version of its file.
    /// </summary>
    /// <remarks>
    /// A file absent at the base makes every method in it new. Any other git failure is an
    /// error: guessing "new" would misreport existing code.
    /// </remarks>
    /// <returns>Null after printing an error.</returns>
    static Dictionary<SourceMethod, SourceMethod>? LoadBaseVersions(
        ComplexityOptions options,
        MetricScope scope,
        List<SourceMethod> methods)
    {
        var matches = new Dictionary<SourceMethod, SourceMethod>();

        var flaggedByFile = methods
            .Where(m => m.Cyclomatic > options.MaxCyclomatic || m.Cognitive > options.MaxCognitive)
            .GroupBy(m => m.FilePath, StringComparer.Ordinal);

        foreach (var flagged in flaggedByFile)
        {
            var relative = Repo.Relative(options.RepoRoot, flagged.Key);

            // ls-tree succeeds with empty output for a path the base does not contain, which
            // separates "new file" from a real failure.
            var tree = Repo.Git(options.RepoRoot, "ls-tree", "-z", "--name-only", scope.BaseRef, "--", relative);
            if (tree.ExitCode != 0)
            {
                Console.Error.WriteLine($"error: git ls-tree {scope.BaseRef} -- {relative} failed: {tree.StdErr.Trim()}");
                return null;
            }

            if (tree.StdOut.Length == 0)
                continue;

            var show = Repo.Git(options.RepoRoot, "show", $"{scope.BaseRef}:./{relative}");
            if (show.ExitCode != 0)
            {
                Console.Error.WriteLine($"error: git show {scope.BaseRef}:./{relative} failed: {show.StdErr.Trim()}");
                return null;
            }

            var before = MethodExtractor.Extract(flagged.Key, show.StdOut, countConditionalAccess: true);
            var after = MethodExtractor.Extract(flagged.Key, File.ReadAllText(flagged.Key), countConditionalAccess: true);

            foreach (var method in flagged)
            {
                if (BaseComparison.FindBase(method, before, after) is { } baseline)
                    matches[method] = baseline;
            }
        }

        return matches;
    }

    static void Report(ComplexityOptions options, MetricScope scope, int fileCount, List<MethodResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("Complexity check (source only, no coverage)");
        Console.WriteLine($"  scope        {scope.Describe()}");
        Console.WriteLine($"  files        {fileCount}");
        Console.WriteLine($"  methods      {results.Count}");
        Console.WriteLine($"  thresholds   cyclomatic > {options.MaxCyclomatic}, cognitive > {options.MaxCognitive}");
        Console.WriteLine();

        if (results.Count == 0)
        {
            Console.WriteLine("No shipping C# methods in scope.");
            return;
        }

        // Action first, then the hardest to read.
        var findings = results
            .Where(r => r.Exceeds)
            .OrderByDescending(r => r.NeedsAction)
            .ThenByDescending(r => r.Method.Cognitive)
            .ThenByDescending(r => r.Method.Cyclomatic)
            .ToList();

        if (findings.Count == 0)
        {
            Console.WriteLine(results.Count == 1
                ? "The 1 method in scope is within thresholds."
                : $"All {results.Count} methods are within thresholds.");
            return;
        }

        PrintTable(options, scope, findings);
        Console.WriteLine();
        PrintSummary(scope, results.Count, findings);
    }

    static void PrintTable(ComplexityOptions options, MetricScope scope, List<MethodResult> findings)
    {
        var withStatus = scope.IsChanged;
        var indent = new string(' ', withStatus ? 25 : 14);

        Console.WriteLine(withStatus
            ? $"{"cc",6}{"cog",6}  {"status",-9}  method"
            : $"{"cc",6}{"cog",6}  method");

        foreach (var finding in findings.Take(options.Top))
        {
            var m = finding.Method;
            var location = $"{Repo.Relative(options.RepoRoot, m.FilePath)}:{m.StartLine}-{m.EndLine}";

            if (withStatus)
            {
                var status = finding.Status?.ToString().ToLowerInvariant() ?? "";
                var was = finding.Status is ChangeStatus.Worse or ChangeStatus.Improved && finding.Base is { } b
                    ? $"  (was {b.Cyclomatic}/{b.Cognitive})"
                    : "";
                Console.WriteLine($"{m.Cyclomatic,6}{m.Cognitive,6}  {status,-9}  {m.Display}{was}");
            }
            else
            {
                Console.WriteLine($"{m.Cyclomatic,6}{m.Cognitive,6}  {m.Display}");
            }

            Console.WriteLine($"{indent}{location}");
        }

        if (findings.Count > options.Top)
            Console.WriteLine($"... and {findings.Count - options.Top} more (raise --top or use --json)");
    }

    static void PrintSummary(MetricScope scope, int methodCount, List<MethodResult> findings)
    {
        var exceed = $"{findings.Count} of {methodCount} {(methodCount == 1 ? "method" : "methods")} {(findings.Count == 1 ? "exceeds" : "exceed")} a threshold";

        if (!scope.IsChanged)
        {
            Console.WriteLine($"{exceed}.");
            return;
        }

        var action = findings.Where(f => f.NeedsAction).ToList();
        var debt = findings.Count - action.Count;
        Console.WriteLine(
            $"{exceed}: {action.Count} {(action.Count == 1 ? "needs" : "need")} action (new or worse), " +
            $"{debt} {(debt == 1 ? "is" : "are")} existing debt.");

        if (action.Count == 0)
        {
            Console.WriteLine("Existing debt only: simplifying these methods is welcome but optional.");
            return;
        }

        Console.WriteLine("For each method that needs action, simplify it or add to the commit message:");
        foreach (var finding in action)
            Console.WriteLine($"  Complexity-Justification: {finding.ShortName}: <why this complexity is warranted>");
    }

    // Utf8JsonWriter rather than JsonSerializer: reflection-based serialization trips the
    // trim and AOT analyzers that the repo treats as errors.
    static void WriteJson(ComplexityOptions options, MetricScope scope, List<MethodResult> results)
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
        writer.WriteString("scope", scope.Describe());
        writer.WriteBoolean("changed", scope.IsChanged);
        if (scope.IsChanged)
            writer.WriteString("base", scope.BaseRef);
        else
            writer.WriteNull("base");

        writer.WriteStartArray("modules");
        foreach (var module in scope.Modules)
            writer.WriteStringValue(module);
        writer.WriteEndArray();

        writer.WriteStartObject("thresholds");
        writer.WriteNumber("cyclomatic", options.MaxCyclomatic);
        writer.WriteNumber("cognitive", options.MaxCognitive);
        writer.WriteEndObject();

        writer.WriteNumber("methodCount", results.Count);
        writer.WriteNumber("findingCount", results.Count(r => r.Exceeds));
        writer.WriteNumber("actionCount", results.Count(r => r.NeedsAction));

        writer.WriteStartArray("methods");
        foreach (var result in results
                     .OrderByDescending(r => r.Method.Cognitive)
                     .ThenByDescending(r => r.Method.Cyclomatic))
        {
            var m = result.Method;
            writer.WriteStartObject();
            writer.WriteString("type", m.TypeName);
            writer.WriteString("method", m.MethodName);
            writer.WriteString("file", Repo.Relative(options.RepoRoot, m.FilePath));
            writer.WriteNumber("startLine", m.StartLine);
            writer.WriteNumber("endLine", m.EndLine);
            writer.WriteNumber("cyclomatic", m.Cyclomatic);
            writer.WriteNumber("cognitive", m.Cognitive);
            writer.WriteBoolean("exceeds", result.Exceeds);
            writer.WriteBoolean("needsAction", result.NeedsAction);

            if (result.Status is { } status)
                writer.WriteString("status", status.ToString().ToLowerInvariant());
            else
                writer.WriteNull("status");

            if (result.Base is { } b)
            {
                writer.WriteNumber("baseCyclomatic", b.Cyclomatic);
                writer.WriteNumber("baseCognitive", b.Cognitive);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        Console.WriteLine();
        Console.WriteLine($"JSON written to {path}");
    }
}
