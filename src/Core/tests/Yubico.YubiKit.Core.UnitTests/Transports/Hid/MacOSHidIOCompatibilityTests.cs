using System.Diagnostics;
using System.Reflection;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSHidIOCompatibilityTests
{
    [Fact]
    public async Task TimeoutDetachesReadAndLateReportIsAvailableOnRetry()
    {
        var bridge = new Bridge();
        using var connection = new MacOSHidIOReportConnection(1, bridge);
        var clock = Stopwatch.StartNew();
        var reading = Task.Run(connection.GetReport);
        try
        {
            await Assert.ThrowsAsync<PlatformApiException>(() => reading.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.InRange(clock.Elapsed, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
            Assert.Equal(0, bridge.CancelCount);
            bridge.Emit(42);
            Assert.Equal((byte)42, connection.GetReport()[0]);
        }
        finally { bridge.Emit(0); }
    }

    [Fact]
    public async Task PendingReadWakesOnDisposeButReleaseWaitsForAcknowledgment()
    {
        var bridge = new Bridge { HoldAck = true };
        var connection = new MacOSHidIOReportConnection(1, bridge);
        var reading = Task.Run(connection.GetReport);
        await bridge.InputReady.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var closing = connection.DisposeAsync().AsTask();
        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() => reading.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            await bridge.AckEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(closing.IsCompleted);
            Assert.Equal(0, bridge.ReleaseCount);
        }
        finally { bridge.ReleaseAck.SetResult(); }
        await closing.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, bridge.ReleaseCount);
        Assert.Equal(1, bridge.DestroyCount);
    }

    [Fact]
    public void FailedConstructorPreservesOpenFailureAndReleasesDevice()
    {
        var bridge = new Bridge { OpenStatus = -1 };
        var error = Assert.Throws<PlatformApiException>(() => new MacOSHidIOReportConnection(1, bridge));
        Assert.Contains("open", Assert.IsType<InvalidOperationException>(error.InnerException).Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, bridge.ReleaseCount);
    }

    [Fact]
    public void ReportMetadataComesFromDeviceAndSetUsesOwner()
    {
        var bridge = new Bridge { InputReportSize = 65, OutputReportSize = 72 };
        using var connection = new MacOSHidIOReportConnection(1, bridge);
        Assert.Equal(65, connection.InputReportSize);
        Assert.Equal(72, connection.OutputReportSize);
        Assert.Equal(Yubico.YubiKit.Core.Devices.ConnectionType.Hid, connection.Type);
        connection.SetReport(new byte[72]);
        Assert.Equal(1, bridge.SendCount);
        Assert.Equal(72, bridge.SentLength);
    }

    [Fact]
    public void QueuedInputReport_TransfersTheOwnersOriginalArray()
    {
        var bridge = new Bridge();
        using var connection = new MacOSHidIOReportConnection(1, bridge);
        bridge.Emit(42);

        // The owner queues a fresh array; inspecting that queue before dequeue pins the ownership contract.
        var facadeField = typeof(MacOSHidIOReportConnection).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Missing FIDO connection field");
        var owner = facadeField.GetValue(connection) ?? throw new InvalidOperationException("Missing FIDO connection");
        var ownerField = owner.GetType().GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Missing input owner field");
        var inputOwner = ownerField.GetValue(owner) ?? throw new InvalidOperationException("Missing input owner");
        var queueField = inputOwner.GetType().GetField("_reports", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Missing input queue field");
        var queue = (Queue<byte[]>)(queueField.GetValue(inputOwner) ?? throw new InvalidOperationException("Missing input queue"));
        byte[] owned = queue.Peek();

        Assert.Same(owned, connection.GetReport());
        Assert.Equal((byte)42, owned[0]);
    }

    [Fact]
    public async Task ArbitraryOutputRetainsBorrowUntilWorkerReturnsAndIsCleared()
    {
        var bridge = new Bridge { HoldSend = true };
        var connection = new MacOSHidIOReportConnection(1, bridge);
        var report = new byte[72];
        report[0] = 51;
        var sending = Task.Run(() => connection.SetReport(report), TestContext.Current.CancellationToken);
        try
        {
            await bridge.SendEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            report[0] = 0;
            var closing = connection.DisposeAsync().AsTask();
            Assert.False(closing.IsCompleted);
            Assert.Equal(0, bridge.ReleaseCount);
            bridge.ReleaseSend.SetResult();
            await sending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await closing.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal((byte)51, bridge.ObservedFirstByte);
            Assert.All(bridge.BorrowedOutput ?? [], value => Assert.Equal((byte)0, value));
            Assert.Equal(1, bridge.ReleaseCount);
        }
        finally
        {
            bridge.ReleaseSend.TrySetResult();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void NativeOutputFailurePreservesPlatformStatus()
    {
        var bridge = new Bridge { SetFailure = new PlatformApiException("IOHIDDeviceSetReport", 123, "Failed to set HID report.") };
        using var connection = new MacOSHidIOReportConnection(1, bridge);
        var failure = Assert.Throws<PlatformApiException>(() => connection.SetReport(new byte[72]));
        Assert.Contains("0000007B", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TypedFidoSendStillRejectsNonPacketLengthWithoutSubmitting()
    {
        var bridge = new Bridge();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        IFidoHidConnection typed = connection;
        await Assert.ThrowsAsync<ArgumentException>(() => typed.SendAsync(new byte[72], TestContext.Current.CancellationToken));
        Assert.Equal(0, bridge.SendCount);
    }

    [Fact]
    public void UnprovenPartialOpenIsNotDisguisedAsOrdinaryPlatformFailure()
    {
        var bridge = new Bridge { OpenStatus = unchecked((int)0xE00002C5), CloseUnstartedSucceeds = false };
        Assert.Throws<UnrecoveredConnectionException>(() => new MacOSHidIOReportConnection(1, bridge));
        Assert.Equal(0, bridge.ReleaseCount);
    }

    private sealed class Bridge : IHidInputBridge
    {
        private Action<ReadOnlyMemory<byte>>? _report;
        public TaskCompletionSource InputReady { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AckEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseAck { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldAck { get; init; }
        public int OpenStatus { get; init; }
        public bool CloseUnstartedSucceeds { get; init; } = true;
        public Exception? SetFailure { get; init; }
        public int InputReportSize { get; init; } = 64;
        public int OutputReportSize { get; init; } = 64;
        public int CancelCount;
        public int ReleaseCount;
        public int DestroyCount;
        public int SendCount;
        public int SentLength;
        public bool HoldSend;
        public TaskCompletionSource SendEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseSend { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public byte ObservedFirstByte;
        public byte[]? BorrowedOutput;

        public void Emit(byte value)
        {
            var packet = new byte[64];
            packet[0] = value;
            (_report ?? throw new InvalidOperationException("Input not started"))(packet);
        }

        public nint CreateDevice(long entryId) => 1;
        public void OpenDevice(nint device) { }
        public int OpenDeviceResult(nint device) => OpenStatus;
        public int InputSize(nint device) => InputReportSize;
        public int OutputSize(nint device) => OutputReportSize;
        public bool CloseUnstarted(nint device) => CloseUnstartedSucceeds;
        public void ReleaseDevice(nint device) => Interlocked.Increment(ref ReleaseCount);
        public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
        {
            _report = report;
            InputReady.SetResult();
            return 2;
        }
        public int Start(nint owner) => 0;
        public void Cancel(nint owner) => Interlocked.Increment(ref CancelCount);
        public int WaitShutdown(nint owner)
        {
            AckEntered.SetResult();
            if (HoldAck) ReleaseAck.Task.GetAwaiter().GetResult();
            return 0;
        }
        public int Destroy(nint owner) { Interlocked.Increment(ref DestroyCount); return 0; }
        public void SetReport(nint device, byte[] report)
        {
            if (SetFailure is not null) throw SetFailure;
            BorrowedOutput = report;
            SendEntered.TrySetResult();
            if (HoldSend) ReleaseSend.Task.GetAwaiter().GetResult();
            ObservedFirstByte = report[0];
            SentLength = report.Length;
            Interlocked.Increment(ref SendCount);
        }
    }
}
