// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;

/// <summary>Selects SmartCard-capable candidates for YubiHSM Auth operations.</summary>
/// <remarks>
/// Opening an HsmAuth session subsequently selects the applet and establishes its availability
/// on the selected key. Interface eligibility supplies the transport prerequisite only.
/// </remarks>
public static class DeviceSelector
{
    private static readonly SharedSelector Selector = new(
        [ConnectionType.SmartCard], NonInteractiveSelection.FirstEligible);

    /// <summary>Finds an eligible YubiKey, choosing the first when multiple devices cannot be prompted for.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        Selector.SelectDeviceAsync(cancellationToken);
}