// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using static Output;

static class BuildTargets
{
    const string WindowsVerifierRoot = "artifacts/windows-hid-verification";
    const string WindowsVerifierProject = "verification/WindowsHidRouteVerification/WindowsHidRouteVerification.csproj";

    private static void PublishWindowsHidVerifier()
    {
        if (!OperatingSystem.IsWindows())
            throw new ToolchainException("Windows native fixture publishing requires Windows and its Native AOT prerequisites. No native checks executed.");

        Directory.CreateDirectory($"{WindowsVerifierRoot}/evidence");
        File.Delete($"{WindowsVerifierRoot}/evidence/publish.json");
        foreach (var (kind, aot) in new[] { ("jit", "false"), ("aot", "true") })
        {
            Directory.CreateDirectory($"{WindowsVerifierRoot}/evidence/{kind}");
            File.Delete($"{WindowsVerifierRoot}/evidence/{kind}/summary.json");
            Dotnet.Run($"restore {WindowsVerifierProject} -r win-x64 -p:Configuration=Release -p:SelfContained=true -p:PublishAot={aot} --artifacts-path {WindowsVerifierRoot}/build/{kind}", $"windows-hid-{kind}-restore");
        }

        var inputs = WindowsVerifierInputs();
        var commit = Exec.RunOrFail("git", "rev-parse HEAD", Exec.Display.ErrorsOnly, "windows-hid-source").Output.Trim();
        var status = Exec.RunOrFail("git", "status --porcelain", Exec.Display.ErrorsOnly, "windows-hid-source-status").Output.Trim();
        var sdk = Exec.RunOrFail("dotnet", "--info", Exec.Display.ErrorsOnly, "windows-hid-sdk").Output;
        foreach (var (kind, aot) in new[] { ("jit", "false"), ("aot", "true") })
        {
            string output = $"{WindowsVerifierRoot}/{kind}";
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
            Dotnet.Run($"publish {WindowsVerifierProject} -c Release -r win-x64 --self-contained --no-restore -p:PublishAot={aot} --artifacts-path {WindowsVerifierRoot}/build/{kind} -o {output}", $"windows-hid-{kind}-publish");
        }
        if (!inputs.SequenceEqual(WindowsVerifierInputs()))
            throw new ToolchainException("Windows verifier source or resolved package inputs changed during publish.");

        File.WriteAllText($"{WindowsVerifierRoot}/evidence/publish.json", JsonSerializer.Serialize(new
        {
            SourceCommit = commit, SourceStatus = status, Sdk = sdk, Inputs = inputs,
            Jit = HashWindowsVerifierFiles(Directory.EnumerateFiles($"{WindowsVerifierRoot}/jit", "*", SearchOption.AllDirectories)),
            Aot = HashWindowsVerifierFiles(Directory.EnumerateFiles($"{WindowsVerifierRoot}/aot", "*", SearchOption.AllDirectories)),
            Packages = WindowsVerifierPackages()
        }, new JsonSerializerOptions { WriteIndented = true }));
        PrintInfo($"Windows verifier publish evidence: {WindowsVerifierRoot}/evidence/publish.json");
    }

    private static SortedDictionary<string, string> WindowsVerifierInputs()
    {
        var listed = Exec.RunOrFail("git", "ls-files -z --cached --others --exclude-standard", Exec.Display.ErrorsOnly, "windows-hid-input-list").Output;
        var files = listed.TrimEnd('\r', '\n').Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Where(File.Exists)
            .Concat(Directory.EnumerateFiles($"{WindowsVerifierRoot}/build", "project.assets.json", SearchOption.AllDirectories));
        return HashWindowsVerifierFiles(files);
    }

