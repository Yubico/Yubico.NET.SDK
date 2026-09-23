// Shared by crap.cs and complexity.cs through #:include. See TOOLCHAIN.md.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
