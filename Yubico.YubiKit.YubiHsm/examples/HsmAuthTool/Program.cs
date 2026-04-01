// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Menus;

// Application banner
AnsiConsole.Write(
    new FigletText("HsmAuth Tool")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[grey]YubiHSM Auth Tool - SDK Example Application[/]");
AnsiConsole.WriteLine();

// Check for command-line subcommands
if (args.Length > 0)
{
    var exitCode = await RunSubcommandAsync(args);
    return exitCode;
}

// Start monitoring for device events
YubiKeyManager.StartMonitoring();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Interactive menu loop
while (!cts.Token.IsCancellationRequested)
{
    string choice;
    try
    {
        choice = await new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .PageSize(15)
            .AddChoices(
            [
                "\ud83d\udcdd List Credentials",
                "\u2795 Add Symmetric Credential",
                "\u2795 Add Derived Credential",
                "\u274c Delete Credential",
                "\ud83d\udd11 Calculate Session Keys",
                "\ud83d\udd12 Change Management Key",
                "\ud83d\udd22 Get Management Key Retries",
                "\u26a0\ufe0f  Factory Reset",
                "\u274c Exit"
            ])
            .ShowAsync(AnsiConsole.Console, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    if (choice == "\u274c Exit")
    {
        AnsiConsole.MarkupLine("[grey]Goodbye![/]");
        break;
    }

    try
    {
        switch (choice)
        {
            case "\ud83d\udcdd List Credentials":
                await CredentialMenu.ListAsync(cts.Token);
                break;

            case "\u2795 Add Symmetric Credential":
                await CredentialMenu.AddSymmetricAsync(cts.Token);
                break;

            case "\u2795 Add Derived Credential":
                await CredentialMenu.AddDerivedAsync(cts.Token);
                break;

            case "\u274c Delete Credential":
                await CredentialMenu.DeleteAsync(cts.Token);
                break;

            case "\ud83d\udd11 Calculate Session Keys":
                await SessionKeyMenu.RunAsync(cts.Token);
                break;

            case "\ud83d\udd12 Change Management Key":
                await ManagementKeyMenu.RunAsync(cts.Token);
                break;

            case "\ud83d\udd22 Get Management Key Retries":
                await ManagementKeyMenu.GetRetriesAsync(cts.Token);
                break;

            case "\u26a0\ufe0f  Factory Reset":
                await ResetMenu.RunAsync(cts.Token);
                break;

            default:
                AnsiConsole.MarkupLine($"[yellow]Selected: {choice} - Not yet implemented[/]");
                break;
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
    }

    AnsiConsole.WriteLine();
}

await YubiKeyManager.ShutdownAsync();

return 0;

// ── Command-line subcommand dispatch ──────────────────────────────────────────

static async Task<int> RunSubcommandAsync(string[] args)
{
    try
    {
        switch (args[0].ToLowerInvariant())
        {
            case "list":
                await CredentialMenu.ListAsync(CancellationToken.None);
                return 0;

            case "add-symmetric":
                await CredentialMenu.AddSymmetricAsync(CancellationToken.None);
                return 0;

            case "add-derived":
                await CredentialMenu.AddDerivedAsync(CancellationToken.None);
                return 0;

            case "delete":
                await CredentialMenu.DeleteAsync(CancellationToken.None);
                return 0;

            case "calc-session-keys":
                await SessionKeyMenu.RunAsync(CancellationToken.None);
                return 0;

            case "change-mgmt-key":
                await ManagementKeyMenu.RunAsync(CancellationToken.None);
                return 0;

            case "retries":
                await ManagementKeyMenu.GetRetriesAsync(CancellationToken.None);
                return 0;

            case "reset":
                await ResetMenu.RunAsync(CancellationToken.None);
                return 0;

            default:
                AnsiConsole.MarkupLine($"[red]Unknown subcommand: {args[0]}[/]");
                PrintUsage();
                return 1;
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        return 1;
    }
}

static void PrintUsage()
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Usage:[/] HsmAuthTool [[subcommand]]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Subcommands:[/]");
    AnsiConsole.MarkupLine("  list               List all credentials");
    AnsiConsole.MarkupLine("  add-symmetric      Add a symmetric (AES-128) credential");
    AnsiConsole.MarkupLine("  add-derived        Add a PBKDF2-derived credential");
    AnsiConsole.MarkupLine("  delete             Delete a credential");
    AnsiConsole.MarkupLine("  calc-session-keys  Calculate session keys");
    AnsiConsole.MarkupLine("  change-mgmt-key   Change the management key");
    AnsiConsole.MarkupLine("  retries            Get management key retries remaining");
    AnsiConsole.MarkupLine("  reset              Factory reset the HsmAuth applet");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Run without arguments for interactive mode.[/]");
}
