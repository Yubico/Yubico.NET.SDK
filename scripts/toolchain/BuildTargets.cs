// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Xml.Linq;
using static Output;

static class BuildTargets
{
    public static void Register(ToolchainContext ctx)
    {
        TargetRegistry.Define("clean", "Empty artifacts/ except the run logs in artifacts/toolchain (--clean also runs dotnet clean)", () =>
        {
            PrintHeader("Cleaning");

            // artifacts/toolchain holds the run logs, including this run's, so it survives a clean.
            if (Directory.Exists(ctx.ArtifactsDir))
            {
                var toolchainDir = RunLog.ToolchainDir(ctx.RepoRoot);
                foreach (var entry in Directory.EnumerateFileSystemEntries(ctx.ArtifactsDir).Where(e => e != toolchainDir))
                {
                    if (Directory.Exists(entry))
                        Directory.Delete(entry, recursive: true);
                    else
                        File.Delete(entry);
                }

                PrintInfo($"Deleted the contents of {ctx.ArtifactsDir} except the run logs in artifacts/toolchain");
            }

            if (ctx.ShouldClean)
            {
                Dotnet.Run($"clean {Conventions.SolutionFile} -c {Conventions.Configuration}", "clean");
                PrintInfo("Cleaned solution");
            }
        });

        TargetRegistry.Define("restore", "Restore NuGet dependencies", () =>
        {
            PrintHeader("Restoring dependencies");
            Dotnet.Run($"restore {Conventions.SolutionFile}", "restore");
            PrintInfo("Dependencies restored");
        });

        TargetRegistry.Define("test-infrastructure-qa", "Validate hardware-test declarations ([WithYubiKey] needs [SkippableTheory])", () =>
        {
            PrintHeader("Validating hardware-test declarations");
            Gates.FailIfAny(ValidateHardwareTestDeclarations(ctx, ctx.IntegrationTestProjects), "Hardware-test declaration validation");
            PrintInfo("All [WithYubiKey] tests use [SkippableTheory] and declare the required package");
        });

        TargetRegistry.Define("native-aot-contract-qa", "Validate that every SDK library is covered by the Native AOT verification host", () =>
        {
            PrintHeader("Validating Native AOT support contract");

            var discovery = DiscoverNativeAotProjects(ctx);
            Gates.FailIfAny(ValidateNativeAotContract(ctx, discovery), "Native AOT support contract validation");

            PrintInfo(
                $"All {discovery.SdkProjects.Length} supported SDK libraries opt in, are referenced by the " +
                "verification host, and are anchored there by an entry type");
            PrintInfo(
                $"Excluded internal tooling ({discovery.ExcludedProjects.Length}): " +
                $"{string.Join(", ", discovery.ExcludedProjects)}");
        });

        TargetRegistry.Define("build", "Build the solution, or the --project matches (--clean: from scratch)", ["test-infrastructure-qa", "native-aot-contract-qa"], () =>
        {
            PrintHeader("Building");

            // --clean rebuilds from scratch, so diagnostics cannot come from stale incremental output.
            var rebuild = ctx.ShouldClean ? " --no-incremental" : "";

            if (string.IsNullOrEmpty(ctx.ProjectFilter))
            {
                if (ctx.ShouldClean)
                    Dotnet.Run($"clean {Conventions.SolutionFile} -c {Conventions.Configuration}", "clean");

                Dotnet.Run($"build {Conventions.SolutionFile} -c {Conventions.Configuration}{rebuild}", "build");
                PrintInfo($"Built {Conventions.SolutionFile} in {Conventions.Configuration} configuration{(ctx.ShouldClean ? " from scratch" : "")}");
                return;
            }

            var matchingProjects = ProjectDiscovery.MatchProjects(ctx.BuildableProjects, ctx.ProjectFilter, "buildable");
            foreach (var project in matchingProjects)
            {
                if (ctx.ShouldClean)
                    Dotnet.Run($"clean {project} -c {Conventions.Configuration}", $"clean-{ProjectDiscovery.ShortName(project)}");
                Dotnet.Run($"build {project} -c {Conventions.Configuration}{rebuild}", $"build-{ProjectDiscovery.ShortName(project)}");
            }

            PrintInfo($"Built {matchingProjects.Length} project(s) matching '{ctx.ProjectFilter}': {string.Join(", ", matchingProjects.Select(ProjectDiscovery.ShortName))}");
        });
    }

