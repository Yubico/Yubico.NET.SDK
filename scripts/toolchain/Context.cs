// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

/// <summary>Everything the targets read: repository layout, parsed options, and discovered projects.</summary>
sealed class ToolchainContext
{
    public required string RepoRoot { get; init; }

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

    /// <summary>Unit test projects, plus integration test projects when --integration is set.</summary>
    public required TestProject[] TestProjects { get; init; }

    public string ArtifactsDir => Conventions.ArtifactsDir(RepoRoot);
    public string PackagesDir => Conventions.PackagesDir(RepoRoot);

    public static ToolchainContext FromArgs(ParsedArgs args)
    {
        var repoRoot = FindRepoRoot();
        var testFilter = args.Get(Options.Filter);
        if (args.Has(Options.Smoke))
            testFilter = Traits.And(testFilter, Traits.SmokeFilter);

        var includeIntegration = args.Has(Options.Integration);
        var buildableProjects = ProjectDiscovery.DiscoverProjects(repoRoot, "src", Conventions.ProjectPrefix);
        var unitTestProjects = ProjectDiscovery.DiscoverProjects(repoRoot, "tests", ".UnitTests", Conventions.ProjectPrefix);
        var integrationTestProjects = ProjectDiscovery.DiscoverProjects(repoRoot, "tests", ".IntegrationTests", Conventions.ProjectPrefix);
        string[] testProjects = includeIntegration
            ? [.. unitTestProjects, .. integrationTestProjects]
            : unitTestProjects;

        return new ToolchainContext
        {
            RepoRoot = repoRoot,
            PackageVersion = args.Get(Options.PackageVersion),
            NugetFeedName = args.Get(Options.NugetFeedName) ?? Conventions.DefaultNugetFeedName,
            NugetFeedPath = args.Get(Options.NugetFeedPath) ?? Conventions.DefaultNugetFeedPath(repoRoot),
            NugetFeedUrl = args.Get(Options.NugetFeedUrl),
            NugetApiKey = args.Get(Options.NugetApiKey) ?? Environment.GetEnvironmentVariable("NUGET_API_KEY"),
            IncludeDocs = args.Has(Options.IncludeDocs),
            EmbedSymbols = args.Has(Options.EmbedSymbols),
            DryRun = args.Has(Options.DryRun),
            ShouldClean = args.Has(Options.Clean),
            TestFilter = testFilter,
            ProjectFilter = args.Get(Options.Project),
            IncludeIntegration = includeIntegration,
            FastMode = args.Has(Options.Fast),
            BenchmarkArgs = args.Get(Options.BenchmarkArgs) ?? "",
            CrapArgs = args.Get(Options.CrapArgs) ?? "",
            ComplexityArgs = args.Get(Options.ComplexityArgs) ?? "",
            BuildableProjects = buildableProjects,
            PackableProjects = buildableProjects
                .Where(p => ProjectDiscovery.IsPackableProject(repoRoot, p))
                .ToArray(),
            UnitTestProjects = unitTestProjects,
            IntegrationTestProjects = integrationTestProjects,
            ActiveDocumentationFiles = ProjectDiscovery.DiscoverActiveDocumentationFiles(repoRoot),
            TestProjects = ProjectDiscovery.ToTestProjects(repoRoot, testProjects),
        };
    }

    static string FindRepoRoot()
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
