// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Commands;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Menus;

// ── Non-interactive CLI mode ─────────────────────────────────────────────────
if (args.Length > 0)
{
    return await RunCommandAsync(args);
}

// ── Interactive mode ─────────────────────────────────────────────────────────

AnsiConsole.Write(
    new FigletText("HsmAuth Tool")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[grey]YubiHSM Auth Tool - SDK Example Application[/]");
AnsiConsole.MarkupLine("[grey]Run with --help for non-interactive usage.[/]");
AnsiConsole.WriteLine();

YubiKeyManager.StartMonitoring();

using var cts = CommandHelper.CreateConsoleCts();

var exitCode = await InteractiveMenuBuilder.Create("What would you like to do?")
    .AddItem("Info", ct => InfoCommand.RunAsync([], ct))
    .AddItem("List Credentials", ct => CredentialsCommand.RunAsync(["credentials", "list"], ct))
    .AddItem("Add Symmetric Credential", ct => CredentialMenu.AddSymmetricAsync(ct))
    .AddItem("Add Derived Credential", ct => CredentialMenu.AddDerivedAsync(ct))
    .AddItem("Delete Credential", ct => CredentialMenu.DeleteAsync(ct))
    .AddItem("Generate Asymmetric Credential", ct => CredentialMenu.GenerateAsync(ct))
    .AddItem("Calculate Session Keys", ct => SessionKeyMenu.RunAsync(ct))
    .AddItem("Change Management Key", ct => ManagementKeyMenu.RunAsync(ct))
    .AddItem("Get Management Key Retries", ct => ManagementKeyMenu.GetRetriesAsync(ct))
    .AddItem("Factory Reset", ct => ResetMenu.RunAsync(ct))
    .RunAsync(cts.Token);

await YubiKeyManager.ShutdownAsync();

return exitCode;

// ── CLI command dispatch ─────────────────────────────────────────────────────

static async Task<int> RunCommandAsync(string[] args)
{
    if (CommandArgs.HasFlag(args, "-h", "--help"))
    {
        PrintUsage();
        return 0;
    }

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    try
    {
        return args[0].ToLowerInvariant() switch
        {
            "info" => await InfoCommand.RunAsync(args, cts.Token),
            "reset" => await ResetCommand.RunAsync(args, cts.Token),
            "access" => await AccessCommand.RunAsync(args, cts.Token),
            "credentials" => await CredentialsCommand.RunAsync(args, cts.Token),
            "-h" or "--help" => PrintUsageAndReturn(),
            _ => PrintUnknownCommand(args[0])
        };
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        return 1;
    }
}

static int PrintUsageAndReturn()
{
    PrintUsage();
    return 0;
}

static int PrintUnknownCommand(string command)
{
    AnsiConsole.MarkupLine($"[red]Unknown command: {Markup.Escape(command)}[/]");
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Usage:[/] HsmAuthTool <command> [options]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Commands:[/]");
    AnsiConsole.MarkupLine("  info                                  Display general status");
    AnsiConsole.MarkupLine("  reset [-f]                            Reset all data");
    AnsiConsole.MarkupLine("  access change-management-key          Change the management key");
    AnsiConsole.MarkupLine("  credentials list                      List all credentials");
    AnsiConsole.MarkupLine("  credentials add LABEL [options]       Add a credential");
    AnsiConsole.MarkupLine("  credentials delete LABEL [-f]         Delete a credential");
    AnsiConsole.MarkupLine("  credentials generate LABEL [options]  Generate asymmetric credential");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Global options:[/]");
    AnsiConsole.MarkupLine("  -h, --help                            Show this help message");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Run without arguments for interactive mode.[/]");
}
