// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.Ctap2.Examples.FidoTool.Cli.Prompts;

/// <summary>
/// Selects exposed FIDO HID or SmartCard candidates, preferring FIDO HID without interaction.
/// </summary>
/// <remarks>
/// Session creation selects the FIDO AID and enforces the USB SmartCard firmware 5.8.0+ gate;
/// NFC SmartCard uses the current connection's NFC classification and requires the FIDO AID.
/// Interface eligibility alone can admit SmartCard-only devices without CTAP support. The shared
/// selector uses cancellable prompts and best-effort display metadata. Both reset flows select again
/// after reinsertion without correlating the new selection to the earlier key. The interactive menu
/// reuses its earlier transport choice; the command-line flow chooses from the later selection.
/// The operator must verify the physical target; selection does not enforce continuity.
/// </remarks>
public static class DeviceSelector
{
    private static readonly SharedSelector Selector = new(
        [ConnectionType.HidFido, ConnectionType.SmartCard], NonInteractiveSelection.PreferFirstTransport);

    /// <summary>Finds an eligible YubiKey, preferring FIDO HID when multiple devices cannot be prompted for.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        Selector.SelectDeviceAsync(cancellationToken);
}