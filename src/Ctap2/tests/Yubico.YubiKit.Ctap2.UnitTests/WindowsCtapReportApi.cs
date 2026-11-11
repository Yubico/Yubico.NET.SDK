using System.Buffers.Binary;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Ctap2.UnitTests;

// The only fake transport layer: native report-ID-zero arrays below the real Windows owner.
internal sealed class WindowsCtapReportApi : IWindowsHidReportApi, IDisposable
{
    internal const uint Channel = 0x01020304;
    private readonly Lock _sync = new();
    private readonly Queue<byte[]> _reports = new();
    private readonly HashSet<WindowsHidOverlappedOperation> _completed = [];
    private WindowsHidOverlappedOperation? _pending;
    private bool _cleanup, _requestComplete;
    private ushort _requestLength;
    internal bool HoldWrite, HoldGetInfo, CancelCredential, HoldClose;
    internal int InitCount, Closes, CancelCount, NativeCancelCount;
    internal CtapStatus InfoStatus;
    internal List<byte[]> Writes { get; } = [];
    internal ManualResetEventSlim ReleaseWrite { get; } = new();
    internal ManualResetEventSlim ReleaseClose { get; } = new();
    internal TaskCompletionSource WriteEntered { get; private set; } = Signal();
    internal TaskCompletionSource GetInfoSent { get; } = Signal();
    internal TaskCompletionSource RecoveryRead { get; } = Signal();
    internal TaskCompletionSource PendingRead { get; } = Signal();
    internal TaskCompletionSource Closing { get; } = Signal();
    private Thread? _worker;

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void HoldNextWrite()
    {
        ReleaseWrite.Reset();
        WriteEntered = Signal();
        HoldWrite = true;
    }

    public WindowsHidReportMetadata Open(string devicePath)
    {
        _worker = Thread.CurrentThread;
        return new(0xF1D0, 1, 65, 65);
    }

    public WindowsHidSubmission Submit(WindowsHidOverlappedOperation operation)
    {
        Assert.Same(_worker, Thread.CurrentThread);
        Assert.Equal(65, operation.NativeReport.Length);
        Assert.Equal(0, operation.NativeReport[0]);
        if (operation.Direction == WindowsHidReportDirection.Output)
        {
            WriteEntered.TrySetResult();
            if (HoldWrite) ReleaseWrite.Wait();
            lock (_sync)
            {
                if (!_cleanup) Write(operation.NativeReport.AsSpan(1));
                Complete(operation, 0);
            }
        }
        else
        {
            lock (_sync)
            {
                Assert.Null(_pending);
                if (_cleanup) Complete(operation, 995);
                else if (_reports.TryDequeue(out var report))
                {
                    report.CopyTo(operation.NativeReport, 0);
                    Complete(operation, 0);
                }
                else
                {
                    _pending = operation;
                    PendingRead.TrySetResult();
                    if (CancelCount > 0) RecoveryRead.TrySetResult();
                }
            }
        }
        // Completion-before-submit-return is legal; retirement belongs to the real worker.
        return new(0);
    }

    private void Write(ReadOnlySpan<byte> payload)
    {
        Writes.Add(payload.ToArray()); // Preserve packets beyond owner storage retirement.
        byte command = payload[4];
        if (command == 0x86)
        {
            InitCount++;
            var init = new byte[17];
            payload.Slice(7, 8).CopyTo(init);
            BinaryPrimitives.WriteUInt32BigEndian(init.AsSpan(8), Channel);
            Emit(Response(uint.MaxValue, 0x86, init));
        }
        else if (command == 0x90)
        {
            _requestLength = BinaryPrimitives.ReadUInt16BigEndian(payload[5..]);
            if (CancelCredential && payload[7] == CtapCommand.MakeCredential)
                Assert.InRange(_requestLength, 58, 116);
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
            Assert.InRange(_requestLength, 58, 116);
            _requestComplete = true;
            Emit(Response(Channel, 0xBB, [2]));
            Emit(Response(Channel, 0xBB, [2]));
        }
        else
        {
            Assert.Equal(0x91, command);
            Assert.True(_requestComplete);
            Assert.Equal(1, ++CancelCount);
            Assert.Equal(0, BinaryPrimitives.ReadUInt16BigEndian(payload[5..]));
            Assert.True(payload[7..].IndexOfAnyExcept((byte)0) < 0);
            var response = new byte[58];
            response[0] = (byte)CtapStatus.KeepAliveCancel;
            Emit(Response(Channel, 0x90, response));
        }
    }

    internal void EmitInfo() => Emit(Response(Channel, 0x90,
        [(byte)InfoStatus, 0xA1, 1, 0x81, 0x68, 0x46, 0x49, 0x44, 0x4F, 0x5F, 0x32, 0x5F, 0x30]));
    internal void ReleaseCancelResponse() => Emit(Response(Channel, 0, []));

    private void Emit(byte[] report)
    {
        lock (_sync)
        {
            if (_pending is { } pending)
            {
                _pending = null;
                report.CopyTo(pending.NativeReport, 0);
                Complete(pending, 0);
            }
            else _reports.Enqueue(report);
        }
    }

    private static byte[] Response(uint channel, byte command, ReadOnlySpan<byte> data)
    {
        var report = new byte[65];
        BinaryPrimitives.WriteUInt32BigEndian(report.AsSpan(1), channel);
        report[5] = command;
        BinaryPrimitives.WriteUInt16BigEndian(report.AsSpan(6), (ushort)data.Length);
        data[..Math.Min(57, data.Length)].CopyTo(report.AsSpan(8));
        return report;
    }

    private void Complete(WindowsHidOverlappedOperation operation, uint error)
    {
        Assert.True(_completed.Add(operation));
        operation.Complete(new(error, error == 0 ? 65u : 0u));
        // No access to operation resources after publishing callback last use.
    }

    public uint Cancel(WindowsHidOverlappedOperation operation)
    {
        Assert.Same(_worker, Thread.CurrentThread);
        lock (_sync)
        {
            // Cleanup can publish native completion after the worker decided to cancel.
            if (_completed.Contains(operation)) return 1168;
            NativeCancelCount++;
            Assert.Same(_pending, operation);
            _pending = null;
            Complete(operation, 995);
        }
        return 0;
    }

    public void Retire(WindowsHidOverlappedOperation operation)
    {
        Assert.Same(_worker, Thread.CurrentThread);
        lock (_sync) Assert.True(_completed.Remove(operation));
    }

    public WindowsHidRelease CloseOnce()
    {
        Assert.Same(_worker, Thread.CurrentThread);
        lock (_sync)
        {
            Assert.Null(_pending);
            Assert.Empty(_completed);
        }
        Closing.TrySetResult();
        if (HoldClose) ReleaseClose.Wait();
        Closes++;
        return new(true, 0);
    }

    internal void ReleaseAll()
    {
        ReleaseWrite.Set();
        ReleaseClose.Set();
        lock (_sync)
        {
            _cleanup = true;
            if (_pending is not { } pending) return;
            _pending = null;
            Complete(pending, 995);
        }
    }

    public void Dispose()
    {
        // Called after bounded owner drain; join proves the worker no longer uses fake gates.
        if (_worker is not null) Assert.True(_worker.Join(TimeSpan.FromSeconds(5)));
        ReleaseWrite.Dispose();
        ReleaseClose.Dispose();
    }
}