// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool;

/// <summary>OTP selection and session creation over SmartCard or OTP HID.</summary>
/// <remarks>
/// The shared selector filters grouped physical views and prefers SmartCard without interaction.
/// Calculation selects OTP HID when available; other operations use the standard SmartCard-first order.
/// A selected transport open failure propagates without fallback. This tool supplies no presence prompt. Selection metadata
/// is best-effort display information; flagged form factors alone establish no product-family eligibility.
/// </remarks>
public static class DeviceHelper
{
    /// <summary>Finds a YubiKey and opens an OTP session, preferring OTP HID for calculation.</summary>
    public static async Task<YubiOtpSession> CreateSessionAsync(CancellationToken cancellationToken, bool calculate = false)
    {
        var selection = await SelectDeviceAsync(cancellationToken)
            ?? throw new InvalidOperationException("No YubiKey selected. Insert a YubiKey and try again.");
        var type = calculate && selection.Device.AvailableConnections.HasFlag(ConnectionType.HidOtp)
            ? ConnectionType.HidOtp
            : (ConnectionType?)null;
        return await selection.Device.CreateYubiOtpSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = type }, cancellationToken);
    }

    /// <summary>Finds an eligible YubiKey, preferring SmartCard without interaction; diagnostics stay on stderr.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        new DeviceSelector([ConnectionType.SmartCard, ConnectionType.HidOtp], NonInteractiveSelection.PreferFirstTransport,
            console: AnsiConsole.Profile.Capabilities.Interactive ? AnsiConsole.Console :
                AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) }))
            .SelectDeviceAsync(cancellationToken);
}