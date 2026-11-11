using System.Buffers.Binary;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2.UnitTests;

public class Ctap2SessionWindowsCrossLayerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task CreateAndGetInfo_HeldNativeWriteReturnPendingAndIdleSessionsReuseBorrowedConnection()
    {
        await using var journey = new Journey();
        var api = journey.First;
        api.HoldWrite = true;
        var connection = await journey.ConnectAsync();
        Task<Task<Ctap2Session>> invocation = Task.Factory.StartNew(
            () => journey.CreateAsync(), Ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            await api.WriteEntered.Task.WaitAsync(Bound, Ct);
            var pending = await invocation.WaitAsync(Bound, Ct);
            Assert.False(pending.IsCompleted);
        }
        finally
        {
            api.ReleaseWrite.Set();
            // Join invocation before Journey enumerates its tracked creations during cleanup.
            await invocation.WaitAsync(Bound, CancellationToken.None);
        }
        var creation = await invocation.WaitAsync(Bound, Ct);
        var first = await creation.WaitAsync(Bound, Ct);
        api.HoldNextWrite();
        api.HoldGetInfo = true;
        Task<Task<AuthenticatorInfo>> infoInvocation = Task.Factory.StartNew(
            () => first.GetInfoAsync(Ct), Ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            await api.WriteEntered.Task.WaitAsync(Bound, Ct);
            Assert.False((await infoInvocation.WaitAsync(Bound, Ct)).IsCompleted);
        }
        finally { api.ReleaseWrite.Set(); }
        var info = await infoInvocation.WaitAsync(Bound, Ct);
        await api.GetInfoSent.Task.WaitAsync(Bound, Ct);
        await api.PendingRead.Task.WaitAsync(Bound, Ct);
        Assert.False(info.IsCompleted);
        api.HoldGetInfo = false;
        api.EmitInfo();
        Assert.Equal("FIDO_2_0", Assert.Single((await info.WaitAsync(Bound, Ct)).Versions));
        await first.DisposeAsync();
        Assert.Equal(0, api.Closes);
        var second = await journey.CreateAsync();
        Assert.True(second.IsInitialized);
        Assert.Equal("FIDO_2_0", Assert.Single((await second.GetInfoAsync(Ct)).Versions));
        Assert.Equal(2, api.InitCount);
        await second.DisposeAsync();
        Assert.Equal(0, api.Closes);
        await connection.DisposeAsync();
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, journey.Opens);
    }

    [Fact]
    public async Task MakeCredential_KeepaliveCancellationDrainsExactlyOneCancelBeforeCorrelatedReuse()
    {
        await using var journey = new Journey();
        var api = journey.First;
        api.CancelCredential = true;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var prompt = new CancellingPrompt(cancellation);
        await journey.ConnectAsync();
        var session = await journey.CreateAsync(new SessionCreationOptions { UserPresencePrompt = prompt });
        var operation = session.MakeCredentialAsync(new byte[32], new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 1 }, "alice", "Alice"),
            [PublicKeyCredentialParameters.CreateES256()], cancellationToken: cancellation.Token);
        try
        {
            // Read of the terminal response's continuation, not just CANCEL submission.
            await api.RecoveryRead.Task.WaitAsync(Bound, Ct);
            Assert.False(operation.IsCompleted);
            Assert.Empty(prompt.Resolved);
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetInfoAsync(Ct));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => journey.Device.ConnectAsync<IFidoHidConnection>(Ct));
        }
        finally { api.ReleaseCancelResponse(); }
        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(Bound, Ct));
        Assert.Equal(cancellation.Token, cancelled.CancellationToken);
        Assert.Equal("FIDO_2_0", Assert.Single((await session.GetInfoAsync(Ct)).Versions));
        Assert.Equal(new byte[] { 0x86, 0x90, 0x90, 0, 0x91, 0x90 }, api.Writes.Select(packet => packet[4]));
        Assert.All(api.Writes.Skip(1), packet =>
            Assert.Equal(WindowsCtapReportApi.Channel, BinaryPrimitives.ReadUInt32BigEndian(packet)));
        Assert.Equal(1, api.CancelCount);
        Assert.Equal(0, api.NativeCancelCount);
        AssertCredentialPackets(api.Writes[2], api.Writes[3]);
        var request = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, request.Basis);
        Assert.Equal(UserPresenceOperations.Ctap2.MakeCredential, request.Operation);
        Assert.Equal("FIDO2", request.Application);
        Assert.Equal("example.com", request.Scope);
        var (context, outcome, token) = Assert.Single(prompt.Resolved);
        Assert.Same(request, context);
        Assert.Equal(UserPresenceOutcome.Cancelled, outcome);
        Assert.Equal(CancellationToken.None, token);
        Assert.Equal(0, api.Closes);

        // CTAP status belongs to HidBackend, not OTP's CRC validator. Valid CBOR behind an error
        // must still fail, and a subsequent correlated GetInfo must remain reusable.
        api.InfoStatus = CtapStatus.InvalidCommand;
        var statusError = await Assert.ThrowsAsync<CtapException>(() => session.GetInfoAsync(Ct));
        Assert.Equal(CtapStatus.InvalidCommand, statusError.Status);
        api.InfoStatus = CtapStatus.Success;
        Assert.Equal("FIDO_2_0", Assert.Single((await session.GetInfoAsync(Ct)).Versions));
        Assert.Single(prompt.Requested);
        Assert.Single(prompt.Resolved);
    }

    [Fact]
    public async Task OwnedSession_PendingExchangeDisposalTerminalizesOperationButClaimWaitsForNativeClose()
    {
        await using var journey = new Journey();
        var api = journey.First;
        var session = await journey.CreateOwnedAsync();
        api.HoldGetInfo = true;
        api.HoldClose = true;
        var operation = session.GetInfoAsync(Ct);
        await api.PendingRead.Task.WaitAsync(Bound, Ct);
        var disposal = session.DisposeAsync().AsTask();
        try
        {
            await api.Closing.Task.WaitAsync(Bound, Ct);
            var failure = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, Ct));
            Assert.True(failure is ObjectDisposedException or IOException, failure?.ToString());
            Assert.True(operation.IsFaulted);
            Assert.False(disposal.IsCompleted);
            Assert.False(session.IsInitialized);
            Assert.Equal(1, api.NativeCancelCount);
            Assert.Equal(0, api.Closes);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => journey.Device.ConnectAsync<IFidoHidConnection>(Ct));
            Assert.Equal(1, journey.Opens);
        }
        finally { api.ReleaseClose.Set(); }
        await disposal.WaitAsync(Bound, Ct);
        Assert.Equal(1, api.Closes);
        await journey.ConnectAsync();
        Assert.Equal(2, journey.Opens);
        Assert.NotSame(api, journey.Latest);
        var reopened = await journey.CreateAsync();
        Assert.Equal("FIDO_2_0", Assert.Single((await reopened.GetInfoAsync(Ct)).Versions));
    }

    private static void AssertCredentialPackets(byte[] initial, byte[] continuation)
    {
        // Independent canonical wire vector: command, hash, RP, user, then ES256 parameters.
        byte[] expected = Convert.FromHexString(
            "01A4015820" +
            "0000000000000000000000000000000000000000000000000000000000000000" +
            "02A16269646B6578616D706C652E636F6D" +
            "03A36269644101646E616D6565616C6963656B646973706C61794E616D6565416C696365" +
            "0481A263616C672664747970656A7075626C69632D6B6579");
        Assert.Equal(expected.Length, BinaryPrimitives.ReadUInt16BigEndian(initial.AsSpan(5)));
        Assert.True(expected.AsSpan(0, 57).SequenceEqual(initial.AsSpan(7, 57)));
        Assert.True(expected.AsSpan(57).SequenceEqual(continuation.AsSpan(5, expected.Length - 57)));
        Assert.True(continuation.AsSpan(5 + expected.Length - 57).IndexOfAnyExcept((byte)0) < 0);
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

    private sealed class Journey : IAsyncDisposable
    {
        private readonly List<WindowsCtapReportApi> _apis = [];
        private readonly List<Task<Ctap2Session>> _sessions = [];
        private IFidoHidConnection? _connection;
        internal WindowsCtapReportApi First { get; } = new();
        internal WindowsCtapReportApi Latest => _apis[^1];
        internal int Opens => _apis.Count;
        internal YubiKeyDevice Device { get; }

        internal Journey()
        {
            var hid = new WindowsHidInterface(new HidDescriptorInfo
            {
                DevicePath = $"w2-ctap-{Guid.NewGuid():N}",
                VendorId = 0x1050,
                ProductId = 0x0407,
                UsagePage = 0xF1D0,
                Usage = 1
            });
            Device = new YubiKeyDevice($"w2-{Guid.NewGuid():N}", null,
                new HidConnectionSlot(hid, windowsReportApiFactory: () =>
                {
                    var api = _apis.Count == 0 ? First : new WindowsCtapReportApi();
                    _apis.Add(api);
                    return api;
                }), null, null);
        }

        internal async Task<IFidoHidConnection> ConnectAsync()
        {
            _connection = await Device.ConnectAsync<IFidoHidConnection>(Ct);
            return _connection;
        }

        internal Task<Ctap2Session> CreateAsync(SessionCreationOptions? options = null)
        {
            var task = Ctap2Session.CreateAsync(_connection ?? throw new InvalidOperationException(), options, Ct);
            _sessions.Add(task);
            return task;
        }

        internal Task<Ctap2Session> CreateOwnedAsync()
        {
            var task = Device.CreateCtap2SessionAsync(cancellationToken: Ct);
            _sessions.Add(task);
            return task;
        }

        public async ValueTask DisposeAsync()
        {
            // Acquisition/admission lives inside this scope. Release every native hold first, then
            // drain without runner cancellation so no worker can outlive the fake gates it uses.
            First.ReleaseAll();
            foreach (var api in _apis) api.ReleaseAll();
            if (_connection is not null)
                await _connection.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None);
            foreach (var creation in _sessions)
            {
                Ctap2Session? session = null;
                try { session = await creation.WaitAsync(Bound, CancellationToken.None); }
                catch (Exception error) when (error is not TimeoutException) { /* Failed creation owns its cleanup. */ }
                if (session is not null)
                    await session.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None);
            }
            foreach (var api in _apis) api.Dispose();
            if (_apis.Count == 0) First.Dispose();
        }
    }
}