// Shared by crap.cs and complexity.cs through #:include. See TOOLCHAIN.md.


static class SelfCheck
{
    public static int Run(string repoRoot)
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

        foreach (var (name, passed, detail) in ScopeFixtures().Concat(BaseComparisonFixtures()).Concat(ComplexityMarkdownFixtures()))
        {
            if (passed)
            {
                passes++;
            }
            else
            {
                failures++;
                Console.Error.WriteLine($"FAIL scope/{name}: {detail}");
            }
        }

        // Anchor against a real method whose complexity was derived by hand.
        var anchor = Path.Combine(repoRoot, "src", "Piv", "src", "Metadata", "PivMetadataProtocol.cs");
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
    /// Golden fixtures for --changed and --module: reading `git diff --unified=0` hunks and
    /// deciding which method spans a change touches.
    /// </summary>
    static IEnumerable<(string Name, bool Passed, string Detail)> ScopeFixtures()
    {
        const string diff = """
            diff --git a/src/Piv/src/A.cs b/src/Piv/src/A.cs
            index 1111111..2222222 100644
            --- a/src/Piv/src/A.cs
            +++ b/src/Piv/src/A.cs
            @@ -10,2 +10,3 @@ class A
            -old
            -old
            +++count;
            +new
            +new
            @@ -40 +41 @@ class A
            -x
            +y
            @@ -60,3 +60,0 @@ class A
            -gone
            -gone
            -gone
            diff --git a/src/Piv/src/Deleted.cs b/src/Piv/src/Deleted.cs
            deleted file mode 100644
            --- a/src/Piv/src/Deleted.cs
            +++ /dev/null
            @@ -1,3 +0,0 @@
            -a
            -b
            -c
            diff --git a/src/Oath/src/With Space.cs b/src/Oath/src/With Space.cs
            --- a/src/Oath/src/With Space.cs<TAB>
            +++ b/src/Oath/src/With Space.cs<TAB>
            @@ -1 +1 @@
            -a
            +b
            diff --git "a/src/Oath/src/Q\"uote\303\251.cs" "b/src/Oath/src/Q\"uote\303\251.cs"
            --- "a/src/Oath/src/Q\"uote\303\251.cs"
            +++ "b/src/Oath/src/Q\"uote\303\251.cs"
            @@ -3 +3 @@
            -a
            +b
            """;

        // git appends a tab after a path containing spaces; spelled out to keep the source clean.
        var parsed = DiffParser.Parse(diff.Replace("<TAB>", "\t", StringComparison.Ordinal));

        yield return ("parses-files-and-skips-deleted",
            parsed.Count == 3 && parsed.ContainsKey("src/Piv/src/A.cs") && parsed.ContainsKey("src/Oath/src/With Space.cs"),
            $"got [{string.Join(", ", parsed.Keys)}]");

        // git C-quotes names with a quote or backslash; octal escapes are UTF-8 bytes.
        yield return ("quoted-path-is-decoded",
            parsed.ContainsKey("src/Oath/src/Q\"uoteé.cs"),
            $"got [{string.Join(", ", parsed.Keys)}]");

        // "+++count;" is an added source line inside a hunk, not a file header.
        yield return ("added-line-starting-with-plus-is-not-a-header",
            !parsed.Keys.Any(k => k.Contains("count", StringComparison.Ordinal)),
            $"got [{string.Join(", ", parsed.Keys)}]");

        if (!parsed.TryGetValue("src/Piv/src/A.cs", out var a))
            yield break;

        yield return ("hunk-range-touches-overlapping-method", a.Touches(5, 10), "lines 10-12 should touch 5-10");
        yield return ("hunk-range-touches-enclosing-method", a.Touches(11, 11), "lines 10-12 should touch 11-11");
        yield return ("hunk-range-misses-later-method", !a.Touches(13, 30), "lines 10-12 should not touch 13-30");
        yield return ("single-line-hunk-without-count", a.Touches(41, 41) && !a.Touches(42, 50), "only line 41 changed");

        // Lines 61-63 were removed after new line 60.
        yield return ("deletion-inside-method-touches", a.Touches(55, 70), "deletion after 60 is inside 55-70");
        yield return ("deletion-after-method-end-misses", !a.Touches(50, 60), "deletion after 60 is past the end of 50-60");
        yield return ("deletion-before-method-start-misses", !a.Touches(61, 70), "deletion after 60 is before 61-70");

        var whole = FileChanges.WholeFile();
        yield return ("untracked-file-touches-everything", whole.Touches(1, 1) && whole.Touches(500, 900), "whole file");

        var piv = new MetricScope { RepoRoot = "/r", Modules = ["Piv"], Changes = null, BaseRef = "HEAD" };
        yield return ("module-includes-its-src", piv.IncludesRelative("src/Piv/src/PivSession.cs"), "src/Piv/src/");
        yield return ("module-excludes-other-module-with-same-prefix", !piv.IncludesRelative("src/Pivot/src/X.cs"), "src/Pivot/src/");
        yield return ("module-excludes-tests", !piv.IncludesRelative("src/Piv/tests/X.cs"), "src/Piv/tests/");

        var args = new ScopeArgs();
        var argv = new[] { "--base", "origin/yubikit" };
        var index = 0;
        yield return ("base-implies-changed",
            args.TryConsume(argv, ref index) && args.Changed && args.BaseRef == "origin/yubikit" && index == 1,
            $"changed={args.Changed}, base={args.BaseRef}, index={index}");
    }

