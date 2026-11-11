// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.Ctap2.Examples.FidoTool.Cli.Prompts;

/// <summary>
/// CTAP2 eligibility and noninteractive preference. Sessions enforce the FIDO AID and firmware requirements:
/// USB SmartCard requires firmware 5.8+, while NFC SmartCard remains supported.
/// </summary>
public static class DeviceSelector
{
    private static readonly SharedSelector Selector = new(
        [ConnectionType.HidFido, ConnectionType.SmartCard], NonInteractiveSelection.PreferFirstTransport);

    /// <summary>Finds an eligible YubiKey, preferring FIDO HID when multiple devices cannot be prompted for.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        Selector.SelectDeviceAsync(cancellationToken);
}