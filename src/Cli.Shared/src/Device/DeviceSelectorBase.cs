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

using Microsoft.Extensions.Logging;
using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Shared.Device;

/// <summary>
/// Abstract base class for YubiKey device discovery and selection in CLI tools.
/// Subclasses define which connection types are supported and how auto-selection
/// works when multiple devices are found in non-interactive mode.
/// </summary>
public abstract class DeviceSelectorBase
{
    private static readonly ILogger Logger = YubiKitLogging.LoggerFactory.CreateLogger<DeviceSelectorBase>();

    /// <summary>
    /// Gets the connection types supported by this CLI tool.
    /// Used to filter discovered devices and to describe supported transports.
    /// </summary>
    protected abstract ConnectionType[] SupportedConnectionTypes { get; }

    /// <summary>
    /// Gets a human-readable description of supported transports for the retry prompt.
    /// Example: "SmartCard (CCID), FIDO HID, OTP HID"
    /// </summary>
    protected abstract string SupportedTransportsDescription { get; }

    /// <summary>
    /// Gets the connection type filter passed to <see cref="YubiKeyManager.FindAllAsync(CancellationToken)"/>.
    /// Defaults to <see cref="ConnectionType.All"/>. Override to narrow the initial query
    /// (e.g., <see cref="ConnectionType.SmartCard"/> for SmartCard-only CLIs).
    /// </summary>
    protected virtual ConnectionType FindAllConnectionTypeFilter => ConnectionType.All;

    /// <summary>
    /// Selects a device from the discovered list when multiple devices are found
    /// and the terminal is not interactive. Returns <c>null</c> to indicate that
    /// the interactive prompt should be shown instead (the default behavior).
    /// </summary>
    /// <param name="devices">The list of discovered devices.</param>
    /// <returns>The auto-selected device, or <c>null</c> to fall through to the interactive prompt.</returns>
    protected virtual IYubiKey? AutoSelectDevice(IReadOnlyList<IYubiKey> devices) => null;

    /// <summary>
    /// Called when no devices are found and the terminal is not interactive.
    /// Override to emit a non-interactive error message (e.g., to stderr) and
    /// return <c>true</c> to suppress the standard Spectre.Console retry prompt.
    /// </summary>
    /// <returns><c>true</c> if the error was handled and no retry should be offered; <c>false</c> to continue with the default retry prompt.</returns>
    protected virtual bool HandleNonInteractiveNoDevices() => false;

    /// <summary>
    /// Finds all connected YubiKeys and allows user to select one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected YubiKey with device info, or null if none available or user cancelled.</returns>
    public async Task<DeviceSelection?> SelectDeviceAsync(
        CancellationToken cancellationToken = default)
    {
        var devices = await FindDevicesWithRetryAsync(cancellationToken);
        if (devices.Count == 0)
        {
            return null;
        }

        if (devices.Count == 1)
        {
            return await CreateDeviceSelectionAsync(devices[0], cancellationToken);
        }

        // Multiple devices - try auto-select for non-interactive mode
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            var autoSelected = AutoSelectDevice(devices);
            if (autoSelected is not null)
            {
                return await CreateDeviceSelectionAsync(autoSelected, cancellationToken);
            }

            AnsiConsole.MarkupLine("[red]Multiple YubiKeys detected but terminal is not interactive. Cannot prompt for selection.[/]");
            return null;
        }

        return await PromptForDeviceSelectionAsync(devices, cancellationToken);
    }

    /// <summary>
    /// Finds all connected YubiKeys, with retry prompt if none found.
    /// Filters devices to the connection types supported by this CLI tool.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<IYubiKey>> FindDevicesWithRetryAsync(
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var allDevices = await YubiKeyManager.FindAllAsync(
                FindAllConnectionTypeFilter, cancellationToken: cancellationToken);

            var devices = FilterDevices(allDevices);

            if (devices.Count > 0)
            {
                return devices;
            }

            if (HandleNonInteractiveNoDevices())
            {
                return [];
            }

            AnsiConsole.MarkupLine("[red]No YubiKey detected. Please insert a YubiKey and try again.[/]");
            AnsiConsole.MarkupLine($"[grey]Supported transports: {SupportedTransportsDescription}[/]");

            if (!AnsiConsole.Profile.Capabilities.Interactive ||
                !AnsiConsole.Confirm("Retry?", defaultValue: true))
            {
                return [];
            }
        }

        return [];
    }

    /// <summary>
    /// Gets device info for a single device.
    /// </summary>
    protected virtual async Task<DeviceInfo?> GetDeviceInfoAsync(
        IYubiKey device,
        CancellationToken cancellationToken)
    {
        try
        {
            return await device.GetDeviceInfoAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "{ConnectionType} device info query failed", device.ConnectionType);
            return null;
        }
    }

    /// <summary>
    /// Filters the discovered devices to only those matching the supported connection types.
    /// Override to customize filtering behavior (e.g., skip filtering when the initial query
    /// already returns only the desired types).
    /// </summary>
    protected virtual IReadOnlyList<IYubiKey> FilterDevices(IReadOnlyList<IYubiKey> allDevices) =>
        allDevices
            .Where(d => SupportedConnectionTypes.Contains(d.ConnectionType))
            .ToList();

    /// <summary>
    /// Prompts user to select from multiple devices.
    /// </summary>
    private async Task<DeviceSelection?> PromptForDeviceSelectionAsync(
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
    /// Creates a <see cref="DeviceSelection"/> from a device by querying its info.
    /// </summary>
    private async Task<DeviceSelection> CreateDeviceSelectionAsync(
        IYubiKey device,
        CancellationToken cancellationToken)
    {
        var info = await GetDeviceInfoAsync(device, cancellationToken);
        return new DeviceSelection(
            device,
            info?.SerialNumber,
            info?.FormFactor ?? FormFactor.Unknown,
            info?.FirmwareVersion.ToString() ?? "Unknown",
            device.ConnectionType);
    }

    /// <summary>
    /// Formats a device choice string for display in the selection prompt.
    /// </summary>
    private static string FormatDeviceChoice(IYubiKey device, DeviceInfo? info)
    {
        var transport = ConnectionTypeFormatter.Format(device.ConnectionType);

        if (info is null)
        {
            return $"YubiKey ({transport})";
        }

        var serial = info.Value.SerialNumber?.ToString() ?? "N/A";
        var firmware = info.Value.FirmwareVersion.ToString();
        var formFactor = FormFactorFormatter.Format(info.Value.FormFactor);

        return $"YubiKey {formFactor} - Serial: {serial}, Firmware: {firmware} ({transport})";
    }
}