    /// <summary>
    /// Golden fixtures for matching a changed method to its base version and classifying it.
    /// </summary>
    static IEnumerable<(string Name, bool Passed, string Detail)> BaseComparisonFixtures()
    {
        const string before = """
            namespace F;
            internal sealed class C
            {
                public int M(int a) => a > 0 ? 1 : 2;
                public int Other() => 1;
                public int Renamed(int a) => a;
            }
            """;

        // An overload is inserted before M, M gains a branch, and Renamed changes parameters.
        const string after = """
            namespace F;
            internal sealed class C
            {
                public int M(string s) => s.Length;
                public int M(int a) => a > 0 ? (a > 9 ? 1 : 3) : 2;
                public int Other() => 1;
                public int Renamed(long a) => (int)a;
                public int Brand() => 1;
            }
            """;

        var b = MethodExtractor.Extract("f.cs", before, countConditionalAccess: true);
        var a = MethodExtractor.Extract("f.cs", after, countConditionalAccess: true);

        SourceMethod Get(List<SourceMethod> list, string name, string parameters) =>
            list.Single(m => m.MethodName == name && m.Parameters == parameters);

        var mInt = Get(a, "M", "(int a)");
        var mString = Get(a, "M", "(string s)");
        var other = Get(a, "Other", "()");
        var renamed = Get(a, "Renamed", "(long a)");
        var brand = Get(a, "Brand", "()");

        var mIntBase = BaseComparison.FindBase(mInt, b, a);
        yield return ("overload-inserted-before-still-matches-by-parameters",
            mIntBase is not null && mIntBase.Parameters == "(int a)", $"got {mIntBase?.Parameters ?? "null"}");
        yield return ("new-overload-is-new",
            BaseComparison.Classify(mString, BaseComparison.FindBase(mString, b, a)) == ChangeStatus.New, "M(string) should be new");
        yield return ("added-branch-is-worse",
            BaseComparison.Classify(mInt, mIntBase) == ChangeStatus.Worse, "M(int) gained a branch");
        yield return ("same-scores-are-unchanged",
            BaseComparison.Classify(other, BaseComparison.FindBase(other, b, a)) == ChangeStatus.Unchanged, "Other is identical");
        yield return ("unique-name-with-new-parameters-still-matches",
            BaseComparison.FindBase(renamed, b, a) is not null, "Renamed(long) should match Renamed(int)");
        yield return ("added-method-is-new",
            BaseComparison.Classify(brand, BaseComparison.FindBase(brand, b, a)) == ChangeStatus.New, "Brand is new");
        yield return ("lower-score-is-improved",
            BaseComparison.Classify(mIntBase!, mInt) == ChangeStatus.Improved, "reverse of the worse case");
    }

