// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

/// <summary>Repository layout and naming conventions the targets rely on.</summary>
static class Conventions
{
    public const string ProjectPrefix = "Yubico.YubiKit.";
    public const string SolutionFile = "Yubico.YubiKit.sln";
    public const string Configuration = "Release";

    // Matches <TargetFramework> in Directory.Build.props; used to locate MTP test executables.
    public const string TestTargetFramework = "net10.0";

    public const string DefaultNugetFeedName = "Yubico.YubiKit-LocalNuGet";
    public const string CoverletRunsettings = "coverlet.runsettings.xml";
    public const string MtpRunnerOptIn = "<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>";

    public static string ArtifactsDir(string repoRoot) => Path.Combine(repoRoot, "artifacts");
    public static string PackagesDir(string repoRoot) => Path.Combine(ArtifactsDir(repoRoot), "packages");
    public static string CoverageDir(string repoRoot) => Path.Combine(ArtifactsDir(repoRoot), "coverage");
    public static string DefaultNugetFeedPath(string repoRoot) => Path.Combine(ArtifactsDir(repoRoot), "nuget-feed");
}

/// <summary>Test trait names and the --filter expressions built from them.</summary>
static class Traits
{
    public const string Category = "Category";
    public const string Slow = "Slow";
    public const string RequiresUserPresence = "RequiresUserPresence";
    public const string RuntimeResilience = "RuntimeResilience";

    public static string Is(string value) => $"{Category}={value}";
    public static string IsNot(string value) => $"{Category}!={value}";

    /// <summary>--smoke: skip slow and user-presence tests.</summary>
    public static readonly string SmokeFilter = $"{IsNot(Slow)}&{IsNot(RequiresUserPresence)}";

    /// <summary>ANDs an extra term onto an optional user filter.</summary>
    public static string And(string? filter, string term) =>
        string.IsNullOrEmpty(filter) ? term : $"{filter}&{term}";
}

/// <summary>Text the test runners print when a filter selects no tests (they still exit 0).</summary>
static class RunnerMarkers
{
    // xUnit v3 MTP 3.0.0 reports zero discovery matches with this summary text.
    public const string MtpNoMatches = "found 0 test(s)";

    // VSTest 18.x reports zero discovery matches with this text.
    public const string VsTestNoMatches = "No test matches the given testcase filter";
}
