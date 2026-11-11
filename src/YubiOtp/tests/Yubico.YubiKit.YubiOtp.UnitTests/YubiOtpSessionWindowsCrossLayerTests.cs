using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class YubiOtpSessionWindowsCrossLayerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task CreateStatusSerial_HeldInitialGetReturnsPendingAndBorrowedConnectionAllowsTwoSessions()
    {
        using var api = new WindowsYubiOtpApi { HoldInitialGet = true };
        using var reopenedApi = new WindowsYubiOtpApi();
        api.InitialStatus();
        api.SerialExchange();
        api.InitialStatus();
        var device = Device(api, reopenedApi);
        IOtpHidConnection? connection = null;
        YubiOtpSession? first = null;
        Task<Task<YubiOtpSession>>? invocation = null;
        try
        {
            // Explicit typed selection preserves the applet's independent SmartCard-first default.
            connection = await device.ConnectAsync<IOtpHidConnection>(Ct);
            invocation = Task.Factory.StartNew(
                () => YubiOtpSession.CreateAsync(connection, cancellationToken: Ct), Ct,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
            await api.InitialGetEntered.Task.WaitAsync(Bound, Ct);
            var pending = await invocation.WaitAsync(Bound, Ct);
            Assert.False(pending.IsCompleted);
            Assert.Equal(1, api.Gets);
            Assert.Empty(api.Sent);
            api.ReleaseInitialGet.Set();
            first = await pending.WaitAsync(Bound, Ct);
            Assert.Equal(2, api.Gets); // ReadStatus precedes Configure; Configure adds no wire work.
            Assert.Equal(ConnectionType.HidOtp, first.ConnectionType);
            Assert.Equal(new FirmwareVersion(5, 4, 3), first.FirmwareVersion);
            Assert.True(first.GetConfigState().IsConfigured(Slot.One));
            Assert.True(first.GetConfigState().IsTouchTriggered(Slot.Two));
            Assert.Equal(12345678, await first.GetSerialNumberAsync(Ct).WaitAsync(Bound, Ct));
            AssertFrame(api.Sent, 0, ConfigSlot.DeviceSerial, ReadOnlySpan<byte>.Empty);
            AssertReset(api.Sent[10]);
            await first.DisposeAsync();
            Assert.Equal(0, api.Closes);
            await using (var second = await YubiOtpSession.CreateAsync(connection, cancellationToken: Ct))
                Assert.True(second.IsInitialized);
            Assert.Equal(17, api.Gets);
            Assert.Empty(api.Reports);
            Assert.Equal(11, api.Sent.Count);
            Assert.Equal(0, api.Closes);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => device.ConnectAsync<IOtpHidConnection>(Ct));
        }
        finally
        {
            api.ReleaseGates();
            // Drain uncancelled before disposing barriers/storage, even if the runner cancelled an assertion.
            try
            {
                if (invocation is not null)
                {
                    var creation = await invocation.WaitAsync(Bound, CancellationToken.None);
                    first ??= await creation.WaitAsync(Bound, CancellationToken.None);
                }
            }
            finally { await DisposeAsync(first, connection); }
        }
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, api.Opens);
        await using (var reopened = await device.ConnectAsync<IOtpHidConnection>(Ct))
            Assert.Equal(1, reopenedApi.Opens);
        Assert.Equal(1, reopenedApi.Closes);
    }

    [Fact]
    public async Task PublicHmac_CancelAtTouchWaitResolvesOnlyAfterAbortThenReadOnlySerialRecovers()
    {
        using var api = new WindowsYubiOtpApi { HoldFirstReset = true };
        api.InitialStatus();
        api.Prelude();
        api.TouchWait();
        api.SerialExchange();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var prompt = new RecordingPrompt(cancellation);
        IOtpHidConnection? connection = null;
        YubiOtpSession? session = null;
        Task<ReadOnlyMemory<byte>>? operation = null;
        byte[] challenge = [1, 2, 3]; // Synthetic test input; never programs a slot.
        try
        {
            connection = await Device(api).ConnectAsync<IOtpHidConnection>(Ct);
            session = await YubiOtpSession.CreateAsync(connection,
                new SessionCreationOptions { UserPresencePrompt = prompt }, Ct);
            operation = session.CalculateHmacSha1Async(Slot.Two, challenge, cancellation.Token);
            await api.ResetEntered.Task.WaitAsync(Bound, Ct);
            Assert.True(cancellation.IsCancellationRequested);
            Assert.False(operation.IsCompleted);
            Assert.Empty(prompt.Resolved);
            Assert.Equal(0, api.Closes);
            AssertReset(api.Sent[10]);
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetSerialNumberAsync(Ct));
            api.ReleaseReset.Set();
            var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(Bound, Ct));
            Assert.Equal(cancellation.Token, cancelled.CancellationToken);
            AssertPresence(prompt, UserPresenceOutcome.Cancelled);
            Assert.Equal(12345678, await session.GetSerialNumberAsync(Ct).WaitAsync(Bound, Ct));
            Assert.Equal(22, api.Sent.Count);
            AssertFrame(api.Sent, 0, ConfigSlot.ChalHmac2, challenge);
            AssertFrame(api.Sent, 11, ConfigSlot.DeviceSerial, ReadOnlySpan<byte>.Empty);
            AssertReset(api.Sent[21]);
            Assert.Empty(api.Reports);
            Assert.Single(prompt.Resolved);
        }
        finally
        {
            api.ReleaseGates();
            try { await DrainAsync(operation); }
            finally
            {
                try { await DisposeAsync(session, connection); }
                finally { CryptographicOperations.ZeroMemory(challenge); }
            }
        }
        Assert.Equal(1, api.Closes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublicHmac_TerminalPresenceOutcomeBelongsToAppletCrcValidation(bool validCrc)
    {
        using var api = new WindowsYubiOtpApi { HoldFirstReset = true };
        api.InitialStatus();
        api.Prelude();
        api.TouchWait();
        // Synthetic HMAC 00..13, independent ISO13239 complement CRC 0x4315.
        byte[] response = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 0x15, 0x43];
        byte[] challenge = [1, 2, 3];
        var prompt = new RecordingPrompt();
        IOtpHidConnection? connection = null;
        YubiOtpSession? session = null;
        Task<ReadOnlyMemory<byte>>? operation = null;
        try
        {
            if (!validCrc) response[21] ^= 1;
            api.Response(response);
            connection = await Device(api).ConnectAsync<IOtpHidConnection>(Ct);
            session = await YubiOtpSession.CreateAsync(connection,
                new SessionCreationOptions { UserPresencePrompt = prompt }, Ct);
            operation = session.CalculateHmacSha1Async(Slot.Two, challenge, Ct);
            await api.ResetEntered.Task.WaitAsync(Bound, Ct);
            Assert.False(operation.IsCompleted);
            Assert.Single(prompt.Requested);
            Assert.Empty(prompt.Resolved); // Core has received every byte, but cannot declare applet success.
            api.ReleaseReset.Set();
            if (validCrc)
            {
                var result = await operation.WaitAsync(Bound, Ct);
                try { Assert.True(CryptographicOperations.FixedTimeEquals(response.AsSpan(0, 20), result.Span)); }
                finally { CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(result).Span); }
            }
            else
                await Assert.ThrowsAsync<BadResponseException>(() => operation.WaitAsync(Bound, Ct));
            AssertPresence(prompt, validCrc ? UserPresenceOutcome.Completed : UserPresenceOutcome.Failed);
            AssertFrame(api.Sent, 0, ConfigSlot.ChalHmac2, challenge);
            Assert.Equal(11, api.Sent.Count);
            AssertReset(api.Sent[10]);
            Assert.Empty(api.Reports);
        }
        finally
        {
            api.ReleaseGates();
            try { await DrainAsync(operation); }
            finally
            {
                try { await DisposeAsync(session, connection); }
                finally
                {
                    CryptographicOperations.ZeroMemory(response);
                    CryptographicOperations.ZeroMemory(challenge);
                }
            }
        }
    }

    private static async Task DisposeAsync(YubiOtpSession? session, IOtpHidConnection? connection)
    {
        // Native gates have been released; drain ownership independently of runner cancellation.
        try
        {
            if (session is not null) await session.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None);
        }
        finally
        {
            if (connection is not null) await connection.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None);
        }
    }

    private static async Task DrainAsync(Task<ReadOnlyMemory<byte>>? operation)
    {
        if (operation is null) return;
        // Expected operation failures are observed; a timeout must fail cleanup rather than hide a live worker.
        var error = await Record.ExceptionAsync(async () =>
        {
            var result = await operation.WaitAsync(Bound, CancellationToken.None);
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(result).Span);
        });
        Assert.IsNotType<TimeoutException>(error);
    }

    private static void AssertPresence(RecordingPrompt prompt, UserPresenceOutcome expected)
    {
        var request = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, request.Basis);
        Assert.Equal("YubiOTP", request.Application);
        Assert.Equal("Two", request.Scope);
        var (context, outcome, token) = Assert.Single(prompt.Resolved);
        Assert.Same(request, context);
        Assert.Equal(expected, outcome);
        Assert.Equal(CancellationToken.None, token);
    }

    private static void AssertReset(byte[] nativeReport) =>
        Assert.True(nativeReport.AsSpan().SequenceEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, OtpConstants.DummyReportWrite }));

    private static void AssertFrame(List<byte[]> reports, int start, ConfigSlot slot, ReadOnlySpan<byte> challenge)
    {
        Span<byte> frame = stackalloc byte[70];
        Span<byte> expected = stackalloc byte[70];
        expected.Clear();
        challenge.CopyTo(expected);
        expected[64] = (byte)slot;
        // Independent fixed CRC vectors for zero-filled serial and 01 02 03 + zero-padded HMAC input.
        BinaryPrimitives.WriteUInt16LittleEndian(expected[65..], challenge.IsEmpty ? (ushort)0x5B6B : (ushort)0x680B);
        try
        {
            for (int sequence = 0; sequence < 10; sequence++)
            {
                var report = reports[start + sequence];
                Assert.Equal(9, report.Length);
                Assert.Equal(0, report[0]);
                Assert.Equal((byte)(OtpConstants.SlotWriteFlag | sequence), report[8]);
                report.AsSpan(1, 7).CopyTo(frame[(sequence * 7)..]);
            }
            Assert.True(CryptographicOperations.FixedTimeEquals(expected, frame));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(frame);
            CryptographicOperations.ZeroMemory(expected);
        }
    }

    private static YubiKeyDevice Device(params WindowsYubiOtpApi[] acquisitions)
    {
        // Each owner acquisition receives a distinct, pre-scripted native seam instance.
        var pending = new Queue<WindowsYubiOtpApi>(acquisitions);
        return new($"w3-otp-{Guid.NewGuid():N}", null, null,
        new HidConnectionSlot(new WindowsHidInterface(new HidDescriptorInfo
        {
            DevicePath = $"w3-otp-{Guid.NewGuid():N}",
            VendorId = 0x1050,
            ProductId = 0x0407,
            UsagePage = 1,
            Usage = 6
        }), windowsFeatureReportApiFactory: () => pending.Dequeue()), null);
    }

    private sealed class RecordingPrompt(CancellationTokenSource? cancellation = null) : IUserPresencePrompt
    {
        internal List<UserPresenceContext> Requested { get; } = [];
        internal List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolved { get; } = [];
        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken token)
        { Requested.Add(context); cancellation?.Cancel(); return ValueTask.CompletedTask; }
        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken token)
        { Resolved.Add((context, outcome, token)); return ValueTask.CompletedTask; }
    }

    // Only the synchronous native seam is controlled. The Windows interface, slot, device,
    // native owner, protocol and public applet all execute their production implementations.
    private sealed class WindowsYubiOtpApi : IWindowsHidFeatureReportApi, IDisposable
    {
        private int _resets;
        private int _workerThread;
        internal bool HoldInitialGet, HoldFirstReset;
        internal int Gets, Opens, Closes;
        internal Queue<byte[]> Reports { get; } = new();
        internal List<byte[]> Sent { get; } = [];
        internal ManualResetEventSlim ReleaseInitialGet { get; } = new();
        internal ManualResetEventSlim ReleaseReset { get; } = new();
        internal TaskCompletionSource InitialGetEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource ResetEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static byte[] Status() => [0, 5, 4, 3, 1, 0x0B, 0, 0];
        internal void InitialStatus() { Reports.Enqueue(Status()); Reports.Enqueue(Status()); }
        internal void Prelude() { for (int i = 0; i < 11; i++) Reports.Enqueue(Status()); }
        internal void TouchWait() => Reports.Enqueue([0, 5, 4, 3, 1, 0x0B, 0, OtpConstants.ResponseTimeoutWaitFlag]);
        internal void SerialExchange()
        {
            Prelude();
            // Serial12345678, independent complement CRC 0x04C8, little endian.
            Reports.Enqueue([0, 0xBC, 0x61, 0x4E, 0xC8, 0x04, 0, OtpConstants.ResponsePendingFlag]);
            Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
        }
        internal void Response(ReadOnlySpan<byte> response)
        {
            for (int sequence = 0; sequence < 4; sequence++)
            {
                var report = new byte[8];
                response.Slice(sequence * 7, Math.Min(7, response.Length - sequence * 7)).CopyTo(report);
                report[7] = (byte)(OtpConstants.ResponsePendingFlag | sequence);
                Reports.Enqueue(report);
            }
            Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
        }
        public WindowsHidFeatureReportMetadata Open(string devicePath)
        {
            Assert.Equal(0, Opens++); // A seam instance belongs to exactly one acquisition.
            _workerThread = Environment.CurrentManagedThreadId;
            return new(1, 6, 9);
        }
        public WindowsHidFeatureResult GetFeature(byte[] nativeReport)
        {
            AssertNativeBorrow(nativeReport);
            if (++Gets == 1 && HoldInitialGet)
            {
                InitialGetEntered.TrySetResult();
                ReleaseInitialGet.Wait(Ct);
            }
            Assert.True(Reports.TryDequeue(out var report), "Unexpected feature GET (including hidden Configure I/O)");
            try { report.CopyTo(nativeReport, 1); }
            finally { CryptographicOperations.ZeroMemory(report); }
            return new(0);
        }
        public WindowsHidFeatureResult SetFeature(byte[] nativeReport)
        {
            AssertNativeBorrow(nativeReport);
            Sent.Add((byte[])nativeReport.Clone()); // Owned capture outlives the synchronous borrow; disposed below.
            if (nativeReport[8] == OtpConstants.DummyReportWrite && ++_resets == 1 && HoldFirstReset)
            {
                ResetEntered.TrySetResult();
                ReleaseReset.Wait(Ct);
                // The array remains valid and unchanged until synchronous native last-use.
                Assert.True(CryptographicOperations.FixedTimeEquals(Sent[^1], nativeReport));
            }
            return new(0);
        }
        private void AssertNativeBorrow(byte[] nativeReport)
        {
            Assert.Equal(_workerThread, Environment.CurrentManagedThreadId);
            Assert.Equal(9, nativeReport.Length);
            Assert.Equal(0, nativeReport[0]);
            Assert.Equal(0, Closes);
        }
        public WindowsHidRelease CloseOnce()
        {
            Assert.Equal(_workerThread, Environment.CurrentManagedThreadId);
            Closes++;
            return new(true, 0);
        }
        internal void ReleaseGates() { ReleaseInitialGet.Set(); ReleaseReset.Set(); }
        public void Dispose()
        {
            ReleaseInitialGet.Dispose();
            ReleaseReset.Dispose();
            foreach (var report in Sent) CryptographicOperations.ZeroMemory(report);
            foreach (var report in Reports) CryptographicOperations.ZeroMemory(report);
        }
    }
}