// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

/// <summary>Everything the targets read: repository layout, parsed options, and discovered projects.</summary>
sealed class ToolchainContext
{
    public const string ProjectPrefix = "Yubico.YubiKit.";

    // Matches <TargetFramework> in Directory.Build.props; used to locate MTP test executables.
    public const string TestTargetFramework = "net10.0";

    // Script options Bullseye must not see. Every option parsed in FromArgs belongs in one of these lists.
    static readonly string[] s_optionsWithValues =
    [
        "--project", "--filter", "--package-version", "--nuget-feed-name", "--nuget-feed-path",
        "--nuget-feed-url", "--nuget-api-key", "--benchmark-args", "--crap-args", "--complexity-args"
    ];

    static readonly string[] s_flags =
        ["--integration", "--include-docs", "--embed-symbols", "--dry-run", "--clean", "--smoke", "--fast"];

    public required string RepoRoot { get; init; }
    public string SolutionFile { get; } = "Yubico.YubiKit.sln";
    public string Configuration { get; } = "Release";

    public required string? PackageVersion { get; init; }
    public required string NugetFeedName { get; init; }
    public required string NugetFeedPath { get; init; }
    public required string? NugetFeedUrl { get; init; }
    public required string? NugetApiKey { get; init; }
    public required bool IncludeDocs { get; init; }
    public required bool EmbedSymbols { get; init; }
    public required bool DryRun { get; init; }
    public required bool ShouldClean { get; init; }
    public required string? TestFilter { get; init; }
    public required string? ProjectFilter { get; init; }
    public required bool IncludeIntegration { get; init; }
    public required bool FastMode { get; init; }
    public required string BenchmarkArgs { get; init; }
    public required string CrapArgs { get; init; }
    public required string ComplexityArgs { get; init; }

    public required string[] BuildableProjects { get; init; }
    public required string[] PackableProjects { get; init; }
    public required string[] UnitTestProjects { get; init; }
    public required string[] IntegrationTestProjects { get; init; }
    public required string[] ActiveDocumentationFiles { get; init; }
    public required string[] TestProjects { get; init; }
    public required (string ProjectPath, bool UsesTestingPlatformRunner)[] TestProjectInfos { get; init; }

    public string ArtifactsDir => Path.Combine(RepoRoot, "artifacts");
    public string PackagesDir => Path.Combine(ArtifactsDir, "packages");

    public static ToolchainContext FromArgs(string[] args)
    {
        // Use the script's own `args`, not Environment.GetCommandLineArgs() which includes the host path.
        string? GetArgument(string name)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index < args.Length - 1 ? args[index + 1] : null;
        }

        bool HasFlag(string name) => args.Contains(name);

        var repoRoot = GetRepoRoot();
        var testFilter = GetArgument("--filter");

        // --smoke injects trait filters to skip slow and user-presence tests
        if (HasFlag("--smoke"))
        {
            var smokeFilter = "Category!=Slow&Category!=RequiresUserPresence";
            testFilter = string.IsNullOrEmpty(testFilter) ? smokeFilter : $"{testFilter}&{smokeFilter}";
        }

        var includeIntegration = HasFlag("--integration");

        // Dynamically discover projects using glob patterns
        var buildableProjects = ProjectDiscovery.DiscoverProjects(repoRoot, "src", ProjectPrefix);
        var unitTestProjects = ProjectDiscovery.DiscoverProjects(repoRoot, "tests", ".UnitTests", ProjectPrefix);
        var integrationTestProjects = ProjectDiscovery.DiscoverProjects(repoRoot, "tests", ".IntegrationTests", ProjectPrefix);
        string[] testProjects = includeIntegration
            ? [.. unitTestProjects, .. integrationTestProjects]
            : unitTestProjects;

        return new ToolchainContext
        {
            RepoRoot = repoRoot,
            PackageVersion = GetArgument("--package-version"),
            NugetFeedName = GetArgument("--nuget-feed-name") ?? "Yubico.YubiKit-LocalNuGet",
            NugetFeedPath = GetArgument("--nuget-feed-path") ?? Path.Combine(repoRoot, "artifacts", "nuget-feed"),
            NugetFeedUrl = GetArgument("--nuget-feed-url"),
            NugetApiKey = GetArgument("--nuget-api-key") ?? Environment.GetEnvironmentVariable("NUGET_API_KEY"),
            IncludeDocs = HasFlag("--include-docs"),
            EmbedSymbols = HasFlag("--embed-symbols"),
            DryRun = HasFlag("--dry-run"),
            ShouldClean = HasFlag("--clean"),
            TestFilter = testFilter,
            ProjectFilter = GetArgument("--project"),
            IncludeIntegration = includeIntegration,
            FastMode = HasFlag("--fast"),
            BenchmarkArgs = GetArgument("--benchmark-args") ?? "",
            CrapArgs = GetArgument("--crap-args") ?? "",
            ComplexityArgs = GetArgument("--complexity-args") ?? "",
            BuildableProjects = buildableProjects,
            PackableProjects = buildableProjects
                .Where(p => ProjectDiscovery.IsPackableProject(repoRoot, p))
                .ToArray(),
            UnitTestProjects = unitTestProjects,
            IntegrationTestProjects = integrationTestProjects,
            ActiveDocumentationFiles = ProjectDiscovery.DiscoverActiveDocumentationFiles(repoRoot),
            TestProjects = testProjects,
            TestProjectInfos = testProjects
                .Select(p => (ProjectPath: p, UsesTestingPlatformRunner: ProjectDiscovery.UsesMicrosoftTestingPlatformRunner(repoRoot, p)))
                .ToArray(),
        };
    }

    /// <summary>Strips every script option so Bullseye only sees target names and its own flags.</summary>
    public static string[] FilterBullseyeArgs(string[] args)
    {
        var valueOptions = new HashSet<string>(s_optionsWithValues, StringComparer.OrdinalIgnoreCase);
        var flagOptions = new HashSet<string>(s_flags, StringComparer.OrdinalIgnoreCase);
        var filtered = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (flagOptions.Contains(arg))
                continue;

            if (valueOptions.Contains(arg))
            {
                if (i + 1 < args.Length)
                    i++;
                continue;
            }

            filtered.Add(arg);
        }

        return [.. filtered];
    }

    static string GetRepoRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current is not null)
        {
            var gitPath = Path.Combine(current, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}
