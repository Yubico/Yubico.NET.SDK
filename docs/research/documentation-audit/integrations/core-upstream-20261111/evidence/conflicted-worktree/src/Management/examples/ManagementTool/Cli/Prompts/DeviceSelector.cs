// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;

<<<<<<< HEAD
/// <summary>
/// Handles YubiKey device discovery and selection for Management operations.
/// The active filter lists SmartCard-capable devices only, although the SDK also supports both HID routes.
/// </summary>
public static class DeviceSelector
{
    // This tool currently filters for SmartCard; it is not an SDK Management requirement.
    // Discovery already groups proven interfaces into physical keys, not three rows per key.
    // The separate three-transport ManagementDeviceSelector is not used by current callers.
    private static readonly ConnectionType[] SupportedConnectionTypes =
    [
        ConnectionType.SmartCard
    ];

    /// <summary>
    /// Finds SmartCard-capable YubiKeys and allows the user to select one.
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
                device.AvailableConnections);
        }

        return await PromptForDeviceSelectionAsync(devices, cancellationToken);
    }

    /// <summary>
    /// Finds SmartCard-capable YubiKeys, with a retry prompt if none match.
    /// </summary>
    public static async Task<IReadOnlyList<IYubiKey>> FindDevicesWithRetryAsync(
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var allDevices = await YubiKeyManager.FindAllAsync(ConnectionType.All, cancellationToken: cancellationToken);
            var devices = allDevices
                .Where(d => SupportedConnectionTypes.Any(d.SupportsConnection))
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
=======
/// <summary>Device selection for the SmartCard-only standalone Management tool.</summary>
public static class DeviceSelector
{
    // Preserve this tool's SmartCard eligibility, including its reset menu. Management itself also supports HID.
    // Discovery still scans all interfaces and retains complete composite devices; no per-interface dedup is needed.
    private static readonly SharedSelector Selector = new(
        [ConnectionType.SmartCard], NonInteractiveSelection.RequireChoice);

    /// <summary>Finds an eligible YubiKey, requiring an explicit choice when multiple devices are present.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        Selector.SelectDeviceAsync(cancellationToken);
>>>>>>> 5746e72ed9a848d08b5782ddde025aa4a4b522ef

    /// <summary>Formats form factor; current Core product-flag retention can produce unnamed enum values.</summary>
    public static string FormatFormFactor(FormFactor formFactor) => FormFactorFormatter.Format(formFactor);
}