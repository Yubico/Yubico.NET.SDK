using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class YubiOtpSessionLinuxCrossLayerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task CreateStatusSerial_HeldInitialGetReturnsPendingAndBorrowedConnectionAllowsTwoSessions()
    {
        using var api = new NativeOtp { HoldInitialGet = true };
        api.InitialStatus();
        api.SerialExchange();
        api.InitialStatus();
        var device = Device(api);
        await using var connection = await device.ConnectAsync<IOtpHidConnection>(Ct);
        Task<Task<YubiOtpSession>> invocation = Task.Factory.StartNew(
            () => YubiOtpSession.CreateAsync(connection, cancellationToken: Ct), Ct,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            await api.InitialGetEntered.Task.WaitAsync(Bound, Ct);
            var pending = await invocation.WaitAsync(Bound, Ct);
            Assert.False(pending.IsCompleted);
        }
        finally { api.ReleaseInitialGet.Set(); }
        var creation = await invocation.WaitAsync(Bound, Ct);
        var first = await creation.WaitAsync(Bound, Ct);
        Assert.Equal(new FirmwareVersion(5, 4, 3), first.FirmwareVersion);
        Assert.True(first.GetConfigState().IsConfigured(Slot.One));
        Assert.True(first.GetConfigState().IsTouchTriggered(Slot.Two));
        Assert.Equal(12345678, await first.GetSerialNumberAsync(Ct).WaitAsync(Bound, Ct));
        AssertFrame(api.Sent, 0, ConfigSlot.DeviceSerial);
        Assert.Equal(OtpConstants.DummyReportWrite, api.Sent[10][7]);
        await first.DisposeAsync();
        Assert.Equal(0, api.Closes);
        await using (var second = await YubiOtpSession.CreateAsync(connection, cancellationToken: Ct))
            Assert.True(second.IsInitialized);
        Assert.Equal(0, api.Closes);
        await Assert.ThrowsAsync<ConnectionInUseException>(() => device.ConnectAsync<IOtpHidConnection>(Ct));
        await connection.DisposeAsync();
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task PublicHmac_CancelAtTouchWaitResolvesOnlyAfterAbortThenReadOnlySerialRecovers()
    {
        using var api = new NativeOtp { HoldFirstReset = true };
        api.InitialStatus();
        api.Prelude();
        api.Reports.Enqueue([0, 5, 4, 3, 1, 0x0B, 0, OtpConstants.ResponseTimeoutWaitFlag]);
        api.SerialExchange();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var prompt = new RecordingPrompt(cancellation);
        await using var connection = await Device(api).ConnectAsync<IOtpHidConnection>(Ct);
        await using var session = await YubiOtpSession.CreateAsync(connection,
            new SessionCreationOptions { UserPresencePrompt = prompt }, Ct);
        var operation = session.CalculateHmacSha1Async(Slot.Two, new byte[] { 1, 2, 3 }, cancellation.Token);
        try
        {
            await api.ResetEntered.Task.WaitAsync(Bound, Ct);
            Assert.False(operation.IsCompleted);
            Assert.Empty(prompt.Resolved);
            Assert.Equal(0, api.Closes);
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetSerialNumberAsync(Ct));
        }
        finally { api.ReleaseReset.Set(); }
        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(Bound, Ct));
        Assert.Equal(cancellation.Token, cancelled.CancellationToken);
        var request = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, request.Basis);
        Assert.Equal("YubiOTP", request.Application);
        Assert.Equal("Two", request.Scope);
        var (Context, Outcome, Token) = Assert.Single(prompt.Resolved);
        Assert.Same(request, Context);
        Assert.Equal(UserPresenceOutcome.Cancelled, Outcome);
        Assert.Equal(CancellationToken.None, Token);
        Assert.Equal(12345678, await session.GetSerialNumberAsync(Ct).WaitAsync(Bound, Ct));
        Assert.Equal(22, api.Sent.Count);
        AssertFrame(api.Sent, 0, ConfigSlot.ChalHmac2);
        AssertFrame(api.Sent, 11, ConfigSlot.DeviceSerial);
        Assert.Equal(OtpConstants.DummyReportWrite, api.Sent[10][7]);
        Assert.Equal(OtpConstants.DummyReportWrite, api.Sent[21][7]);
        Assert.Single(prompt.Resolved);
        foreach (var report in api.Sent) CryptographicOperations.ZeroMemory(report);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublicHmac_TerminalPresenceOutcomeBelongsToAppletCrcValidation(bool validCrc)
    {
        using var api = new NativeOtp();
        api.InitialStatus();
        api.Prelude();
        api.Reports.Enqueue([0, 5, 4, 3, 1, 0x0B, 0, OtpConstants.ResponseTimeoutWaitFlag]);
        // Synthetic HMAC result 00..13; independently calculated ISO13239 complement CRC = 0x4315.
        byte[] response = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 0x15, 0x43];
        if (!validCrc) response[21] ^= 1;
        for (int sequence = 0; sequence < 4; sequence++)
        {
            var report = new byte[8];
            response.AsSpan(sequence * 7, Math.Min(7, response.Length - sequence * 7)).CopyTo(report);
            report[7] = (byte)(OtpConstants.ResponsePendingFlag | sequence);
            api.Reports.Enqueue(report);
        }
        api.Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
        var prompt = new RecordingPrompt();
        await using var connection = await Device(api).ConnectAsync<IOtpHidConnection>(Ct);
        await using var session = await YubiOtpSession.CreateAsync(connection,
            new SessionCreationOptions { UserPresencePrompt = prompt }, Ct);
        var operation = session.CalculateHmacSha1Async(Slot.Two, new byte[] { 1, 2, 3 }, Ct);
        if (validCrc)
        {
            var result = await operation.WaitAsync(Bound, Ct);
            Assert.Equal(response.AsSpan(0, 20).ToArray(), result.ToArray());
            CryptographicOperations.ZeroMemory(System.Runtime.InteropServices.MemoryMarshal.AsMemory(result).Span);
        }
        else
            await Assert.ThrowsAsync<BadResponseException>(() => operation.WaitAsync(Bound, Ct));
        var request = Assert.Single(prompt.Requested);
        var (Context, Outcome, Token) = Assert.Single(prompt.Resolved);
        Assert.Same(request, Context);
        Assert.Equal(validCrc ? UserPresenceOutcome.Completed : UserPresenceOutcome.Failed, Outcome);
        Assert.Equal(CancellationToken.None, Token);
        Assert.Equal(OtpConstants.DummyReportWrite, api.Sent[10][7]);
        CryptographicOperations.ZeroMemory(response);
    }

    private static void AssertFrame(List<byte[]> reports, int start, ConfigSlot slot)
    {
        Span<byte> frame = stackalloc byte[70];
        for (int sequence = 0; sequence < 10; sequence++)
        {
            Assert.Equal((byte)(OtpConstants.SlotWriteFlag | sequence), reports[start + sequence][7]);
            reports[start + sequence].AsSpan(0, 7).CopyTo(frame[(sequence * 7)..]);
        }
        Assert.Equal((byte)slot, frame[64]);
        Assert.Equal(ChecksumUtils.CalculateCrc(frame[..64], 64), BinaryPrimitives.ReadUInt16LittleEndian(frame[65..]));
        CryptographicOperations.ZeroMemory(frame);
    }

    private static YubiKeyDevice Device(NativeOtp api) => new($"l6-otp-{Guid.NewGuid():N}", null, null,
        new HidConnectionSlot(new LinuxHidInterface(new HidDescriptorInfo
        {
            DevicePath = $"l6-otp-{Guid.NewGuid():N}",
            VendorId = 0x1050,
            ProductId = 0x0407,
            UsagePage = 1,
            Usage = 6
        }), linuxReportApi: api), null);

    private sealed class RecordingPrompt(CancellationTokenSource? cancellation = null) : IUserPresencePrompt
    {
        internal List<UserPresenceContext> Requested { get; } = [];
        internal List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolved { get; } = [];
        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken token)
        { Requested.Add(context); cancellation?.Cancel(); return ValueTask.CompletedTask; }
        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken token)
        { Resolved.Add((context, outcome, token)); return ValueTask.CompletedTask; }
    }

    // Fixed fake device bytes, below LinuxOtpHidNativeOwner; no real slot provisioning or typed-connection fake.
    private sealed class NativeOtp : ILinuxHidReportApi, IDisposable
    {
        private int _gets, _resets;
        internal bool HoldInitialGet, HoldFirstReset;
        internal int Closes;
        internal Queue<byte[]> Reports { get; } = new();
        internal List<byte[]> Sent { get; } = [];
        internal ManualResetEventSlim ReleaseInitialGet { get; } = new();
        internal ManualResetEventSlim ReleaseReset { get; } = new();
        internal TaskCompletionSource InitialGetEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource ResetEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static byte[] Status() => [0, 5, 4, 3, 1, 0x0B, 0, 0];
        internal void InitialStatus() { Reports.Enqueue(Status()); Reports.Enqueue(Status()); }
        internal void Prelude() { for (int i = 0; i < 11; i++) Reports.Enqueue(Status()); }
        internal void SerialExchange()
        {
            Prelude();
            // Serial12345678, independently calculated complement CRC 0x04C8 (little endian).
            byte[] data = [0, 0xBC, 0x61, 0x4E, 0xC8, 0x04, 0, OtpConstants.ResponsePendingFlag];
            Reports.Enqueue(data);
            Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
        }
        public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags)
        { var handle = new LinuxFileSafeHandle(_ => new(0, 0)); handle.Initialize(42); return handle; }
        public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload)
        {
            if (++_gets == 1 && HoldInitialGet) { InitialGetEntered.TrySetResult(); ReleaseInitialGet.Wait(Ct); }
            Assert.True(Reports.TryDequeue(out var report), "Unexpected feature GET (including hidden Configure I/O)");
            report.CopyTo(payload);
        }
        public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            Sent.Add(payload.ToArray()); // Fake wire capture escapes the native borrow; cleared at disposal.
            if (payload[7] == OtpConstants.DummyReportWrite && ++_resets == 1 && HoldFirstReset)
            { ResetEntered.TrySetResult(); ReleaseReset.Wait(Ct); }
        }
        public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle) { Closes++; handle.Dispose(); return new(true, true, 0, 0); }
        public (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle) => throw new NotSupportedException();
        public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload) => throw new NotSupportedException();
        public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) => throw new NotSupportedException();
        public void Dispose()
        {
            ReleaseInitialGet.Dispose(); ReleaseReset.Dispose();
            foreach (var report in Sent) CryptographicOperations.ZeroMemory(report);
        }
    }
}