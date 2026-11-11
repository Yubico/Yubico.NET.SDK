// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Buffers.Binary;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using ReportApi = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxOtpHidConnectionTests.ReportApi;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

public class LinuxOtpRecoveryTests
{
    [Fact]
    public async Task CancelAfterAcceptedFrame_WaitsForGetThenResetsBeforeCorrelatedCrcReuse()
    {
        var token = TestContext.Current.CancellationToken;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var release = new ManualResetEventSlim();
        var touch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new ReportApi();
        byte[] expected = [0x03, 0x01, 0x01, 0x02, 0, 0];
        ushort fcs = (ushort)~ChecksumUtils.CalculateCrc(expected.AsSpan(0, 4), 4);
        BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(4), fcs);
        api.Reports.Enqueue(Status(0));
        Prelude(api);
        api.Reports.Enqueue([0, 5, 4, 3, 1, 0, 0, OtpConstants.ResponseTimeoutWaitFlag]);
        Prelude(api);
        byte[] data = new byte[8];
        expected.CopyTo(data, 0);
        data[7] = OtpConstants.ResponsePendingFlag;
        api.Reports.Enqueue(data);
        api.Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
        api.Before = operation =>
        {
            if (operation != "get") return;
            if (api.Gets == 14 && (api.Sets != 11 || api.SentReports[10][7] != OtpConstants.DummyReportWrite))
                throw new InvalidOperationException("Reused exchange before acknowledged abort.");
            if (api.Gets != 13) return;
            touch.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5), token)) throw new TimeoutException("Touch GET not released.");
        };
        await using var connection = await LinuxOtpHidConnection.OpenAsync("fake", api, token);
        await using var protocol = new OtpHidProtocol(connection);
        var abandoned = protocol.SendAndReceiveAsync(OtpConstants.CmdYk4Capabilities, ReadOnlyMemory<byte>.Empty, cancellation.Token);
        try
        {
            await touch.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
            cancellation.Cancel();
            Assert.False(abandoned.IsCompleted); // No early native GET detach.
            Assert.Equal(10, api.Sets);
        }
        finally { release.Set(); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned.WaitAsync(TimeSpan.FromSeconds(5), token));
        var response = await protocol.SendAndReceiveAsync(OtpConstants.CmdYk4Capabilities, ReadOnlyMemory<byte>.Empty, token)
            .WaitAsync(TimeSpan.FromSeconds(5), token);
        Assert.Equal(expected, response.Span[..expected.Length].ToArray());
        Assert.True(ChecksumUtils.CheckCrc(response.Span, expected.Length));
        Assert.Equal(22, api.Sets);
        Assert.Equal(OtpConstants.DummyReportWrite, api.SentReports[10][7]);
        Assert.Equal(OtpConstants.DummyReportWrite, api.SentReports[21][7]);
        foreach (int start in new[] { 0, 11 })
        {
            byte[] frame = new byte[OtpConstants.FrameSize];
            for (int sequence = 0; sequence < 10; sequence++)
            {
                Assert.Equal((byte)(OtpConstants.SlotWriteFlag | sequence), api.SentReports[start + sequence][7]);
                api.SentReports[start + sequence].AsSpan(0, 7).CopyTo(frame.AsSpan(sequence * 7));
            }
            Assert.Equal(OtpConstants.CmdYk4Capabilities, frame[OtpConstants.SlotDataSize]);
            Assert.Equal(ChecksumUtils.CalculateCrc(frame.AsSpan(0, OtpConstants.SlotDataSize), OtpConstants.SlotDataSize),
                BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(OtpConstants.SlotDataSize + 1)));
        }
    }

    private static void Prelude(ReportApi api)
    {
        api.Reports.Enqueue(Status(1));
        for (int i = 0; i < 10; i++) api.Reports.Enqueue(Status(1));
    }

    private static byte[] Status(byte sequence) => [0, 5, 4, 3, sequence, 0, 0, 0];
}