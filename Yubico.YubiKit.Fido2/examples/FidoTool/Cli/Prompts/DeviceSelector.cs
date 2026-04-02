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

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;

/// <summary>
/// Represents a selected YubiKey device with its identifying information.
/// </summary>
/// <param name="Device">The selected YubiKey device.</param>
/// <param name="SerialNumber">The device serial number, if available.</param>
/// <param name="FormFactor">The device form factor.</param>
/// <param name="FirmwareVersion">The firmware version string.</param>
/// <param name="ConnectionType">The connection type used to connect to this device.</param>
public record DeviceSelection(
    IYubiKey Device,
    int? SerialNumber,
    FormFactor FormFactor,
    string FirmwareVersion,
    ConnectionType ConnectionType)
{
    /// <summary>
    /// Gets a display string for this device (e.g., "YubiKey 5C - S/N: 12345678 (FIDO HID)").
    /// </summary>
    public string DisplayName =>
        SerialNumber.HasValue
            ? $"YubiKey {FormatFormFactor(FormFactor)} - S/N: {SerialNumber} ({FormatConnectionType(ConnectionType)})"
            : $"YubiKey {FormatFormFactor(FormFactor)} ({FormatConnectionType(ConnectionType)})";

    private static string FormatFormFactor(FormFactor formFactor) =>
        DeviceSelector.FormatFormFactor(formFactor);

    private static string FormatConnectionType(ConnectionType connectionType) =>
        DeviceSelector.FormatConnectionType(connectionType);
}

/// <summary>
/// Handles YubiKey device discovery and selection for FIDO2 operations.
/// Supports FIDO HID (USB) and SmartCard (NFC) transports.
/// </summary>
/// <remarks>
/// FIDO2 is NOT available over OTP HID. USB uses the FIDO HID interface;
/// SmartCard transport is supported only over NFC.
/// </remarks>
public static class DeviceSelector
{
    /// <summary>
    /// Connection types supported for FIDO2 operations.
    /// </summary>
    private static readonly ConnectionType[] SupportedConnectionTypes =
    [
        ConnectionType.HidFido,
        ConnectionType.SmartCard
    ];

    /// <summary>
    /// Finds all connected YubiKeys and allows user to select one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected YubiKey with device info, or null if none available or user cancelled.</returns>
    public static async Task<DeviceSelection?> SelectDeviceAsync(
        CancellationToken cancellationToken = default)
    {
        var devices = await FindDevicesWithRetryAsync(cancellationToken);
        if (devices.Count == 0)
        {
            return null;
        }

        if (devices.Count == 1)
        {
            var device = devices[0];
            var info = await GetDeviceInfoAsync(device, cancellationToken);
            return new DeviceSelection(
                device,
                info?.SerialNumber,
                info?.FormFactor ?? FormFactor.Unknown,
                info?.FirmwareVersion.ToString() ?? "Unknown",
                device.ConnectionType);
        }

        // Multiple devices - prefer HID FIDO in non-interactive mode (FIDO2 native transport)
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            var hidFido = devices.FirstOrDefault(d => d.ConnectionType == ConnectionType.HidFido)
                ?? devices[0];
            var info = await GetDeviceInfoAsync(hidFido, cancellationToken);
            return new DeviceSelection(
                hidFido,
                info?.SerialNumber,
                info?.FormFactor ?? FormFactor.Unknown,
                info?.FirmwareVersion.ToString() ?? "Unknown",
                hidFido.ConnectionType);
        }

