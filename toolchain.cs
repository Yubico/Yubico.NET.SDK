#!/usr/bin/env dotnet run

#:package Bullseye
#:package SimpleExec

#:include scripts/toolchain/Conventions.cs
#:include scripts/toolchain/Options.cs
#:include scripts/toolchain/Context.cs
#:include scripts/toolchain/Discovery.cs
#:include scripts/toolchain/Output.cs
#:include scripts/toolchain/Process.cs
#:include scripts/toolchain/Results.cs
#:include scripts/toolchain/TargetRegistry.cs
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
    return;
}

BuildTargets.Register(ctx);
TestTargets.Register(ctx);
CoverageTargets.Register(ctx);
BenchmarkTargets.Register(ctx);
DocsTargets.Register(ctx);
TargetRegistry.Describe("docs-list-active", "Print the exact active documentation set used by docs-qa (only as the sole argument)");
PackageTargets.Register(ctx);
TargetRegistry.Define("default", "Run tests and publish", ["test", "publish"]);

if (args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp(ctx);
    return;
}

var bullseyeArgs = parsedArgs.ForBullseye();

// Bullseye colours its own lines even when output is redirected; agents and CI logs read plain text.
if ((Console.IsOutputRedirected || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"))) &&
    !bullseyeArgs.Contains("--no-color"))
{
    bullseyeArgs = [.. bullseyeArgs, "--no-color"];
}

// Expected failures (failed tests or gates, bad usage, a failing dotnet command whose output is already
// above) print their message alone. Anything else is a toolchain bug and keeps its stack trace.
await Bullseye.Targets.RunTargetsAndExitAsync(
    bullseyeArgs,
    messageOnly: ex => ex is ToolchainException or SimpleExec.ExitCodeException);
