// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Text.RegularExpressions;
using static Output;

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

    public static bool UsesMicrosoftTestingPlatformRunner(string repoRoot, string projectPath)
    {
        var fullPath = Path.Combine(repoRoot, projectPath);
        return File.ReadAllText(fullPath).Contains(
            "<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>",
            StringComparison.OrdinalIgnoreCase);
    }

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
        var normalized = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        return normalized.StartsWith("docs/archive/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("docs/completed/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("docs/plans/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("docs/research/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("docs/reviews/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("docs/specs/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("docs/templates/", StringComparison.OrdinalIgnoreCase);
    }

    // Shared helper: filter a flat array of project paths by name, warn+list on miss.
    // Returns null when nothing matched and the caller should return early.
    public static string[]? FilterProjectPaths(string[] projectPaths, string? filter, string noMatchMessage)
    {
        if (string.IsNullOrEmpty(filter)) return projectPaths;

        var matched = projectPaths
            .Where(p => Path.GetFileNameWithoutExtension(p).Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matched.Length > 0) return matched;

        PrintColored($"⚠ {noMatchMessage} '{filter}'", ConsoleColor.Yellow);
        Console.WriteLine("Available projects:");
        PrintProjectList(projectPaths);
        return null;
    }

    // Returns null when no projects matched and an error was already printed (caller should return early).
    public static List<(string ProjectPath, bool UsesTestingPlatformRunner)>? FilterToProject(
        (string ProjectPath, bool UsesTestingPlatformRunner)[] projectInfos,
        string? filter)
    {
        var paths = projectInfos.Select(p => p.ProjectPath).ToArray();
        var matched = FilterProjectPaths(paths, filter, "No test projects match");
        if (matched is null) return null;

        var matchedSet = new HashSet<string>(matched, StringComparer.Ordinal);
        return [.. projectInfos.Where(p => matchedSet.Contains(p.ProjectPath))];
    }
}
