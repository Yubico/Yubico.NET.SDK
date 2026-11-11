using System.Buffers.Binary;
using System.Collections.Concurrent;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2.UnitTests;

public class Ctap2SessionLinuxCrossLayerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);
    private const uint Channel = 0x01020304;

    [Fact]
    public async Task CreateAndGetInfo_HeldNativeWriteReturnPendingAndIdleSessionsReuseBorrowedConnection()
    {
        using var api = new NativeFido { HoldWrite = true };
        await using var connection = await Device(api).ConnectAsync<IFidoHidConnection>(Ct);
        Task<Task<Ctap2Session>> invocation = Task.Factory.StartNew(
            () => Ctap2Session.CreateAsync(connection, cancellationToken: Ct), Ct,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            await api.WriteEntered.Task.WaitAsync(Bound, Ct);
            var pending = await invocation.WaitAsync(Bound, Ct);
            Assert.False(pending.IsCompleted);
        }
        finally { api.ReleaseWrite.Set(); }
        var creation = await invocation.WaitAsync(Bound, Ct);
        var first = await creation.WaitAsync(Bound, Ct);
        api.HoldGetInfo = true;
        var info = first.GetInfoAsync(Ct);
        await api.GetInfoSent.Task.WaitAsync(Bound, Ct);
        Assert.False(info.IsCompleted);
        api.HoldGetInfo = false;
        api.EmitInfo();
        Assert.Equal("FIDO_2_0", Assert.Single((await info.WaitAsync(Bound, Ct)).Versions));
        await first.DisposeAsync();
        Assert.Equal(0, api.Closes);
        await using (var second = await Ctap2Session.CreateAsync(connection, cancellationToken: Ct))
        {
            Assert.True(second.IsInitialized);
            Assert.Equal("FIDO_2_0", Assert.Single((await second.GetInfoAsync(Ct)).Versions));
            Assert.Equal(2, api.InitCount);
        }
        Assert.Equal(0, api.Closes);
        await connection.DisposeAsync();
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task MakeCredential_KeepaliveCancellationDrainsExactlyOneCancelBeforeCorrelatedReuse()
    {
        using var api = new NativeFido { CancelCredential = true };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var prompt = new CancellingPrompt(cancellation);
        var device = Device(api);
        await using var connection = await device.ConnectAsync<IFidoHidConnection>(Ct);
        await using var session = await Ctap2Session.CreateAsync(connection,
            new SessionCreationOptions { UserPresencePrompt = prompt }, Ct);
        var operation = session.MakeCredentialAsync(new byte[32], new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 1 }, "alice", "Alice"),
            [PublicKeyCredentialParameters.CreateES256()], cancellationToken: cancellation.Token);
        try
        {
            await api.CancelSent.Task.WaitAsync(Bound, Ct);
            Assert.False(operation.IsCompleted);
            Assert.Empty(prompt.Resolved);
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetInfoAsync(Ct));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => device.ConnectAsync<IFidoHidConnection>(Ct));
        }
        finally { api.ReleaseCancelResponse(); }
        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(Bound, Ct));
        Assert.Equal(cancellation.Token, cancelled.CancellationToken);
        Assert.Equal("FIDO_2_0", Assert.Single((await session.GetInfoAsync(Ct)).Versions));
        Assert.Equal(new byte[] { 0x86, 0x90, 0x90, 0, 0x91, 0x90 }, api.Commands);
        Assert.All(api.Channels.Skip(1), channel => Assert.Equal(Channel, channel));
        var request = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, request.Basis);
        Assert.Equal("FIDO2", request.Application);
        Assert.Equal("example.com", request.Scope);
        var (Context, Outcome, Token) = Assert.Single(prompt.Resolved);
        Assert.Same(request, Context);
        Assert.Equal(UserPresenceOutcome.Cancelled, Outcome);
        Assert.Equal(CancellationToken.None, Token);
        Assert.Equal(0, api.Closes);
    }

    [Fact]
    public async Task OwnedSession_PendingExchangeDisposalTerminalizesOperationButClaimWaitsForTrueDrain()
    {
        using var api = new NativeFido();
        var device = Device(api);
        var session = await device.CreateCtap2SessionAsync(cancellationToken: Ct);
        api.HoldGetInfo = true;
        api.HoldDrain = true;
        var operation = session.GetInfoAsync(Ct);
        await api.GetInfoSent.Task.WaitAsync(Bound, Ct);
        var disposal = session.DisposeAsync().AsTask();
        try
        {
            await api.Removal.Task.WaitAsync(Bound, Ct);
            // Write acceptance does not synchronize the following receive admission. A live reader
            // gets the terminal ODE; a receive entering after terminal wake gets its IOException wrapper.
            var failure = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, Ct));
            Assert.True(failure is ObjectDisposedException or IOException, failure?.ToString());
            Assert.True(operation.IsFaulted);
            Assert.False(disposal.IsCompleted);
            Assert.False(session.IsInitialized);
            Assert.Equal(0, api.Closes);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => device.ConnectAsync<IFidoHidConnection>(Ct));
        }
        finally { api.Drain.TrySetResult(); await disposal.WaitAsync(Bound, Ct); }
        Assert.Equal(1, api.Closes);
        await using var reopened = await device.ConnectAsync<IFidoHidConnection>(Ct);
    }

    private static YubiKeyDevice Device(NativeFido api)
    {
        var hid = new LinuxHidInterface(new HidDescriptorInfo
        {
            DevicePath = $"l6-ctap-{Guid.NewGuid():N}",
            VendorId = 0x1050,
            ProductId = 0x0407,
            UsagePage = 0xF1D0,
            Usage = 1
        });
        return new YubiKeyDevice($"l6-{Guid.NewGuid():N}", null,
            new HidConnectionSlot(hid, linuxReportApi: api, linuxReadReactor: api), null, null);
    }

    private sealed class CancellingPrompt(CancellationTokenSource cancellation) : IUserPresencePrompt
    {
        internal List<UserPresenceContext> Requested { get; } = [];
        internal List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolved { get; } = [];
        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken token)
        {
            Requested.Add(context);
            cancellation.Cancel();
            return ValueTask.CompletedTask;
        }
        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken token)
        {
            Resolved.Add((context, outcome, token));
            return ValueTask.CompletedTask;
        }
    }

    // Fake hidraw payloads and readiness BELOW the production Linux owner, never a fake typed connection.
    private sealed class NativeFido : ILinuxHidReportApi, ILinuxHidReadReactor, ILinuxHidReadRegistration, IDisposable
    {
        private readonly ConcurrentQueue<byte[]> _reports = new();
        private Action<long, short>? _ready;
        private bool _requestComplete, _cancelled;
        private ushort _requestLength;
        internal bool HoldWrite, HoldGetInfo, CancelCredential, HoldDrain;
        internal int InitCount, Closes;
        internal List<byte> Commands { get; } = [];
        internal List<uint> Channels { get; } = [];
        internal ManualResetEventSlim ReleaseWrite { get; } = new();
        internal TaskCompletionSource WriteEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource GetInfoSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource CancelSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Removal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Drain { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public long Generation { get; private set; }
        public Task<Exception?> Activation => Task.FromResult<Exception?>(null);
        public Task Quiescence => Drain.Task;
        public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags) => new();
        public (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle) => (64, 64);
        public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload)
        {
            if (!_reports.TryDequeue(out var report)) return new(LinuxReadDisposition.WouldBlock, 0, 11);
            report.CopyTo(payload);
            return new(LinuxReadDisposition.Data, report.Length, 0);
        }
        public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            WriteEntered.TrySetResult();
            if (HoldWrite) ReleaseWrite.Wait(Ct);
            var command = payload[4];
            Commands.Add(command);
            Channels.Add(BinaryPrimitives.ReadUInt32BigEndian(payload));
            if (command == 0x86)
            {
                InitCount++;
                byte[] init = new byte[17];
                payload.Slice(7, 8).CopyTo(init);
                BinaryPrimitives.WriteUInt32BigEndian(init.AsSpan(8), Channel);
                Emit(Response(uint.MaxValue, 0x86, init));
            }
            else if (command == 0x90)
            {
                _requestLength = BinaryPrimitives.ReadUInt16BigEndian(payload[5..]);
                if (CancelCredential && payload[7] == CtapCommand.MakeCredential)
                    Assert.True(_requestLength > 57);
                else
                {
                    Assert.Equal(CtapCommand.GetInfo, payload[7]);
                    Assert.Equal(1, _requestLength);
                    if (HoldGetInfo) GetInfoSent.TrySetResult(); else EmitInfo();
                }
            }
            else if (command == 0)
            {
                Assert.False(_requestComplete);
                Assert.True(_requestLength > 57);
                Assert.True(payload.Slice(5, _requestLength - 57).IndexOfAnyExcept((byte)0) >= 0);
                _requestComplete = true;
                Emit(Response(Channel, 0xBB, [2]));
                Emit(Response(Channel, 0xBB, [2]));
            }
            else
            {
                Assert.Equal(0x91, command);
                Assert.True(_requestComplete);
                Assert.False(_cancelled);
                _cancelled = true;
                byte[] response = new byte[58];
                response[0] = (byte)CtapStatus.KeepAliveCancel;
                Emit(Response(Channel, 0x90, response));
                CancelSent.TrySetResult();
            }
        }
        internal void EmitInfo() => Emit(Response(Channel, 0x90, [0, 0xA1, 1, 0x81, 0x68, 0x46, 0x49, 0x44, 0x4F, 0x5F, 0x32, 0x5F, 0x30]));
        internal void ReleaseCancelResponse() => Emit(Response(Channel, 0, []));
        private void Emit(byte[] report) { _reports.Enqueue(report); _ready?.Invoke(Generation, 1); }
        private static byte[] Response(uint channel, byte command, ReadOnlySpan<byte> data)
        {
            var report = new byte[64];
            BinaryPrimitives.WriteUInt32BigEndian(report, channel);
            report[4] = command;
            BinaryPrimitives.WriteUInt16BigEndian(report.AsSpan(5), (ushort)data.Length);
            data[..Math.Min(57, data.Length)].CopyTo(report.AsSpan(7));
            return report;
        }
        public ILinuxHidReadRegistration Register(LinuxFileSafeHandle handle, long generation, Action<long, short> onReady, Action<Exception> onTerminal)
        { Generation = generation; _ready = onReady; return this; }
        public Exception? RequestRemoval() { Removal.TrySetResult(); if (!HoldDrain) Drain.TrySetResult(); return null; }
        public void ReleaseReference() => Assert.True(Drain.Task.IsCompleted);
        public void Signal() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle) { Closes++; handle.Dispose(); return new(true, true, 0, 0); }
        public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload) => throw new NotSupportedException();
        public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) => throw new NotSupportedException();
        public void Dispose() => ReleaseWrite.Dispose();
    }
}