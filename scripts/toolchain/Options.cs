// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

/// <summary>A script option. <paramref name="Value"/> is the placeholder shown in help, or null for a flag.</summary>
sealed record ToolchainOption(string Name, string? Value, string Description);

/// <summary>
/// The single list of script options. Parsing, the arguments hidden from Bullseye, and --help all
/// read it, so an option added here is accepted, filtered, and documented in one step.
/// </summary>
static class Options
{
    public static readonly ToolchainOption PackageVersion = new("--package-version", "<version>", "Override NuGet package version");
    public static readonly ToolchainOption NugetFeedName = new("--nuget-feed-name", "<name>", $"NuGet feed name (default: {Conventions.DefaultNugetFeedName})");
    public static readonly ToolchainOption NugetFeedPath = new("--nuget-feed-path", "<path>", "NuGet feed path (default: artifacts/nuget-feed)");
    public static readonly ToolchainOption NugetFeedUrl = new("--nuget-feed-url", "<url>", "Remote NuGet feed URL (required for publish-remote)");
    public static readonly ToolchainOption NugetApiKey = new("--nuget-api-key", "<key>", "API key for remote NuGet feed (required for publish-remote; or NUGET_API_KEY)");
    public static readonly ToolchainOption IncludeDocs = new("--include-docs", null, "Include XML documentation in packages");
    public static readonly ToolchainOption EmbedSymbols = new("--embed-symbols", null, "Embed PDB in .nupkg instead of a separate .snupkg (for the static alpha feed)");
    public static readonly ToolchainOption DryRun = new("--dry-run", null, "Show what would be published without publishing");
    public static readonly ToolchainOption Clean = new("--clean", null, "With build: clean, then rebuild from scratch. With clean: also run dotnet clean");
    public static readonly ToolchainOption Filter = new("--filter", "<expression>", "Test filter, e.g. \"FullyQualifiedName~MyTest\" (see FILTER SYNTAX)");
    public static readonly ToolchainOption Project = new("--project", "<name>", "Build/test only projects whose name contains <name>");
    public static readonly ToolchainOption Integration = new("--integration", null, "Include integration tests (requires --project)");
    public static readonly ToolchainOption Smoke = new("--smoke", null, "Skip Slow and RequiresUserPresence tests");
    public static readonly ToolchainOption Fast = new("--fast", null, "Required fast mode for resilience gates");
    public static readonly ToolchainOption BenchmarkArgs = new("--benchmark-args", "<args>", "Arguments passed to BenchmarkDotNet");
    public static readonly ToolchainOption CrapArgs = new("--crap-args", "<args>", "Arguments passed to crap.cs (e.g. \"--module Piv --top 50\")");
    public static readonly ToolchainOption Verbose = new("--verbose", null, "Stream all build/test output to the console (it is always saved in the run log)");
    public static readonly ToolchainOption ComplexityArgs = new("--complexity-args", "<args>", "Arguments passed to complexity.cs (e.g. \"--module Piv\")");

    public static readonly ToolchainOption[] All =
    [
        PackageVersion, NugetFeedName, NugetFeedPath, NugetFeedUrl, NugetApiKey, IncludeDocs, EmbedSymbols,
        DryRun, Clean, Filter, Project, Integration, Smoke, Fast, Verbose, BenchmarkArgs, CrapArgs, ComplexityArgs
    ];
}

/// <summary>The command line, read through the <see cref="Options"/> table.</summary>
sealed class ParsedArgs(string[] args)
{
    public string? Get(ToolchainOption option)
    {
        var index = Array.IndexOf(args, option.Name);
        return index >= 0 && index < args.Length - 1 ? args[index + 1] : null;
    }

    public bool Has(ToolchainOption option) => args.Contains(option.Name);

    /// <summary>Strips every script option so Bullseye only sees target names and its own flags.</summary>
    public string[] ForBullseye()
    {
        var byName = Options.All.ToDictionary(o => o.Name, StringComparer.OrdinalIgnoreCase);
        var filtered = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            if (!byName.TryGetValue(args[i], out var option))
            {
                filtered.Add(args[i]);
                continue;
            }

            if (option.Value is not null && i + 1 < args.Length)
                i++;
        }

        return [.. filtered];
    }
}
