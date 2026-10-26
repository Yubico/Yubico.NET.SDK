#!/usr/bin/env dotnet run

#:package Bullseye

#:include scripts/toolchain/Conventions.cs
#:include scripts/toolchain/Options.cs
#:include scripts/toolchain/Context.cs
#:include scripts/toolchain/Discovery.cs
#:include scripts/toolchain/Output.cs
#:include scripts/toolchain/Process.cs
#:include scripts/toolchain/Results.cs
#:include scripts/toolchain/TargetRegistry.cs
#:include scripts/toolchain/RunLog.cs
#:include scripts/toolchain/Usage.cs
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
 *   dotnet toolchain.cs -- [target] [options]   (the -- is required before script long options)
 *   dotnet toolchain.cs -- --help               (targets, options, filter syntax, discovered projects)
 *
 * LAYOUT:
 *   This file only parses options and registers targets. The targets live in
 *   scripts/toolchain/*Targets.cs, the shared helpers beside them, and the option list in
 *   scripts/toolchain/Options.cs (see "Script Layout" in TOOLCHAIN.md).
 *
 * See TOOLCHAIN.md for full documentation.
 */

using static Output;

var parsedArgs = new ParsedArgs(args);
var ctx = ToolchainContext.FromArgs(parsedArgs);

if (args.Length == 1 && string.Equals(args[0], "docs-list-active", StringComparison.OrdinalIgnoreCase))
{
    PrintActiveDocumentationFiles(ctx.ActiveDocumentationFiles);
    return 0;
}

BuildTargets.Register(ctx);
TestTargets.Register(ctx);
CoverageTargets.Register(ctx);
BenchmarkTargets.Register(ctx);
DocsTargets.Register(ctx);
TargetRegistry.Describe("docs-list-active", "Print the exact active documentation set used by docs-qa (only as the sole argument)");
PackageTargets.Register(ctx);
TargetRegistry.Define("default", "Run tests and publish to the local feed (adds a NuGet source; run only when named)", ["test", "publish"]);

TargetRegistry.Describe("last", "Reprint the RESULT block of the newest saved run, without running anything (only as the sole argument)");

if (args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp(ctx);
    return 0;
}

if (args.Length == 1 && args[0] == "last")
    return RunLog.PrintLast(ctx.RepoRoot);

var bullseyeArgs = parsedArgs.ForBullseye();
var requestedTargets = bullseyeArgs.Where(a => !a.StartsWith('-')).ToArray();
var listing = bullseyeArgs.Any(Usage.ListingOptions.Contains);

// Bare `dotnet toolchain.cs` used to run `default` (test + publish, which edits nuget.config). Asking
// for a target explicitly keeps an exploratory run free of side effects.
if (requestedTargets.Length == 0 && !listing)
{
    PrintUsage();
    return 0;
}

RunLog.Start(ctx.RepoRoot, args, requestedTargets, parsedArgs.Has(Options.Verbose));

var usageError = Usage.Validate(args, [.. TargetRegistry.All.Select(t => t.Name)]);
if (usageError is not null)
    return RunLog.Finish(usageError);

// Bullseye colours its own lines even when output is redirected; agents and CI logs read plain text.
if ((Console.IsOutputRedirected || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"))) &&
    !bullseyeArgs.Contains("--no-color"))
{
    bullseyeArgs = [.. bullseyeArgs, "--no-color"];
}

// When targets run, the toolchain reports progress and the RESULT block summarises the run, so
// Bullseye's own progress lines and summary table would only repeat it. Listings keep its output.
var bullseyeOutput = listing ? Console.Out : TextWriter.Null;
try
{
    await Bullseye.Targets.RunTargetsWithoutExitingAsync(
        bullseyeArgs, messageOnly: ex => ex is ToolchainException, outputWriter: bullseyeOutput);
}
catch (Bullseye.InvalidUsageException ex)
{
    usageError = ex.Message;
}
catch (Bullseye.TargetFailedException)
{
    // The failing target was reported when it failed; the RESULT block below summarises the run.
}

return RunLog.Finish(usageError);
