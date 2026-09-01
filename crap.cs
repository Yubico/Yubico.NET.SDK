#!/usr/bin/env dotnet run

#:package Microsoft.CodeAnalysis.CSharp

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
 * EXIT CODES:
 *   0  success
 *   1  usage / IO error, a self-check fixture failed, or (with
 *      --fail-on-crap-increase) total CRAP increased versus --baseline
 *   2  coverage could not be reconciled with the source tree
 *
 * See TOOLCHAIN.md and docs/TESTING.md.
 */

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var options = CrapOptions.Parse(args);
if (options is null)
    return 1;

if (options.SelfCheck)
    return SelfCheck.Run(options);

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

    public static CrapOptions? Parse(string[] args)
    {
        var repoRoot = FindRepoRoot();
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

        for (var i = 0; i < args.Length; i++)
        {
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
            """);

    static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "toolchain.cs")))
            dir = dir.Parent;

        return dir?.FullName;
    }
}

// ---------------------------------------------------------------------------
// Source model
// ---------------------------------------------------------------------------

sealed record SourceMethod
{
    public required string FilePath { get; init; }
    public required string TypeName { get; init; }
    public required string MethodName { get; init; }
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
    public required int Cyclomatic { get; init; }

    /// <summary>SonarSource cognitive complexity: how hard the control flow is to follow.</summary>
    public required int Cognitive { get; init; }

    /// <summary>False for abstract, interface, extern, and auto-property members, which emit no code.</summary>
    public required bool HasImplementation { get; init; }

    public string Display => $"{TypeName}.{MethodName}";
}

/// <summary>
/// Extracts every method-like declaration with its source span and cyclomatic complexity.
/// </summary>
/// <remarks>
/// Lambdas and local functions are deliberately folded into their enclosing member so that
/// complexity and coverage describe the same unit: coverlet attributes a lambda's lines to
/// the file region of its parent, so splitting them would desynchronize the two halves of
/// the CRAP formula.
/// </remarks>
static class MethodExtractor
{
    public static List<SourceMethod> Extract(string filePath, string text, bool countConditionalAccess)
    {
        var tree = CSharpSyntaxTree.ParseText(text, path: filePath);
        var root = tree.GetRoot();
        var results = new List<SourceMethod>();

        foreach (var node in root.DescendantNodes())
        {
            if (!IsMemberDeclaration(node))
                continue;

            // A local function is part of its parent member's complexity, not its own entry.
            if (node.Ancestors().Any(IsMemberDeclaration))
                continue;

            var span = tree.GetLineSpan(node.Span);
            results.Add(new SourceMethod
            {
                FilePath = filePath,
                TypeName = TypeNameOf(node),
                MethodName = MemberNameOf(node),
                StartLine = span.StartLinePosition.Line + 1,
                EndLine = span.EndLinePosition.Line + 1,
                Cyclomatic = CyclomaticComplexity.Compute(node, countConditionalAccess),
                Cognitive = CognitiveComplexity.Compute(node),
                HasImplementation = HasImplementation(node),
            });
        }

        return results;
    }

    /// <summary>
    /// True for any node that owns a body worth measuring, including local functions.
    /// </summary>
    /// <remarks>
    /// This is the single definition of "member" for the whole script.
    /// <see cref="CyclomaticComplexity"/> derives its own narrower notion from it rather
    /// than repeating the list, so the two cannot drift apart and silently mis-count.
    /// </remarks>
    public static bool IsMemberDeclaration(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax or
        ConstructorDeclarationSyntax or
        DestructorDeclarationSyntax or
        OperatorDeclarationSyntax or
        ConversionOperatorDeclarationSyntax or
        AccessorDeclarationSyntax or
        LocalFunctionStatementSyntax => true,

        // An expression-bodied property or indexer has no AccessorDeclarationSyntax, so it
        // would otherwise be skipped entirely. One with an accessor list is not itself a
        // member here — its accessors are picked up individually.
        PropertyDeclarationSyntax p => p.ExpressionBody is not null,
        IndexerDeclarationSyntax i => i.ExpressionBody is not null,

        _ => false,
    };

    /// <summary>
    /// True when the member has a body the compiler can instrument.
    /// </summary>
    /// <remarks>
    /// Abstract, interface, extern, and partial declarations, and auto-property accessors,
    /// emit no code, so coverage tools never report them. Distinguishing these from members
    /// that are simply absent from the coverage data is what lets the report treat the
    /// latter as genuinely untested rather than silently dropping them.
    /// </remarks>
    public static bool HasImplementation(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax m => m.Body is not null || m.ExpressionBody is not null,
        AccessorDeclarationSyntax a => a.Body is not null || a.ExpressionBody is not null,
        LocalFunctionStatementSyntax l => l.Body is not null || l.ExpressionBody is not null,
        PropertyDeclarationSyntax p => p.ExpressionBody is not null,
        IndexerDeclarationSyntax i => i.ExpressionBody is not null,
        _ => false,
    };

    static string MemberNameOf(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax m => m.Identifier.ValueText,
        ConstructorDeclarationSyntax c => c.Identifier.ValueText,
        DestructorDeclarationSyntax d => "~" + d.Identifier.ValueText,
        OperatorDeclarationSyntax o => "operator " + o.OperatorToken.ValueText,
        ConversionOperatorDeclarationSyntax => "operator",
        LocalFunctionStatementSyntax l => l.Identifier.ValueText,
        AccessorDeclarationSyntax a => AccessorName(a),
        PropertyDeclarationSyntax p => p.Identifier.ValueText,
        IndexerDeclarationSyntax => "this[]",
        _ => "?",
    };

    static string AccessorName(AccessorDeclarationSyntax accessor)
    {
        var owner = accessor.Ancestors()
            .OfType<BasePropertyDeclarationSyntax>()
            .FirstOrDefault();

        var ownerName = owner switch
        {
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            EventDeclarationSyntax e => e.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            _ => "?",
        };

        return $"{accessor.Keyword.ValueText}_{ownerName}";
    }

    static string TypeNameOf(SyntaxNode node)
    {
        var type = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        if (type is null)
            return "<global>";

        var names = new Stack<string>();
        for (var current = type; current is not null; current = current.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault())
            names.Push(current.Identifier.ValueText);

        var ns = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var prefix = ns is null ? string.Empty : ns.Name.ToString() + ".";

        return prefix + string.Join(".", names);
    }
}

/// <summary>
/// Source-level McCabe cyclomatic complexity: one plus the number of decision points.
/// </summary>
/// <remarks>
/// Counted: if, the four loop forms, each non-default switch case / switch-expression arm,
/// each catch clause, the short-circuiting and null-coalescing operators (&amp;&amp;, ||, ??, ??=),
/// conditional access (?. and ?[], unless disabled), the ternary conditional, `when` guards,
/// and each `and`/`or` pattern combinator.
///
/// `else` is not counted: it adds no independent path beyond the `if` that introduced it.
/// `default:` and the `_` switch arm are not counted for the same reason.
/// </remarks>
static class CyclomaticComplexity
{
    public static int Compute(SyntaxNode member, bool countConditionalAccess)
    {
        var complexity = 1;

        foreach (var node in member.DescendantNodes())
        {
            // Nested members carry their own complexity except local functions and lambdas,
            // which are intentionally folded into the enclosing member.
            if (IsSeparatelyCountedMember(node))
                continue;

            complexity += Increment(node, countConditionalAccess);
        }

        return complexity;
    }

    // Derived from the single member definition rather than restating it: every member kind
    // gets its own entry except local functions, which fold into their enclosing member.
    static bool IsSeparatelyCountedMember(SyntaxNode node) =>
        MethodExtractor.IsMemberDeclaration(node) && node is not LocalFunctionStatementSyntax;

    static int Increment(SyntaxNode node, bool countConditionalAccess) => node switch
    {
        IfStatementSyntax => 1,
        WhileStatementSyntax => 1,
        DoStatementSyntax => 1,
        ForStatementSyntax => 1,
        ForEachStatementSyntax or ForEachVariableStatementSyntax => 1,
        CaseSwitchLabelSyntax => 1,
        CasePatternSwitchLabelSyntax => 1,
        CatchClauseSyntax => 1,
        ConditionalExpressionSyntax => 1,

        // `case x when guard:` — a switch-label guard.
        WhenClauseSyntax => 1,

        // `catch (E e) when (filter)` — a distinct node type from the switch-label guard.
        CatchFilterClauseSyntax => 1,

        // `default:` adds no independent path.
        DefaultSwitchLabelSyntax => 0,

        // The discard arm `_ =>` is the switch-expression equivalent of `default:`.
        SwitchExpressionArmSyntax arm => arm.Pattern is DiscardPatternSyntax ? 0 : 1,

        BinaryExpressionSyntax b when b.IsKind(SyntaxKind.LogicalAndExpression)
                                   || b.IsKind(SyntaxKind.LogicalOrExpression)
                                   || b.IsKind(SyntaxKind.CoalesceExpression) => 1,

        AssignmentExpressionSyntax a when a.IsKind(SyntaxKind.CoalesceAssignmentExpression) => 1,

        ConditionalAccessExpressionSyntax => countConditionalAccess ? 1 : 0,

        // `x is A or B` introduces one extra path per combinator.
        BinaryPatternSyntax => 1,

        _ => 0,
    };
}

/// <summary>
/// Cognitive complexity: how hard the control flow is to follow, per the SonarSource
/// specification (rule S3776, white paper Appendix B).
/// </summary>
/// <remarks>
/// This exists because cyclomatic complexity answers "how many paths", which is not the
/// same question as "how risky is this to change". A flat 63-arm switch that maps status
/// words to strings has very high cyclomatic complexity and almost no cognitive
/// complexity; a short method nested four levels deep is the reverse. Gating on both
/// separates large-but-obvious code from genuinely difficult code without anyone
/// maintaining a hand-written ignore list.
///
/// Three rules from the specification drive the difference:
///   1. A `switch` increments once, no matter how many arms it has.
///   2. Nesting compounds: a structure inside N nesting structures costs 1 + N.
///   3. Readable shorthand is ignored, so `??`, `??=`, and `?.` cost nothing.
///
/// Not implemented: the recursion increment, which needs a semantic model to resolve call
/// targets. Scores for directly recursive methods are therefore low by one.
/// </remarks>
static class CognitiveComplexity
{
    public static int Compute(SyntaxNode member)
    {
        var score = 0;

        foreach (var child in BodyOf(member))
            Walk(child, nesting: 0, ref score);

        return score;
    }

    static IEnumerable<SyntaxNode> BodyOf(SyntaxNode member) => member switch
    {
        BaseMethodDeclarationSyntax m => Bodies(m.Body, m.ExpressionBody),
        AccessorDeclarationSyntax a => Bodies(a.Body, a.ExpressionBody),
        LocalFunctionStatementSyntax l => Bodies(l.Body, l.ExpressionBody),
        PropertyDeclarationSyntax p => Bodies(null, p.ExpressionBody),
        IndexerDeclarationSyntax i => Bodies(null, i.ExpressionBody),
        _ => [],
    };

    static IEnumerable<SyntaxNode> Bodies(SyntaxNode? body, ArrowExpressionClauseSyntax? arrow)
    {
        if (body is not null)
            yield return body;

        if (arrow?.Expression is not null)
            yield return arrow.Expression;
    }

    static void Walk(SyntaxNode node, int nesting, ref int score)
    {
        switch (node)
        {
            // Structural increments: cost one, plus one per enclosing nesting level, and
            // raise the nesting level for whatever they contain.
            case IfStatementSyntax ifStatement:
                score += 1 + nesting;
                Walk(ifStatement.Condition, nesting, ref score);
                Walk(ifStatement.Statement, nesting + 1, ref score);
                WalkElse(ifStatement.Else, nesting, ref score);
                return;

            case SwitchStatementSyntax switchStatement:
                // One increment for the whole switch regardless of arm count.
                score += 1 + nesting;
                Walk(switchStatement.Expression, nesting, ref score);
                foreach (var section in switchStatement.Sections)
                    WalkChildren(section, nesting + 1, ref score);
                return;

            case SwitchExpressionSyntax switchExpression:
                score += 1 + nesting;
                Walk(switchExpression.GoverningExpression, nesting, ref score);
                foreach (var arm in switchExpression.Arms)
                    WalkChildren(arm, nesting + 1, ref score);
                return;

            case WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax
                or ForEachStatementSyntax or ForEachVariableStatementSyntax:
                score += 1 + nesting;
                WalkChildren(node, nesting + 1, ref score);
                return;

            case CatchClauseSyntax:
                // try and finally are free; only the handler is a flow break.
                score += 1 + nesting;
                WalkChildren(node, nesting + 1, ref score);
                return;

            case ConditionalExpressionSyntax conditional:
                score += 1 + nesting;
                Walk(conditional.Condition, nesting, ref score);
                Walk(conditional.WhenTrue, nesting + 1, ref score);
                Walk(conditional.WhenFalse, nesting + 1, ref score);
                return;

            // Fundamental increment, no nesting cost: a labelled jump.
            case GotoStatementSyntax:
                score += 1;
                WalkChildren(node, nesting, ref score);
                return;

            // Lambdas and local functions raise the nesting level but cost nothing
            // themselves, because extracting code into a named unit aids readability.
            case AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax:
                WalkChildren(node, nesting + 1, ref score);
                return;

            // A run of the same logical operator reads as one condition, so a sequence
            // costs one regardless of length. Only the root of the tree is scored.
            case BinaryExpressionSyntax binary when IsLogical(binary) && !IsLogical(node.Parent):
                score += CountOperatorRuns(binary);
                WalkChildren(node, nesting, ref score);
                return;

            default:
                WalkChildren(node, nesting, ref score);
                return;
        }
    }

    static void WalkElse(ElseClauseSyntax? elseClause, int nesting, ref int score)
    {
        if (elseClause is null)
            return;

        // `else` and `else if` are hybrid increments: they cost one but take no nesting
        // increment, because the reader already paid that cost at the opening `if`.
        score += 1;

        if (elseClause.Statement is IfStatementSyntax elseIf)
        {
            Walk(elseIf.Condition, nesting, ref score);
            Walk(elseIf.Statement, nesting + 1, ref score);
            WalkElse(elseIf.Else, nesting, ref score);
            return;
        }

        Walk(elseClause.Statement, nesting + 1, ref score);
    }

    static void WalkChildren(SyntaxNode node, int nesting, ref int score)
    {
        foreach (var child in node.ChildNodes())
            Walk(child, nesting, ref score);
    }

    static bool IsLogical(SyntaxNode? node) =>
        node is BinaryExpressionSyntax b
        && (b.IsKind(SyntaxKind.LogicalAndExpression) || b.IsKind(SyntaxKind.LogicalOrExpression));

    /// <summary>
    /// Counts maximal runs of the same logical operator, left to right.
    /// </summary>
    /// <remarks>
    /// <c>a &amp;&amp; b &amp;&amp; c</c> is one run and costs 1;
    /// <c>a &amp;&amp; b || c &amp;&amp; d</c> is three runs and costs 3, because mixing
    /// operators is what makes a condition hard to read.
    /// </remarks>
    static int CountOperatorRuns(BinaryExpressionSyntax root)
    {
        var kinds = new List<SyntaxKind>();
        Flatten(root, kinds);

        var runs = 0;
        for (var i = 0; i < kinds.Count; i++)
        {
            if (i == 0 || kinds[i] != kinds[i - 1])
                runs++;
        }

        return runs;

        static void Flatten(SyntaxNode node, List<SyntaxKind> kinds)
        {
            if (node is not BinaryExpressionSyntax b || !IsLogical(b))
                return;

            Flatten(b.Left, kinds);
            kinds.Add(b.Kind());
            Flatten(b.Right, kinds);
        }
    }
}

// ---------------------------------------------------------------------------
// Self-check: golden fixtures for the complexity rules
// ---------------------------------------------------------------------------

static class SelfCheck
{
    public static int Run(CrapOptions options)
    {
        var failures = 0;
        var passes = 0;

        foreach (var (name, source, expected) in Fixtures())
        {
            var methods = MethodExtractor.Extract("fixture.cs", Wrap(source), countConditionalAccess: true);
            var actual = methods.Count == 1 ? methods[0].Cyclomatic : -methods.Count;

            if (actual == expected)
            {
                passes++;
            }
            else
            {
                failures++;
                Console.Error.WriteLine($"FAIL {name}: expected cc={expected}, got {actual}");
            }
        }

        foreach (var (name, source, expected) in CognitiveFixtures())
        {
            var methods = MethodExtractor.Extract("fixture.cs", Wrap(source), countConditionalAccess: true);
            var actual = methods.Count == 1 ? methods[0].Cognitive : -methods.Count;

            if (actual == expected)
            {
                passes++;
            }
            else
            {
                failures++;
                Console.Error.WriteLine($"FAIL cognitive/{name}: expected {expected}, got {actual}");
            }
        }

        foreach (var (name, source, expected) in ImplementationFixtures())
        {
            var methods = MethodExtractor.Extract("fixture.cs", source, countConditionalAccess: true);
            var actual = methods.Count > 0 && methods.All(m => m.HasImplementation == expected);

            if (actual && methods.Count > 0)
            {
                passes++;
            }
            else
            {
                failures++;
                Console.Error.WriteLine(
                    $"FAIL {name}: expected HasImplementation={expected} on all {methods.Count} extracted member(s)");
            }
        }

        foreach (var (name, passed, detail) in ModuleReportFixtures())
        {
            if (passed)
            {
                passes++;
            }
            else
            {
                failures++;
                Console.Error.WriteLine($"FAIL module-report/{name}: {detail}");
            }
        }

        // Anchor against a real method whose complexity was derived by hand.
        var anchor = Path.Combine(options.RepoRoot, "src", "Piv", "src", "Metadata", "PivMetadataProtocol.cs");
        if (File.Exists(anchor))
        {
            var target = MethodExtractor
                .Extract(anchor, File.ReadAllText(anchor), countConditionalAccess: true)
                .FirstOrDefault(m => m.MethodName == "GetSlotMetadataAsync");

            const int expectedAnchor = 15;
            if (target is null)
            {
                failures++;
                Console.Error.WriteLine("FAIL anchor: GetSlotMetadataAsync not found in PivMetadataProtocol.cs");
            }
            else if (target.Cyclomatic != expectedAnchor)
            {
                failures++;
                Console.Error.WriteLine(
                    $"FAIL anchor GetSlotMetadataAsync: expected cc={expectedAnchor}, got {target.Cyclomatic}. " +
                    "If the method changed, re-derive the expected value by hand before editing this number.");
            }
            else
            {
                passes++;
            }
        }

        Console.WriteLine($"self-check: {passes} passed, {failures} failed");
        return failures == 0 ? 0 : 1;
    }

    static string Wrap(string body) => $$"""
        namespace Fixture;
        internal sealed class C
        {
        {{body}}
        }
        """;

    /// <summary>
    /// Golden fixtures for the --baseline/--markdown module report: grouping a file path into
    /// a module, ordering modules, and diffing module aggregates when a module exists on only
    /// one side of the comparison (renamed/moved methods, or a module added/removed outright).
    /// </summary>
    static IEnumerable<(string Name, bool Passed, string Detail)> ModuleReportFixtures()
    {
        // Module grouping from a file path.
        var pivModule = ModuleAggregator.ModuleOf("src/Piv/src/Metadata/PivMetadataProtocol.cs");
        yield return ("module-of-src-path", pivModule == "Piv", $"expected 'Piv', got '{pivModule ?? "null"}'");

        var testsModule = ModuleAggregator.ModuleOf("src/Piv/tests/PivMetadataProtocolTests.cs");
        yield return ("module-of-tests-path-is-excluded", testsModule is null, $"expected null, got '{testsModule}'");

        var order = ModuleAggregator.OrderModules(["OpenPgp", "Cli.Shared", "Core", "Piv", "Cli.Commands", "Fido2"]);
        var expectedOrder = new[] { "Core", "Piv", "Fido2", "OpenPgp", "Cli.Commands", "Cli.Shared" };
        yield return ("module-order-core-then-applets-then-alphabetical",
            order.SequenceEqual(expectedOrder),
            $"expected [{string.Join(",", expectedOrder)}], got [{string.Join(",", order)}]");

        // A module present only in the baseline must still appear, with the head side at zero.
        var headWithoutOath = new Dictionary<string, ModuleStats>();
        var baselineWithOath = new Dictionary<string, ModuleStats>
        {
            ["Oath"] = new() { MethodCount = 10, TotalCrap = 50, CountCrapAtLeast8 = 2, CountCognitiveOver15 = 1, MeanCoveragePercent = 60 },
        };
        var (removedModuleText, removedModuleDelta) = ModuleReportBuilder.Build(headWithoutOath, baselineWithOath, markdown: false);
        yield return ("module-only-in-baseline-compares-against-zero",
            removedModuleDelta < 0 && removedModuleText.Contains("Oath", StringComparison.Ordinal),
            $"expected negative total CRAP delta and 'Oath' listed, got delta={removedModuleDelta}, text=\n{removedModuleText}");

        // A module present only in the head must still appear, with the baseline side at zero.
        var headWithYubiHsm = new Dictionary<string, ModuleStats>
        {
            ["YubiHsm"] = new() { MethodCount = 5, TotalCrap = 40, CountCrapAtLeast8 = 1, CountCognitiveOver15 = 0, MeanCoveragePercent = 80 },
        };
        var baselineWithoutYubiHsm = new Dictionary<string, ModuleStats>();
        var (newModuleText, newModuleDelta) = ModuleReportBuilder.Build(headWithYubiHsm, baselineWithoutYubiHsm, markdown: false);
        yield return ("module-only-in-head-compares-against-zero",
            newModuleDelta > 0 && newModuleText.Contains("YubiHsm", StringComparison.Ordinal),
            $"expected positive total CRAP delta and 'YubiHsm' listed, got delta={newModuleDelta}, text=\n{newModuleText}");

        // An unchanged module renders "." in both delta columns rather than "+0"/"-0".
        var unchangedCore = new Dictionary<string, ModuleStats>
        {
            ["Core"] = new() { MethodCount = 100, TotalCrap = 500, CountCrapAtLeast8 = 20, CountCognitiveOver15 = 5, MeanCoveragePercent = 70 },
        };
        var (unchangedText, unchangedDelta) = ModuleReportBuilder.Build(unchangedCore, unchangedCore, markdown: true);
        const string expectedUnchangedRow = "| **Core** | 100 | 500 | . | 20 | 5 | 70.0% | . |";
        yield return ("unchanged-module-renders-dot-not-zero",
            Math.Abs(unchangedDelta) < 0.5 && unchangedText.Contains(expectedUnchangedRow, StringComparison.Ordinal),
            $"expected row '{expectedUnchangedRow}' and unchanged verdict, got delta={unchangedDelta}, text=\n{unchangedText}");

        // Verdict selection is driven by total CRAP delta alone.
        yield return ("verdict-decreased", CrapVerdict.Render(-42) == "**CRAP decreased by 42.**", CrapVerdict.Render(-42));
        yield return ("verdict-increased", CrapVerdict.Render(42) == "**CRAP increased by 42.**", CrapVerdict.Render(42));
        yield return ("verdict-unchanged", CrapVerdict.Render(0.2) == "**CRAP unchanged.**", CrapVerdict.Render(0.2));
    }

    /// <summary>
    /// Members that emit no code must be distinguishable from members that were simply
    /// never exercised, otherwise untested code is silently dropped from the report.
    /// </summary>
    static IEnumerable<(string Name, string Source, bool Expected)> ImplementationFixtures()
    {
        yield return ("abstract-method-has-no-implementation",
            "namespace F; internal abstract class A { public abstract void M(); }", false);
        yield return ("interface-method-has-no-implementation",
            "namespace F; internal interface I { void M(); }", false);
        yield return ("extern-method-has-no-implementation",
            "namespace F; internal static class E { public static extern void M(); }", false);
        yield return ("auto-property-accessors-have-no-implementation",
            "namespace F; internal sealed class C { public int P { get; set; } }", false);
        yield return ("concrete-method-has-implementation",
            "namespace F; internal sealed class C { public void M() { } }", true);
        yield return ("expression-bodied-property-has-implementation",
            "namespace F; internal sealed class C { public int P => 1; }", true);
    }

    /// <summary>
    /// Cognitive complexity fixtures, several taken directly from the SonarSource white
    /// paper so the implementation can be checked against the published specification.
    /// </summary>
    static IEnumerable<(string Name, string Source, int Expected)> CognitiveFixtures()
    {
        yield return ("straight-line-is-zero", "void M() { var x = 1; }", 0);

        // White paper: a switch costs one increment regardless of arm count. This is the
        // rule that stops flat lookup tables from dominating the report.
        yield return ("switch-counts-once-not-per-case",
            "string M(int n) { switch (n) { case 1: return \"one\"; case 2: return \"two\"; " +
            "case 3: return \"three\"; default: return \"lots\"; } }", 1);
        yield return ("switch-expression-counts-once",
            "string M(int n) => n switch { 1 => \"a\", 2 => \"b\", 3 => \"c\", _ => \"d\" };", 1);

        // Nesting compounds; a flat sequence does not.
        yield return ("three-sequential-ifs", "void M(int a) { if (a>0){} if (a>1){} if (a>2){} }", 3);
        yield return ("nested-if-costs-one-plus-depth",
            "void M(int a, int b) { if (a > 0) { if (b > 0) { } } }", 3);
        yield return ("triple-nested",
            "void M(int a, int b, int c) { if (a>0) { if (b>0) { if (c>0) { } } } }", 6);

        // else and else if are hybrid: +1 each, no nesting increment.
        yield return ("if-else", "void M(int a) { if (a > 0) { } else { } }", 2);
        yield return ("if-elseif-else", "void M(int a) { if (a>0) { } else if (a<0) { } else { } }", 3);

        // White paper: a run of one operator costs 1; mixing operators costs per run.
        yield return ("uniform-operator-sequence-costs-one", "bool M(bool a, bool b, bool c, bool d) => a && b && c && d;", 1);
        yield return ("mixed-operator-sequence-costs-per-run", "bool M(bool a, bool b, bool c, bool d) => a && b || c && d;", 3);

        // Appendix A: readable shorthand is ignored.
        yield return ("null-coalescing-ignored", "string M(string? a) => a ?? \"x\";", 0);
        yield return ("conditional-access-ignored", "int? M(string? s) => s?.Length;", 0);

        yield return ("loop-with-nested-if",
            "void M(int[] xs) { foreach (var x in xs) { if (x > 0) { } } }", 3);
        yield return ("catch-costs-one-try-is-free", "void M() { try { } catch { } }", 1);
        yield return ("lambda-adds-nesting-but-no-increment",
            "void M(System.Collections.Generic.List<int> xs) { xs.RemoveAll(x => { if (x > 0) { return true; } return false; }); }", 2);
    }

    static IEnumerable<(string Name, string Source, int Expected)> Fixtures()
    {
        yield return ("straight-line", "void M() { var x = 1; }", 1);
        yield return ("single-if", "void M(int a) { if (a > 0) { } }", 2);
        yield return ("if-else-counts-once", "void M(int a) { if (a > 0) { } else { } }", 2);
        yield return ("else-if-chain", "void M(int a) { if (a > 0) { } else if (a < 0) { } else { } }", 3);
        yield return ("three-sequential-ifs", "void M(int a) { if (a>0){} if (a>1){} if (a>2){} }", 4);
        yield return ("while", "void M(bool b) { while (b) { } }", 2);
        yield return ("do-while", "void M(bool b) { do { } while (b); }", 2);
        yield return ("for", "void M() { for (var i = 0; i < 3; i++) { } }", 2);
        yield return ("foreach", "void M(int[] xs) { foreach (var x in xs) { } }", 2);
        yield return ("logical-and", "bool M(bool a, bool b) => a && b;", 2);
        yield return ("logical-or", "bool M(bool a, bool b) => a || b;", 2);
        yield return ("two-ands", "bool M(bool a, bool b, bool c) => a && b && c;", 3);
        yield return ("coalesce", "string M(string? a) => a ?? \"x\";", 2);
        yield return ("coalesce-assign", "void M(ref string? a) { a ??= \"x\"; }", 2);
        yield return ("ternary", "int M(bool b) => b ? 1 : 2;", 2);
        yield return ("conditional-access", "int? M(string? s) => s?.Length;", 2);
        yield return ("catch-single", "void M() { try { } catch { } }", 2);
        yield return ("catch-multiple", "void M() { try { } catch (System.IO.IOException) { } catch { } }", 3);
        yield return ("catch-when-filter", "void M() { try { } catch (System.Exception e) when (e.Message.Length > 0) { } }", 3);
        yield return ("switch-statement-three-cases",
            "void M(int a) { switch (a) { case 1: break; case 2: break; default: break; } }", 3);
        yield return ("switch-expression-three-arms",
            "string M(int a) => a switch { 1 => \"a\", 2 => \"b\", _ => \"c\" };", 3);
        // `is` alone is not a branch; only the `or` combinator adds a path.
        yield return ("pattern-or-combinator", "bool M(int a) => a is 1 or 2;", 2);
        yield return ("pattern-two-combinators", "bool M(int a) => a is 1 or 2 or 3;", 3);
        yield return ("local-function-folds-into-parent",
            "void M(int a) { Inner(); void Inner() { if (a > 0) { } } }", 2);

        // Introducing a lambda is not itself a branch; only the decisions inside it count.
        yield return ("lambda-folds-into-parent",
            "void M(System.Collections.Generic.List<int> xs) { xs.RemoveAll(x => x > 0 && x < 9); }", 2);

        // Expression-bodied members have no AccessorDeclarationSyntax and were once skipped.
        yield return ("expression-bodied-property", "int P => 1;", 1);
        yield return ("expression-bodied-property-ternary",
            "int P => System.Environment.TickCount > 0 ? 1 : 2;", 2);
        yield return ("expression-bodied-indexer", "int this[int i] => i > 0 ? 1 : 2;", 2);
        yield return ("expression-bodied-method-coalesce", "string M(string? s) => s ?? \"x\";", 2);
    }
}

// ---------------------------------------------------------------------------
// Coverage ingest and CRAP analysis
// ---------------------------------------------------------------------------

sealed record CrapRow
{
    public required SourceMethod Method { get; init; }
    public required int CoveredLines { get; init; }
    public required int TotalLines { get; init; }

    /// <summary>True when no coverage report mentioned this member at all.</summary>
    public bool NeverObserved => TotalLines == 0;

    public double Coverage => TotalLines == 0 ? 0 : (double)CoveredLines / TotalLines;

    public double Crap
    {
        get
        {
            var cc = (double)Method.Cyclomatic;
            var uncovered = 1 - Coverage;
            return (cc * cc * uncovered * uncovered * uncovered) + cc;
        }
    }
}

// ---------------------------------------------------------------------------
// Module-level report: --baseline and --markdown
// ---------------------------------------------------------------------------

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
    public static List<MethodSample>? Load(CrapOptions options, string path)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(options.RepoRoot, path);
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

    /// <summary>"." when the absolute change is under half a CRAP point; otherwise a signed whole number.</summary>
    public static string SignedCrapDelta(double? delta)
    {
        if (delta is null)
            return string.Empty;

        if (Math.Abs(delta.Value) < 0.5)
            return ".";

        var rounded = Math.Round(delta.Value, MidpointRounding.AwayFromZero);
        return rounded > 0 ? $"+{WholeNumber(rounded)}" : WholeNumber(rounded);
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

    static string RenderMarkdown(List<ModuleRow> rows, bool hasBaseline, double totalCrapDelta)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!-- yubikit-crap-report -->");
        sb.AppendLine("### Coverage and CRAP");
        sb.AppendLine();
        sb.AppendLine(hasBaseline
            ? "| module | methods | CRAP | Δ CRAP | ≥8 | cog>15 | coverage | Δ cov |"
            : "| module | methods | CRAP | ≥8 | cog>15 | coverage |");
        sb.AppendLine(hasBaseline
            ? "|---|---:|---:|---:|---:|---:|---:|---:|"
            : "|---|---:|---:|---:|---:|---:|");

        foreach (var row in rows)
        {
            var name = row.Bold ? $"**{row.Name}**" : row.Name;
            var crap = Fmt.WholeNumber(row.Crap);
            var coverage = row.CoveragePercent is null
                ? ""
                : $"{row.CoveragePercent.Value.ToString("F1", CultureInfo.InvariantCulture)}%";

            if (hasBaseline)
            {
                var crapDelta = Fmt.SignedCrapDelta(row.CrapDelta);
                if (row.IsTotal && crapDelta != ".")
                    crapDelta = $"**{crapDelta}**";

                var coverageDelta = Fmt.SignedCoverageDeltaPp(row.CoverageDeltaPp);

                sb.AppendLine(
                    $"| {name} | {row.Methods} | {crap} | {crapDelta} | {row.CrapAtLeast8} | {row.CognitiveOver15} | {coverage} | {coverageDelta} |");
            }
            else
            {
                sb.AppendLine($"| {name} | {row.Methods} | {crap} | {row.CrapAtLeast8} | {row.CognitiveOver15} | {coverage} |");
            }
        }

        if (hasBaseline)
        {
            sb.AppendLine();
            sb.AppendLine(CrapVerdict.Render(totalCrapDelta));

            var increased = rows.Where(r => !r.IsTotal && r.CrapDelta is > 0.5).ToList();
            if (increased.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("> [!NOTE]");
                var names = string.Join(", ", increased.Select(r => $"{r.Name} (+{Fmt.WholeNumber(r.CrapDelta!.Value)})"));
                sb.AppendLine($"> CRAP increased in: {names}.");
            }
        }

        return sb.ToString().TrimEnd();
    }

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

static class CrapAnalysis
{
    public static int Run(CrapOptions options)
    {
        var methods = LoadSourceMethods(options);
        if (methods.Count == 0)
        {
            Console.Error.WriteLine("error: no source methods found; check --source");
            return 1;
        }

        var reports = DiscoverReports(options);
        if (reports.Count == 0)
        {
            Console.Error.WriteLine(
                $"error: no coverage.cobertura.xml under '{options.CoverageGlob}'. " +
                "Run 'dotnet toolchain.cs coverage' first.");
            return 1;
        }

        var hits = LoadCoverage(reports);
        var rows = Correlate(methods, hits, out var unmatchedCoverage, out var uninstrumented);
        var ranked = rows.OrderByDescending(r => r.Crap).ToList();

        Dictionary<string, ModuleStats>? baselineByModule = null;
        if (options.BaselinePath is not null)
        {
            var baselineSamples = BaselineReport.Load(options, options.BaselinePath);
            if (baselineSamples is null)
                return 1;

            baselineByModule = ModuleAggregator.Aggregate(baselineSamples);
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
            Report(options, ranked, methods.Count, reports.Count, unmatchedCoverage, uninstrumented);
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

    static List<SourceMethod> LoadSourceMethods(CrapOptions options)
    {
        var methods = new List<SourceMethod>();

        foreach (var root in options.SourceRoots)
        {
            var full = Path.IsPathRooted(root) ? root : Path.Combine(options.RepoRoot, root);
            if (!Directory.Exists(full))
            {
                Console.Error.WriteLine($"warning: source root not found: {full}");
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(full, "*.cs", SearchOption.AllDirectories))
            {
                if (IsExcluded(file))
                    continue;

                methods.AddRange(MethodExtractor.Extract(
                    Path.GetFullPath(file),
                    File.ReadAllText(file),
                    options.CountConditionalAccess));
            }
        }

        return methods;
    }

    // Scope is the shipping SDK. Each module is laid out as src/<Module>/{src,tests,examples},
    // and only the inner src/ ships: tests are not the subject of the metric, and examples are
    // sample apps outside the solution. Shared test infrastructure sits directly under src/ as
    // src/Tests.Shared and src/Tests.TestProject, which have no "tests" path segment, so those
    // are matched by name. Build output and generated files are never source.
    static bool IsExcluded(string path)
    {
        var normalized = path.Replace('\\', '/');

        if (normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var segment in normalized.Split('/'))
        {
            if (segment is "obj" or "bin" or "tests" or "examples")
                return true;

            if (segment.StartsWith("Tests.", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static List<string> DiscoverReports(CrapOptions options)
    {
        var dir = Path.IsPathRooted(options.CoverageGlob)
            ? options.CoverageGlob
            : Path.Combine(options.RepoRoot, options.CoverageGlob);

        return Directory.Exists(dir)
            ? [.. Directory.EnumerateFiles(dir, "coverage.cobertura.xml", SearchOption.AllDirectories).Order()]
            : [];
    }

    /// <summary>
    /// Reads every Cobertura report into a merged (absolute path, line) -> hits map.
    /// </summary>
    /// <remarks>
    /// Each report's <c>&lt;sources&gt;</c> root must be applied to that report's own
    /// <c>filename</c> values. coverlet derives the root from the common prefix of the
    /// assemblies it instrumented, so a project that only touches Core emits
    /// <c>Protocols/.../SWConstants.cs</c> while every other project emits
    /// <c>Core/src/Protocols/.../SWConstants.cs</c> for the same file. Keying on the raw
    /// relative path therefore double-counts shared code.
    ///
    /// Hits are accumulated rather than overwritten so a line executed by any one test
    /// project counts as covered overall.
    /// </remarks>
    static Dictionary<(string Path, int Line), int> LoadCoverage(List<string> reports)
    {
        var hits = new Dictionary<(string, int), int>();

        foreach (var report in reports)
        {
            var doc = XDocument.Load(report);
            var roots = doc.Descendants("source")
                .Select(s => s.Value.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            foreach (var cls in doc.Descendants("class"))
            {
                var filename = cls.Attribute("filename")?.Value;
                if (string.IsNullOrEmpty(filename))
                    continue;

                var resolved = ResolveAgainstRoots(roots, filename);
                if (resolved is null)
                    continue;

                foreach (var line in cls.Descendants("line"))
                {
                    if (!int.TryParse(line.Attribute("number")?.Value, out var number))
                        continue;
                    if (!int.TryParse(line.Attribute("hits")?.Value, out var count))
                        continue;

                    var key = (resolved, number);
                    hits[key] = hits.TryGetValue(key, out var existing) ? existing + count : count;
                }
            }
        }

        return hits;
    }

    static string? ResolveAgainstRoots(List<string> roots, string filename)
    {
        foreach (var root in roots)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, filename));
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Assigns each covered line to the innermost source method whose span contains it.
    /// </summary>
    /// <remarks>
    /// Matching on spans rather than names is what makes async work. coverlet records an
    /// async body under the compiler-generated <c>&lt;Name&gt;d__N::MoveNext</c>, but it
    /// keeps the original file and line numbers, so the lines fall inside the source
    /// method's span. Name-based matching misses this and scores every async method 0%.
    /// </remarks>
    static List<CrapRow> Correlate(
        List<SourceMethod> methods,
        Dictionary<(string Path, int Line), int> hits,
        out int unmatchedCoverage,
        out int uninstrumented)
    {
        var byFile = methods
            .GroupBy(m => m.FilePath)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.EndLine - m.StartLine).ToList());

        var covered = new Dictionary<SourceMethod, (int Covered, int Total)>();
        unmatchedCoverage = 0;

        foreach (var ((path, line), count) in hits)
        {
            if (!byFile.TryGetValue(path, out var candidates))
            {
                unmatchedCoverage++;
                continue;
            }

            // Candidates are ordered narrowest-first, so the first containing span is innermost.
            var owner = candidates.FirstOrDefault(m => line >= m.StartLine && line <= m.EndLine);
            if (owner is null)
            {
                unmatchedCoverage++;
                continue;
            }

            var entry = covered.TryGetValue(owner, out var e) ? e : (0, 0);
            covered[owner] = (entry.Item1 + (count > 0 ? 1 : 0), entry.Item2 + 1);
        }

        var rows = covered.Select(kv => new CrapRow
        {
            Method = kv.Key,
            CoveredLines = kv.Value.Covered,
            TotalLines = kv.Value.Total,
        }).ToList();

        // A member with a body that never appeared in any coverage report was not exercised
        // at all — most often because its assembly has no unit test project. That is the
        // most dangerous code in the repo, so it must be reported as 0% covered rather than
        // dropped. Only members that emit no code are genuinely unmeasurable.
        uninstrumented = 0;
        foreach (var method in methods)
        {
            if (covered.ContainsKey(method))
                continue;

            if (!method.HasImplementation)
            {
                uninstrumented++;
                continue;
            }

            rows.Add(new CrapRow { Method = method, CoveredLines = 0, TotalLines = 0 });
        }

        return rows;
    }

    static void Report(
        CrapOptions options,
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
