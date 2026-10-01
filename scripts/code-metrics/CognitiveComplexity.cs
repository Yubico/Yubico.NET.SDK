// Shared by crap.cs and complexity.cs through #:include. See TOOLCHAIN.md.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
