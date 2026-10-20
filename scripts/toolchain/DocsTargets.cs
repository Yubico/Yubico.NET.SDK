// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Text.RegularExpressions;
using static Output;

static class DocsTargets
{
    public static void Register(ToolchainContext ctx)
    {
        TargetRegistry.Define("docs-qa", "Validate active documentation hygiene and package readmes", () =>
        {
            PrintHeader("Validating active documentation");

            var failures = ValidateActiveDocumentation(ctx, ctx.ActiveDocumentationFiles);
            failures.AddRange(ValidatePackageReadmes(ctx, ctx.PackableProjects));

            Gates.FailIfAny(failures, "Documentation validation");

            PrintInfo($"Validated {ctx.ActiveDocumentationFiles.Length} active documentation file(s)");
            PrintInfo($"Validated {ctx.PackableProjects.Length} package readme(s)");
        });

        TargetRegistry.Define("docs-inventory", "Generate the report-only active documentation inventory (requires Bash)", () =>
        {
            PrintHeader("Generating active documentation inventory");

            Bash.RunScript(ctx.RepoRoot, "scripts/docs/build-docs-inventory.sh");
            PrintInfo("Active documentation inventory generated");
        });

        TargetRegistry.Define("docs-architecture", "Validate the architecture diagram evidence map and image freshness (requires Bash)", () =>
        {
            PrintHeader("Validating architecture diagrams (evidence map + image freshness)");

            // 1. Diagram-to-source evidence map resolves (types/projects/edges present).
            // 2. Rendered images exist and are fresh vs their source Mermaid blocks.
            // Both scripts are bash 3.2-portable and exit non-zero on any drift.
            Bash.RunScript(ctx.RepoRoot, "scripts/architecture/check-architecture-map.sh");
            Bash.RunScript(ctx.RepoRoot, "scripts/architecture/check-architecture-images.sh");

            PrintInfo("Architecture diagrams validated (map resolves; images present and fresh)");
        });
    }

    static List<string> ValidateActiveDocumentation(ToolchainContext ctx, string[] documentationFiles)
    {
        var failures = new List<string>();

        foreach (var relativePath in documentationFiles)
        {
            var fullPath = Path.Combine(ctx.RepoRoot, relativePath);
            var lines = File.ReadAllLines(fullPath);

            ValidateCodeFences(relativePath, lines, failures);
            ValidateKnownStaleDocPatterns(relativePath, lines, failures);
            ValidateNoAgentDocReferences(relativePath, lines, failures);
            ValidateLocalMarkdownLinks(relativePath, fullPath, lines, failures);
        }

        return failures;
    }

    static List<string> ValidatePackageReadmes(ToolchainContext ctx, string[] packableProjectPaths)
    {
        // The canonical alpha banner. Every package readme must carry this block verbatim so a single
        // edit here is the only thing needed to find every copy when the SDK exits alpha.
        const string alphaBanner = """
            > ## Alpha - not for production
            >
            > This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
            > security audit**. No security guarantees are made until that audit is complete. Packages are
            > unsigned, and package names and namespaces may change. Provided for evaluation only.
            """;

        var failures = new List<string>();
        if (packableProjectPaths.Length == 0)
            return failures;

        var docsBranch = ReadDocsBranch(ctx, packableProjectPaths[0]);

        foreach (var projectPath in packableProjectPaths)
        {
            // src/<Module>/src/<PackageId>.csproj -> src/<Module>/PACKAGE_README.md
            var packageId = Path.GetFileNameWithoutExtension(projectPath);
            var projectDir = Path.GetDirectoryName(Path.Combine(ctx.RepoRoot, projectPath))!;
            var readmePath = Path.Combine(Path.GetDirectoryName(projectDir)!, "PACKAGE_README.md");
            var relativePath = ProjectDiscovery.RepoRelative(ctx.RepoRoot, readmePath);

            if (!File.Exists(readmePath))
            {
                failures.Add($"{relativePath}: missing package readme for {packageId}; see the per-package readme block in Directory.Build.props");
                continue;
            }

            var text = File.ReadAllText(readmePath).Replace("\r\n", "\n", StringComparison.Ordinal);

            var installCommand = $"dotnet add package {packageId} --prerelease";
            if (!text.Contains(installCommand, StringComparison.Ordinal))
                failures.Add($"{relativePath}: install command must read '{installCommand}'");

            if (!text.Contains(alphaBanner, StringComparison.Ordinal))
                failures.Add($"{relativePath}: alpha banner is missing or altered; it must match ValidatePackageReadmes in scripts/toolchain/DocsTargets.cs verbatim");

            ValidatePackageReadmeLinks(relativePath, text, docsBranch, failures);
        }

        // The root readme is packed by any packable project that has no module readme of its own
        // (see the per-package readme block in Directory.Build.props), so its links reach nuget.org too.
        var rootReadmePath = Path.Combine(ctx.RepoRoot, "PACKAGE_README.md");
        if (File.Exists(rootReadmePath))
        {
            var rootText = File.ReadAllText(rootReadmePath).Replace("\r\n", "\n", StringComparison.Ordinal);
            ValidatePackageReadmeLinks("PACKAGE_README.md", rootText, docsBranch, failures);
        }

        return failures;
    }

