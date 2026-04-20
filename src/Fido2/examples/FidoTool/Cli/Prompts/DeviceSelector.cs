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
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;

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
    private static readonly ConnectionType[] SupportedConnectionTypes =
    [
        ConnectionType.HidFido,
        ConnectionType.SmartCard
    ];

    /// <summary>
    /// Finds all connected YubiKeys and allows user to select one.
    /// </summary>
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
    /// </summary>
    public static async Task<IReadOnlyList<IYubiKey>> FindDevicesWithRetryAsync(
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var allDevices = await YubiKeyManager.FindAllAsync(
                ConnectionType.All, cancellationToken: cancellationToken);

            var devices = allDevices
                .Where(d => SupportedConnectionTypes.Contains(d.ConnectionType))
                .ToList();

            if (devices.Count > 0)
            {
                return devices;
            }

            AnsiConsole.MarkupLine("[red]No YubiKey detected. Please insert a YubiKey and try again.[/]");
            AnsiConsole.MarkupLine("[grey]FIDO2 supports: FIDO HID (USB), SmartCard (NFC)[/]");

            if (!AnsiConsole.Confirm("Retry?", defaultValue: true))
            {
                return [];
            }
        }

        return [];
    }

    /// <summary>Formats form factor for display.</summary>
    public static string FormatFormFactor(FormFactor formFactor) => FormFactorFormatter.Format(formFactor);

    /// <summary>Formats connection type for display.</summary>
    public static string FormatConnectionType(ConnectionType connectionType) => ConnectionTypeFormatter.Format(connectionType);

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

    private static async Task<DeviceSelection?> PromptForDeviceSelectionAsync(
        IReadOnlyList<IYubiKey> devices,
        CancellationToken cancellationToken)
    {
        var deviceInfos = new List<(IYubiKey Device, DeviceInfo? Info)>();

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

    private static string FormatDeviceChoice(IYubiKey device, DeviceInfo? info)
    {
        var transport = ConnectionTypeFormatter.Format(device.ConnectionType);

        if (info is null)
        {
            return $"YubiKey ({transport})";
        }

        return $"YubiKey {FormFactorFormatter.Format(info.Value.FormFactor)} - Serial: {info.Value.SerialNumber?.ToString() ?? "N/A"}, Firmware: {info.Value.FirmwareVersion} ({transport})";
    }
}
