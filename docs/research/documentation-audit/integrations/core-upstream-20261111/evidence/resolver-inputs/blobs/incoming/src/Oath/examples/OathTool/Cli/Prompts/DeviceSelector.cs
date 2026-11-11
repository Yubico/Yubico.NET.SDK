// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

/// <summary>SmartCard-only selection for OATH operations.</summary>
public static class DeviceSelector
{
    private static readonly SharedSelector Selector = new(
        [ConnectionType.SmartCard], NonInteractiveSelection.FirstEligible);

    /// <summary>Finds an eligible YubiKey, choosing the first when multiple devices cannot be prompted for.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        Selector.SelectDeviceAsync(cancellationToken);
}