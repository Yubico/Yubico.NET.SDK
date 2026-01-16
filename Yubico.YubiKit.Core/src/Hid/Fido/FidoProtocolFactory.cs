// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.Hid.Fido;

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
    public IFidoHidProtocol Create(TConnection connection)
    {
        if (connection is not IFidoHidConnection fidoConnection)
            throw new NotSupportedException(
                $"The connection type {typeof(TConnection).Name} is not supported by FidoProtocolFactory. Expected IFidoConnection.");

        return new FidoHidProtocol(fidoConnection, loggerFactory.CreateLogger<FidoHidProtocol>());
    }

    /// <summary>
    /// Creates a factory instance with optional logger factory.
    /// </summary>
    public static FidoProtocolFactory<TConnection> Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? YubiKitLogging.LoggerFactory);
}
