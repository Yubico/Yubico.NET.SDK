// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Buffers.Binary;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSOtpRecoveryTests
{
    [Fact]
    public async Task CancelAfterAcceptedReadOnlyFrame_ResetsBeforeReuseAndReturnsCorrelatedCrcResponse()
    {
        using var cancellation = new CancellationTokenSource();
        using var native = new TouchThenDataLifetime();
        await using var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None);
        await using var protocol = new OtpHidProtocol(connection);

        Task<ReadOnlyMemory<byte>> abandoned = protocol.SendAndReceiveAsync(
            OtpConstants.CmdYk4Capabilities, ReadOnlyMemory<byte>.Empty, cancellation.Token);
        try
        {
            await native.TouchReportReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(10, native.Sent.Count);
            cancellation.Cancel();
        }
        finally
        {
            native.ReleaseTouchReport.Set();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned.WaitAsync(TimeSpan.FromSeconds(5)));

        ReadOnlyMemory<byte> response = await protocol.SendAndReceiveAsync(
            OtpConstants.CmdYk4Capabilities, ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(native.ExpectedResponse, response.Span[..native.ExpectedResponse.Length].ToArray());
        Assert.True(ChecksumUtils.CheckCrc(response.Span, native.ExpectedResponse.Length));
        Assert.Equal(22, native.Sent.Count); // ten accepted reports, abort, ten reused reports, read-completion reset
        Assert.Equal(OtpConstants.DummyReportWrite, native.Sent[10][7]);
        Assert.Equal(OtpConstants.DummyReportWrite, native.Sent[21][7]);
        foreach (int start in new[] { 0, 11 })
        {
            byte[] frame = new byte[OtpConstants.FrameSize];
            for (int sequence = 0; sequence < 10; sequence++)
            {
                Assert.Equal((byte)(OtpConstants.SlotWriteFlag | sequence), native.Sent[start + sequence][7]);
                native.Sent[start + sequence].AsSpan(0, 7).CopyTo(frame.AsSpan(sequence * 7));
            }
            Assert.Equal(OtpConstants.CmdYk4Capabilities, frame[OtpConstants.SlotDataSize]);
            Assert.All(frame.AsSpan(0, OtpConstants.SlotDataSize).ToArray(), value => Assert.Equal(0, value));
            Assert.Equal(ChecksumUtils.CalculateCrc(frame.AsSpan(0, OtpConstants.SlotDataSize), OtpConstants.SlotDataSize),
                BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(OtpConstants.SlotDataSize + 1)));
        }
    }

    private sealed class TouchThenDataLifetime : IIOKitDeviceLifetime, IDisposable
    {
        private readonly Queue<byte[]> _reads = new();
        private int _readCount;
        public List<byte[]> Sent { get; } = [];
        public TaskCompletionSource TouchReportReached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim ReleaseTouchReport { get; } = new(false);
        public byte[] ExpectedResponse { get; }

        public TouchThenDataLifetime()
        {
            ExpectedResponse = [0x03, 0x01, 0x01, 0x02, 0, 0];
            ushort fcs = (ushort)~ChecksumUtils.CalculateCrc(ExpectedResponse.AsSpan(0, 4), 4);
            BinaryPrimitives.WriteUInt16LittleEndian(ExpectedResponse.AsSpan(4), fcs);
            _reads.Enqueue(Status(0)); // initialization
            QueuePrelude();
            _reads.Enqueue([0, 5, 4, 3, 1, 0, 0, OtpConstants.ResponseTimeoutWaitFlag]);
            QueuePrelude();
            byte[] data = new byte[8];
            ExpectedResponse.CopyTo(data, 0);
            data[7] = OtpConstants.ResponsePendingFlag;
            _reads.Enqueue(data);
            _reads.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
        }

        private void QueuePrelude()
        {
            _reads.Enqueue(Status(1));
            for (int i = 0; i < 10; i++) _reads.Enqueue(Status(1));
        }

        private static byte[] Status(byte sequence) => [0, 5, 4, 3, sequence, 0, 0, 0];

        public nint CreateDevice(long entryId) => 42;
        public void OpenDevice(nint device) { }
        public void CloseDevice(nint device) { }
        public bool CloseDeviceChecked(nint device) => true;
        public void ReleaseCFObject(nint device) { }
        public nint CreateRunLoopMode(string name) => 1;
        public int GetIntProperty(nint device, string name) => 8;
        public void RegisterInputReportCallback(nint device, byte[] buffer, int length, nint callback, nint context) { }
        public void RegisterRemovalCallback(nint device, nint callback, nint context) { }

        public int GetFeatureReport(nint device, byte[] buffer, ref long length)
        {
            if (_readCount == 13 && (Sent.Count != 11 || Sent[10][7] != OtpConstants.DummyReportWrite))
                throw new InvalidOperationException("A new exchange began without an acknowledged abort.");
            if (_readCount == 24 && Sent.Count != 21)
                throw new InvalidOperationException("Data was read before the second frame completed.");
            byte[] report = _reads.Dequeue();
            if (++_readCount == 13)
            {
                TouchReportReached.SetResult();
                if (!ReleaseTouchReport.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Touch report barrier was not released.");
            }
            report.CopyTo(buffer, 0);
            length = report.Length;
            return 0;
        }

        public int SetFeatureReport(nint device, byte[] buffer)
        {
            Sent.Add([.. buffer]);
            return 0;
        }

        public void Dispose() => ReleaseTouchReport.Dispose();
    }
}