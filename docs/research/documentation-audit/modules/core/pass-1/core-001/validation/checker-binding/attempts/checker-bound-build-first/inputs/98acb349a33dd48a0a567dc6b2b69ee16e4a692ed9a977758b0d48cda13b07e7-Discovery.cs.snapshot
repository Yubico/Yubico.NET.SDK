// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Text.RegularExpressions;

/// <summary>A test project path and which runner it uses (xUnit v3 on MTP, or xUnit v2 on VSTest).</summary>
sealed record TestProject(string Path, bool UsesTestingPlatformRunner);

static class ProjectDiscovery
{
    public static string[] DiscoverProjects(string repoRoot, string subdirectory, string nameFilter, string? additionalFilter = null) =>
        Directory.GetFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}{subdirectory}{Path.DirectorySeparatorChar}") &&
                        p.Contains(nameFilter) &&
                        (additionalFilter is null || p.Contains(additionalFilter)) &&
                        !p.Contains($"{Path.DirectorySeparatorChar}.claude{Path.DirectorySeparatorChar}"))
            .Select(p => Path.GetRelativePath(repoRoot, p))
            .OrderBy(p => p)
            .ToArray();

    public static bool IsPackableProject(string repoRoot, string projectPath)
    {
        var projectXml = File.ReadAllText(Path.Combine(repoRoot, projectPath));
        return !Regex.IsMatch(
            projectXml,
            @"<IsPackable(?:\s+[^>]*)?>\s*false\s*</IsPackable>",
            RegexOptions.IgnoreCase);
    }

    public static TestProject[] ToTestProjects(string repoRoot, IEnumerable<string> projectPaths) =>
        projectPaths
            .Select(p => new TestProject(p, UsesMicrosoftTestingPlatformRunner(repoRoot, p)))
            .ToArray();

    static bool UsesMicrosoftTestingPlatformRunner(string repoRoot, string projectPath) =>
        File.ReadAllText(Path.Combine(repoRoot, projectPath))
            .Contains(Conventions.MtpRunnerOptIn, StringComparison.OrdinalIgnoreCase);

    /// <summary>"src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Yubico.YubiKit.Piv.UnitTests.csproj" -> "Piv.UnitTests".</summary>
    public static string ShortName(string projectPath)
    {
        var name = Path.GetFileNameWithoutExtension(projectPath);
        return name.StartsWith(Conventions.ProjectPrefix, StringComparison.Ordinal) && name.Length > Conventions.ProjectPrefix.Length
            ? name[Conventions.ProjectPrefix.Length..]
            : name;
    }

    /// <summary>Repo-relative path with forward slashes, e.g. "src/Piv/src/Yubico.YubiKit.Piv.csproj".</summary>
    public static string RepoRelative(string repoRoot, string path) =>
        Path.GetRelativePath(repoRoot, path).Replace(Path.DirectorySeparatorChar, '/');

    public static string[] DiscoverActiveDocumentationFiles(string repoRoot)
    {
        var rootDocs = Directory.GetFiles(repoRoot, "*.md", SearchOption.TopDirectoryOnly);
        var docsRoot = Directory.Exists(Path.Combine(repoRoot, "docs"))
            ? Directory.GetFiles(Path.Combine(repoRoot, "docs"), "*.md", SearchOption.TopDirectoryOnly)
            : [];
        var docsUsage = GetMarkdownFilesUnder(repoRoot, "docs", "usage");
        var docsTroubleshooting = GetMarkdownFilesUnder(repoRoot, "docs", "troubleshooting");
        var docsArchitecture = GetMarkdownFilesUnder(repoRoot, "docs", "architecture");
        var srcReadmes = Directory.GetFiles(Path.Combine(repoRoot, "src"), "README.md", SearchOption.AllDirectories);
        var srcClaudeDocs = Directory.GetFiles(Path.Combine(repoRoot, "src"), "CLAUDE.md", SearchOption.AllDirectories);

        return [..rootDocs
            .Concat(docsRoot)
            .Concat(docsUsage)
            .Concat(docsTroubleshooting)
            .Concat(docsArchitecture)
            .Concat(srcReadmes)
            .Concat(srcClaudeDocs)
            .Select(p => Path.GetRelativePath(repoRoot, p))
            .Where(p => !IsArchivedOrPlanningDoc(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];
    }

    static string[] GetMarkdownFilesUnder(string repoRoot, params string[] pathParts)
    {
        var path = Path.Combine([repoRoot, .. pathParts]);
        return Directory.Exists(path)
            ? Directory.GetFiles(path, "*.md", SearchOption.AllDirectories)
            : [];
    }

    static bool IsArchivedOrPlanningDoc(string relativePath)
    {
        string[] excludedPrefixes =
            ["docs/archive/", "docs/completed/", "docs/plans/", "docs/research/", "docs/reviews/", "docs/specs/", "docs/templates/"];

        var normalized = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        return excludedPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The projects whose file name contains <paramref name="filter"/> (all of them when there is no filter).
    /// A filter that matches nothing fails the target and lists the valid names: silently doing nothing
    /// would read as success.
    /// </summary>
    public static string[] MatchProjects(string[] projectPaths, string? filter, string kind)
    {
        if (string.IsNullOrEmpty(filter)) return projectPaths;

        var matched = projectPaths
            .Where(p => Path.GetFileNameWithoutExtension(p).Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matched.Length > 0) return matched;

        throw new ToolchainException(
            $"No {kind} project matches --project '{filter}'. Available: {string.Join(", ", projectPaths.Select(ShortName))}");
    }

    /// <inheritdoc cref="MatchProjects(string[], string?, string)"/>
    public static TestProject[] MatchTestProjects(TestProject[] projects, string? filter)
    {
        var matched = MatchProjects([.. projects.Select(p => p.Path)], filter, "test");

        var matchedSet = new HashSet<string>(matched, StringComparer.Ordinal);
        return [.. projects.Where(p => matchedSet.Contains(p.Path))];
    }
}
