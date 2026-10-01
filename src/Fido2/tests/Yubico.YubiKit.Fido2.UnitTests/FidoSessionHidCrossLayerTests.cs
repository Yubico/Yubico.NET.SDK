using System.Buffers.Binary;
using System.Formats.Cbor;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.UnitTests;

public class FidoSessionHidCrossLayerTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);
    private const uint Channel = 0x01020304;

    [Fact]
    public async Task CreateAsync_AfterIdleSessionDisposal_ReusesBorrowedMacConnection()
    {
        var bridge = new FramedFidoBridge(getInfoOnly: true);
        await using var connection = await MacOSFidoHidConnection.OpenAsync(
            1, bridge, TestContext.Current.CancellationToken);

        var first = await FidoSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        await first.DisposeAsync();

        Assert.False(bridge.Released);
        var second = await FidoSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(Bound, TestContext.Current.CancellationToken);
        try
        {
            Assert.Equal("FIDO_2_0", Assert.Single((await second.GetInfoAsync(TestContext.Current.CancellationToken)
                .WaitAsync(Bound, TestContext.Current.CancellationToken)).Versions));
            Assert.Equal(2, bridge.InitCount);
            Assert.Equal(3, bridge.CborCount);
            Assert.False(bridge.Released);
        }
        finally
        {
            await second.DisposeAsync();
        }

        await connection.DisposeAsync();
        Assert.True(bridge.Released);
    }

    [Fact]
    public async Task DisposeAsync_WithPendingGetInfo_TerminallyWakesButDoesNotReleaseBorrowedMacConnection()
    {
        var bridge = new FramedFidoBridge(getInfoOnly: true);
        await using var connection = await MacOSFidoHidConnection.OpenAsync(
            1, bridge, TestContext.Current.CancellationToken);
        var session = await FidoSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        bridge.HoldGetInfo = true;
        Task<AuthenticatorInfo> operation = session.GetInfoAsync(TestContext.Current.CancellationToken);
        await bridge.GetInfoSent.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
        Assert.False(operation.IsCompleted);
        await session.DisposeAsync().AsTask().WaitAsync(Bound, TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            operation.WaitAsync(Bound, TestContext.Current.CancellationToken));
        Assert.True(operation.IsFaulted);
        Assert.False(bridge.Released);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            FidoSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));
        await connection.DisposeAsync();
        Assert.True(bridge.Released);
    }

    [Fact]
    public async Task MakeCredentialAsync_CancelledAtDeviceWaiting_DrainsBeforeRecoveryAndDoesNotReleaseBorrowedConnection()
    {
        using var cancellation = new CancellationTokenSource();
        var bridge = new FramedFidoBridge();
        var prompt = new CancellingPrompt(cancellation);
        await using var connection = await MacOSFidoHidConnection.OpenAsync(
            1, bridge, TestContext.Current.CancellationToken);
        var options = new SessionCreationOptions { UserPresencePrompt = prompt };
        var session = await FidoSession.CreateAsync(connection, options, TestContext.Current.CancellationToken);
        try
        {
            Task<MakeCredentialResponse> operation = session.MakeCredentialAsync(
                new byte[32], new PublicKeyCredentialRpEntity("example.com"),
                new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
                [PublicKeyCredentialParameters.CreateES256()], cancellationToken: cancellation.Token);
            try
            {
                await bridge.CancelSent.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
                Assert.False(operation.IsCompleted);
                Assert.Empty(prompt.Resolved);
                InvalidOperationException overlap = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    session.GetInfoAsync(TestContext.Current.CancellationToken));
                Assert.Contains("already has an exchange in flight", overlap.Message, StringComparison.Ordinal);
            }
            finally
            {
                bridge.ReleaseContinuation();
            }

            OperationCanceledException cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                operation.WaitAsync(Bound, TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, cancelled.CancellationToken);
            Assert.Equal("FIDO_2_0", Assert.Single((await session.GetInfoAsync(TestContext.Current.CancellationToken)
                .WaitAsync(Bound, TestContext.Current.CancellationToken)).Versions));
            Assert.Equal(1, bridge.InitCount);
            Assert.Equal(3, bridge.CborCount);
            Assert.Equal(new byte[] { 0x86, 0x90, 0x90, 0x00, 0x91, 0x90 }, bridge.Commands);
            Assert.All(bridge.Channels.Skip(1), channel => Assert.Equal(Channel, channel));
            UserPresenceContext request = Assert.Single(prompt.Requested);
            Assert.Equal(UserPresenceBasis.DeviceWaiting, request.Basis);
            Assert.Equal("FIDO2", request.Application);
            Assert.Equal("example.com", request.Scope);
            var resolution = Assert.Single(prompt.Resolved);
            Assert.Same(request, resolution.Context);
            Assert.Equal(UserPresenceOutcome.Cancelled, resolution.Outcome);
            Assert.Equal(CancellationToken.None, resolution.Token);
        }
        finally
        {
            await session.DisposeAsync();
        }

        Assert.False(bridge.Released);
        await connection.DisposeAsync();
        Assert.True(bridge.Released);
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

    private sealed class CancellingPrompt(CancellationTokenSource cancellation) : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];
        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolved { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken token)
        {
            Assert.False(token.IsCancellationRequested);
            Requested.Add(context);
            cancellation.Cancel();
            return ValueTask.CompletedTask;
        }

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome,
            CancellationToken token)
        {
            Resolved.Add((context, outcome, token));
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FramedFidoBridge(bool getInfoOnly = false) : IHidInputBridge
    {
        private Action<ReadOnlyMemory<byte>>? _report;
        private byte[]? _continuation;
        private bool _cancelAccepted;
        private bool _requestComplete;
        private ushort _requestLength;
        public TaskCompletionSource CancelSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource GetInfoSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldGetInfo { get; set; }
        public List<byte> Commands { get; } = [];
        public List<uint> Channels { get; } = [];
        public int InitCount { get; private set; }
        public int CborCount { get; private set; }
        public bool Released { get; private set; }

        public nint CreateDevice(long entryId) => 1;
        public void OpenDevice(nint device) { }
        public int InputSize(nint device) => 64;
        public bool CloseUnstarted(nint device) => true;
        public void ReleaseDevice(nint device) => Released = true;
        public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
        {
            _report = report;
            return 2;
        }
        public int Start(nint owner) => 0;
        public void Cancel(nint owner) { }
        public int WaitShutdown(nint owner) => 0;
        public int Destroy(nint owner) => 0;

        public void SetReport(nint device, byte[] packet)
        {
            byte command = packet[4];
            Commands.Add(command);
            Channels.Add(BinaryPrimitives.ReadUInt32BigEndian(packet));
            _ = command switch
            {
                0x86 => SendInit(packet),
                0x90 => AcceptCbor(packet),
                0x00 => AcceptContinuation(packet),
                0x91 => AcceptCancel(),
                _ => throw new InvalidOperationException($"Unexpected HID command {command:X2}")
            };
        }

        private bool SendInit(byte[] packet)
        {
            InitCount++;
            byte[] init = Response(uint.MaxValue, 0x86, new byte[17]);
            packet.AsSpan(7, 8).CopyTo(init.AsSpan(7));
            BinaryPrimitives.WriteUInt32BigEndian(init.AsSpan(15), Channel);
            _report?.Invoke(init);
            return true;
        }

        private bool AcceptCbor(byte[] packet)
        {
            if (BinaryPrimitives.ReadUInt32BigEndian(packet) != Channel)
                throw new InvalidOperationException("Wrong channel");
            CborCount++;
            bool makeCredential = CborCount == 2 && !getInfoOnly;
            ushort length = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(5));
            ValidateCborRequest(packet[7], length, makeCredential);
            if (makeCredential)
                _requestLength = length;
            else
                SendGetInfoResponse();
            return true;
        }

        private static void ValidateCborRequest(byte command, ushort length, bool makeCredential)
        {
            bool expected = makeCredential
                ? command == CtapCommand.MakeCredential && length > 57
                : command == CtapCommand.GetInfo && length == 1;
            if (!expected)
                throw new InvalidOperationException("Unexpected CBOR request");
        }

        private void SendGetInfoResponse()
        {
            if (HoldGetInfo)
            {
                GetInfoSent.TrySetResult();
                return;
            }
            _report?.Invoke(Response(Channel, 0x90, [0x00, .. MinimalGetInfoResponse()]));
        }

        private bool AcceptContinuation(byte[] packet)
        {
            if (CborCount != 2 || _requestComplete || packet[4] != 0 ||
                packet.AsSpan(5, _requestLength - 57).IndexOfAnyExcept((byte)0) < 0)
                throw new InvalidOperationException("Unexpected request continuation");
            _requestComplete = true;
            _report?.Invoke(Response(Channel, 0xBB, [0x02]));
            _report?.Invoke(Response(Channel, 0xBB, [0x02]));
            return true;
        }

        private bool AcceptCancel()
        {
            if (!_requestComplete || _cancelAccepted)
                throw new InvalidOperationException("CANCEL before full request or repeated CANCEL");
            _cancelAccepted = true;
            byte[] payload = new byte[58];
            payload[0] = (byte)CtapStatus.KeepAliveCancel;
            _report?.Invoke(Response(Channel, 0x90, payload));
            _continuation = new byte[64];
            BinaryPrimitives.WriteUInt32BigEndian(_continuation, Channel);
            CancelSent.TrySetResult();
            return true;
        }

        public void ReleaseContinuation()
        {
            if (_continuation is { } continuation)
            {
                _continuation = null;
                _report?.Invoke(continuation);
            }
        }

        private static byte[] Response(uint channel, byte command, ReadOnlySpan<byte> payload)
        {
            byte[] packet = new byte[64];
            BinaryPrimitives.WriteUInt32BigEndian(packet, channel);
            packet[4] = command;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(5), (ushort)payload.Length);
            payload[..Math.Min(payload.Length, 57)].CopyTo(packet.AsSpan(7));
            return packet;
        }
    }
}