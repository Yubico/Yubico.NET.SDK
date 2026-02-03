// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Output;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

/// <summary>
/// CLI menu for comprehensive device information display.
/// </summary>
public static class DeviceInfoMenu
{
    public static async Task RunAsync(IYubiKeyManager manager, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manager);
        
        OutputHelpers.WriteHeader("Device Information");

        var selection = await DeviceSelector.SelectDeviceAsync(manager, cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await AnsiConsole.Status()
            .StartAsync("Getting device information...", async ctx =>
            {
                await using var session = await selection.Device.CreateManagementSessionAsync(cancellationToken: cancellationToken);

                var result = await DeviceInfoQuery.GetDeviceInfoAsync(session, cancellationToken);

                if (!result.Success || !result.DeviceInfo.HasValue)
                {
                    OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to get device info");
                    return;
                }

                DisplayDeviceDetails(result.DeviceInfo.Value);
            });
    }

    private static void DisplayDeviceDetails(DeviceInfo info)
    {
        // Basic device info
        AnsiConsole.MarkupLine("[bold]Basic Information[/]");
        OutputHelpers.WriteKeyValue("Serial Number", info.SerialNumber?.ToString());
        OutputHelpers.WriteKeyValue("Firmware Version", info.VersionName);
        OutputHelpers.WriteKeyValue("Form Factor", DeviceSelector.FormatFormFactor(info.FormFactor));

        if (info.PartNumber is not null)
        {
            OutputHelpers.WriteKeyValue("Part Number", info.PartNumber);
        }

        // Status flags
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Status[/]");

        if (info.IsFips)
        {
            OutputHelpers.WriteKeyValueMarkup("FIPS Mode", "[green]Yes[/]");
        }
        else
        {
            OutputHelpers.WriteKeyValue("FIPS Mode", "No");
        }

        if (info.IsSky)
        {
            OutputHelpers.WriteKeyValueMarkup("Security Key", "[blue]Yes[/]");
        }

        OutputHelpers.WriteBoolValue("Configuration Locked", info.IsLocked, "Locked", "Unlocked");
        OutputHelpers.WriteBoolValue("PIN Complexity", info.HasPinComplexity);
        OutputHelpers.WriteBoolValue("NFC Restricted", info.IsNfcRestricted);

        // USB Capabilities
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]USB Capabilities[/]");
        OutputHelpers.WriteCapabilities("Supported", info.UsbSupported);
        OutputHelpers.WriteCapabilities("Enabled", info.UsbEnabled);

        // NFC Capabilities (if supported)
        if (info.NfcSupported != DeviceCapabilities.None)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]NFC Capabilities[/]");
            OutputHelpers.WriteCapabilities("Supported", info.NfcSupported);
            OutputHelpers.WriteCapabilities("Enabled", info.NfcEnabled);
        }

        // FIPS Capabilities (if applicable)
        if (info.FipsCapabilities != DeviceCapabilities.None)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]FIPS Information[/]");
            OutputHelpers.WriteCapabilities("FIPS Capable", info.FipsCapabilities);
            OutputHelpers.WriteCapabilities("FIPS Approved", info.FipsApproved);
        }

        // Reset Blocked
        if (info.ResetBlocked != DeviceCapabilities.None)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Reset Blocked[/]");
            OutputHelpers.WriteCapabilities("Applications", info.ResetBlocked);
        }

        // Timeouts
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Timeouts[/]");
        OutputHelpers.WriteKeyValue("Auto-Eject Timeout", info.AutoEjectTimeout > 0 ? $"{info.AutoEjectTimeout} seconds" : "Disabled");

        // Device Flags
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Device Flags[/]");
        OutputHelpers.WriteDeviceFlags("Flags", info.DeviceFlags);

        // Firmware details
        if (info.FpsVersion is not null || info.StmVersion is not null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Component Versions[/]");

            if (info.FpsVersion is { } fpsVersion)
            {
                OutputHelpers.WriteKeyValue("FPS Version", fpsVersion.ToString());
            }

            if (info.StmVersion is { } stmVersion)
            {
                OutputHelpers.WriteKeyValue("STM Version", stmVersion.ToString());
            }
        }

        // Factory reset eligibility
        AnsiConsole.WriteLine();
        if (DeviceReset.IsResetSupported(info.FirmwareVersion))
        {
            OutputHelpers.WriteInfo("Factory reset is available on this device");
        }
        else
        {
            OutputHelpers.WriteWarning($"Factory reset requires firmware 5.6.0+ (current: {info.FirmwareVersion})");
        }
    }
}
