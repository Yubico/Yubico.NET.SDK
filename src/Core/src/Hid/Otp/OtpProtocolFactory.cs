// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Interfaces;

namespace Yubico.YubiKit.Core.Hid.Otp;

/// <summary>
/// Factory for creating OTP protocol instances for OTP HID connections.
/// </summary>
public class OtpProtocolFactory(ILoggerFactory loggerFactory)
{
    /// <summary>
    /// Creates an OTP protocol instance for the given HID connection.
    /// </summary>
    /// <param name="connection">The HID connection to wrap with OTP protocol handling.</param>
    /// <returns>A configured OTP protocol instance.</returns>
    public IOtpHidProtocol Create(IOtpHidConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new OtpHidProtocol(connection, loggerFactory.CreateLogger<OtpHidProtocol>());
    }

    /// <summary>
    /// Creates a factory instance with optional logger factory.
    /// </summary>
    public static OtpProtocolFactory Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? YubiKitLogging.LoggerFactory);
}
