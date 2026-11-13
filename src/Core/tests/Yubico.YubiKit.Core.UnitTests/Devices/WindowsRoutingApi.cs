// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Buffers.Binary;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

internal sealed class WindowsRoutingApi : IWindowsHidReportApi, IWindowsHidFeatureReportApi
{
    internal Action<string>? Before { get; set; }
    internal ushort Length = 65;
    internal int Opens, Closes;
    internal Exception? OpenError { get; set; }
    internal byte[]? Sent;
    internal byte[]? NativeInput;
    internal WindowsHidRelease Release = new(true, 0);
    internal Queue<byte[]> Reports { get; } = new();
    internal bool SessionReports { get; set; }
    internal int Sends, Gets;

    private void Open()
    {
        Assert.Equal(1, Interlocked.Increment(ref Opens));
        Before?.Invoke("open");
        if (OpenError is not null) throw OpenError;
    }

    WindowsHidReportMetadata IWindowsHidReportApi.Open(string devicePath)
    {
        Open();
        return new(0xf1d0, 1, Length, Length);
    }

    WindowsHidFeatureReportMetadata IWindowsHidFeatureReportApi.Open(string devicePath)
    {
        Open();
        return new(1, 6, Length);
    }

    public WindowsHidSubmission Submit(WindowsHidOverlappedOperation operation)
    {
        Before?.Invoke(operation.Direction == WindowsHidReportDirection.Input ? "get" : "set");
        if (operation.Direction == WindowsHidReportDirection.Input)
        {
            NativeInput = operation.NativeReport;
            ReadPayload(operation.NativeReport.AsSpan(1));
        }
        else
        {
            Interlocked.Increment(ref Sends);
            Sent = operation.NativeReport.ToArray(); // Snapshot escapes the borrowed native call.
            if (SessionReports) QueueFidoResponse(operation.NativeReport.AsSpan(1));
        }
        operation.Complete(new(0, (uint)operation.NativeReport.Length));
        Before?.Invoke("last-use"); // Completion alone cannot retire a held Submit.
        return new(0);
    }

    public uint Cancel(WindowsHidOverlappedOperation operation) => 1168;
    public void Retire(WindowsHidOverlappedOperation operation) { }
    public WindowsHidFeatureResult GetFeature(byte[] nativeReport)
    {
        Before?.Invoke("get");
        NativeInput = nativeReport;
        ReadPayload(nativeReport.AsSpan(1));
        return new(0);
    }
    public WindowsHidFeatureResult SetFeature(byte[] nativeReport)
    {
        Before?.Invoke("set");
        Interlocked.Increment(ref Sends);
        Sent = nativeReport.ToArray(); // Snapshot escapes the borrowed native call.
        return new(0);
    }
    public WindowsHidRelease CloseOnce()
    {
        Interlocked.Increment(ref Closes);
        Before?.Invoke("close");
        return Release;
    }

    private void ReadPayload(Span<byte> payload)
    {
        Interlocked.Increment(ref Gets);
        if (Reports.TryDequeue(out var report)) report.CopyTo(payload);
        else payload.Fill(7);
    }

    private void QueueFidoResponse(ReadOnlySpan<byte> payload)
    {
        var response = new byte[64];
        payload[..5].CopyTo(response);
        if (payload[4] == 0x86)
        {
            response[6] = 17;
            payload.Slice(7, 8).CopyTo(response.AsSpan(7));
            BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(15), 0x01020304);
            response[19] = 2; response[20] = 5; response[21] = 8;
        }
        else { response[6] = 1; response[7] = 0xAC; }
        Reports.Enqueue(response);
    }
}