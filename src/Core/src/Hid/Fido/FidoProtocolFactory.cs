// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging;

namespace Yubico.YubiKit.Core.Hid.Fido;

/// <summary>
/// Factory for creating FIDO protocol instances for FIDO HID connections.
/// </summary>
public class FidoProtocolFactory(ILoggerFactory loggerFactory)
{
    /// <summary>
    /// Creates a FIDO protocol instance for the given HID connection.
    /// </summary>
    /// <param name="connection">The HID connection to wrap with FIDO protocol handling.</param>
    /// <returns>A configured FIDO protocol instance.</returns>
    public IFidoHidProtocol Create(IFidoHidConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new FidoHidProtocol(connection, loggerFactory.CreateLogger<FidoHidProtocol>());
    }

    /// <summary>
    /// Creates a factory instance with optional logger factory.
    /// </summary>
    public static FidoProtocolFactory Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? YubiKitLogging.LoggerFactory);
}
