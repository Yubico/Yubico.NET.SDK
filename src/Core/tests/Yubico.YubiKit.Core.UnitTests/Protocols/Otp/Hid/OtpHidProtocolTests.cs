// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Buffers;
using System.Diagnostics;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.Otp.Hid;

/// <summary>
/// Unit tests for OtpHidProtocol behavior and no-hardware runtime resilience invariants.
/// </summary>
public class OtpHidProtocolTests
{
    /// <summary>
    /// Mock sync connection for testing protocol logic without hardware.
    /// </summary>
    private class MockHidConnection : IHidConnection
    {
        private readonly Queue<byte[]> _reportsToReturn = new();
        private readonly List<byte[]> _reportsSent = new();

        public int InputReportSize => 8;
        public int OutputReportSize => 8;
        public ConnectionType Type => ConnectionType.Hid;

        public void QueueReport(byte[] report) => _reportsToReturn.Enqueue(report);
        public IReadOnlyList<byte[]> SentReports => _reportsSent;
        public int ReportsRemaining => _reportsToReturn.Count;
        public Action<byte[]>? OnReportDequeued { get; set; }

        public byte[] GetReport()
        {
            if (_reportsToReturn.Count == 0)
                throw new InvalidOperationException("No reports queued - test setup incomplete");
            byte[] report = _reportsToReturn.Dequeue();
            OnReportDequeued?.Invoke(report);
            return report;
        }

        public void SetReport(byte[] report)
        {
            _reportsSent.Add(report.ToArray());
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static OtpHidProtocol CreateProtocolWithMock(MockHidConnection mock)
    {
        // Queue initial status report for initialization (firmware 5.4.3)
        mock.QueueReport([0x00, 0x05, 0x04, 0x03, 0x00, 0x00, 0x00, 0x00]);
        return new OtpHidProtocol(new OtpHidConnection(mock));
    }

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OtpHidProtocol(null!));
    }

