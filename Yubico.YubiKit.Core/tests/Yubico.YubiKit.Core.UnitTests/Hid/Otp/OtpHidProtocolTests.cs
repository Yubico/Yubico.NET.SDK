// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.UnitTests.Hid.Otp;

/// <summary>
/// Unit tests for OtpHidProtocol to verify behavior before refactoring.
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

        public byte[] GetReport()
        {
            if (_reportsToReturn.Count == 0)
                throw new InvalidOperationException("No reports queued - test setup incomplete");
            return _reportsToReturn.Dequeue();
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
            () => protocol.SendAndReceiveAsync(0x13, oversizedPayload));
    }

    [Fact]
    public async Task SendAndReceiveAsync_EmptyPayload_Succeeds()
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

        var result = await protocol.SendAndReceiveAsync(0x13, ReadOnlyMemory<byte>.Empty);

        Assert.NotNull(result);
    }

    [Fact]
    public void FirmwareVersion_AfterInitialization_ReturnsCorrectVersion()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);

        // Trigger initialization by calling Configure
        protocol.Configure(new YubiKey.FirmwareVersion(5, 4, 3));

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

        var status = await protocol.ReadStatusAsync();

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
            () => protocol.SendAndReceiveAsync(0x13, ReadOnlyMemory<byte>.Empty));
    }
}