    /// <summary>
    /// Golden fixtures for the complexity section of the pull request comment.
    /// </summary>
    static IEnumerable<(string Name, bool Passed, string Detail)> ComplexityMarkdownFixtures()
    {
        static SourceMethod Method(string name, int cc, int cog, int start = 10) => new()
        {
            FilePath = "/r/src/Oath/src/OathSession.cs",
            TypeName = "Yubico.YubiKit.Oath.OathSession",
            NestedTypeName = "OathSession",
            MethodName = name,
            Parameters = "()",
            StartLine = start,
            EndLine = start + 20,
            Cyclomatic = cc,
            Cognitive = cog,
            HasImplementation = true,
        };

        static MethodResult Result(SourceMethod m, ChangeStatus? status = null, SourceMethod? baseline = null) => new()
        {
            Method = m,
            Exceeds = m.Cyclomatic > 10 || m.Cognitive > 20,
            Status = status,
            Base = baseline,
        };

        var settings = new ComplexityReportSettings("/r", 10, 20, 25);
        var changed = new MetricScope
        {
            RepoRoot = "/r", Modules = [], Changes = new Dictionary<string, FileChanges>(), BaseRef = "abc1234",
        };
        var full = MetricScope.Everything("/r");

        var empty = MarkdownReport.Render(settings, changed, []);
        yield return ("markdown-empty-changed-scope",
            empty.StartsWith("### Complexity", StringComparison.Ordinal) && empty.EndsWith("No shipping C# methods changed.", StringComparison.Ordinal),
            empty);

        var clean = MarkdownReport.Render(settings, changed, [Result(Method("A", 3, 2)), Result(Method("B", 4, 1))]);
        yield return ("markdown-clean-names-base-and-count",
            clean.Contains("Methods changed vs `abc1234`", StringComparison.Ordinal)
            && clean.EndsWith("No changed method exceeds a threshold (2 methods checked).", StringComparison.Ordinal),
            clean);

        var worse = Method("ValidateAsync", 14, 12);
        var debt = Method("ParseUri", 17, 12, start: 80);
        var mixed = MarkdownReport.Render(settings, changed,
        [
            Result(debt, ChangeStatus.Unchanged, debt),
            Result(worse, ChangeStatus.Worse, Method("ValidateAsync", 12, 11)),
            Result(Method("Small", 2, 1)),
        ]);
        yield return ("markdown-status-table-action-first",
            mixed.Contains("| cc | cog | status | method | location |", StringComparison.Ordinal)
            && mixed.IndexOf("ValidateAsync", StringComparison.Ordinal) < mixed.IndexOf("ParseUri", StringComparison.Ordinal),
            mixed);
        yield return ("markdown-worse-is-bold-with-previous-scores",
            mixed.Contains("| 14 | 12 | **worse (was 12/11)** | `OathSession.ValidateAsync` | `src/Oath/src/OathSession.cs:10-30` |", StringComparison.Ordinal)
            && mixed.Contains("| 17 | 12 | unchanged | `OathSession.ParseUri` |", StringComparison.Ordinal),
            mixed);
        yield return ("markdown-summary-and-justification",
            mixed.Contains("**1 method needs action** (new or worse); 1 is existing debt.", StringComparison.Ordinal)
            && mixed.Contains("```\nComplexity-Justification: OathSession.ValidateAsync: <why this complexity is warranted>\n```", StringComparison.Ordinal),
            mixed);

        var debtOnly = MarkdownReport.Render(settings, changed, [Result(debt, ChangeStatus.Unchanged, debt)]);
        yield return ("markdown-debt-only-is-optional",
            debtOnly.EndsWith("No new or worse methods. 1 touched method is existing debt; simplifying it is welcome but optional.", StringComparison.Ordinal)
            && !debtOnly.Contains("Complexity-Justification", StringComparison.Ordinal),
            debtOnly);

        var scan = MarkdownReport.Render(settings, full, [Result(worse), Result(debt)]);
        yield return ("markdown-full-scan-has-no-status",
            scan.Contains("| cc | cog | method | location |", StringComparison.Ordinal)
            && !scan.Contains("status", StringComparison.Ordinal)
            && scan.Contains("Methods in the shipping SDK", StringComparison.Ordinal)
            && scan.EndsWith("2 methods exceed a threshold.", StringComparison.Ordinal),
            scan);

        var truncated = MarkdownReport.Render(settings with { Top = 1 }, full, [Result(worse), Result(debt)]);
        yield return ("markdown-top-truncates-table",
            truncated.Contains("… and 1 more.", StringComparison.Ordinal)
            && truncated.Split('\n').Count(l => l.StartsWith("| 1", StringComparison.Ordinal)) == 1,
            truncated);
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
