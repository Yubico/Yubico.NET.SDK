using FluentAssertions;
using NSubstitute;
using System.Formats.Cbor;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Fido2.UnitTests;

public class FidoSessionTests
{
    [Fact]
    public async Task CreateAsync_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => FidoSession.CreateAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_UnsupportedConnectionType_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedConnection = Substitute.For<IConnection>();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => FidoSession.CreateAsync(unsupportedConnection, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Transport.Returns(Transport.Usb);
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReadOnlyMemory<byte>>(
                new InvalidOperationException("session-init probe failure")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FidoSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // Borrowed: the session did not create this connection, so disposal is the caller's.
        // Upstream asserted 1 here because its protocols disposed the connection; this branch
        // deliberately removed that (see ProtocolConnectionOwnershipTests).
        connection.DidNotReceive().Dispose();
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_UsbBefore58_ThrowsNotSupportedException()
    {
        var exception = Assert.Throws<NotSupportedException>(() =>
            FidoSession.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(5, 7, 2)));

        exception.Message.Should().Contain("firmware 5.8.0");
        exception.Message.Should().Contain("IFidoHidConnection");
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_Usb58_Succeeds()
    {
        FidoSession.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(5, 8, 0));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 255, 255)]
    public void EnsureSmartCardTransportSupported_UsbSentinelFirmware_Succeeds(int major, int minor, int patch)
    {
        FidoSession.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(major, minor, patch));
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_ReportedNfcBefore58_Succeeds()
    {
        FidoSession.EnsureSmartCardTransportSupported(Transport.Nfc, new FirmwareVersion(5, 0, 0));
    }

    [Fact]
    public async Task DisposeAsync_OwnedConnection_UsesAsyncDisposalAndIsIdempotent()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00]);
        var device = new SingleConnectionYubiKey(connection);
        var session = await device.CreateFidoSessionAsync(
            preferredConnection: ConnectionType.SmartCard,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.DisposeAsync();
        await session.DisposeAsync();

        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(1, connection.DisposeAsyncCount);
    }

    [Fact]
    public async Task DisposeAsync_OwnedConnectionFailure_IsSharedAndLeavesSessionDisposed()
    {
        var expected = new InvalidOperationException("async connection teardown failed");
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00])
        {
            DisposeAsyncException = expected
        };
        var device = new SingleConnectionYubiKey(connection);
        var session = await device.CreateFidoSessionAsync(
            preferredConnection: ConnectionType.SmartCard,
            cancellationToken: TestContext.Current.CancellationToken);

        Exception? first = await Record.ExceptionAsync(async () => await session.DisposeAsync());
        Exception? second = await Record.ExceptionAsync(async () => await session.DisposeAsync());

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(1, connection.DisposeAsyncCount);
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.GetInfoAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DisposeAsync_WhileOwnedConnectionTeardownIsPaused_GetInfoAsyncThrowsObjectDisposedException()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00])
        {
            PauseAsyncDisposal = true
        };
        var device = new SingleConnectionYubiKey(connection);
        var session = await device.CreateFidoSessionAsync(
            preferredConnection: ConnectionType.SmartCard,
            cancellationToken: TestContext.Current.CancellationToken);

        Task disposal = session.DisposeAsync().AsTask();
        await connection.AsyncDisposalStarted.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => session.GetInfoAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            connection.ResumeAsyncDisposal();
            await disposal;
        }
    }

    [Fact]
    public async Task MakeCredentialAsync_AfterDisposal_InvalidArgumentsThrowObjectDisposedBeforeValidation()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00]);
        var session = await FidoSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.MakeCredentialAsync(
                default,
                null!,
                null!,
                null!,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(typeof(FidoSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
    }

    private static byte[] MinimalGetInfoResponse()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(0x01);
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteEndArray();
        writer.WriteEndMap();
        return writer.Encode();
    }

    private sealed class DisposeTrackingSmartCardConnection(params byte[][] responses) : ISmartCardConnection
    {
        private readonly Queue<byte[]> _responses = new(responses);
        private readonly TaskCompletionSource _asyncDisposalStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _resumeAsyncDisposal = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int DisposeCount { get; private set; }
        public int DisposeAsyncCount { get; private set; }
        public Exception? DisposeAsyncException { get; init; }
        public bool PauseAsyncDisposal { get; init; }
        public Task AsyncDisposalStarted => _asyncDisposalStarted.Task;
        public List<byte[]> TransmittedCommands { get; } = [];
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            TransmittedCommands.Add(command.ToArray());
            return Task.FromResult((ReadOnlyMemory<byte>)_responses.Dequeue());
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            NullDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose() => DisposeCount++;

        public async ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            _asyncDisposalStarted.TrySetResult();

            if (PauseAsyncDisposal)
            {
                await _resumeAsyncDisposal.Task.ConfigureAwait(false);
            }

            if (DisposeAsyncException is not null)
            {
                throw DisposeAsyncException;
            }
        }

        public void ResumeAsyncDisposal() => _resumeAsyncDisposal.TrySetResult();
    }

    private sealed class SingleConnectionYubiKey(ISmartCardConnection connection) : IYubiKey
    {
        public string DeviceId => "fido-disposal-probe";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromResult((connection as TConnection)!);
    }

    private sealed class NullDisposable : IDisposable
    {
        public static NullDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
