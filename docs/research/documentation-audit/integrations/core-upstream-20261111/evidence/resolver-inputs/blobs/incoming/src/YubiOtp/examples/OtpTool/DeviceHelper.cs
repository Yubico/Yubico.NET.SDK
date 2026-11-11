// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool;

/// <summary>OTP selection and session creation over SmartCard or OTP HID.</summary>
public static class DeviceHelper
{
    /// <summary>Finds a YubiKey and creates a YubiOTP session using its usual SmartCard-first transport order.</summary>
    public static async Task<YubiOtpSession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var selection = await SelectDeviceAsync(cancellationToken)
            ?? throw new InvalidOperationException("No YubiKey selected. Insert a YubiKey and try again.");
        return await selection.Device.CreateYubiOtpSessionAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Finds an eligible YubiKey, preferring SmartCard without interaction; diagnostics stay on stderr.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        new DeviceSelector([ConnectionType.SmartCard, ConnectionType.HidOtp], NonInteractiveSelection.PreferFirstTransport,
            console: AnsiConsole.Profile.Capabilities.Interactive ? AnsiConsole.Console :
                AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) }))
            .SelectDeviceAsync(cancellationToken);
}