    [Fact]
    public async Task SendAndReceiveAsync_PayloadTooLarge_ThrowsArgumentException()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);

        var oversizedPayload = new byte[65]; // Max is 64

        await Assert.ThrowsAsync<ArgumentException>(
            () => protocol.SendAndReceiveAsync(0x13, oversizedPayload, TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task SendAndReceiveAsync_WhenReadyToWriteImmediately_DoesNotSleepBeforePolling()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);

        // Queue reports for: initial status read, 10 frame writes (each needs status check),
        // and response polling
        // Initial programming sequence read
        mock.QueueReport([0x00, 0x05, 0x04, 0x03, 0x01, 0x00, 0x00, 0x00]); // progSeq=1

        // For each of the 10 frame packets, AwaitReadyToWrite reads status
        for (int i = 0; i < 10; i++)
        {
            mock.QueueReport([0x00, 0x05, 0x04, 0x03, 0x01, 0x00, 0x00, 0x00]); // Write flag clear
        }

        // Response: sequence incremented (no data response)
        mock.QueueReport([0x00, 0x05, 0x04, 0x03, 0x02, 0x00, 0x00, 0x00]); // progSeq=2

        var stopwatch = Stopwatch.StartNew();
        var result = await protocol.SendAndReceiveAsync(0x13, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken);
        stopwatch.Stop();

        Assert.Equal(6, result.Length); // Status-only response returns 6 status bytes
        Assert.Equal(10, mock.SentReports.Count);

        // This fake path is intentionally no-hardware and immediately write-ready. The old
        // sleep-first loop added at least 10 x 50ms before these writes, so a loose 200ms budget
        // catches that regression without relying on BenchmarkDotNet in normal unit runs.
        Assert.True(stopwatch.ElapsedMilliseconds < 200, $"Ready-to-write polling took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task SendAndReceiveAsync_TouchWait_RequestsOnceWithoutResolving()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);
        QueueTouchExchange(mock, repeatedWaitReport: true);
        var prompt = new RecordingUserPresencePrompt();
        var context = CreatePresenceContext();

        ReadOnlyMemory<byte> result = await protocol.SendAndReceiveAsync(
            0x30,
            ReadOnlyMemory<byte>.Empty,
            UserPresenceNotification.Create(prompt, context),
            TestContext.Current.CancellationToken);

        Assert.Equal(6, result.Length);
        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
        Assert.Equal("YubiOTP", requested.Application);
        Assert.Equal("One", requested.Scope);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task SendAndReceiveAsync_CancelledDuringTouch_ResetsStateWithoutResolving()
    {
        using var cancellation = new CancellationTokenSource();
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);
        QueueUntilTouchWait(mock);
        var prompt = new RecordingUserPresencePrompt(() => cancellation.Cancel());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => protocol.SendAndReceiveAsync(
            0x30,
            ReadOnlyMemory<byte>.Empty,
            CreatePresenceNotification(prompt),
            cancellation.Token));

        Assert.Single(prompt.Requested);
        Assert.Empty(prompt.Resolved);
        Assert.Equal(OtpConstants.DummyReportWrite, mock.SentReports[^1][OtpConstants.FeatureReportDataSize]);
    }

    [Fact]
    public async Task SendAndReceiveAsync_RequestCallbackFails_ResetsStateBeforePropagating()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);
        QueueUntilTouchWait(mock);
        var expected = new InvalidOperationException("Prompt failed.");
        var prompt = new RecordingUserPresencePrompt(() => throw expected);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendAndReceiveAsync(
                0x30,
                ReadOnlyMemory<byte>.Empty,
                CreatePresenceNotification(prompt),
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Empty(prompt.Resolved);
        Assert.Equal(OtpConstants.DummyReportWrite, mock.SentReports[^1][OtpConstants.FeatureReportDataSize]);
    }

    [Fact]
    public async Task SendAndReceiveAsync_CancelledDuringTouchWithoutPrompt_ResetsStateAndCancels()
    {
        using var cancellation = new CancellationTokenSource();
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);
        QueueUntilTouchWait(mock);
        mock.OnReportDequeued = report =>
        {
            if ((report[OtpConstants.FeatureReportDataSize] & OtpConstants.ResponseTimeoutWaitFlag) != 0)
                cancellation.Cancel();
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => protocol.SendAndReceiveAsync(
            0x30,
            ReadOnlyMemory<byte>.Empty,
            cancellation.Token));

        Assert.Equal(OtpConstants.DummyReportWrite, mock.SentReports[^1][OtpConstants.FeatureReportDataSize]);
    }

    [Fact]
    public async Task SendAndReceiveAsync_WhenTouchCompletionAndCancellationSharePoll_ReturnsDeviceCompletion()
    {
        using var cancellation = new CancellationTokenSource();
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);
        QueueTouchExchange(mock, repeatedWaitReport: false);
        var prompt = new RecordingUserPresencePrompt();
        var touchSeen = false;
        mock.OnReportDequeued = report =>
        {
            byte status = report[OtpConstants.FeatureReportDataSize];
            if ((status & OtpConstants.ResponseTimeoutWaitFlag) != 0)
            {
                touchSeen = true;
            }
            else if (touchSeen)
            {
                cancellation.Cancel();
                mock.OnReportDequeued = null;
            }
        };

        ReadOnlyMemory<byte> response = await protocol.SendAndReceiveAsync(
            0x30,
            ReadOnlyMemory<byte>.Empty,
            CreatePresenceNotification(prompt),
            cancellation.Token);

        Assert.Equal(6, response.Length);
        Assert.Single(prompt.Requested);
        Assert.Empty(prompt.Resolved);
        Assert.DoesNotContain(
            mock.SentReports,
            report => report[OtpConstants.FeatureReportDataSize] == OtpConstants.DummyReportWrite);
    }

    [Fact]
    public void FirmwareVersion_AfterInitialization_ReturnsCorrectVersion()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);

        // Trigger initialization by calling Configure
        protocol.Configure(new FirmwareVersion(5, 4, 3));

        Assert.NotNull(protocol.FirmwareVersion);
        Assert.Equal(5, protocol.FirmwareVersion.Major);
        Assert.Equal(4, protocol.FirmwareVersion.Minor);
        Assert.Equal(3, protocol.FirmwareVersion.Patch);
    }

    [Fact]
    public async Task ReadStatusAsync_ReturnsStatusBytes()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);

        // Queue status report for ReadStatusAsync
        mock.QueueReport([0x00, 0x05, 0x04, 0x03, 0x01, 0x02, 0x03, 0x00]);

        var status = await protocol.ReadStatusAsync(TestContext.Current.CancellationToken);

        // Should return bytes 1-6 (skip first and last)
        Assert.Equal(6, status.Length);
        Assert.Equal(0x05, status.Span[0]); // Major version
        Assert.Equal(0x04, status.Span[1]); // Minor version
        Assert.Equal(0x03, status.Span[2]); // Patch version
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);

        protocol.Dispose();
        protocol.Dispose(); // Should not throw
    }

    [Fact]
    public async Task SendAndReceiveAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);
        protocol.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => protocol.SendAndReceiveAsync(0x13, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendAndReceiveAsync_AfterSuccessfulSends_ZerosSdkOwnedReportsButNotCallerPayload()
    {
        var connection = new RetainingOtpHidConnection();
        QueueStatusOnlyExchange(connection);
        var protocol = new OtpHidProtocol(connection);
        byte[] callerPayload = [0x11, 0x22, 0x33];

        _ = await protocol.SendAndReceiveAsync(
            0x13,
            callerPayload,
            TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, callerPayload);
        Assert.Equal(10, connection.SentReportSnapshots.Count);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, connection.SentReportSnapshots[0][..3]);
        Assert.All(connection.RetainedSentReports, report =>
            Assert.All(report.ToArray(), value => Assert.Equal(0, value)));
    }

    [Fact]
    public async Task SendAndReceiveAsync_WhenSendThrows_ZerosSdkOwnedReportButNotCallerPayload()
    {
        var connection = new RetainingOtpHidConnection();
        connection.Enqueue(Status(versionMajor: 5));
        connection.Enqueue(Status(programmingSequence: 1));
        connection.Enqueue(Status(programmingSequence: 1));
        connection.ThrowOnNextSend = true;
        var protocol = new OtpHidProtocol(connection);
        byte[] callerPayload = [0x11, 0x22, 0x33];

        await Assert.ThrowsAsync<IOException>(() => protocol.SendAndReceiveAsync(
            0x13,
            callerPayload,
            TestContext.Current.CancellationToken));

        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, callerPayload);
        ReadOnlyMemory<byte> retained = Assert.Single(connection.RetainedSentReports);
        Assert.All(retained.ToArray(), value => Assert.Equal(0, value));
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, Assert.Single(connection.SentReportSnapshots)[..3]);
    }

    [Fact]
    public async Task SendAndReceiveAsync_DataResponse_ZerosRentedAccumulationBufferAndPreservesReturnedCopy()
    {
        var connection = new RetainingOtpHidConnection();
        QueueDataExchange(connection, completeResponse: true);
        var pool = new RetainingArrayPool();
        var protocol = new OtpHidProtocol(connection, bufferPool: pool);

        ReadOnlyMemory<byte> response = await protocol.SendAndReceiveAsync(
            0x13,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        Assert.Equal(Enumerable.Range(1, 14).Select(value => (byte)value), response.ToArray());
        Assert.NotNull(pool.ReturnedArray);
        Assert.All(pool.ReturnedArray, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task SendAndReceiveAsync_WhenDataResponseReceiveFails_ZerosRentedAccumulationBuffer()
    {
        var connection = new RetainingOtpHidConnection();
        QueueDataExchange(connection, completeResponse: false);
        var pool = new RetainingArrayPool();
        var protocol = new OtpHidProtocol(connection, bufferPool: pool);

        await Assert.ThrowsAsync<InvalidOperationException>(() => protocol.SendAndReceiveAsync(
            0x13,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken));

        Assert.NotNull(pool.ReturnedArray);
        Assert.All(pool.ReturnedArray, value => Assert.Equal(0, value));
    }

    private static void QueueStatusOnlyExchange(RetainingOtpHidConnection connection)
    {
        connection.Enqueue(Status(versionMajor: 5));
        connection.Enqueue(Status(programmingSequence: 1));
        for (int i = 0; i < 10; i++)
            connection.Enqueue(Status(programmingSequence: 1));
        connection.Enqueue(Status(programmingSequence: 2));
    }

    private static UserPresenceContext CreatePresenceContext() => new()
    {
        Basis = UserPresenceBasis.PolicyMayRequire,
        Application = "YubiOTP",
        Scope = "One"
    };

    private static UserPresenceNotification CreatePresenceNotification(IUserPresencePrompt prompt) =>
        UserPresenceNotification.Create(prompt, CreatePresenceContext());

    private static void QueueTouchExchange(MockHidConnection connection, bool repeatedWaitReport)
    {
        QueueUntilTouchWait(connection);
        if (repeatedWaitReport)
        {
            connection.QueueReport(TouchWaitReport());
        }

        connection.QueueReport(Status(programmingSequence: 2));
        connection.QueueReport(Status(programmingSequence: 2));
    }

    private static void QueueUntilTouchWait(MockHidConnection connection)
    {
        connection.QueueReport(Status(programmingSequence: 1));
        for (int i = 0; i < 10; i++)
        {
            connection.QueueReport(Status(programmingSequence: 1));
        }

        connection.QueueReport(TouchWaitReport());
    }

    private static byte[] TouchWaitReport()
    {
        byte[] report = Status(programmingSequence: 1);
        report[OtpConstants.FeatureReportDataSize] = OtpConstants.ResponseTimeoutWaitFlag;
        return report;
    }

    private static void QueueDataExchange(RetainingOtpHidConnection connection, bool completeResponse)
    {
        connection.Enqueue(Status(versionMajor: 5));
        connection.Enqueue(Status(programmingSequence: 1));
        for (int i = 0; i < 10; i++)
            connection.Enqueue(Status(programmingSequence: 1));

        connection.Enqueue(DataReport(sequence: 0, startValue: 1));
        if (completeResponse)
        {
            connection.Enqueue(DataReport(sequence: 1, startValue: 8));
            connection.Enqueue(DataReport(sequence: 0, startValue: 0));
        }
    }

    private static byte[] DataReport(byte sequence, byte startValue)
    {
        var report = new byte[OtpConstants.FeatureReportSize];
        for (int i = 0; i < OtpConstants.FeatureReportDataSize; i++)
            report[i] = (byte)(startValue + i);
        report[OtpConstants.FeatureReportDataSize] = (byte)(OtpConstants.ResponsePendingFlag | sequence);
        return report;
    }

    private static byte[] Status(byte versionMajor = 0, byte programmingSequence = 0) =>
        [0x00, versionMajor, 0x04, 0x03, programmingSequence, 0x00, 0x00, 0x00];

    private sealed class RetainingOtpHidConnection : IOtpHidConnection
    {
        private readonly Queue<ReadOnlyMemory<byte>> _reports = new();

        public int FeatureReportSize => 8;
        public ConnectionType Type => ConnectionType.HidOtp;
        public bool ThrowOnNextSend { get; set; }
        public List<ReadOnlyMemory<byte>> RetainedSentReports { get; } = [];
        public List<byte[]> SentReportSnapshots { get; } = [];

        public void Enqueue(ReadOnlyMemory<byte> report) => _reports.Enqueue(report);

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RetainedSentReports.Add(report);
            SentReportSnapshots.Add(report.ToArray());
            if (ThrowOnNextSend)
            {
                ThrowOnNextSend = false;
                throw new IOException("Scripted send failure.");
            }

            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_reports.Dequeue());
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RetainingArrayPool : ArrayPool<byte>
    {
        public byte[]? ReturnedArray { get; private set; }

        public override byte[] Rent(int minimumLength) => new byte[minimumLength];

        public override void Return(byte[] array, bool clearArray = false) => ReturnedArray = array;
    }

    private sealed class RecordingUserPresencePrompt(
        Action? onRequested = null,
        Exception? resolvedException = null) : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];
        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken)> Resolved { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requested.Add(context);
            onRequested?.Invoke();
            return ValueTask.CompletedTask;
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            return resolvedException is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(resolvedException);
        }
    }
}