    // Native AOT support covers the published SDK libraries, which all sit at src/<Module>/src/*.csproj.
    // Deriving the set from that layout rather than a hand-maintained list means a newly added module is
    // held to the contract automatically: ValidateNativeAotContract will name it and fail the build until
    // it carries the IsYubiKitSdkLibrary opt-in, is referenced by the verification host, and is anchored
    // there by an entry type.
    static (string[] SdkProjects, string[] ExcludedProjects, string[] UnclassifiedProjects) DiscoverNativeAotProjects(ToolchainContext ctx)
    {
        // Modules that sit at the shipped-library layout but are internal tooling (IsPackable=false,
        // never shipped), so they stay outside the Native AOT support envelope. This is a closed set,
        // not a "Cli.*" prefix rule: any other Cli.* module appearing at that layout is reported as
        // unclassified rather than silently skipped, so shipping one forces the in/out decision to be
        // made here. The exclusion is deliberately path-based rather than read from the project file:
        // a check that a project can opt itself out of is not a safety net.
        string[] excludedModules = ["Cli.Commands", "Cli.Shared"];

        // The slash count pins the depth and Contains("/src/") pins the middle segment. That second
        // predicate is load-bearing in a non-obvious way: it is the only thing excluding
        // src/Cli/YkTool/Yubico.YubiKit.Cli.YkTool.csproj, which also has three slashes and whose module
        // segment is "Cli", not "Cli.<something>", so neither the depth test nor the excluded-module set
        // would reject it.
        var candidates = Directory
            .GetFiles(Path.Combine(ctx.RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => ProjectDiscovery.RepoRelative(ctx.RepoRoot, path))
            .Where(path => path.Count(character => character == '/') == 3 &&
                           path.StartsWith("src/", StringComparison.Ordinal) &&
                           path.Contains("/src/", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        static string ModuleSegment(string path) => path.Split('/')[1];

        bool IsTooling(string path) => ModuleSegment(path).StartsWith("Cli.", StringComparison.Ordinal);
        bool IsKnownExclusion(string path) =>
            excludedModules.Contains(ModuleSegment(path), StringComparer.Ordinal);

        return (
            SdkProjects: candidates.Where(path => !IsTooling(path)).ToArray(),
            ExcludedProjects: candidates.Where(IsKnownExclusion).ToArray(),
            UnclassifiedProjects: candidates.Where(path => IsTooling(path) && !IsKnownExclusion(path)).ToArray());
    }

    static List<string> ValidateNativeAotContract(
        ToolchainContext ctx,
        (string[] SdkProjects, string[] ExcludedProjects, string[] UnclassifiedProjects) discovery)
    {
        // Maps each SDK library to the public type the verification host must root. A <ProjectReference>
        // on its own proves nothing: with no reachable type from the assembly, ILC trims it straight out
        // of the host's whole-program closure, so the publish would verify nothing for that library while
        // this target still reported it as anchored. Discovery drives the iteration below; this map only
        // answers "which type anchors this library", so a newly discovered library with no entry fails
        // until a maintainer picks one and roots it in Program.cs.
        var entryTypes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Yubico.YubiKit.Core.csproj"] = "YubiKeyManager",
            ["Yubico.YubiKit.Management.csproj"] = "ManagementSession",
            ["Yubico.YubiKit.Piv.csproj"] = "PivSession",
            ["Yubico.YubiKit.Fido2.csproj"] = "FidoSession",
            ["Yubico.YubiKit.WebAuthn.csproj"] = "WebAuthnClient",
            ["Yubico.YubiKit.Oath.csproj"] = "OathSession",
            ["Yubico.YubiKit.OpenPgp.csproj"] = "OpenPgpSession",
            ["Yubico.YubiKit.SecurityDomain.csproj"] = "SecurityDomainSession",
            ["Yubico.YubiKit.YubiOtp.csproj"] = "YubiOtpSession",
            ["Yubico.YubiKit.YubiHsm.csproj"] = "HsmAuthSession"
        };

        var failures = new List<string>();
        var sdkProjects = discovery.SdkProjects;

        if (sdkProjects.Length == 0)
        {
            // Discovery returning nothing would silently pass every per-project check below.
            failures.Add("no SDK libraries discovered under src/<Module>/src/*.csproj");
        }

        foreach (var project in discovery.UnclassifiedProjects)
        {
            failures.Add(
                $"{project}: internal-tooling module at the SDK library layout is neither a supported SDK " +
                "library nor in the excludedModules set in DiscoverNativeAotProjects (scripts/toolchain/BuildTargets.cs); " +
                "classify it explicitly");
        }

        var verificationDirectory = Path.Combine(ctx.RepoRoot, "verification", "NativeAotVerification");
        var verificationProjectPath = Path.Combine(
            verificationDirectory,
            "Yubico.YubiKit.NativeAotVerification.csproj");
        var verificationProgramPath = Path.Combine(verificationDirectory, "Program.cs");
        var verificationProgram = File.ReadAllText(verificationProgramPath);

        // Parsed as XML rather than substring-matched against the raw file text: a commented-out
        // <ProjectReference>, or a project file name that merely appears in the host csproj's header
        // comment, satisfies a substring check while referencing nothing at all.
        var referencedSdkProjects = XDocument.Load(verificationProjectPath)
            .Descendants("ProjectReference")
            .Attributes("Include")
            .Select(include => Path.GetFullPath(Path.Combine(
                verificationDirectory,
                include.Value.Replace('\\', Path.DirectorySeparatorChar))))
            .Select(fullPath => ProjectDiscovery.RepoRelative(ctx.RepoRoot, fullPath))
            .Where(path => path.StartsWith("src/", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var project in sdkProjects)
        {
            var projectContents = File.ReadAllText(Path.Combine(ctx.RepoRoot, project));
            if (!projectContents.Contains(
                    "<IsYubiKitSdkLibrary>true</IsYubiKitSdkLibrary>",
                    StringComparison.Ordinal))
            {
                failures.Add($"{project}: missing IsYubiKitSdkLibrary opt-in");
            }

            if (!referencedSdkProjects.Contains(project))
            {
                failures.Add(
                    $"{project}: verification host does not reference it; add a <ProjectReference> to " +
                    "verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj");
            }

            if (!entryTypes.TryGetValue(Path.GetFileName(project), out var entryType))
            {
                failures.Add(
                    $"{project}: no verification-host entry type mapped; add one to entryTypes in " +
                    "scripts/toolchain/BuildTargets.cs and root it in verification/NativeAotVerification/Program.cs");
            }
            else if (!verificationProgram.Contains($"typeof({entryType})", StringComparison.Ordinal))
            {
                failures.Add(
                    $"{project}: verification host does not anchor typeof({entryType}) in " +
                    "verification/NativeAotVerification/Program.cs");
            }
        }

        // The other direction: every SDK project the host references must still be a discovered library.
        // Without this, a library that moves out of the src/<Module>/src/ shape simply drops out of the
        // loop above and the contract passes green with a smaller count, leaving it outside AOT coverage.
        // It also catches a reference left behind by a deleted or renamed module.
        foreach (var referenced in referencedSdkProjects
                     .Except(sdkProjects, StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            failures.Add(
                $"{referenced}: referenced by the verification host but not discovered as a supported SDK " +
                "library at src/<Module>/src/*.csproj (moved, renamed, deleted, or internal tooling)");
        }

        return failures;
    }

    static List<string> ValidateHardwareTestDeclarations(ToolchainContext ctx, string[] projects)
    {
        var failures = new List<string>();

        foreach (var project in projects)
        {
            var projectPath = Path.Combine(ctx.RepoRoot, project);
            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException($"Project has no directory: {project}");
            var sourceFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .ToArray();
            var filesWithYubiKeyTests = sourceFiles
                .Where(path => File.ReadAllText(path).Contains("[WithYubiKey", StringComparison.Ordinal))
                .ToArray();

            if (filesWithYubiKeyTests.Length == 0)
                continue;

            var projectXml = File.ReadAllText(projectPath);
            if (!projectXml.Contains(
                    "<PackageReference Include=\"Xunit.SkippableFact\"",
                    StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{project}: projects containing [WithYubiKey] must reference Xunit.SkippableFact directly");
            }

            foreach (var sourceFile in filesWithYubiKeyTests)
            {
                var lines = File.ReadAllLines(sourceFile);
                var plainTheoryLine = 0;
                var withYubiKeyLine = 0;
                var hasWithYubiKey = false;
                var hasSkippableTheory = false;
                var readingAttribute = false;

                for (var i = 0; i < lines.Length; i++)
                {
                    var trimmed = lines[i].Trim();
                    if (readingAttribute)
                    {
                        hasWithYubiKey |= trimmed.Contains("WithYubiKey", StringComparison.Ordinal);
                        readingAttribute = !trimmed.Contains(']');
                        continue;
                    }

                    if (trimmed.StartsWith("[", StringComparison.Ordinal))
                    {
                        plainTheoryLine = trimmed == "[Theory]" ? i + 1 : plainTheoryLine;
                        hasSkippableTheory |= trimmed == "[SkippableTheory]";
                        if (trimmed.Contains("WithYubiKey", StringComparison.Ordinal))
                        {
                            hasWithYubiKey = true;
                            withYubiKeyLine = i + 1;
                        }
                        readingAttribute = !trimmed.Contains(']');
                        continue;
                    }

                    if ((plainTheoryLine > 0 || hasWithYubiKey || hasSkippableTheory) &&
                        (trimmed.Length == 0 ||
                         trimmed.StartsWith("//", StringComparison.Ordinal) ||
                         trimmed.StartsWith("/*", StringComparison.Ordinal) ||
                         trimmed.StartsWith("*", StringComparison.Ordinal) ||
                         trimmed.StartsWith("*/", StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    ReportDeclaration();

                    plainTheoryLine = 0;
                    withYubiKeyLine = 0;
                    hasWithYubiKey = false;
                    hasSkippableTheory = false;
                }

                ReportDeclaration();

                // Checks the attributes collected for one member.
                void ReportDeclaration()
                {
                    var location = ProjectDiscovery.RepoRelative(ctx.RepoRoot, sourceFile);
                    if (plainTheoryLine > 0 && hasWithYubiKey)
                        failures.Add($"{location}:{plainTheoryLine}: [WithYubiKey] must use [SkippableTheory], not [Theory]");
                    else if (hasWithYubiKey && !hasSkippableTheory)
                        failures.Add($"{location}:{withYubiKeyLine}: [WithYubiKey] requires [SkippableTheory]");
                }
            }
        }

        return failures;
    }
}
