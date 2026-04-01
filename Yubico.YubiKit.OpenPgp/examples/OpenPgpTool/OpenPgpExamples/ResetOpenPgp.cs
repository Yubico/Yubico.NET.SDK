// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates factory reset of the OpenPGP application.
/// This blocks both PINs, terminates, and reactivates the applet.
/// </summary>
public static class ResetOpenPgp
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        // Show current card state
        var pinStatus = await session.GetPinStatusAsync(cancellationToken);

        AnsiConsole.MarkupLine("[bold]Current PIN Status[/]");
        OutputHelpers.WriteKeyValue("User PIN Attempts", pinStatus.AttemptsUser.ToString());
        OutputHelpers.WriteKeyValue("Reset Code Attempts", pinStatus.AttemptsReset.ToString());
        OutputHelpers.WriteKeyValue("Admin PIN Attempts", pinStatus.AttemptsAdmin.ToString());
        AnsiConsole.WriteLine();

        // Severe warning
        AnsiConsole.MarkupLine("[red bold]This operation will:[/]");
        AnsiConsole.MarkupLine("[red]  - Delete ALL OpenPGP private keys[/]");
        AnsiConsole.MarkupLine("[red]  - Delete ALL stored certificates[/]");
        AnsiConsole.MarkupLine("[red]  - Reset PINs to factory defaults (User: 123456, Admin: 12345678)[/]");
        AnsiConsole.MarkupLine("[red]  - Clear all fingerprints and metadata[/]");
        AnsiConsole.MarkupLine("[red]  - Reset algorithm attributes to defaults[/]");
        AnsiConsole.WriteLine();

        if (!OutputHelpers.ConfirmDestructive(
                "factory reset the OpenPGP application, permanently destroying all keys and data"))
        {
            OutputHelpers.WriteInfo("Factory reset cancelled");
            return;
        }

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Resetting OpenPGP application...", async _ =>
                {
                    await session.ResetAsync(cancellationToken);
                });

            AnsiConsole.WriteLine();
            OutputHelpers.WriteSuccess("OpenPGP application reset to factory defaults");
            OutputHelpers.WriteInfo("Default User PIN: 123456");
            OutputHelpers.WriteInfo("Default Admin PIN: 12345678");
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteError("Factory reset requires firmware 1.0.6 or later");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Factory reset failed: {ex.Message}");
        }
    }
}
