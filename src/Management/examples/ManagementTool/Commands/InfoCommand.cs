// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Output;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Commands;

/// <summary>
/// Implements 'managementtool info' - displays device information.
/// </summary>
public static class InfoCommand
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
        OutputHelpers.WriteKeyValue("Form Factor", DeviceSelector.FormatFormFactor(info.FormFactor));

        if (info.PartNumber is not null)
        {
            OutputHelpers.WriteKeyValue("Part Number", info.PartNumber);
        }

        OutputHelpers.WriteKeyValue("FIPS Mode", info.IsFips ? "Yes" : "No");
        OutputHelpers.WriteKeyValue("Security Key", info.IsSky ? "Yes" : "No");
        OutputHelpers.WriteKeyValue("Configuration Locked", info.IsLocked ? "Yes" : "No");

        Console.WriteLine();
        OutputHelpers.WriteCapabilities("USB Supported", info.UsbSupported);
        OutputHelpers.WriteCapabilities("USB Enabled", info.UsbEnabled);

        if (info.NfcSupported != DeviceCapabilities.None)
        {
            Console.WriteLine();
            OutputHelpers.WriteCapabilities("NFC Supported", info.NfcSupported);
            OutputHelpers.WriteCapabilities("NFC Enabled", info.NfcEnabled);
        }

        if (info.FipsCapabilities != DeviceCapabilities.None)
        {
            Console.WriteLine();
            OutputHelpers.WriteCapabilities("FIPS Capable", info.FipsCapabilities);
            OutputHelpers.WriteCapabilities("FIPS Approved", info.FipsApproved);
        }

        return 0;
    }
}
