// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Factory for creating FIDO protocol instances for FIDO HID connections.
/// </summary>
public class FidoProtocolFactory<TConnection>(ILoggerFactory loggerFactory)
    where TConnection : IConnection
{
    /// <summary>
    /// Creates a FIDO protocol instance for the given HID connection.
    /// </summary>
    /// <param name="connection">The HID connection to wrap with FIDO protocol handling.</param>
    /// <returns>A configured FIDO protocol instance.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if the connection type is not an IHidConnection.
    /// </exception>
    public IFidoProtocol Create(TConnection connection)
    {
        if (connection is not IFidoConnection fidoConnection)
            throw new NotSupportedException(
                $"The connection type {typeof(TConnection).Name} is not supported by FidoProtocolFactory. Expected IFidoConnection.");

        return new FidoProtocol(fidoConnection, loggerFactory.CreateLogger<FidoProtocol>());
    }

    /// <summary>
    /// Creates a factory instance with optional logger factory.
    /// </summary>
    public static FidoProtocolFactory<TConnection> Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? YubiKitLogging.LoggerFactory);
}
