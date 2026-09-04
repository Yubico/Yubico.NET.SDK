// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class SessionTransportTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void ResolveSessionTransport_DefaultOrder_ReturnsFirstSupportedTransport()
    {
        var device = new ProbeYubiKey()
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido))
            .Returns(ConnectionType.SmartCard, new RecordingConnection(ConnectionType.SmartCard));

        var transport = device.ResolveSessionTransport(
            null, "Test", ConnectionType.SmartCard, ConnectionType.HidFido);

        Assert.Equal(ConnectionType.SmartCard, transport);
    }

    [Fact]
    public void ResolveSessionTransport_ValidOverride_ReturnsOverride()
    {
        var device = new ProbeYubiKey()
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido))
            .Returns(ConnectionType.SmartCard, new RecordingConnection(ConnectionType.SmartCard));

        var transport = device.ResolveSessionTransport(
            ConnectionType.HidFido, "Test", ConnectionType.SmartCard, ConnectionType.HidFido);

        Assert.Equal(ConnectionType.HidFido, transport);
    }

    [Fact]
    public async Task CreateSessionOverTransportAsync_SmartCardSharingViolation_PropagatesWithoutHidFallback()
    {
        var held = new SCardException("held by another process", (long)ErrorCode.SCARD_E_SHARING_VIOLATION);
        var device = new ProbeYubiKey()
            .Throws(ConnectionType.SmartCard, held)
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        var exception = await Assert.ThrowsAsync<SCardException>(() =>
            device.CreateSessionOverTransportAsync(
                ConnectionType.SmartCard,
                static (connection, _) => Task.FromResult(connection),
                Ct));

        Assert.Same(held, exception);
        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    [Fact]
    public async Task CreateSessionOverTransportAsync_ConnectionInUse_PropagatesWithoutFallback()
    {
        var held = new ConnectionInUseException("held");
        var device = new ProbeYubiKey()
            .Throws(ConnectionType.SmartCard, held)
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        var exception = await Assert.ThrowsAsync<ConnectionInUseException>(() =>
            device.CreateSessionOverTransportAsync(
                ConnectionType.SmartCard,
                static (connection, _) => Task.FromResult(connection),
                Ct));

        Assert.Same(held, exception);
        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    [Fact]
    public async Task CreateSessionOverTransportAsync_SessionCreationFails_DisposesConnection()
    {
        var connection = new RecordingConnection(ConnectionType.SmartCard);
        var device = new ProbeYubiKey()
            .Returns(ConnectionType.SmartCard, connection)
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            device.CreateSessionOverTransportAsync<object>(
                ConnectionType.SmartCard,
                static (_, _) => throw new InvalidOperationException("initialization failed"),
                Ct));

        Assert.True(connection.Disposed);
        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    [Fact]
    public async Task CreateSessionOverTransportAsync_CleanupFails_PreservesOriginalCreationFailure()
    {
        var creationFailure = new InvalidOperationException("initialization failed");
        var cleanupFailure = new IOException("cleanup failed");
        var leaseId = $"cleanup-failure-{Guid.NewGuid():N}";
        var lease = await DeviceConnectionRegistry.AcquireConnectionAsync([leaseId], Ct);
        var innerConnection = new RecordingConnection(ConnectionType.SmartCard)
        {
            DisposeAsyncException = cleanupFailure
        };
        var connection = new RegisteredSmartCardConnection(innerConnection, lease);
        var device = new ProbeYubiKey().Returns(ConnectionType.SmartCard, connection);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            device.CreateSessionOverTransportAsync<object>(
                ConnectionType.SmartCard,
                (_, _) => Task.FromException<object>(creationFailure),
                Ct));

        Assert.Same(creationFailure, exception);
        Assert.Equal(1, innerConnection.DisposeAsyncCalls);
        Assert.True(innerConnection.Disposed);
        Assert.False(DeviceConnectionRegistry.IsInUse(leaseId));
    }

    [Fact]
    public async Task CreateSessionOverTransportAsync_CleanupAndLoggingFail_PreservesOriginalCreationFailure()
    {
        var creationFailure = new InvalidOperationException("initialization failed");
        var cleanupFailure = new IOException("cleanup failed");
        var leaseId = $"cleanup-logging-failure-{Guid.NewGuid():N}";
        var lease = await DeviceConnectionRegistry.AcquireConnectionAsync([leaseId], Ct);
        var innerConnection = new RecordingConnection(ConnectionType.SmartCard)
        {
            DisposeAsyncException = cleanupFailure
        };
        var connection = new RegisteredSmartCardConnection(innerConnection, lease);
        var device = new ProbeYubiKey().Returns(ConnectionType.SmartCard, connection);
        var logger = new ThrowingLogger();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            device.CreateSessionOverTransportAsync<object>(
                ConnectionType.SmartCard,
                (_, _) => Task.FromException<object>(creationFailure),
                logger,
                Ct));

        Assert.Same(creationFailure, exception);
        Assert.Equal(1, logger.LogCalls);
        Assert.Equal(1, innerConnection.DisposeAsyncCalls);
        Assert.False(DeviceConnectionRegistry.IsInUse(leaseId));
    }

    [Fact]
    public async Task CreateSessionOverTransportAsync_Succeeds_LeavesConnectionOwnedByResult()
    {
        var connection = new RecordingConnection(ConnectionType.HidOtp);
        var device = new ProbeYubiKey().Returns(ConnectionType.HidOtp, connection);

        var result = await device.CreateSessionOverTransportAsync(
            ConnectionType.HidOtp,
            static (opened, _) => Task.FromResult(opened),
            Ct);

        Assert.Same(connection, result);
        Assert.False(connection.Disposed);
        Assert.Equal([ConnectionType.HidOtp], device.Attempts);
    }

    [Fact]
    public async Task CreateSessionOverTransportAsync_PreCanceledToken_DoesNotOpenConnection()
    {
        var device = new ProbeYubiKey()
            .Returns(ConnectionType.SmartCard, new RecordingConnection(ConnectionType.SmartCard));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            device.CreateSessionOverTransportAsync(
                ConnectionType.SmartCard,
                static (connection, _) => Task.FromResult(connection),
                cancellation.Token));

        Assert.Empty(device.Attempts);
    }

    [Theory]
    [InlineData(ConnectionType.Unknown)]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.All)]
    public async Task CreateSessionOverTransportAsync_NonConcreteTransport_ThrowsWithoutOpening(
        ConnectionType transport)
    {
        var device = new ProbeYubiKey();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            device.CreateSessionOverTransportAsync(
                transport,
                static (connection, _) => Task.FromResult(connection),
                Ct));

        Assert.Empty(device.Attempts);
    }

    private sealed class ProbeYubiKey : IYubiKey
    {
        private readonly Dictionary<ConnectionType, Func<IConnection>> _behaviors = new();

        public string DeviceId => "session-transport-probe";
        public List<ConnectionType> Attempts { get; } = [];

        public ConnectionType AvailableConnections
        {
            get
            {
                var combined = ConnectionType.Unknown;
                foreach (var connection in _behaviors.Keys)
                    combined |= connection;
                return combined;
            }
        }

        public ProbeYubiKey Returns(ConnectionType transport, IConnection connection)
        {
            _behaviors[transport] = () => connection;
            return this;
        }

        public ProbeYubiKey Throws(ConnectionType transport, Exception exception)
        {
            _behaviors[transport] = () => throw exception;
            return this;
        }

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            cancellationToken.ThrowIfCancellationRequested();
            var transport = TransportOf(typeof(TConnection));
            Attempts.Add(transport);
            if (!_behaviors.TryGetValue(transport, out var behavior))
                throw new InvalidOperationException($"No behavior configured for {transport}.");
            return Task.FromResult((TConnection)behavior());
        }

        private static ConnectionType TransportOf(Type connectionType) => connectionType switch
        {
            _ when connectionType == typeof(ISmartCardConnection) => ConnectionType.SmartCard,
            _ when connectionType == typeof(IFidoHidConnection) => ConnectionType.HidFido,
            _ when connectionType == typeof(IOtpHidConnection) => ConnectionType.HidOtp,
            _ => throw new InvalidOperationException($"Unexpected connection type {connectionType.Name}.")
        };
    }

    private sealed class RecordingConnection(ConnectionType type)
        : ISmartCardConnection, IFidoHidConnection, IOtpHidConnection
    {
        public ConnectionType Type { get; } = type;
        public bool Disposed { get; private set; }
        public int DisposeAsyncCalls { get; private set; }
        public Exception? DisposeAsyncException { get; init; }
        public Transport Transport => Transport.Usb;
        public int PacketSize => 64;
        public int FeatureReportSize => 8;

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            DisposeAsyncCalls++;
            if (DisposeAsyncException is not null)
                return ValueTask.FromException(DisposeAsyncException);
            return ValueTask.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => false;

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingLogger : ILogger
    {
        public int LogCalls { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogCalls++;
            throw new InvalidOperationException("logging failed");
        }
    }
}