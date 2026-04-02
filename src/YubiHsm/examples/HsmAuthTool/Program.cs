// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
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

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

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
                "Info",
                "List Credentials",
                "Add Symmetric Credential",
                "Add Derived Credential",
                "Delete Credential",
                "Generate Asymmetric Credential",
                "Calculate Session Keys",
                "Change Management Key",
                "Get Management Key Retries",
                "Factory Reset",
                "Exit"
            ])
            .ShowAsync(AnsiConsole.Console, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    if (choice == "Exit")
    {
        AnsiConsole.MarkupLine("[grey]Goodbye![/]");
        break;
    }

    try
    {
        switch (choice)
        {
            case "Info":
                await InfoCommand.RunAsync([], cts.Token);
                break;

            case "List Credentials":
                await CredentialsCommand.RunAsync(["credentials", "list"], cts.Token);
                break;

            case "Add Symmetric Credential":
                await CredentialMenu.AddSymmetricAsync(cts.Token);
                break;

            case "Add Derived Credential":
                await CredentialMenu.AddDerivedAsync(cts.Token);
                break;

            case "Delete Credential":
                await CredentialMenu.DeleteAsync(cts.Token);
                break;

            case "Generate Asymmetric Credential":
                await CredentialMenu.GenerateAsync(cts.Token);
                break;

            case "Calculate Session Keys":
                await SessionKeyMenu.RunAsync(cts.Token);
                break;

            case "Change Management Key":
                await ManagementKeyMenu.RunAsync(cts.Token);
                break;

            case "Get Management Key Retries":
                await ManagementKeyMenu.GetRetriesAsync(cts.Token);
                break;

            case "Factory Reset":
                await ResetMenu.RunAsync(cts.Token);
                break;
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
    }

    AnsiConsole.WriteLine();
}

await YubiKeyManager.ShutdownAsync();

return 0;

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
