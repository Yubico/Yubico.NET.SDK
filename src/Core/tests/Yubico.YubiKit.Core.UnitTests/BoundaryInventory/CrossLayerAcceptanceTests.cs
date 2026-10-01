using System.Buffers.Binary;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

public class CrossLayerAcceptanceTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);
    private const uint Channel = 0x01020304;

    [Fact]
    public async Task FidoCancellation_AfterAllRequestFrames_SendsOneCancelAndDrainsBeforeSameChannelReuse()
    {
        using var abandonment = new CancellationTokenSource();
        var bridge = new FramedFidoBridge();
        var prompt = new RecordingPrompt(abandonment);
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        await using var protocol = new FidoHidProtocol(connection);
        byte[] request = new byte[58];
        request.AsSpan().Fill(0x5A);

        Task<ReadOnlyMemory<byte>> first = protocol.SendVendorCommandAsync(0x41, request,
            UserPresenceNotification.Create(prompt, new UserPresenceContext
            {
                Application = "FIDO2",
                Basis = UserPresenceBasis.PolicyRequires
            }), abandonment.Token);
        try
        {
            await bridge.CancelSent.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
            Assert.False(first.IsCompleted);
            InvalidOperationException overlap = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                protocol.SendVendorCommandAsync(0x41, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));
            Assert.Contains("already has an exchange in flight", overlap.Message, StringComparison.Ordinal);
        }
        finally
        {
            bridge.ReleaseContinuation();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(Bound, TestContext.Current.CancellationToken));
        ReadOnlyMemory<byte> second = await protocol.SendVendorCommandAsync(0x41, ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken).WaitAsync(Bound, TestContext.Current.CancellationToken);

        Assert.Equal((byte)0xAC, second.Span[0]);
        Assert.Equal(2, bridge.VendorCount);
        Assert.Equal(1, bridge.InitCount);
        Assert.Equal(new byte[] { 0x86, 0xC1, 0x00, 0x91, 0xC1 }, bridge.Commands);
        Assert.All(bridge.Channels.Skip(1), channel => Assert.Equal(Channel, channel));
        Assert.Equal(1, prompt.RequestCount);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, prompt.RequestBasis);
        Assert.Equal(0, prompt.ResolutionCount);
    }

    private sealed class RecordingPrompt(CancellationTokenSource abandonment) : IUserPresencePrompt
    {
        public int RequestCount { get; private set; }
        public int ResolutionCount { get; private set; }
        public UserPresenceBasis? RequestBasis { get; private set; }

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestBasis = context.Basis;
            Assert.False(cancellationToken.IsCancellationRequested);
            Assert.False(abandonment.IsCancellationRequested);
            abandonment.Cancel();
            return ValueTask.CompletedTask;
        }

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            ResolutionCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FramedFidoBridge : IHidInputBridge
    {
        private Action<ReadOnlyMemory<byte>>? _report;
        private byte[]? _continuation;
        private ushort _requestLength;
        private bool _requestCompleted;
        private bool _cancelAccepted;
        public TaskCompletionSource CancelSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<byte> Commands { get; } = [];
        public List<uint> Channels { get; } = [];
        public int VendorCount { get; private set; }
        public int InitCount { get; private set; }

        public nint CreateDevice(long entryId) => 1;
        public void OpenDevice(nint device) { }
        public int InputSize(nint device) => 64;
        public bool CloseUnstarted(nint device) => true;
        public void ReleaseDevice(nint device) { }
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
            if (command == 0x86)
            {
                SendInit(packet);
            }
            else if (command == 0x00)
            {
                AcceptContinuation(packet);
            }
            else if (command == 0x91)
            {
                AcceptCancel();
            }
            else if (command == 0xC1)
            {
                AcceptVendor(packet);
            }
        }

        private void SendInit(byte[] packet)
        {
            InitCount++;
            byte[] init = Response(uint.MaxValue, 0x86, new byte[17]);
            packet.AsSpan(7, 8).CopyTo(init.AsSpan(7));
            BinaryPrimitives.WriteUInt32BigEndian(init.AsSpan(15), Channel);
            _report?.Invoke(init);
        }

        private void AcceptContinuation(byte[] packet)
        {
            if (VendorCount != 1 || _requestLength != 58 || packet[5] != 0x5A)
                throw new InvalidOperationException("Incomplete vendor request before keep-alive");
            // Only after the last request frame has been accepted may the device ask for touch.
            _requestCompleted = true;
            _report?.Invoke(Response(Channel, 0xBB, [0x02]));
            _report?.Invoke(Response(Channel, 0xBB, [0x02]));
        }

        private void AcceptCancel()
        {
            if (!_requestCompleted || _cancelAccepted)
                throw new InvalidOperationException("CANCEL before full request or repeated CANCEL");
            _cancelAccepted = true;
            byte[] payload = new byte[58];
            payload.AsSpan().Fill(0x2D);
            _report?.Invoke(Response(Channel, 0xC1, payload));
            _continuation = new byte[64];
            BinaryPrimitives.WriteUInt32BigEndian(_continuation, Channel);
            _continuation[5] = payload[57];
            CancelSent.TrySetResult();
        }

        private void AcceptVendor(byte[] packet)
        {
            VendorCount++;
            if (VendorCount == 1)
            {
                _requestLength = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(5));
                if (packet.AsSpan(7, 57).IndexOfAnyExcept((byte)0x5A) >= 0)
                    throw new InvalidOperationException("Incomplete first vendor frame");
            }
            if (VendorCount == 2)
                _report?.Invoke(Response(Channel, 0xC1, [0xAC]));
        }

        public void ReleaseContinuation()
        {
            if (_continuation is { } continuation)
                _report?.Invoke(continuation);
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