        return await PromptForDeviceSelectionAsync(devices, cancellationToken);
    }

    /// <summary>
    /// Finds all connected YubiKeys, with retry prompt if none found.
    /// Supports FIDO HID and SmartCard connection types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<IReadOnlyList<IYubiKey>> FindDevicesWithRetryAsync(
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var allDevices = await YubiKeyManager.FindAllAsync(
                ConnectionType.All, cancellationToken: cancellationToken);

            // Filter to supported connection types for FIDO2
            var devices = allDevices
                .Where(d => SupportedConnectionTypes.Contains(d.ConnectionType))
                .ToList();

            if (devices.Count > 0)
            {
                return devices;
            }

            AnsiConsole.MarkupLine("[red]No YubiKey detected. Please insert a YubiKey and try again.[/]");
            AnsiConsole.MarkupLine("[grey]Supported transports: FIDO HID (USB), SmartCard (NFC)[/]");

            if (!AnsiConsole.Confirm("Retry?", defaultValue: true))
            {
                return [];
            }
        }

        return [];
    }

    /// <summary>
    /// Gets device info for a single device.
    /// </summary>
    private static async Task<DeviceInfo?> GetDeviceInfoAsync(
        IYubiKey device,
        CancellationToken cancellationToken)
    {
        try
        {
            return await device.GetDeviceInfoAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[grey]Debug: {device.ConnectionType} device info failed: " +
                $"{Markup.Escape(ex.GetType().Name)}: {Markup.Escape(ex.Message)}[/]");
            return null;
        }
    }

    /// <summary>
    /// Prompts user to select from multiple devices.
    /// </summary>
    private static async Task<DeviceSelection?> PromptForDeviceSelectionAsync(
        IReadOnlyList<IYubiKey> devices,
        CancellationToken cancellationToken)
    {
        var deviceInfos = new List<(IYubiKey Device, DeviceInfo? Info)>();

        // Get device info for each device to display details
        await AnsiConsole.Status()
            .StartAsync("Querying device information...", async _ =>
            {
                var tasks = devices
                    .Select(device => GetDeviceInfoAsync(device, cancellationToken))
                    .ToArray();

                var infos = await Task.WhenAll(tasks);

                for (int i = 0; i < devices.Count; i++)
                {
                    deviceInfos.Add((devices[i], infos[i]));
                }
            });

        // Create indexed choices and sort by name, keeping original index
        var indexedChoices = deviceInfos
            .Select((d, index) => (Choice: FormatDeviceChoice(d.Device, d.Info), OriginalIndex: index))
            .OrderBy(x => x.Choice)
            .ToList();

        var choices = indexedChoices.Select(x => x.Choice).ToList();
        choices.Add("Cancel");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Multiple YubiKeys detected. Select one:")
                .PageSize(10)
                .AddChoices(choices));

        if (selection == "Cancel")
        {
            return null;
        }

        // Find the original index from the sorted list
        var selectedSortedIndex = choices.IndexOf(selection);
        var originalIndex = indexedChoices[selectedSortedIndex].OriginalIndex;
        var selected = deviceInfos[originalIndex];
        return new DeviceSelection(
            selected.Device,
            selected.Info?.SerialNumber,
            selected.Info?.FormFactor ?? FormFactor.Unknown,
            selected.Info?.FirmwareVersion.ToString() ?? "Unknown",
            selected.Device.ConnectionType);
    }

    /// <summary>
    /// Formats a device choice string for display.
    /// </summary>
    private static string FormatDeviceChoice(IYubiKey device, DeviceInfo? info)
    {
        var transport = FormatConnectionType(device.ConnectionType);

        if (info is null)
        {
            return $"YubiKey ({transport})";
        }

        var serial = info.Value.SerialNumber?.ToString() ?? "N/A";
        var firmware = info.Value.FirmwareVersion.ToString();
        var formFactor = FormatFormFactor(info.Value.FormFactor);

        return $"YubiKey {formFactor} - Serial: {serial}, Firmware: {firmware} ({transport})";
    }

    /// <summary>
    /// Formats form factor for display.
    /// </summary>
    public static string FormatFormFactor(FormFactor formFactor) =>
        formFactor switch
        {
            FormFactor.UsbAKeychain => "5A",
            FormFactor.UsbANano => "5 Nano",
            FormFactor.UsbCKeychain => "5C",
            FormFactor.UsbCNano => "5C Nano",
            FormFactor.UsbCLightning => "5Ci",
            FormFactor.UsbABiometricKeychain => "Bio",
            FormFactor.UsbCBiometricKeychain => "Bio (USB-C)",
            _ => "Unknown"
        };

    /// <summary>
    /// Formats connection type for display.
    /// </summary>
    public static string FormatConnectionType(ConnectionType connectionType) =>
        connectionType switch
        {
            ConnectionType.SmartCard => "SmartCard/NFC",
            ConnectionType.HidFido => "FIDO HID",
            _ => "Unknown"
        };
}