    private static SortedDictionary<string, string> HashWindowsVerifierFiles(IEnumerable<string> files)
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in files)
        {
            using FileStream stream = File.OpenRead(file);
            hashes[file.Replace('\\', '/')] = Convert.ToHexString(SHA256.HashData(stream));
        }
        return hashes;
    }

    private static SortedSet<string> WindowsVerifierPackages()
    {
        var packages = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string assets in Directory.EnumerateFiles($"{WindowsVerifierRoot}/build", "project.assets.json", SearchOption.AllDirectories))
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assets));
            foreach (JsonProperty library in document.RootElement.GetProperty("libraries").EnumerateObject())
                packages.Add(library.Name);
        }
        return packages;
    }

    private static void RunWindowsHidVerifier()
    {
        if (!OperatingSystem.IsWindows()) throw new ToolchainException("Windows native checks require Windows.");
        var failures = new List<string>();
        foreach (string kind in new[] { "jit", "aot" })
        {
            string output = $"{WindowsVerifierRoot}/{kind}";
            var before = HashWindowsVerifierFiles(Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories));
            var result = Exec.Run($"{output}/WindowsHidRouteVerification.exe", $"--native {kind} {WindowsVerifierRoot}/evidence/{kind}", Exec.Display.Stream, $"windows-hid-{kind}-native");
            if (result.ExitCode != 0) failures.Add($"{kind} exited {result.ExitCode}; {result.LogPath}");
            if (!before.SequenceEqual(HashWindowsVerifierFiles(Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories))))
                throw new ToolchainException($"Windows verifier {kind} published files changed during execution.");
        }
        if (failures.Count > 0) throw new ToolchainException(string.Join(Environment.NewLine, failures));
        PrintInfo($"Windows no-hardware JIT/AOT evidence: {WindowsVerifierRoot}/evidence");
    }

    public static void Register(ToolchainContext ctx)
    {
        TargetRegistry.Define("windows-hid-verification-build", "Compile the Windows no-hardware verifier on the current host (does not execute native checks)", () =>
            Dotnet.Run("build verification/WindowsHidRouteVerification/WindowsHidRouteVerification.csproj -c Release -p:PublishAot=false", "windows-hid-verification-build"));

        TargetRegistry.Define("windows-hid-verification-publish", "Windows-only win-x64 JIT/AOT native fixture publish with source/package/artifact hashes", PublishWindowsHidVerifier);
        // One target invocation publishes AND executes both runtimes. Runner clients must not
        // concatenate separate target names with '+'. Hardware acceptance is a separate phase.
        TargetRegistry.Define("windows-hid-verification-nohardware", "Windows-only publish and run isolated native pipe checks under JIT and AOT", ["windows-hid-verification-publish"], RunWindowsHidVerifier);

        // Opt-in regression gate, explicitly invoked by Linux Native AOT CI. Registering this
        // action does not publish anything; build/test/default do not depend on this target.
        TargetRegistry.Define("linux-hid-verification-publish", "Opt-in Linux HID contract host publish (JIT and Native AOT; not part of build)", () =>
        {
            const string project = "verification/LinuxHidRouteVerification/LinuxHidRouteVerification.csproj";
            // Prepare both conditional dependency/import closures before binding inputs. Separate
            // intermediate trees prevent one runtime's restore from replacing the other's assets.
            foreach (var (kind, aot) in new[] { ("jit", "false"), ("aot", "true") })
            {
                Dotnet.Run($"restore {project} -r linux-x64 -p:Configuration=Release -p:SelfContained=true -p:PublishAot={aot} --artifacts-path artifacts/linux-hid-verification/build/{kind}", $"linux-hid-{kind}-restore");
            }
            Exec.RunOrFail("python3", "verification/LinuxHidRouteVerification/publish_contract.py begin", Exec.Display.Stream, "linux-hid-publish-inputs");
            Dotnet.Run($"publish {project} -c Release -r linux-x64 --self-contained --no-restore -p:PublishAot=false --artifacts-path artifacts/linux-hid-verification/build/jit -o artifacts/linux-hid-verification/jit", "linux-hid-jit-publish");
            Dotnet.Run($"publish {project} -c Release -r linux-x64 --self-contained --no-restore -p:PublishAot=true --artifacts-path artifacts/linux-hid-verification/build/aot -o artifacts/linux-hid-verification/aot", "linux-hid-aot-publish");
            Exec.RunOrFail("python3", "verification/LinuxHidRouteVerification/publish_contract.py seal", Exec.Display.Stream, "linux-hid-publish-binding");
        });

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
            ["Yubico.YubiKit.Ctap2.csproj"] = "Ctap2Session",
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
