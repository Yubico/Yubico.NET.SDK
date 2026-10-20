#!/usr/bin/env dotnet run

#:package Bullseye
#:package SimpleExec
#:include scripts/toolchain/Context.cs
#:include scripts/toolchain/Discovery.cs
#:include scripts/toolchain/Output.cs
#:include scripts/toolchain/Process.cs
#:include scripts/toolchain/BuildTargets.cs
#:include scripts/toolchain/TestTargets.cs
#:include scripts/toolchain/CoverageTargets.cs
#:include scripts/toolchain/BenchmarkTargets.cs
#:include scripts/toolchain/DocsTargets.cs
#:include scripts/toolchain/PackageTargets.cs

/*
 * Yubico.YubiKit Toolchain Script
 * =================================
 *
 * .NET 10 build automation script using Bullseye task runner.
 *
 * USAGE:
 *   dotnet toolchain.cs [target]
 *   dotnet toolchain.cs -- [target] [options]   (required before script long options)
 *
 * NOTE: The -- separator passes arguments to the script instead of dotnet.
 *       It is required before every script long option, including --project:
 *   dotnet toolchain.cs -- --help
 *   dotnet toolchain.cs -- build --project Piv
 *
 * TARGETS:
 *   clean          - Remove artifacts directory
 *   restore        - Restore NuGet dependencies
 *   build          - Build the solution (restores only if needed)
 *   test           - Run unit tests with summary output
 *   resilience     - Run fast no-hardware runtime resilience gates
 *   test-infrastructure-qa - Validate hardware-test declaration invariants
 *   benchmark      - Run performance benchmarks (manual, not CI)
 *   docs-qa        - Validate active documentation hygiene
 *   docs-list-active - Print the exact active documentation set used by docs-qa
 *   docs-inventory - Generate report-only active documentation inventory (requires Bash)
 *   docs-architecture - Validate architecture diagram evidence map + image freshness (requires Bash)
 *   coverage       - Run tests with code coverage
 *   crap           - Rank methods by CRAP score from collected coverage (requires: coverage)
 *   complexity     - Flag complex methods in your uncommitted changes (source only, no coverage)
 *   pack           - Create NuGet packages
 *   setup-feed     - Configure local NuGet feed
 *   publish        - Publish packages to local feed
 *   publish-remote - Push packages to a remote NuGet feed (e.g. GitHub Packages)
 *   default        - Run tests and publish
 *
 * OPTIONS:
 *   --package-version <version>    Override NuGet package version
 *   --nuget-feed-name <name>       NuGet feed name (default: Yubico.YubiKit-LocalNuGet)
 *   --nuget-feed-path <path>       NuGet feed path (default: artifacts/nuget-feed)
 *   --nuget-feed-url <url>         Remote NuGet feed URL (required for publish-remote)
 *   --nuget-api-key <key>          API key for remote NuGet feed (required for publish-remote)
 *   --include-docs                 Include XML documentation in packages
 *   --embed-symbols                Embed PDB in .nupkg instead of a separate .snupkg (for the static alpha feed)
 *   --dry-run                      Show what would be published without publishing
 *   --clean                        Run dotnet clean before build
 *   --filter <expression>          Test filter expression (e.g., "FullyQualifiedName~MyTest").
 *                                  "A&B" = all terms, "A|B" = any term (alternatives of one kind);
 *                                  mixing & and | is rejected for xUnit v3 projects.
 *   --project <name>               Build/test specific project only (partial match)
 *   --integration                  Include integration tests (requires --project, unit tests only by default)
 *   --smoke                        Smoke test mode: skip Slow and RequiresUserPresence tests
 *   --fast                         Required fast mode for resilience gates
 *   --benchmark-args <args>         Arguments passed to BenchmarkDotNet
 *   --crap-args <args>             Arguments passed to crap.cs (e.g. "--module Piv --top 50")
 *   --complexity-args <args>       Arguments passed to complexity.cs (e.g. "--module Piv")
 *
 * EXAMPLES:
 *   dotnet toolchain.cs build
 *   dotnet toolchain.cs -- build --project Piv
 *   dotnet toolchain.cs test
 *   dotnet toolchain.cs docs-qa
 *   dotnet toolchain.cs -- test --filter "FullyQualifiedName~MyTestClass"
 *   dotnet toolchain.cs -- test --project Piv --filter "Method~Sign"
 *   dotnet toolchain.cs -- test --project WebAuthn.UnitTests --filter "FullyQualifiedName~YESDK1619|FullyQualifiedName~YESDK1613"
 *
 * RUNNING A UNIT PROJECT BY HAND:
 *   Use `dotnet run --project <proj>` or the built apphost (`bin/.../Yubico.YubiKit.X.UnitTests`),
 *   not `dotnet <proj>.dll`: Core's boundary-inventory and PC/SC-isolation probes re-launch the
 *   test host and resolve repo-relative paths from the entry assembly; under `dotnet X.dll` they
 *   mis-resolve and fail spuriously.
 *   dotnet toolchain.cs -- resilience --fast
 *   dotnet toolchain.cs -- benchmark --benchmark-args "--list flat"
 *   dotnet toolchain.cs -- test --integration --project Piv --smoke   (quick integration smoke test)
 *   dotnet toolchain.cs coverage
 *   dotnet toolchain.cs complexity
 *   dotnet toolchain.cs -- complexity --complexity-args "--module Piv"
 *   dotnet toolchain.cs -- publish --package-version 1.0.0-preview.1
 *   dotnet toolchain.cs -- publish-remote --nuget-feed-url https://nuget.pkg.github.com/Yubico/index.json --nuget-api-key $TOKEN
 *   dotnet toolchain.cs -- --help
 *
 * TEST TRAIT FILTERS:
 *   Tests are categorized with traits. Use --filter to include/exclude:
 *
 *   Categories:
 *     RequiresHardware      - Tests needing physical YubiKey connected
 *     RequiresUserPresence  - Tests needing user to insert/remove/touch device
 *     Slow                  - Tests taking >5 seconds
 *     Integration           - Tests exercising multiple components
 *
 *   Filter Examples:
 *     --filter "Category!=RequiresUserPresence"     Skip user presence tests (for CI/agents)
 *     --filter "Category!=RequiresHardware"         Skip hardware tests (unit tests only)
 *     --filter "Category!=Slow"                     Skip slow tests
 *     --filter "Category!=RequiresHardware&Category!=RequiresUserPresence&Category!=Slow"
 *                                                   Run only fast unit tests
 *
 *   AI AGENTS: Always exclude RequiresUserPresence tests (cannot insert/remove devices).
 *
 * XUNIT V2 VS V3 TEST RUNNER DETECTION:
 *   This script automatically detects which test runner each project uses:
 *
 *   - xUnit v3 (Microsoft.Testing.Platform): Projects with
 *     <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
 *     These use: dotnet run --project <proj> -- --filter-method/--filter-trait "..."
 *     VSTest --filter expressions are auto-translated to xUnit v3 native options
 *
 *   - xUnit v2 (traditional): Projects without that setting
 *     These use: dotnet test <proj> --filter "..."
 *
 *   IMPORTANT: Always use "dotnet toolchain.cs test" instead of invoking dotnet test
 *   directly. The build script handles this detection automatically, preventing
 *   failures from using the wrong command syntax for each test project.
 *
 * LAYOUT:
 *   This file only parses options and registers targets. The targets live in
 *   scripts/toolchain/*Targets.cs, the shared helpers beside them (see "Script Layout" in TOOLCHAIN.md).
 *
 * See TOOLCHAIN.md for full documentation.
 */

using static Bullseye.Targets;
using static Output;

var ctx = ToolchainContext.FromArgs(args);

if (args.Length == 1 && string.Equals(args[0], "docs-list-active", StringComparison.OrdinalIgnoreCase))
{
    PrintActiveDocumentationFiles(ctx.ActiveDocumentationFiles);
    return;
}

BuildTargets.Register(ctx);
TestTargets.Register(ctx);
CoverageTargets.Register(ctx);
BenchmarkTargets.Register(ctx);
DocsTargets.Register(ctx);
PackageTargets.Register(ctx);

Target("default", DependsOn("test", "publish"));

// Handle --help before Bullseye processes args
if (args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp(ctx.PackableProjects, ctx.TestProjects);
    return;
}

await RunTargetsAndExitAsync(ToolchainContext.FilterBullseyeArgs(args));
