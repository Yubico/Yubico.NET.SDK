// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Output;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

/// <summary>
/// CLI menu for factory reset operation.
/// </summary>
public static class ResetMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Factory Reset");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateManagementSessionAsync(cancellationToken: cancellationToken);

        var infoResult = await DeviceInfoQuery.GetDeviceInfoAsync(session, cancellationToken);
        if (!infoResult.Success || !infoResult.DeviceInfo.HasValue)
        {
            OutputHelpers.WriteError(infoResult.ErrorMessage ?? "Failed to get device info");
            return;
        }

        var info = infoResult.DeviceInfo.Value;

        // Check firmware version
        if (!DeviceReset.IsResetSupported(info.FirmwareVersion))
        {
            OutputHelpers.WriteError($"Factory reset requires firmware 5.6.0 or later.");
            OutputHelpers.WriteKeyValue("Current firmware", info.FirmwareVersion.ToString());
            OutputHelpers.WriteKeyValue("Required firmware", DeviceReset.MinimumResetVersion.ToString());
            return;
        }

        // Display device info and what will be lost
        AnsiConsole.MarkupLine("[bold]Device to Reset[/]");
        OutputHelpers.WriteKeyValue("Serial Number", info.SerialNumber?.ToString());
        OutputHelpers.WriteKeyValue("Firmware Version", info.FirmwareVersion.ToString());
        OutputHelpers.WriteKeyValue("Form Factor", DeviceSelector.FormatFormFactor(info.FormFactor));
        AnsiConsole.WriteLine();

        // Severe warning
        AnsiConsole.MarkupLine("[red bold]╔══════════════════════════════════════════════════════════════╗[/]");
        AnsiConsole.MarkupLine("[red bold]║                    ⚠️  EXTREME WARNING ⚠️                     ║[/]");
        AnsiConsole.MarkupLine("[red bold]╠══════════════════════════════════════════════════════════════╣[/]");
        AnsiConsole.MarkupLine("[red bold]║  This operation will PERMANENTLY DESTROY all data including: ║[/]");
        AnsiConsole.MarkupLine("[red bold]║                                                              ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • All PIV keys and certificates                             ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • All FIDO2/WebAuthn credentials                            ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • All OATH (TOTP/HOTP) accounts                             ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • All OpenPGP keys                                          ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • All Yubico OTP configurations                             ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • The configuration lock code                               ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • All device configuration settings                         ║[/]");
        AnsiConsole.MarkupLine("[red bold]║                                                              ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  THIS CANNOT BE UNDONE.                                      ║[/]");
        AnsiConsole.MarkupLine("[red bold]╚══════════════════════════════════════════════════════════════╝[/]");
        AnsiConsole.WriteLine();

        // Double confirmation
        if (!OutputHelpers.ConfirmDestructive("completely wipe this YubiKey and reset it to factory defaults"))
        {
            OutputHelpers.WriteInfo("Factory reset cancelled");
            return;
        }

        // Execute reset
        AnsiConsole.WriteLine();
        OutputHelpers.WriteInfo("Performing factory reset...");

        var result = await DeviceReset.ResetDeviceAsync(session, info, cancellationToken);

        if (result.Success)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteSuccess("Factory reset completed successfully");
            OutputHelpers.WriteInfo("The YubiKey has been reset to factory defaults.");
            OutputHelpers.WriteWarning("All PINs and PUKs are now set to their default values.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to reset device");
        }
    }
}
