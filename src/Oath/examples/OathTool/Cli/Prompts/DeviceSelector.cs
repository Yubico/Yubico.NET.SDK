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
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

/// <summary>
/// Handles YubiKey device discovery and selection for OATH operations.
/// OATH only supports SmartCard (CCID) transport.
/// </summary>
public static class DeviceSelector
{
    /// <summary>
    /// Finds all connected YubiKeys and allows user to select one.
    /// </summary>
    public static async Task<DeviceSelection?> SelectDeviceAsync(
        CancellationToken cancellationToken = default,
        IAnsiConsole? console = null,
        Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>>? discover = null)
    {
        console ??= AnsiConsole.Console;
        var devices = await FindDevicesWithRetryAsync(cancellationToken, console, discover);
        cancellationToken.ThrowIfCancellationRequested();
        if (devices.Count == 0)
        {
            return null;
        }

        if (devices.Count == 1)
        {
            var device = devices[0];
            var info = await GetDeviceInfoAsync(device, console, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new DeviceSelection(
                device,
                info?.SerialNumber,
                info?.FormFactor ?? FormFactor.Unknown,
                info?.FirmwareVersion.ToString() ?? "Unknown",
                device.AvailableConnections);
        }

        // Multiple devices - auto-select first in non-interactive mode
        if (!console.Profile.Capabilities.Interactive)
        {
            var first = devices[0];
            var info = await GetDeviceInfoAsync(first, console, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new DeviceSelection(
                first,
                info?.SerialNumber,
                info?.FormFactor ?? FormFactor.Unknown,
                info?.FirmwareVersion.ToString() ?? "Unknown",
                first.AvailableConnections);
        }

        return await PromptForDeviceSelectionAsync(devices, console, cancellationToken);
    }

    /// <summary>
    /// Finds all connected YubiKeys, with retry prompt if none found.
    /// OATH only supports SmartCard transport.
    /// </summary>
    public static async Task<IReadOnlyList<IYubiKey>> FindDevicesWithRetryAsync(
        CancellationToken cancellationToken = default,
        IAnsiConsole? console = null,
        Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>>? discover = null)
    {
        console ??= AnsiConsole.Console;
        discover ??= token => YubiKeyManager.FindAllAsync(ConnectionType.SmartCard, cancellationToken: token);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var allDevices = await discover(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var devices = allDevices
                .Where(d => d.SupportsConnection(ConnectionType.SmartCard))
                .ToList();

            if (devices.Count > 0)
            {
                return devices;
            }

            console.MarkupLine("[red]No YubiKey detected. Please insert a YubiKey and try again.[/]");
            console.MarkupLine("[grey]OATH requires SmartCard (CCID) transport.[/]");

            if (!console.Profile.Capabilities.Interactive)
            {
                return [];
            }

            bool retry = await new ConfirmationPrompt("Retry?") { DefaultValue = true }
                .ShowAsync(console, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!retry)
            {
                return [];
            }
        }
    }

    /// <summary>Formats form factor for display.</summary>
    public static string FormatFormFactor(FormFactor formFactor) => FormFactorFormatter.Format(formFactor);

    /// <summary>Formats connection type for display.</summary>
    public static string FormatConnectionType(ConnectionType connectionType) => ConnectionTypeFormatter.Format(connectionType);

    private static async Task<DeviceInfo?> GetDeviceInfoAsync(
        IYubiKey device,
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        try
        {
            return await device.GetDeviceInfoAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            console.MarkupLine(
                $"[grey]Debug: {device.AvailableConnections} device info failed: " +
                $"{Markup.Escape(ex.GetType().Name)}: {Markup.Escape(ex.Message)}[/]");
            return null;
        }
    }

    private static async Task<DeviceSelection?> PromptForDeviceSelectionAsync(
        IReadOnlyList<IYubiKey> devices,
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var deviceInfos = new List<(IYubiKey Device, DeviceInfo? Info)>();

        await console.Status()
            .StartAsync("Querying device information...", async _ =>
            {
                var tasks = devices
                    .Select(device => GetDeviceInfoAsync(device, console, cancellationToken))
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

        cancellationToken.ThrowIfCancellationRequested();
        var selection = await new SelectionPrompt<string>()
                .Title("Multiple YubiKeys detected. Select one:")
                .PageSize(10)
                .AddChoices(choices)
                .ShowAsync(console, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

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
            selected.Device.AvailableConnections);
    }

    private static string FormatDeviceChoice(IYubiKey device, DeviceInfo? info)
    {
        var transport = ConnectionTypeFormatter.Format(device.AvailableConnections);

        if (info is null)
        {
            return $"YubiKey ({transport})";
        }

        return $"YubiKey {FormFactorFormatter.Format(info.Value.FormFactor)} - Serial: {info.Value.SerialNumber?.ToString() ?? "N/A"}, Firmware: {info.Value.FirmwareVersion} ({transport})";
    }
}