    // The v2 branch name, read from the single place that owns it. PackageProjectUrl is derived from
    // this property in MSBuild; the package readmes cannot be, because they are packed verbatim.
    static string ReadDocsBranch(ToolchainContext ctx, string anyPackableProjectPath)
    {
        // Ask MSBuild for the evaluated value rather than regexing Directory.Build.props. Pack uses the
        // evaluated value, so this is the only reading that cannot drift from it if the property is ever
        // moved behind a Condition, overridden in Directory.Build.targets, or passed on the command line.
        // Evaluating one project is enough: the property is defined once, repo-wide.
        var (exitCode, output) = Dotnet.Capture(
        [
            "msbuild", Path.Combine(ctx.RepoRoot, anyPackableProjectPath),
            "-getProperty:YubiKitDocsBranch",
            "-getProperty:PackageProjectUrl",
            "-nologo"
        ]);
        if (exitCode != 0)
            throw new InvalidOperationException($"MSBuild evaluation of {anyPackableProjectPath} failed; cannot read YubiKitDocsBranch:\n{output}");

        using var json = System.Text.Json.JsonDocument.Parse(output);
        var properties = json.RootElement.GetProperty("Properties");
        var branch = properties.GetProperty("YubiKitDocsBranch").GetString()?.Trim();
        var projectUrl = properties.GetProperty("PackageProjectUrl").GetString()?.Trim();

        if (string.IsNullOrEmpty(branch))
            throw new InvalidOperationException("YubiKitDocsBranch evaluates to empty; package readme links cannot be checked against it");

        // The same property must have produced the project URL nuget.org shows, or the two can disagree.
        var expectedProjectUrl = $"https://github.com/Yubico/Yubico.NET.SDK/tree/{branch}";
        if (projectUrl != expectedProjectUrl)
            throw new InvalidOperationException($"PackageProjectUrl evaluates to '{projectUrl}' but YubiKitDocsBranch is '{branch}'; expected '{expectedProjectUrl}'. Derive PackageProjectUrl from the property.");

        return branch;
    }

    static void ValidatePackageReadmeLinks(string relativePath, string text, string docsBranch, List<string> failures)
    {
        var lines = text.Split('\n');
        var inFence = false;
        for (var i = 0; i < lines.Length; i++)
        {
            // Code fences are not prose; `handlers[i](arg)` is not a broken link.
            if (IsCodeFenceLine(lines[i]))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
                continue;

            // Inline links, reference definitions, and autolinks all reach the package page.
            var targets = Regex.Matches(lines[i], @"\]\(([^)\s]+)")
                .Concat(Regex.Matches(lines[i], @"^\s*\[[^\]]+\]:\s*(\S+)"))
                .Concat(Regex.Matches(lines[i], @"<((?:https?|ftp)://[^>\s]+)>"))
                .Select(match => match.Groups[1].Value);

            foreach (var target in targets)
            {
                if (!target.StartsWith("https://", StringComparison.Ordinal))
                {
                    failures.Add($"{relativePath}:{i + 1}: package readme links must be absolute https, because nuget.org does not resolve '{target}'");
                    continue;
                }

                if (!Regex.IsMatch(target, @"^https://github\.com/Yubico/Yubico\.NET\.SDK(/|$)"))
                    continue;

                // The repository default branch is v1. An unqualified repository link silently lands
                // a v2 package-page reader on the wrong SDK, which is worse than a broken link.
                var branchMatch = Regex.Match(target, @"^https://github\.com/Yubico/Yubico\.NET\.SDK/(?:tree|blob)/([^/#?]+)");
                if (!branchMatch.Success)
                {
                    failures.Add($"{relativePath}:{i + 1}: repository link '{target}' must name the v2 branch (/tree/<branch> or /blob/<branch>/...); the default branch is v1");
                    continue;
                }

                // Every readme link must name the same branch as $(YubiKitDocsBranch), so flipping the
                // branch is one property edit plus whatever this check reports.
                var linkBranch = branchMatch.Groups[1].Value;
                if (!string.Equals(linkBranch, docsBranch, StringComparison.Ordinal))
                {
                    failures.Add($"{relativePath}:{i + 1}: repository link '{target}' names branch '{linkBranch}', expected '{docsBranch}' (YubiKitDocsBranch in Directory.Build.props)");
                }
            }
        }
    }

