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

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;

/// <summary>
/// Represents a selected YubiKey device with its identifying information.
/// </summary>
/// <param name="Device">The selected YubiKey device.</param>
/// <param name="SerialNumber">The device serial number, if available.</param>
/// <param name="FormFactor">The device form factor.</param>
/// <param name="FirmwareVersion">The firmware version string.</param>
/// <param name="AvailableConnections">The connection types available for this device.</param>
public record DeviceSelection(
    IYubiKey Device,
    int? SerialNumber,
    FormFactor FormFactor,
    string FirmwareVersion,
    IReadOnlyList<ConnectionType> AvailableConnections)
{
    /// <summary>
    /// Gets a display string for this device (e.g., "YubiKey 5C - S/N: 12345678 (SmartCard, FIDO HID)").
    /// </summary>
    public string DisplayName =>
        SerialNumber.HasValue
            ? $"YubiKey {FormatFormFactor(FormFactor)} - S/N: {SerialNumber} ({FormatConnectionTypes(AvailableConnections)})"
            : $"YubiKey {FormatFormFactor(FormFactor)} ({FormatConnectionTypes(AvailableConnections)})";

    private static string FormatFormFactor(FormFactor formFactor) =>
        DeviceSelector.FormatFormFactor(formFactor);

    private static string FormatConnectionTypes(IReadOnlyList<ConnectionType> connectionTypes) =>
        string.Join(", ", connectionTypes.Select(DeviceSelector.FormatConnectionType));
}

/// <summary>
/// Handles YubiKey device discovery and selection for Management operations.
/// Supports SmartCard, FIDO HID, and OTP HID transports.
/// </summary>
public static class DeviceSelector
{
    /// <summary>
    /// Connection types supported for Management operations.
    /// </summary>
    private static readonly ConnectionType[] SupportedConnectionTypes =
    [
        ConnectionType.SmartCard,
        ConnectionType.HidFido,
        ConnectionType.HidOtp
    ];

    /// <summary>
    /// Finds all connected YubiKeys and allows user to select one.
    /// </summary>
    /// <param name="manager">YubiKeyManager for device enumeration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected YubiKey with device info, or null if none available or user cancelled.</returns>
    public static async Task<DeviceSelection?> SelectDeviceAsync(
        IYubiKeyManager manager,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manager);
        
        var devices = await FindDevicesWithRetryAsync(manager, cancellationToken);
        if (devices.Count == 0)
        {
            return null;
        }

        if (devices.Count == 1)
        {
            var device = devices[0];
            return new DeviceSelection(
                device,
                device.Identity.SerialNumber,
                device.Identity.FormFactor,
                device.Identity.FirmwareVersion.ToString(),
                device.AvailableConnections);
        }

        // Multiple devices - let user choose
        return await PromptForDeviceSelectionAsync(devices, cancellationToken);
    }

    /// <summary>
    /// Finds all connected YubiKeys, with retry prompt if none found.
    /// Supports SmartCard, FIDO HID, and OTP HID connection types.
    /// </summary>
    /// <param name="manager">YubiKeyManager for device enumeration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<IReadOnlyList<IYubiKey>> FindDevicesWithRetryAsync(
        IYubiKeyManager manager,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manager);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var allDevices = await manager.FindAllAsync(ConnectionType.All, cancellationToken);

            // Filter to supported connection types for Management
            var devices = allDevices
                .Where(d => d.AvailableConnections.Any(c => SupportedConnectionTypes.Contains(c)))
                .ToList();

            if (devices.Count > 0)
            {
                return devices;
            }

            AnsiConsole.MarkupLine("[red]No YubiKey detected. Please insert a YubiKey and try again.[/]");
            AnsiConsole.MarkupLine("[grey]Supported transports: SmartCard (CCID), FIDO HID, OTP HID[/]");

            if (!AnsiConsole.Confirm("Retry?", defaultValue: true))
            {
                return [];
            }
        }

        return [];
    }

    /// <summary>
    /// Prompts user to select from multiple devices.
    /// </summary>
    private static async Task<DeviceSelection?> PromptForDeviceSelectionAsync(
        IReadOnlyList<IYubiKey> devices,
        CancellationToken cancellationToken)
    {
        // Create indexed choices and sort by name, keeping original index
        var indexedChoices = devices
            .Select((d, index) => (Choice: FormatDeviceChoice(d), OriginalIndex: index))
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
        var selected = devices[originalIndex];
        
        return new DeviceSelection(
            selected,
            selected.Identity.SerialNumber,
            selected.Identity.FormFactor,
            selected.Identity.FirmwareVersion.ToString(),
            selected.AvailableConnections);
    }

    /// <summary>
    /// Formats a device choice string for display.
    /// </summary>
    private static string FormatDeviceChoice(IYubiKey device)
    {
        var transports = string.Join(", ", device.AvailableConnections.Select(FormatConnectionType));
        var identity = device.Identity;

        var serial = identity.SerialNumber?.ToString() ?? "N/A";
        var firmware = identity.FirmwareVersion.ToString();
        var formFactor = FormatFormFactor(identity.FormFactor);

        return $"YubiKey {formFactor} - Serial: {serial}, Firmware: {firmware} ({transports})";
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
            ConnectionType.SmartCard => "SmartCard",
            ConnectionType.HidFido => "FIDO HID",
            ConnectionType.HidOtp => "OTP HID",
            _ => "Unknown"
        };
}
