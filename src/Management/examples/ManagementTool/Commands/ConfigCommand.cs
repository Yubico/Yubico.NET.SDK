// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Output;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Commands;

/// <summary>
/// Implements 'managementtool config' - displays current device configuration.
/// </summary>
public static class ConfigCommand
{
    public static async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            Console.Error.WriteLine("No YubiKey detected.");
            return 1;
        }

        await using var session = await selection.Device.CreateManagementSessionAsync(
            cancellationToken: cancellationToken);

        var result = await DeviceInfoQuery.GetDeviceInfoAsync(session, cancellationToken);

        if (!result.Success || !result.DeviceInfo.HasValue)
        {
            Console.Error.WriteLine(result.ErrorMessage ?? "Failed to get device info");
            return 1;
        }

        var info = result.DeviceInfo.Value;

        OutputHelpers.WriteKeyValue("Serial Number", info.SerialNumber?.ToString() ?? "N/A");
        OutputHelpers.WriteKeyValue("Firmware Version", info.VersionName);

        Console.WriteLine();
        Console.WriteLine("USB Capabilities:");
        OutputHelpers.WriteCapabilities("  Supported", info.UsbSupported);
        OutputHelpers.WriteCapabilities("  Enabled", info.UsbEnabled);

        if (info.NfcSupported != DeviceCapabilities.None)
        {
            Console.WriteLine();
            Console.WriteLine("NFC Capabilities:");
            OutputHelpers.WriteCapabilities("  Supported", info.NfcSupported);
            OutputHelpers.WriteCapabilities("  Enabled", info.NfcEnabled);
        }

        Console.WriteLine();
        Console.WriteLine("Timeouts:");
        OutputHelpers.WriteKeyValue("  Auto-Eject Timeout",
            info.AutoEjectTimeout > 0 ? $"{info.AutoEjectTimeout} seconds" : "Disabled");

        Console.WriteLine();
        Console.WriteLine("Device Flags:");
        OutputHelpers.WriteDeviceFlags("  Flags", info.DeviceFlags);

        Console.WriteLine();
        OutputHelpers.WriteKeyValue("Configuration Locked", info.IsLocked ? "Yes" : "No");
        OutputHelpers.WriteKeyValue("PIN Complexity", info.HasPinComplexity ? "Enabled" : "Disabled");
        OutputHelpers.WriteKeyValue("NFC Restricted", info.IsNfcRestricted ? "Yes" : "No");

        if (info.ResetBlocked != DeviceCapabilities.None)
        {
            Console.WriteLine();
            OutputHelpers.WriteCapabilities("Reset Blocked", info.ResetBlocked);
        }

        return 0;
    }
}
