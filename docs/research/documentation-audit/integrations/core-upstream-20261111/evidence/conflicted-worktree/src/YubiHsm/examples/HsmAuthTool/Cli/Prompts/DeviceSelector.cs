// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;

<<<<<<< HEAD
/// <summary>
/// Handles YubiKey device discovery and selection for HsmAuth operations.
/// HsmAuth only supports SmartCard (CCID) transport.
/// </summary>
/// <remarks>
///     Discovery filters for SmartCard access; opening an HsmAuth session subsequently selects the
///     applet and establishes its availability on the selected key.
/// </remarks>
=======
/// <summary>SmartCard-only selection for YubiHSM Auth operations.</summary>
>>>>>>> 5746e72ed9a848d08b5782ddde025aa4a4b522ef
public static class DeviceSelector
{
    private static readonly SharedSelector Selector = new(
        [ConnectionType.SmartCard], NonInteractiveSelection.FirstEligible);

    /// <summary>Finds an eligible YubiKey, choosing the first when multiple devices cannot be prompted for.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        Selector.SelectDeviceAsync(cancellationToken);
}