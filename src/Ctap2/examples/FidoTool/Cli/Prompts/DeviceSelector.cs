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
/// selector uses cancellable prompts and best-effort display metadata. Reset retains the selected
/// device view and transport rather than selecting a different device after reinsertion.
/// </remarks>
public static class DeviceSelector
{
    /// <summary>Finds an eligible YubiKey from an interactive terminal.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        SelectDeviceAsync(null, cancellationToken);

    /// <summary>Finds the specified serial, or prompts for an interactive choice.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(int? serialNumber, CancellationToken cancellationToken)
    {
        bool interactive = Spectre.Console.AnsiConsole.Profile.Capabilities.Interactive && !Console.IsInputRedirected;
        if (!interactive && serialNumber is null)
            throw new InvalidOperationException("Non-interactive use requires an explicit --serial target.");

        var selector = new SharedSelector([ConnectionType.HidFido, ConnectionType.SmartCard],
            NonInteractiveSelection.RequireChoice, serialNumber);
        return selector.SelectDeviceAsync(cancellationToken, allowInteraction: interactive);
    }
}