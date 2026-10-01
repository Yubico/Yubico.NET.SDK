// Shared by crap.cs (self-check fixtures) and complexity.cs through #:include. See TOOLCHAIN.md.

enum ChangeStatus
{
    New,
    Worse,
    Unchanged,
    Improved,
}

/// <summary>
/// Compares a method with the same member in the base version of its file.
/// </summary>
static class BaseComparison
{
    public static ChangeStatus Classify(SourceMethod current, SourceMethod? baseline)
    {
        if (baseline is null)
            return ChangeStatus.New;

        if (current.Cyclomatic > baseline.Cyclomatic || current.Cognitive > baseline.Cognitive)
            return ChangeStatus.Worse;

        return current.Cyclomatic == baseline.Cyclomatic && current.Cognitive == baseline.Cognitive
            ? ChangeStatus.Unchanged
            : ChangeStatus.Improved;
    }

    /// <summary>
    /// Finds <paramref name="method"/> among the members of the base version of its file.
    /// </summary>
    /// <remarks>
    /// A member matches on type, name, and parameter list, so moving it within its file or
    /// adding an overload next to it does not confuse the comparison. When the parameter list
    /// changed, a name that is unique in both versions still matches.
    /// </remarks>
    /// <param name="method">A member of the current version of the file.</param>
    /// <param name="before">Every member of the base version of the file.</param>
    /// <param name="after">Every member of the current version of the file.</param>
    public static SourceMethod? FindBase(SourceMethod method, List<SourceMethod> before, List<SourceMethod> after)
    {
        var exact = before.Where(b => b.Display == method.Display && b.Parameters == method.Parameters).ToList();
        if (exact.Count == 1)
            return exact[0];

        // Identical signatures, such as same-named accessors in nested types of the same
        // name: fall back to their order of appearance.
        if (exact.Count > 1)
        {
            var twins = after.Where(a => a.Display == method.Display && a.Parameters == method.Parameters).ToList();
            var position = twins.IndexOf(method);
            return position >= 0 && position < exact.Count ? exact[position] : null;
        }

        // The parameter list changed. Only a name that is unique on both sides is unambiguous.
        var sameNameBefore = before.Where(b => b.Display == method.Display).ToList();
        var sameNameAfter = after.Count(a => a.Display == method.Display);
        return sameNameBefore.Count == 1 && sameNameAfter == 1 ? sameNameBefore[0] : null;
    }
}