    static void ValidateCodeFences(string relativePath, string[] lines, List<string> failures)
    {
        var openFenceLine = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (!IsCodeFenceLine(lines[i]))
                continue;

            if (openFenceLine == 0)
            {
                openFenceLine = i + 1;
            }
            else
            {
                openFenceLine = 0;
            }
        }

        if (openFenceLine != 0)
            failures.Add($"{relativePath}:{openFenceLine}: unclosed fenced code block");
    }

    static void ValidateKnownStaleDocPatterns(string relativePath, string[] lines, List<string> failures)
    {
        var stalePatterns = new[]
        {
            "RequiresUserPresence!=true",
            "RequiresUserPresence=true",
            "Trait(\"RequiresUserPresence\""
        };

        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var pattern in stalePatterns)
            {
                if (lines[i].Contains(pattern, StringComparison.Ordinal))
                    failures.Add($"{relativePath}:{i + 1}: stale FIDO2 user-presence doc pattern '{pattern}'");
            }
        }
    }

    static void ValidateNoAgentDocReferences(string relativePath, string[] lines, List<string> failures)
    {
        // CLAUDE.md and AGENTS.md are instructions for coding agents, not documentation for humans.
        // Human-facing docs must not point readers at them; the developer guide is the contributor entry point.
        var fileName = Path.GetFileName(relativePath);
        if (fileName is "CLAUDE.md" or "AGENTS.md")
            return;

        var isHumanFacing = fileName is "README.md" or "PACKAGE_README.md" ||
                            relativePath.StartsWith("docs/usage/", StringComparison.OrdinalIgnoreCase);
        if (!isHumanFacing)
            return;

        for (var i = 0; i < lines.Length; i++)
        {
            if (Regex.IsMatch(lines[i], @"\b(CLAUDE|AGENTS)\.md\b"))
                failures.Add($"{relativePath}:{i + 1}: human-facing docs must not reference agent instruction files; link docs/DEV-GUIDE.md instead");
        }
    }

    static void ValidateLocalMarkdownLinks(
        string relativePath,
        string fullPath,
        string[] lines,
        List<string> failures)
    {
        var inCodeFence = false;
        for (var i = 0; i < lines.Length; i++)
        {
            if (IsCodeFenceLine(lines[i]))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
                continue;

            foreach (Match match in Regex.Matches(lines[i], @"(?<!!)\[[^\]]+\]\((?<target>[^)]+)\)"))
            {
                var target = match.Groups["target"].Value.Trim();
                if (ShouldSkipMarkdownLink(target))
                    continue;

                var pathOnly = target.Split('#', 2)[0];
                var decodedPath = Uri.UnescapeDataString(pathOnly);
                var containingDirectory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
                var resolvedPath = Path.GetFullPath(Path.Combine(containingDirectory, decodedPath));

                if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
                    failures.Add($"{relativePath}:{i + 1}: local markdown link target not found: {target}");
            }
        }
    }

    static bool ShouldSkipMarkdownLink(string target)
    {
        if (string.IsNullOrWhiteSpace(target) || target.StartsWith('#'))
            return true;

        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            return true;

        if (target.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    static bool IsCodeFenceLine(string line) => Regex.IsMatch(line, @"^\s*```");
}
