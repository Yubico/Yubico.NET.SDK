// Shared by crap.cs and complexity.cs through #:include. See TOOLCHAIN.md.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

sealed record SourceMethod
{
    public required string FilePath { get; init; }
    public required string TypeName { get; init; }

    /// <summary>The type name without its namespace, e.g. "MacOSOtpHidConnection.Owner".</summary>
    public required string NestedTypeName { get; init; }
    public required string MethodName { get; init; }

    /// <summary>Parameter list with whitespace collapsed, e.g. "(int a, string b)"; empty for accessors.</summary>
    public required string Parameters { get; init; }
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
            var (typeName, nestedTypeName) = TypeNamesOf(node);
            results.Add(new SourceMethod
            {
                FilePath = filePath,
                TypeName = typeName,
                NestedTypeName = nestedTypeName,
                MethodName = MemberNameOf(node),
                Parameters = ParametersOf(node),
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

    static string ParametersOf(SyntaxNode node)
    {
        var list = node switch
        {
            BaseMethodDeclarationSyntax m => m.ParameterList.ToString(),
            LocalFunctionStatementSyntax l => l.ParameterList.ToString(),
            IndexerDeclarationSyntax i => i.ParameterList.ToString(),
            _ => string.Empty,
        };

        return string.Join(' ', list.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

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

    static (string Full, string Nested) TypeNamesOf(SyntaxNode node)
    {
        var type = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        if (type is null)
            return ("<global>", "<global>");

        var names = new Stack<string>();
        for (var current = type; current is not null; current = current.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault())
            names.Push(current.Identifier.ValueText);

        var ns = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var prefix = ns is null ? string.Empty : ns.Name.ToString() + ".";

        var nested = string.Join(".", names);
        return (prefix + nested, nested);
    }
}

/// <summary>
/// Finds the shipping SDK source files that every metric measures.
/// </summary>
static class ShippingSource
{
    public static List<SourceMethod> LoadMethods(
        string repoRoot,
        IReadOnlyList<string> sourceRoots,
        bool countConditionalAccess,
        Func<string, bool>? includeFile = null)
    {
        var methods = new List<SourceMethod>();

        foreach (var file in EnumerateFiles(repoRoot, sourceRoots))
        {
            if (includeFile is not null && !includeFile(file))
                continue;

            methods.AddRange(MethodExtractor.Extract(file, File.ReadAllText(file), countConditionalAccess));
        }

        return methods;
    }

    /// <summary>Absolute paths of every shipping source file under the given roots.</summary>
    public static IEnumerable<string> EnumerateFiles(string repoRoot, IReadOnlyList<string> sourceRoots)
    {
        foreach (var root in sourceRoots)
        {
            var full = Path.IsPathRooted(root) ? root : Path.Combine(repoRoot, root);
            if (!Directory.Exists(full))
            {
                Console.Error.WriteLine($"warning: source root not found: {full}");
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(full, "*.cs", SearchOption.AllDirectories))
            {
                var absolute = Path.GetFullPath(file);
                if (!IsExcluded(Repo.Relative(repoRoot, absolute)))
                    yield return absolute;
            }
        }
    }

    // Scope is the shipping SDK. Each module is laid out as src/<Module>/{src,tests,examples},
    // and only the inner src/ ships: tests are not the subject of the metric, and examples are
    // sample apps outside the solution. Shared test infrastructure sits directly under src/ as
    // src/Tests.Shared and src/Tests.TestProject, which have no "tests" path segment, so those
    // are matched by name. Build output and generated files are never source.
    //
    // The path is repo-relative so that folders above the repo (for example a checkout under
    // ~/tests/) cannot exclude everything.
    public static bool IsExcluded(string repoRelativePath)
    {
        var normalized = repoRelativePath.Replace('\\', '/');

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
}
