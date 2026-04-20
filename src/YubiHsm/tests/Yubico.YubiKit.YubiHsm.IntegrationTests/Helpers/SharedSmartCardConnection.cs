// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiHsm.IntegrationTests.Helpers;

/// <summary>
///     A wrapper around an <see cref="ISmartCardConnection" /> that ignores calls to
///     <see cref="IDisposable.Dispose" /> and <see cref="IAsyncDisposable.DisposeAsync" />.
/// </summary>
/// <remarks>
///     Use this when you want to pass a connection to a component that would otherwise
///     dispose of it, but you want to maintain control over the connection's lifecycle.
/// </remarks>
internal sealed class SharedSmartCardConnection(ISmartCardConnection connection) : ISmartCardConnection
{
    public Transport Transport => connection.Transport;

    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default) =>
        connection.TransmitAndReceiveAsync(command, cancellationToken);

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        connection.BeginTransaction(cancellationToken);

    public bool SupportsExtendedApdu() => connection.SupportsExtendedApdu();

    public void Dispose()
    {
        // Do nothing - connection lifecycle is managed by the owner
    }

    public ValueTask DisposeAsync() =>
        // Do nothing - connection lifecycle is managed by the owner
        default;

    public ConnectionType Type { get; } = ConnectionType.SmartCard;
}
