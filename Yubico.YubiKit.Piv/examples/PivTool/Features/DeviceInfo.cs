// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Spectre.Console;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Features;

/// <summary>
/// Handles device enumeration and information display.
/// </summary>
public static class DeviceInfoFeature
{
    /// <summary>
    /// Runs the device info feature.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Device Information");

        var devices = await FindDevicesAsync(cancellationToken);
        if (devices.Count == 0)
        {
            return;
        }

        if (devices.Count == 1)
        {
            await DisplayDeviceInfoAsync(devices[0], cancellationToken);
        }
        else
        {
            await DisplayMultipleDevicesAsync(devices, cancellationToken);
        }
    }

    /// <summary>
    /// Finds all connected YubiKeys.
    /// </summary>
    private static async Task<IReadOnlyList<IYubiKey>> FindDevicesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<IYubiKey>? devices = null;

        await AnsiConsole.Status()
            .StartAsync("Scanning for YubiKeys...", async ctx =>
            {
                devices = await YubiKey.FindAllAsync(cancellationToken);
            });

        if (devices is null || devices.Count == 0)
        {
            OutputHelpers.WriteError("No YubiKey detected. Please insert a YubiKey and try again.");
            return [];
        }

        return devices;
    }

    /// <summary>
    /// Displays information for a single device.
    /// </summary>
    private static async Task DisplayDeviceInfoAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        try
        {
            // Get device info directly using extension method
            var deviceInfo = await device.GetDeviceInfoAsync(cancellationToken);
            DisplayDeviceDetails(deviceInfo);

            // Get PIV-specific info
            await DisplayPivInfoAsync(device, cancellationToken);
        }
        catch (Exception ex)
        {
            HandleDeviceError(ex);
        }
    }

    /// <summary>
    /// Displays information for multiple devices.
    /// </summary>
    private static async Task DisplayMultipleDevicesAsync(
        IReadOnlyList<IYubiKey> devices,
        CancellationToken cancellationToken)
    {
        OutputHelpers.WriteInfo($"Found {devices.Count} YubiKey(s)");
        AnsiConsole.WriteLine();

        var table = OutputHelpers.CreateTable("Device", "Serial", "Firmware", "Form Factor", "PIV Support");

        foreach (var device in devices)
        {
            await AddDeviceToTableAsync(table, device, cancellationToken);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Offer to show details for a specific device
        if (AnsiConsole.Confirm("View detailed information for a device?", defaultValue: false))
        {
            var selectedDevice = await DeviceSelector.SelectDeviceAsync(cancellationToken);
            if (selectedDevice is not null)
            {
                AnsiConsole.WriteLine();
                await DisplayDeviceInfoAsync(selectedDevice, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Adds a device row to the table.
    /// </summary>
    private static async Task AddDeviceToTableAsync(
        Table table,
        IYubiKey device,
        CancellationToken cancellationToken)
    {
        try
        {
            var deviceInfo = await device.GetDeviceInfoAsync(cancellationToken);

            var pivSupport = deviceInfo.UsbSupported.HasFlag(DeviceCapabilities.Piv)
                ? "[green]Yes[/]"
                : "[red]No[/]";

            table.AddRow(
                device.DeviceId[..Math.Min(8, device.DeviceId.Length)] + "...",
                deviceInfo.SerialNumber?.ToString() ?? "N/A",
                deviceInfo.FirmwareVersion.ToString(),
                DeviceSelector.FormatFormFactor(deviceInfo.FormFactor),
                pivSupport);
        }
        catch
        {
            table.AddRow(
                device.DeviceId[..Math.Min(8, device.DeviceId.Length)] + "...",
                "Error",
                "Error",
                "Unknown",
                "[red]Error[/]");
        }
    }

    /// <summary>
    /// Displays detailed device information.
    /// </summary>
    private static void DisplayDeviceDetails(DeviceInfo info)
    {
        OutputHelpers.WriteKeyValue("Serial Number", info.SerialNumber?.ToString());
        OutputHelpers.WriteKeyValue("Firmware Version", info.VersionName);
        OutputHelpers.WriteKeyValue("Form Factor", DeviceSelector.FormatFormFactor(info.FormFactor));

        // Flags
        if (info.IsFips)
        {
            OutputHelpers.WriteKeyValueMarkup("FIPS Mode", "[green]Yes[/]");
        }

        if (info.IsSky)
        {
            OutputHelpers.WriteKeyValueMarkup("Security Key", "[blue]Yes[/]");
        }

        // Capabilities
        AnsiConsole.WriteLine();
        OutputHelpers.WriteKeyValue("USB Supported", FormatCapabilities(info.UsbSupported));
        OutputHelpers.WriteKeyValue("USB Enabled", FormatCapabilities(info.UsbEnabled));

        if (info.NfcSupported != DeviceCapabilities.None)
        {
            OutputHelpers.WriteKeyValue("NFC Supported", FormatCapabilities(info.NfcSupported));
            OutputHelpers.WriteKeyValue("NFC Enabled", FormatCapabilities(info.NfcEnabled));
        }

        // PIV-specific capability check
        AnsiConsole.WriteLine();
        if (!info.UsbSupported.HasFlag(DeviceCapabilities.Piv))
        {
            OutputHelpers.WriteError("This YubiKey does not support PIV. Minimum firmware: 4.0");
        }
        else
        {
            OutputHelpers.WriteSuccess("PIV application is supported");
        }
    }

    /// <summary>
    /// Displays PIV application-specific information.
    /// </summary>
    private static async Task DisplayPivInfoAsync(
        IYubiKey device,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var pivSession = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("PIV Application", "Connected");
            OutputHelpers.WriteKeyValue("Management Key Type", pivSession.ManagementKeyType.ToString());

            // Try to get serial number via PIV
            try
            {
                var serial = await pivSession.GetSerialNumberAsync(cancellationToken);
                OutputHelpers.WriteKeyValue("PIV Serial", serial.ToString());
            }
            catch (NotSupportedException)
            {
                OutputHelpers.WriteKeyValue("PIV Serial", "Not supported (requires firmware 5.0+)");
            }

            // Display feature support indicators
            AnsiConsole.WriteLine();
            DisplayFeatureSupport();
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to query PIV application: {ex.Message}");
        }
    }

    /// <summary>
    /// Displays feature support indicators based on firmware version.
    /// </summary>
    private static void DisplayFeatureSupport()
    {
        // Note: These are general indicators. Actual support depends on firmware version.
        var features = new[]
        {
            ("Key Management", true),
            ("Certificate Storage", true),
            ("Digital Signatures", true),
            ("Key Attestation", true),
            ("Bio Match on Card", false) // Requires specific firmware
        };

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn("Feature")
            .AddColumn("Status");

        foreach (var (feature, supported) in features)
        {
            var status = supported ? "[green]✓[/]" : "[grey]○[/]";
            table.AddRow(feature, status);
        }

        AnsiConsole.MarkupLine("[grey]Supported Features:[/]");
        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Formats device capabilities for display.
    /// </summary>
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

    /// <summary>
    /// Handles device connection errors with user-friendly messages.
    /// </summary>
    private static void HandleDeviceError(Exception ex)
    {
        var message = ex switch
        {
            InvalidOperationException { Message: var m } when m.Contains("removed") =>
                "YubiKey was removed. Please reconnect to continue.",

            NotSupportedException =>
                "This YubiKey does not support PIV. Minimum firmware: 4.0",

            _ => $"Error communicating with device: {ex.Message}"
        };

        OutputHelpers.WriteError(message);
    }
}
