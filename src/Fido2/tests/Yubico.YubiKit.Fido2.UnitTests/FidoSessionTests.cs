using FluentAssertions;
using NSubstitute;
using System.Buffers.Binary;
using System.Formats.Cbor;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Fido2.Backend;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;

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
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
            TestContext.Current.CancellationToken);

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
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
            TestContext.Current.CancellationToken);

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
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
            TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task MakeCredentialAsync_WithSmartCardPrompt_NotifiesRequiredRpScopeAndTimeout()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [(byte)CtapStatus.UserActionTimeout, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await FidoSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        CtapException exception = await Assert.ThrowsAsync<CtapException>(() =>
            session.MakeCredentialAsync(
                new byte[32],
                new PublicKeyCredentialRpEntity("example.com"),
                new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
                [PublicKeyCredentialParameters.CreateES256()],
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(CtapStatus.UserActionTimeout, exception.Status);
        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.PolicyRequires, requested.Basis);
        Assert.Equal("FIDO2", requested.Application);
        Assert.Equal(UserPresenceOperations.Fido2.MakeCredential, requested.Operation);
        Assert.Equal("example.com", requested.Scope);
        var resolved = Assert.Single(prompt.Resolved);
        Assert.Same(requested, resolved.Context);
        Assert.Equal(UserPresenceOutcome.TimedOut, resolved.Outcome);
        Assert.Equal(CancellationToken.None, resolved.CancellationToken);
    }

    [Fact]
    public async Task SmartCardBackend_WhenResolutionThrowsAfterSuccess_PropagatesResolutionException()
    {
        var connection = new DisposeTrackingSmartCardConnection([0x00, 0xAA, 0x90, 0x00]);
        var expected = new InvalidOperationException("resolution failed");
        var prompt = new RecordingUserPresencePrompt(resolutionException: expected);
        var backend = new SmartCardBackend(new PcscProtocol(connection));
        UserPresenceNotification notification =
            UserPresenceNotification.Create(prompt, CreatePolicyContext());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            backend.SendCborAsync(
                new byte[] { (byte)CtapCommand.MakeCredential },
                notification,
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task SmartCardBackend_WhenOperationAndResolutionThrow_PreservesOperationException()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [(byte)CtapStatus.UserActionTimeout, 0x90, 0x00]);
        var resolutionException = new InvalidOperationException("resolution failed");
        var prompt = new RecordingUserPresencePrompt(resolutionException: resolutionException);
        var backend = new SmartCardBackend(new PcscProtocol(connection));
        UserPresenceNotification notification =
            UserPresenceNotification.Create(prompt, CreatePolicyContext());

        CtapException actual = await Assert.ThrowsAsync<CtapException>(() => backend.SendCborAsync(
            new byte[] { (byte)CtapCommand.MakeCredential },
            notification,
            TestContext.Current.CancellationToken));

        Assert.Equal(CtapStatus.UserActionTimeout, actual.Status);
        Assert.Single(prompt.Resolved);
    }

    [Fact]
    public async Task SmartCardBackend_WhenRequestThrows_DoesNotTransmitOrResolve()
    {
        var connection = new DisposeTrackingSmartCardConnection([0x00, 0xAA, 0x90, 0x00]);
        var expected = new InvalidOperationException("request failed");
        var prompt = new RecordingUserPresencePrompt(requestException: expected);
        var backend = new SmartCardBackend(new PcscProtocol(connection));
        UserPresenceNotification notification =
            UserPresenceNotification.Create(prompt, CreatePolicyContext());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            backend.SendCborAsync(
                new byte[] { (byte)CtapCommand.MakeCredential },
                notification,
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Empty(connection.TransmittedCommands);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task GetAssertionAsync_WithSmartCardPrompt_ReportsOperation()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [(byte)CtapStatus.NoCredentials, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await FidoSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await Assert.ThrowsAsync<CtapException>(() => session.GetAssertionAsync(
            "example.com", new byte[32], cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(UserPresenceOperations.Fido2.GetAssertion, Assert.Single(prompt.Requested).Operation);
        Assert.Same(prompt.Requested[0], Assert.Single(prompt.Resolved).Context);
    }

    [Fact]
    public async Task GetAssertionAsync_WithUserPresenceFalse_DoesNotNotifySmartCardPrompt()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [(byte)CtapStatus.NoCredentials, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await FidoSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        CtapException exception = await Assert.ThrowsAsync<CtapException>(() =>
            session.GetAssertionAsync(
                "example.com",
                new byte[32],
                new GetAssertionOptions { UserPresence = false },
                TestContext.Current.CancellationToken));

        Assert.Equal(CtapStatus.NoCredentials, exception.Status);
        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task SendCborRequestAsync_WithPrompt_DoesNotInferUserPresence()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [0x00, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await FidoSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await session.SendCborRequestAsync(
            new byte[] { CtapCommand.Selection },
            TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task SelectionAsync_WithSmartCardPrompt_DoesNotPredictUserPresence()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [0x00, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await FidoSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        await session.SelectionAsync(TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Theory]
    [InlineData(false, UserPresenceOperations.Fido2.Selection)]
    [InlineData(true, UserPresenceOperations.Fido2.Reset)]
    public async Task HidWait_ReportsSessionOperation(bool reset, string expectedOperation)
    {
        var connection = new TouchWaitingHidConnection(MinimalGetInfoResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await FidoSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await Assert.ThrowsAsync<CtapException>(() => reset
            ? session.ResetAsync(TestContext.Current.CancellationToken)
            : session.SelectionAsync(TestContext.Current.CancellationToken));

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceApplications.Fido2, requested.Application);
        Assert.Equal(expectedOperation, requested.Operation);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
        Assert.Same(requested, Assert.Single(prompt.Resolved).Context);
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

    private static UserPresenceContext CreatePolicyContext() => new()
    {
        Basis = UserPresenceBasis.PolicyRequires,
        Application = "FIDO2",
        Scope = "example.com"
    };

    private sealed class TouchWaitingHidConnection(byte[] getInfo) : IFidoHidConnection
    {
        private byte[]? _init;
        private int _responseCount;
        public int PacketSize => CtapConstants.PacketSize;
        public ConnectionType Type => ConnectionType.HidFido;

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
        {
            if ((packet.Span[4] & 0x7F) == CtapConstants.CtapHidInit)
                _init = packet.ToArray();
            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            byte[] response = _responseCount++ switch
            {
                0 => InitResponse(),
                1 => Packet(0x01020304, CtapConstants.CtapHidCbor, [0x00, .. getInfo]),
                2 => Packet(0x01020304, CtapConstants.CtapHidKeepAlive, [0x02]),
                _ => Packet(0x01020304, CtapConstants.CtapHidCbor, [(byte)CtapStatus.UserActionTimeout])
            };
            return Task.FromResult<ReadOnlyMemory<byte>>(response);
        }

        private byte[] InitResponse()
        {
            var payload = new byte[17];
            (_init ?? throw new InvalidOperationException("INIT not sent"))
                .AsSpan(CtapConstants.InitHeaderSize, 8).CopyTo(payload);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8), 0x01020304);
            payload[12] = 2;
            return Packet(CtapConstants.BroadcastChannelId, CtapConstants.CtapHidInit, payload);
        }

        private static byte[] Packet(uint channel, byte command, ReadOnlySpan<byte> data)
        {
            var packet = new byte[CtapConstants.PacketSize];
            BinaryPrimitives.WriteUInt32BigEndian(packet, channel);
            packet[4] = (byte)(command | 0x80);
            packet[5] = (byte)(data.Length >> 8);
            packet[6] = (byte)data.Length;
            data.CopyTo(packet.AsSpan(CtapConstants.InitHeaderSize));
            return packet;
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
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

    private sealed class RecordingUserPresencePrompt(
        Exception? requestException = null,
        Exception? resolutionException = null) : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];
        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken)>
            Resolved
        { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requested.Add(context);
            return requestException is null
                ? default
                : ValueTask.FromException(requestException);
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            return resolutionException is null
                ? default
                : ValueTask.FromException(resolutionException);
        }
    }
}