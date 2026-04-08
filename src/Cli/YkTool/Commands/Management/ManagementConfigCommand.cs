// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.YkTool.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.YkTool.Commands.Management;

/// <summary>
///     Displays the current device configuration: USB/NFC enabled capabilities,
///     timeouts, device flags, lock status, and PIN complexity.
/// </summary>
public sealed class ManagementConfigCommand : YkCommandBase<GlobalSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];

    protected override Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        OutputHelpers.WriteHeader("Device Configuration");

        var info = deviceContext.Info;
        if (info is null)
        {
            OutputHelpers.WriteWarning("Could not retrieve device configuration.");
            return Task.FromResult(ExitCode.Success);
        }

        var deviceInfo = info.Value;

        OutputHelpers.WriteKeyValue("Serial Number", deviceInfo.SerialNumber?.ToString() ?? "N/A");
        OutputHelpers.WriteKeyValue("Firmware Version", deviceInfo.VersionName);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]USB Capabilities[/]");
        OutputHelpers.WriteKeyValue("Supported", FormatCapabilities(deviceInfo.UsbSupported));
        OutputHelpers.WriteKeyValue("Enabled", FormatCapabilities(deviceInfo.UsbEnabled));

        if (deviceInfo.NfcSupported != DeviceCapabilities.None)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]NFC Capabilities[/]");
            OutputHelpers.WriteKeyValue("Supported", FormatCapabilities(deviceInfo.NfcSupported));
            OutputHelpers.WriteKeyValue("Enabled", FormatCapabilities(deviceInfo.NfcEnabled));
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Timeouts[/]");
        OutputHelpers.WriteKeyValue("Auto-Eject Timeout",
            deviceInfo.AutoEjectTimeout > 0 ? $"{deviceInfo.AutoEjectTimeout} seconds" : "Disabled");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Device Flags[/]");
        OutputHelpers.WriteKeyValue("Flags", FormatDeviceFlags(deviceInfo.DeviceFlags));

        AnsiConsole.WriteLine();
        OutputHelpers.WriteBoolValue("Configuration Locked", deviceInfo.IsLocked);
        OutputHelpers.WriteBoolValue("PIN Complexity", deviceInfo.HasPinComplexity, "Enabled", "Disabled");
        OutputHelpers.WriteBoolValue("NFC Restricted", deviceInfo.IsNfcRestricted);

        if (deviceInfo.ResetBlocked != DeviceCapabilities.None)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("Reset Blocked", FormatCapabilities(deviceInfo.ResetBlocked));
        }

        return Task.FromResult(ExitCode.Success);
    }

    private static string FormatCapabilities(DeviceCapabilities capabilities) =>
        capabilities == DeviceCapabilities.None ? "None" : capabilities.ToString();

    private static string FormatDeviceFlags(DeviceFlags flags) =>
        flags == DeviceFlags.None ? "None" : flags.ToString();
}
