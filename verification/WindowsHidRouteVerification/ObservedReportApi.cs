// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace WindowsHidRouteVerification;

// Verification-only transparent operation proxy. Every submission/cancel/free/close reaches
// the production API. No manufactured completion, pointer mutation or alternate native API.
internal sealed class ObservedReportApi : IWindowsHidReportApi, IDisposable
{
    private readonly WindowsHidReportApi _inner = new(NativePipeFixture.Metadata);
    private WindowsHidOverlappedOperation? _native;
    internal ManualResetEventSlim ReturnSubmit { get; } = new(true);
    internal ManualResetEventSlim DeliverCallback { get; } = new(true);
    internal TaskCompletionSource Submitted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource CallbackArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource CallbackPublished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource CancelReturned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal byte[]? Storage { get; private set; }
    internal int CallbackCount;
    internal int RetireCount;
    internal int CloseCount;
    internal Task? PublicOperation;
    internal bool PublicTerminalAtClose;
    internal uint CancelResult { get; private set; }
    internal uint SubmissionError { get; private set; }
    internal WindowsHidCompletion Completion { get; private set; }
    internal WindowsHidRelease Release { get; private set; }

    public WindowsHidReportMetadata Open(string devicePath) => _inner.Open(devicePath);

    public WindowsHidSubmission Submit(WindowsHidOverlappedOperation operation)
    {
        Storage = operation.NativeReport;
        // Capture independent signals before last-use publication; the callback touches no
        // operation, native pointer, storage or binding after forwarding Complete.
        TaskCompletionSource published = CallbackPublished;
        _native = new WindowsHidOverlappedOperation(operation.Direction, operation.NativeReport, (_, completion) =>
        {
            Interlocked.Increment(ref CallbackCount);
            Completion = completion;
            CallbackArrived.TrySetResult();
            DeliverCallback.Wait();
            operation.Complete(completion);
            published.TrySetResult();
        });
        WindowsHidSubmission result;
        try { result = _inner.Submit(_native); }
        catch
        {
            operation.PreparationFailedBeforeNativeDispatch = _native.PreparationFailedBeforeNativeDispatch;
            throw;
        }
        SubmissionError = result.ErrorCode;
        Submitted.TrySetResult();
        ReturnSubmit.Wait();
        return result;
    }

    public uint Cancel(WindowsHidOverlappedOperation operation)
    {
        CancelResult = _inner.Cancel(_native ?? throw new InvalidOperationException("No native request."));
        CancelReturned.TrySetResult();
        return CancelResult;
    }

    public void Retire(WindowsHidOverlappedOperation operation)
    {
        _inner.Retire(_native ?? throw new InvalidOperationException("No native request."));
        Interlocked.Increment(ref RetireCount);
    }

    public WindowsHidRelease CloseOnce()
    {
        PublicTerminalAtClose = PublicOperation?.IsCompleted == true;
        Release = _inner.CloseOnce();
        Interlocked.Increment(ref CloseCount);
        return Release;
    }

    public void Dispose()
    {
        ReturnSubmit.Dispose();
        DeliverCallback.Dispose();
    }
}