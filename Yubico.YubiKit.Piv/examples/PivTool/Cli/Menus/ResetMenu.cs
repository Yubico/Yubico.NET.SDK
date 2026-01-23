// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

/// <summary>
/// CLI menu for PIV application reset.
/// </summary>
public static class ResetMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Reset PIV Application");

        OutputHelpers.WriteError("WARNING: This will delete ALL PIV data including:");
        AnsiConsole.MarkupLine("  • All private keys");
        AnsiConsole.MarkupLine("  • All certificates");
        AnsiConsole.MarkupLine("  • PIN, PUK, and management key will be reset to defaults");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("[red]Are you sure you want to proceed?[/]", defaultValue: false))
        {
            OutputHelpers.WriteInfo("Reset cancelled");
            return;
        }

        AnsiConsole.WriteLine();
        OutputHelpers.WriteInfo("To reset, you must first block the PIN and PUK.");
        OutputHelpers.WriteInfo("This requires entering wrong values 3+ times each.");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Continue with reset process?", defaultValue: false))
        {
            OutputHelpers.WriteInfo("Reset cancelled");
            return;
        }

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);
        OutputHelpers.SetupTouchNotification(session);

        await AnsiConsole.Status()
            .StartAsync("Resetting PIV application...", async ctx =>
            {
                var result = await Reset.ResetPivApplicationAsync(session, cancellationToken);

                if (result.Success)
                {
                    OutputHelpers.WriteSuccess("PIV application has been reset to factory defaults");
                    AnsiConsole.WriteLine();
                    OutputHelpers.WriteInfo("Default credentials:");
                    OutputHelpers.WriteKeyValue("PIN", "123456");
                    OutputHelpers.WriteKeyValue("PUK", "12345678");
                    OutputHelpers.WriteKeyValue("Management Key", "010203040506070801020304050607080102030405060708");
                }
                else
                {
                    OutputHelpers.WriteError(result.ErrorMessage ?? "Reset failed");
                }
            });
    }
}
