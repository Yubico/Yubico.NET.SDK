// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

/// <summary>
/// CLI menu for device information display.
/// </summary>
public static class DeviceInfoMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Device Information");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await AnsiConsole.Status()
            .StartAsync("Getting device information...", async ctx =>
            {
                // Get device info using the Management extension
                var deviceInfo = await selection.Device.GetDeviceInfoAsync(cancellationToken);
                DisplayDeviceDetails(deviceInfo);

                // Get PIV-specific retry info
                await using var session = await selection.Device.CreatePivSessionAsync(cancellationToken: cancellationToken);
                OutputHelpers.SetupTouchNotification(session);
                var retryResult = await DeviceInfoQuery.GetPivRetryInfoAsync(session, cancellationToken);
                
                if (retryResult.Success)
                {
                    AnsiConsole.WriteLine();
                    OutputHelpers.WriteKeyValue("PIN Retries", retryResult.PinRetriesRemaining?.ToString() ?? "Unknown");
                    OutputHelpers.WriteKeyValue("PUK Retries", retryResult.PukRetriesRemaining?.ToString() ?? "Unknown");
                }
            });
    }

    private static void DisplayDeviceDetails(Management.DeviceInfo info)
    {
        OutputHelpers.WriteKeyValue("Serial Number", info.SerialNumber?.ToString());
        OutputHelpers.WriteKeyValue("Firmware Version", info.VersionName);
        OutputHelpers.WriteKeyValue("Form Factor", DeviceSelector.FormatFormFactor(info.FormFactor));

        if (info.IsFips)
        {
            OutputHelpers.WriteKeyValueMarkup("FIPS Mode", "[green]Yes[/]");
        }

        if (info.IsSky)
        {
            OutputHelpers.WriteKeyValueMarkup("Security Key", "[blue]Yes[/]");
        }

        AnsiConsole.WriteLine();
        OutputHelpers.WriteKeyValue("USB Supported", FormatCapabilities(info.UsbSupported));
        OutputHelpers.WriteKeyValue("USB Enabled", FormatCapabilities(info.UsbEnabled));

        if (info.NfcSupported != DeviceCapabilities.None)
        {
            OutputHelpers.WriteKeyValue("NFC Supported", FormatCapabilities(info.NfcSupported));
            OutputHelpers.WriteKeyValue("NFC Enabled", FormatCapabilities(info.NfcEnabled));
        }

        AnsiConsole.WriteLine();
        if (!info.UsbSupported.HasFlag(DeviceCapabilities.Piv))
        {
            OutputHelpers.WriteError("This YubiKey does not support PIV.");
        }
        else
        {
            OutputHelpers.WriteSuccess("PIV application is supported");
        }
    }

    private static string FormatCapabilities(DeviceCapabilities caps)
    {
        if (caps == DeviceCapabilities.None)
        {
            return "None";
        }

        var parts = new List<string>();
        if (caps.HasFlag(DeviceCapabilities.Otp)) parts.Add("OTP");
        if (caps.HasFlag(DeviceCapabilities.U2f)) parts.Add("U2F");
        if (caps.HasFlag(DeviceCapabilities.Fido2)) parts.Add("FIDO2");
        if (caps.HasFlag(DeviceCapabilities.Oath)) parts.Add("OATH");
        if (caps.HasFlag(DeviceCapabilities.Piv)) parts.Add("PIV");
        if (caps.HasFlag(DeviceCapabilities.OpenPgp)) parts.Add("OpenPGP");
        if (caps.HasFlag(DeviceCapabilities.HsmAuth)) parts.Add("HSM");

        return string.Join(", ", parts);
    }
}
