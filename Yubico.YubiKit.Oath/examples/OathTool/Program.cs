// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Menus;

// Application banner
AnsiConsole.Write(
    new FigletText("OATH Tool")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[grey]YubiKey OATH Tool - TOTP/HOTP Credential Manager[/]");
AnsiConsole.WriteLine();

// Check for CLI arguments for non-interactive mode
if (args.Length > 0)
{
    await RunCommandAsync(args);
    return 0;
}

// Start monitoring for device events
YubiKeyManager.StartMonitoring();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Main menu loop
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
                "📋 List Credentials",
                "➕ Add Credential",
                "🔢 Calculate Code",
                "🔢 Calculate All Codes",
                "🗑️  Delete Credential",
                "✏️  Rename Credential",
                "🔒 Set Password",
                "🔓 Remove Password",
                "⚠️  Reset OATH",
                "❌ Exit"
            ])
            .ShowAsync(AnsiConsole.Console, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    if (choice == "❌ Exit")
    {
        AnsiConsole.MarkupLine("[grey]Goodbye![/]");
        break;
    }

    try
    {
        switch (choice)
        {
            case "📋 List Credentials":
                await CredentialMenu.ListAsync(cts.Token);
                break;

            case "➕ Add Credential":
                await CredentialMenu.AddAsync(cancellationToken: cts.Token);
                break;

            case "🔢 Calculate Code":
                await CodeMenu.CalculateAsync(cancellationToken: cts.Token);
                break;

            case "🔢 Calculate All Codes":
                await CodeMenu.CalculateAllAsync(cts.Token);
                break;

            case "🗑️  Delete Credential":
                await CredentialMenu.DeleteAsync(cancellationToken: cts.Token);
                break;

            case "✏️  Rename Credential":
                await CredentialMenu.RenameAsync(cancellationToken: cts.Token);
                break;

            case "🔒 Set Password":
                await AccessKeyMenu.SetKeyAsync(cts.Token);
                break;

            case "🔓 Remove Password":
                await AccessKeyMenu.UnsetKeyAsync(cts.Token);
                break;

            case "⚠️  Reset OATH":
                await AccessKeyMenu.ResetAsync(cts.Token);
                break;

            default:
                AnsiConsole.MarkupLine($"[yellow]Selected: {choice} - Not yet implemented[/]");
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

// --- CLI argument handling ---

static async Task RunCommandAsync(string[] args)
{
    var command = args[0].ToLowerInvariant();

    switch (command)
    {
        case "list":
            await CredentialMenu.ListAsync();
            break;

        case "add":
            var uri = GetArgValue(args, "--uri");
            if (uri is null)
            {
                AnsiConsole.MarkupLine("[red]Usage: OathTool add --uri \"otpauth://...\"[/]");
                return;
            }

            await CredentialMenu.AddAsync(uri);
            break;

        case "calculate":
            var calcName = GetArgValue(args, "--name");
            await CodeMenu.CalculateAsync(calcName);
            break;

        case "calculate-all":
            await CodeMenu.CalculateAllAsync();
            break;

        case "delete":
            var deleteName = GetArgValue(args, "--name");
            await CredentialMenu.DeleteAsync(deleteName);
            break;

        case "rename":
            var oldName = GetArgValue(args, "--old");
            var newName = GetArgValue(args, "--new");
            await CredentialMenu.RenameAsync(oldName, newName);
            break;

        case "set-key":
            await AccessKeyMenu.SetKeyAsync();
            break;

        case "unset-key":
            await AccessKeyMenu.UnsetKeyAsync();
            break;

        case "reset":
            await AccessKeyMenu.ResetAsync();
            break;

        default:
            AnsiConsole.MarkupLine($"[red]Unknown command: {Markup.Escape(command)}[/]");
            PrintUsage();
            break;
    }
}

static string? GetArgValue(string[] args, string flag)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static void PrintUsage()
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Usage:[/]");
    AnsiConsole.MarkupLine("  OathTool                                    Interactive mode");
    AnsiConsole.MarkupLine("  OathTool list                               List all credentials");
    AnsiConsole.MarkupLine("  OathTool add --uri \"otpauth://...\"           Add a credential from URI");
    AnsiConsole.MarkupLine("  OathTool calculate --name \"GitHub\"           Calculate a single code");
    AnsiConsole.MarkupLine("  OathTool calculate-all                      Calculate all codes");
    AnsiConsole.MarkupLine("  OathTool delete --name \"GitHub\"              Delete a credential");
    AnsiConsole.MarkupLine("  OathTool rename --old \"GitHub\" --new \"GH\"    Rename a credential");
    AnsiConsole.MarkupLine("  OathTool set-key                            Set OATH password");
    AnsiConsole.MarkupLine("  OathTool unset-key                          Remove OATH password");
    AnsiConsole.MarkupLine("  OathTool reset                              Reset OATH application");
}
