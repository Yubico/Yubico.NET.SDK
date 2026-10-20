// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Text;
using System.Xml.Linq;
using static Output;

static class CoverageTargets
{
    public static void Register(ToolchainContext ctx)
    {
        TargetRegistry.Define("coverage", "Run unit tests with code coverage (artifacts/coverage)", () =>
        {
            PrintHeader("Running tests with coverage");

            // Coverage collection differs per test platform:
            //
            //   xUnit v3 / MTP  -> coverlet.console (out of process).
            //                      --collect:"XPlat Code Coverage" is a VSTest data collector and needs
            //                      Microsoft.NET.Test.Sdk, which MTP projects deliberately omit, so it
            //                      aborts with a missing testhost.deps.json. The MTP-native alternative,
            //                      Microsoft.Testing.Extensions.CodeCoverage, currently throws
            //                      TypeLoadException against xunit.v3 3.0.0 (its Microsoft.Testing.Platform.MSBuild
            //                      1.7.3 dependency is not binary compatible with Microsoft.Testing.Platform 2.3.3).
            //   xUnit v2        -> the existing VSTest data collector path.
            //
            // coverlet is also required for CRAP analysis: it is the only one of these that writes a
            // per-method `complexity` attribute into the Cobertura XML, which ReportGenerator's risk
            // hotspots and any CRAP tooling read. See docs/TESTING.md.
            var coverageResultsDir = Conventions.CoverageDir(ctx.RepoRoot);
            Directory.CreateDirectory(coverageResultsDir);

            var projectsToCover = ProjectDiscovery.MatchTestProjects(
                ProjectDiscovery.ToTestProjects(ctx.RepoRoot, ctx.UnitTestProjects),
                ctx.ProjectFilter);
            if (projectsToCover is null)
                return;

            if (projectsToCover.Any(p => p.UsesTestingPlatformRunner))
                Dotnet.Run("tool restore");

            var results = new List<ProjectResult>();

            foreach (var project in projectsToCover)
            {
                var projectName = Path.GetFileNameWithoutExtension(project.Path);
                PrintProjectHeader("Coverage", projectName);

                try
                {
                    if (project.UsesTestingPlatformRunner)
                        CollectMtpCoverage(ctx, project.Path, projectName, coverageResultsDir);
                    else
                        Dotnet.Run($"test {project.Path} -c {Conventions.Configuration} --settings {Conventions.CoverletRunsettings} --collect:\"XPlat Code Coverage\" --results-directory {coverageResultsDir}");

                    results.Add(new ProjectResult(projectName, Outcome.Passed));
                    PrintColored($"✓ {projectName} - Coverage collected", ConsoleColor.Green);
                }
                catch (Exception ex)
                {
                    results.Add(new ProjectResult(projectName, Outcome.Failed, ex.Message));
                    PrintColored($"✗ {projectName} - Coverage collection failed", ConsoleColor.Red);
                }
            }

            PrintTestSummary(results, "COVERAGE");

            var coverageFiles = Directory.GetFiles(coverageResultsDir, "coverage.cobertura.xml", SearchOption.AllDirectories);
            if (coverageFiles.Length > 0)
            {
                PrintColored("\nCoverage reports generated:", ConsoleColor.Cyan);
                foreach (var file in coverageFiles)
                    Console.WriteLine($"  {file}");
            }

            PrintInfo($"Coverage results saved to {coverageResultsDir}");

            Gates.EnsureTestsRan(results, "coverage test", requireExecution: false);
        });

        TargetRegistry.Define("crap", "Rank methods by CRAP score from collected coverage (run coverage first)", () =>
        {
            // The analysis itself lives in crap.cs so it stays independently runnable and
            // self-checkable; this target exists because CRAP consumes the reports that the
            // coverage target produces, so it belongs on the same discovery surface.
            PrintHeader("Computing CRAP scores");

            var coverageResultsDir = Conventions.CoverageDir(ctx.RepoRoot);
            if (!Directory.Exists(coverageResultsDir))
                throw new InvalidOperationException(
                    "No coverage results found. Run 'dotnet toolchain.cs coverage' first.");

            Dotnet.Run($"crap.cs {ctx.CrapArgs}".TrimEnd(), ctx.RepoRoot);
        });

        TargetRegistry.Define("complexity", "Flag complex methods in your uncommitted changes (source only, no coverage)", () =>
        {
            // Source-only, so unlike crap it needs neither a build nor a coverage run. With no
            // --complexity-args, complexity.cs checks your uncommitted changes against HEAD; this is
            // the soft pre-commit gate described in CLAUDE.md.
            PrintHeader("Checking method complexity");
            Dotnet.Run($"complexity.cs {ctx.ComplexityArgs}".TrimEnd(), ctx.RepoRoot);
        });
    }

