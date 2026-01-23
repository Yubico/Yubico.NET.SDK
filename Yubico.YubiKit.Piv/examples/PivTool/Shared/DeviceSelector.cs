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
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Shared;

/// <summary>
/// Handles YubiKey device discovery and selection.
/// </summary>
public static class DeviceSelector
{
    /// <summary>
    /// Finds all connected YubiKeys and allows user to select one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected YubiKey, or null if none available or user cancelled.</returns>
    public static async Task<IYubiKey?> SelectDeviceAsync(CancellationToken cancellationToken = default)
    {
        var devices = await FindDevicesWithRetryAsync(cancellationToken);
        if (devices.Count == 0)
        {
            return null;
        }

        if (devices.Count == 1)
        {
            return devices[0];
        }

        // Multiple devices - let user choose
        return await PromptForDeviceSelectionAsync(devices, cancellationToken);
    }

    /// <summary>
    /// Finds all connected YubiKeys, with retry prompt if none found.
    /// </summary>
    public static async Task<IReadOnlyList<IYubiKey>> FindDevicesWithRetryAsync(
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var allDevices = await YubiKey.FindAllAsync(cancellationToken);
            
            // Filter to only SmartCard/PCSC devices (required for PIV operations)
            var devices = allDevices.Where(d => d.ConnectionType == ConnectionType.SmartCard).ToList();

            if (devices.Count > 0)
            {
                return devices;
            }

            AnsiConsole.MarkupLine("[red]No YubiKey with SmartCard interface detected. Please insert a YubiKey and try again.[/]");

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
    private static async Task<IYubiKey?> PromptForDeviceSelectionAsync(
        IReadOnlyList<IYubiKey> devices,
        CancellationToken cancellationToken)
    {
        var deviceInfos = new List<(IYubiKey Device, DeviceInfo? Info)>();

        // Get device info for each device to display details
        await AnsiConsole.Status()
            .StartAsync("Querying device information...", async ctx =>
            {
                foreach (var device in devices)
                {
                    DeviceInfo? info = null;
                    try
                    {
                        await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
                        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);
                        info = await session.GetDeviceInfoAsync(cancellationToken);
                    }
                    catch
                    {
                        // Device info unavailable - continue with null
                    }

                    deviceInfos.Add((device, info));
                }
            });

        var choices = deviceInfos.Select(d => FormatDeviceChoice(d.Device, d.Info)).ToList();
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

        var index = choices.IndexOf(selection);
        return deviceInfos[index].Device;
    }

    /// <summary>
    /// Formats a device choice string for display.
    /// </summary>
    private static string FormatDeviceChoice(IYubiKey device, DeviceInfo? info)
    {
        if (info is null)
        {
            return $"YubiKey ({device.ConnectionType})";
        }

        var serial = info.Value.SerialNumber?.ToString() ?? "N/A";
        var firmware = info.Value.FirmwareVersion.ToString();
        var formFactor = FormatFormFactor(info.Value.FormFactor);

        return $"YubiKey {formFactor} - Serial: {serial}, Firmware: {firmware}";
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
}
