// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Commands.Management;

/// <summary>
///     Displays comprehensive device information: part number, firmware version,
///     serial number, capabilities, FIPS status, and lock status.
/// </summary>
public sealed class ManagementInfoCommand : YkCommandBase<GlobalSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];

    protected override Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        OutputHelpers.WriteHeader("Device Information");

        var info = deviceContext.Info;
        if (info is null)
        {
            OutputHelpers.WriteWarning("Could not retrieve full device information.");
            return Task.FromResult(ExitCode.Success);
        }

        var deviceInfo = info.Value;

        OutputHelpers.WriteKeyValue("Part Number", deviceInfo.PartNumber ?? "Unknown");
        OutputHelpers.WriteKeyValue("Firmware Version", deviceInfo.VersionName);
        OutputHelpers.WriteKeyValue("Serial Number", deviceInfo.SerialNumber?.ToString() ?? "N/A");
        OutputHelpers.WriteKeyValue("Form Factor", deviceContext.Selection.FormFactor.ToString());
        OutputHelpers.WriteBoolValue("FIPS Device", deviceInfo.IsFips);
        OutputHelpers.WriteBoolValue("Security Key", deviceInfo.IsSky);
        OutputHelpers.WriteBoolValue("Configuration Locked", deviceInfo.IsLocked);

        AnsiConsole.WriteLine();
        OutputHelpers.WriteKeyValue("USB Supported", FormatCapabilities(deviceInfo.UsbSupported));
        OutputHelpers.WriteKeyValue("USB Enabled", FormatCapabilities(deviceInfo.UsbEnabled));

        if (deviceInfo.NfcSupported != DeviceCapabilities.None)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("NFC Supported", FormatCapabilities(deviceInfo.NfcSupported));
            OutputHelpers.WriteKeyValue("NFC Enabled", FormatCapabilities(deviceInfo.NfcEnabled));
        }

        if (deviceInfo.FipsCapabilities != DeviceCapabilities.None)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("FIPS Capable", FormatCapabilities(deviceInfo.FipsCapabilities));
            OutputHelpers.WriteKeyValue("FIPS Approved", FormatCapabilities(deviceInfo.FipsApproved));
        }

        return Task.FromResult(ExitCode.Success);
    }

    private static string FormatCapabilities(DeviceCapabilities capabilities) =>
        capabilities == DeviceCapabilities.None ? "None" : capabilities.ToString();
}
