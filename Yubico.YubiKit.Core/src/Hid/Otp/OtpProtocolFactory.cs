// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.Hid.Otp;

/// <summary>
/// Factory for creating OTP protocol instances for OTP HID connections.
/// </summary>
public class OtpProtocolFactory<TConnection>(ILoggerFactory loggerFactory)
    where TConnection : IConnection
{
    /// <summary>
    /// Creates an OTP protocol instance for the given HID connection.
    /// </summary>
    /// <param name="connection">The HID connection to wrap with OTP protocol handling.</param>
    /// <returns>A configured OTP protocol instance.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if the connection type is not an IOtpHidConnection.
    /// </exception>
    public IOtpHidProtocol Create(TConnection connection)
    {
        if (connection is not IOtpHidConnection otpConnection)
            throw new NotSupportedException(
                $"The connection type {typeof(TConnection).Name} is not supported by OtpProtocolFactory. Expected IOtpHidConnection.");

        return new OtpHidProtocol(otpConnection, loggerFactory.CreateLogger<OtpHidProtocol>());
    }

    /// <summary>
    /// Creates a factory instance with optional logger factory.
    /// </summary>
    public static OtpProtocolFactory<TConnection> Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? YubiKitLogging.LoggerFactory);
}
