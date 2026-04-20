// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;

/// <summary>
/// Handles YubiKey device discovery and selection for OpenPGP operations.
/// OpenPGP uses SmartCard (CCID) transport only.
/// </summary>
public static class DeviceSelector
{
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

        // Multiple devices -- in non-interactive mode, auto-select first device
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            var first = devices[0];
            var info = await GetDeviceInfoAsync(first, cancellationToken);
            return new DeviceSelection(
                first,
                info?.SerialNumber,
                info?.FormFactor ?? FormFactor.Unknown,
                info?.FirmwareVersion.ToString() ?? "Unknown",
                first.ConnectionType);
        }

        return await PromptForDeviceSelectionAsync(devices, cancellationToken);
    }

    /// <summary>
    /// Finds all connected YubiKeys with SmartCard support, with retry prompt if none found.
    /// </summary>
    public static async Task<IReadOnlyList<IYubiKey>> FindDevicesWithRetryAsync(
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var allDevices = await YubiKeyManager.FindAllAsync(
                ConnectionType.SmartCard,
                cancellationToken: cancellationToken);

            var devices = allDevices.ToList();

            if (devices.Count > 0)
            {
                return devices;
            }

            // Non-interactive: fail immediately, never prompt
            if (!AnsiConsole.Profile.Capabilities.Interactive)
            {
                Console.Error.WriteLine("Error: No YubiKey detected. OpenPGP requires SmartCard (CCID) transport.");
                return [];
            }

            AnsiConsole.MarkupLine("[red]No YubiKey detected. Please insert a YubiKey and try again.[/]");
            AnsiConsole.MarkupLine("[grey]OpenPGP requires SmartCard (CCID) transport.[/]");

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
        catch
        {
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
