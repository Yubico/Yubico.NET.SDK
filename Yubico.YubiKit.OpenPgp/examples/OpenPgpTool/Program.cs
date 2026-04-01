// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Menus;

// Application banner
AnsiConsole.Write(
    new FigletText("OpenPGP Tool")
        .LeftJustified()
        .Color(Color.Green));

AnsiConsole.MarkupLine("[grey]YubiKey OpenPGP Tool - SDK Example Application[/]");
AnsiConsole.WriteLine();

// Start monitoring for device events
YubiKeyManager.StartMonitoring();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Check for command-line arguments for automation
if (args.Length > 0)
{
    try
    {
        await RunCommandAsync(args, cts.Token);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
    }

    await YubiKeyManager.ShutdownAsync();
    return 1;
}

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
                "Card Info",
                "PIN Management",
                "Key Management",
                "Certificates",
                "Cryptographic Operations",
                "Configuration",
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
            case "Card Info":
                await InfoMenu.RunAsync(cts.Token);
                break;

            case "PIN Management":
                await PinMenu.RunAsync(cts.Token);
                break;

            case "Key Management":
                await KeyMenu.RunAsync(cts.Token);
                break;

            case "Certificates":
                await CertificateMenu.RunAsync(cts.Token);
                break;

            case "Cryptographic Operations":
                await CryptoMenu.RunAsync(cts.Token);
                break;

            case "Configuration":
                await ConfigMenu.RunAsync(cts.Token);
                break;

            case "Factory Reset":
                await ResetMenu.RunAsync(cts.Token);
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

// ── CLI argument routing ─────────────────────────────────────────────────
static async Task RunCommandAsync(string[] args, CancellationToken ct)
{
    var command = args[0].ToLowerInvariant();
    var subCommand = args.Length > 1 ? args[1].ToLowerInvariant() : null;

    switch (command)
    {
        case "info":
            await InfoMenu.RunAsync(ct);
            break;

        case "pin":
            switch (subCommand)
            {
                case "verify":
                    await PinMenu.RunVerifyAsync(ct);
                    break;
                case "change":
                    await PinMenu.RunChangeAsync(ct);
                    break;
                case "reset":
                    await PinMenu.RunResetAsync(ct);
                    break;
                default:
                    await PinMenu.RunAsync(ct);
                    break;
            }

            break;

        case "key":
            switch (subCommand)
            {
                case "generate":
                    await KeyMenu.RunGenerateAsync(ct);
                    break;
                case "delete":
                    await KeyMenu.RunDeleteAsync(ct);
                    break;
                case "attest":
                    await KeyMenu.RunAttestAsync(ct);
                    break;
                default:
                    await KeyMenu.RunAsync(ct);
                    break;
            }

            break;

        case "cert":
            switch (subCommand)
            {
                case "import":
                    await CertificateMenu.RunImportAsync(ct);
                    break;
                case "export":
                    await CertificateMenu.RunExportAsync(ct);
                    break;
                case "delete":
                    await CertificateMenu.RunDeleteAsync(ct);
                    break;
                default:
                    await CertificateMenu.RunAsync(ct);
                    break;
            }

            break;

        case "sign":
            await CryptoMenu.RunSignAsync(ct);
            break;

        case "decrypt":
            await CryptoMenu.RunDecryptAsync(ct);
            break;

        case "config":
            switch (subCommand)
            {
                case "uif":
                    await ConfigMenu.RunUifAsync(ct);
                    break;
                case "algorithm":
                    await ConfigMenu.RunAlgorithmAsync(ct);
                    break;
                case "kdf":
                    await ConfigMenu.RunKdfAsync(ct);
                    break;
                default:
                    await ConfigMenu.RunAsync(ct);
                    break;
            }

            break;

        case "reset":
            await ResetMenu.RunAsync(ct);
            break;

        default:
            AnsiConsole.MarkupLine($"[red]Unknown command: {Markup.Escape(command)}[/]");
            AnsiConsole.MarkupLine("[grey]Available commands: info, pin, key, cert, sign, decrypt, config, reset[/]");
            break;
    }
}