    // Collects coverage for a Microsoft Testing Platform (xUnit v3) test project.
    //
    // MTP test projects are plain executables, so coverlet.console can instrument the output
    // directory and launch the test binary directly.
    static void CollectMtpCoverage(ToolchainContext ctx, string projectPath, string projectName, string coverageResultsDir)
    {
        // DeterministicSourcePaths must be off for this build. Directory.Build.props turns on
        // ContinuousIntegrationBuild whenever GITHUB_ACTIONS is set, and with SourceLink that
        // rewrites the source paths recorded in the PDB to the deterministic "/_/" form.
        // coverlet resolves the files it instruments through those paths, so on a deterministic
        // build it finds nothing and emits a 231-byte cobertura report containing "<sources />"
        // and "<packages />" - no classes, no lines, silently 0% for everything.
        //
        // These binaries exist only to be executed once for coverage and are never shipped or
        // attested, so real paths here cost nothing. The packages published by the `pack` target
        // build separately and keep deterministic paths.
        Dotnet.Run($"build {projectPath} -c {Conventions.Configuration} -p:DeterministicSourcePaths=false");

        var projectDir = Path.GetDirectoryName(Path.Combine(ctx.RepoRoot, projectPath))!;
        var outputDir = Path.Combine(projectDir, "bin", Conventions.Configuration, Conventions.TestTargetFramework);
        // Windows appends .exe to the apphost; Linux and macOS do not.
        var testExecutable = Path.Combine(outputDir, projectName);
        if (!File.Exists(testExecutable) && File.Exists(testExecutable + ".exe"))
            testExecutable += ".exe";

        if (!File.Exists(testExecutable))
            throw new FileNotFoundException(
                $"MTP test executable not found at {testExecutable}. Expected an OutputType=Exe test project.",
                testExecutable);

        var outputFile = Path.Combine(coverageResultsDir, projectName, "coverage.cobertura.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

        var coverletArgs = new StringBuilder();
        coverletArgs.Append($"coverlet \"{outputDir}\"");
        coverletArgs.Append($" --target \"{testExecutable}\"");
        coverletArgs.Append(" --format cobertura");
        coverletArgs.Append($" --output \"{outputFile}\"");
        coverletArgs.Append(TranslateRunsettingsToCoverletArgs(ctx));

        Dotnet.Run(coverletArgs.ToString());

        // An instrumentation failure does not fail coverlet: it writes a well-formed cobertura
        // report containing "<sources />" and "<packages />" and exits 0. Every downstream
        // consumer then reads a valid file that says nothing is covered, so CRAP scores every
        // method 0% and the real cause surfaces far away, if at all. Fail here instead, where
        // the cause is still obvious.
        var reportClassCount = XDocument.Load(outputFile).Descendants("class").Count();
        if (reportClassCount == 0)
            throw new InvalidOperationException(
                $"coverlet produced an empty coverage report for {projectName} ({outputFile}). " +
                "It exited successfully but instrumented nothing, so the report contains no classes. " +
                "The usual cause is source paths coverlet cannot resolve on disk - check that this " +
                "build is not producing deterministic '/_/' source paths.");
    }

    // Translates coverlet.runsettings.xml into coverlet.console command-line flags.
    //
    // The VSTest data collector reads the runsettings file directly; coverlet.console cannot.
    // Rather than restating the filters here, the runsettings file stays the single definition
    // of coverage policy and this function projects it onto the CLI, so both collection paths
    // cannot drift apart.
    static string TranslateRunsettingsToCoverletArgs(ToolchainContext ctx)
    {
        var settingsPath = Path.Combine(ctx.RepoRoot, Conventions.CoverletRunsettings);
        if (!File.Exists(settingsPath))
            throw new FileNotFoundException(
                $"coverlet.runsettings.xml not found at {settingsPath}; it defines the coverage filters.",
                settingsPath);

        var settings = XDocument.Load(settingsPath)
            .Descendants("Configuration")
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "coverlet.runsettings.xml has no <Configuration> element.");

        var args = new StringBuilder();

        foreach (var (element, flag) in new[]
                 {
                     ("Exclude", "--exclude"),
                     ("Include", "--include"),
                     ("ExcludeByFile", "--exclude-by-file"),
                     ("ExcludeByAttribute", "--exclude-by-attribute"),
                 })
        {
            foreach (var value in SplitSettingList(settings.Element(element)?.Value))
                args.Append($" {flag} \"{value}\"");
        }

        if (IsSettingTrue(settings.Element("SkipAutoProps")?.Value))
            args.Append(" --skipautoprops");

        if (IsSettingTrue(settings.Element("SingleHit")?.Value))
            args.Append(" --single-hit");

        if (IsSettingTrue(settings.Element("UseSourceLink")?.Value))
            args.Append(" --use-source-link");

        // coverlet.console omits the test assembly unless asked; VSTest needs it stated.
        if (IsSettingTrue(settings.Element("IncludeTestAssembly")?.Value))
            args.Append(" --include-test-assembly");

        return args.ToString();
    }

    static IEnumerable<string> SplitSettingList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    static bool IsSettingTrue(string? value) =>
        string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
}
