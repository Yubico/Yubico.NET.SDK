// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;

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

    /// <summary>Formats form factor for display.</summary>
    public static string FormatFormFactor(FormFactor formFactor) => FormFactorFormatter.Format(formFactor);
}