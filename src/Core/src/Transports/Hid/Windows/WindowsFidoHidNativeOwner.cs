// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Owns one overlapped Windows report lifetime and its single ordinary operation slot.</summary>
/// <remarks>
/// The original worker owns every API call. Completion and cancellation callbacks only record and
/// signal. Accepted storage joins Submit return with callback resource last use before retirement.
/// Missing terminal evidence retains the rooted lifetime indefinitely, without force close or replay.
/// </remarks>
internal sealed class WindowsFidoHidNativeOwner
{
    private readonly IWindowsHidReportApi _reportApi;
    private readonly IDisposable? _physicalClaim;
    private readonly Lock _sync = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly TaskCompletionSource _opened = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private GCHandle _root;
    private Request? _request;
    private Exception? _failure;
    private bool _stopping;
    private bool _signalRetired;
    private int _workerId;

    private WindowsFidoHidNativeOwner(IWindowsHidReportApi reportApi, IDisposable? physicalClaim)
    {
        _reportApi = reportApi;
        _root = GCHandle.Alloc(this); // Acquisition cannot begin before this independent owner is rooted.
        _physicalClaim = physicalClaim;
    }

    internal WindowsHidReportMetadata Metadata { get; private set; }

    /// <summary>Transfers the claim before worker acquisition; failed opens await checked cleanup.</summary>
    internal static async Task<WindowsFidoHidNativeOwner> OpenAsync(string path, IWindowsHidReportApi reportApi,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null,
        Func<Task, Task>? beforeOpenContinuation = null)
    {
        WindowsFidoHidNativeOwner owner;
        try { owner = new WindowsFidoHidNativeOwner(reportApi, physicalClaim); }
        catch (Exception constructionError)
        {
            try { physicalClaim?.Dispose(); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
        try
        {
            using var registration = cancellationToken.UnsafeRegister(static state =>
                ((WindowsFidoHidNativeOwner?)state)?.CancelOpen(), owner);
            owner.Start(path, cancellationToken);
            // Per-call test seam: hold the factory while the worker publishes its open result.
            if (beforeOpenContinuation is not null)
                await beforeOpenContinuation(owner._opened.Task).ConfigureAwait(false);
            await owner._opened.Task.ConfigureAwait(false);
            return owner;
        }
        catch (Exception openError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception releaseError) { throw new AggregateException(openError, releaseError); }
            throw;
        }
    }

    private void CancelOpen()
    {
        lock (_sync)
        {
            // Success publication ends opening-token ownership, even before the factory resumes.
            if (_opened.Task.IsCompleted) return;
            _stopping = true;
            if (!_signalRetired) _signal.Set();
        }
    }

    private void Start(string path, CancellationToken cancellationToken)
    {
        try
        {
            var worker = new Thread(() => WorkLoop(path, cancellationToken))
            { IsBackground = true, Name = "YubiKit Windows FIDO" };
            if (ExecutionContext.IsFlowSuppressed()) worker.Start();
            else { using (ExecutionContext.SuppressFlow()) worker.Start(); }
        }
        catch (Exception error)
        {
            // Thread.Start failed, so no native acquisition occurred and no API call is needed.
            lock (_sync) _stopping = true;
            FinishRelease(error);
            RetireSignal();
            _opened.TrySetException(error);
        }
    }

    private void Open(string path, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Metadata = _reportApi.Open(path);
            if (Metadata.InputReportByteLength < 2 || Metadata.OutputReportByteLength < 2)
                throw new IOException("Windows HID descriptor has no report payload.");
            lock (_sync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_stopping, this);
                _opened.TrySetResult();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RequestShutdown();
            _opened.TrySetCanceled(cancellationToken);
        }
        catch (Exception error)
        {
            RequestShutdown();
            _opened.TrySetException(error);
        }
    }

    /// <summary>Copies descriptor-sized payload into owned report-ID-zero framing before publication.</summary>
    internal Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (payload.Length != Metadata.OutputReportByteLength - 1)
            throw new ArgumentException("Payload must match the output descriptor length.", nameof(payload));
        return Admit(WindowsHidReportDirection.Output, payload, false, cancellationToken);
    }

    /// <summary>Returns the actual operation TCS task; successful payload storage escapes independently.</summary>
    internal Task<ReadOnlyMemory<byte>> ReceiveAsync(bool typed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (typed && Metadata.InputReportByteLength != 65)
            throw new IOException("FIDO input reports must contain exactly 64 payload bytes.");
        return Admit(WindowsHidReportDirection.Input, default, typed, cancellationToken);
    }

    private Task<ReadOnlyMemory<byte>> Admit(WindowsHidReportDirection direction, ReadOnlyMemory<byte> payload,
        bool typed, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);
            if (_request is not null) throw new InvalidOperationException("Another raw Windows FIDO operation is active.");
            int length = direction == WindowsHidReportDirection.Input
                ? Metadata.InputReportByteLength : Metadata.OutputReportByteLength;
            var report = new byte[length];
            payload.Span.CopyTo(report.AsSpan(1));
            var request = new Request(this, new WindowsHidOverlappedOperation(direction, report, RecordCompletion), cancellationToken, typed);
            try
            {
                request.Registration = cancellationToken.UnsafeRegister(static state =>
                {
                    if (state is Request pending) pending.Owner.RecordCancellation(pending);
                }, request);
            }
            catch { CryptographicOperations.ZeroMemory(report); throw; }
            _request = request;
            _signal.Set();
            return request.Completion.Task;
        }
    }

    private void RecordCancellation(Request request)
    {
        lock (_sync)
        {
            request.CancellationRequested = true;
            if (!_signalRetired) _signal.Set();
        }
    }

    private void RecordCompletion(WindowsHidOverlappedOperation operation, WindowsHidCompletion completion)
    {
        lock (_sync)
        {
            if (_request is not { } request || !ReferenceEquals(request.Operation, operation) || request.NativeCompletion.HasValue) return;
            request.NativeCompletion = completion;
            // Resource-last-use publication: after signaling, neither this recorder nor its native
            // caller may touch request storage, pointer or handle. Runtime epilogue is BCL-owned.
            if (!_signalRetired) _signal.Set();
        }
    }

    private void WorkLoop(string path, CancellationToken cancellationToken)
    {
        Volatile.Write(ref _workerId, Environment.CurrentManagedThreadId);
        try
        {
            Open(path, cancellationToken);
            Drain();
        }
        catch (Exception error)
        {
            lock (_sync) { _stopping = true; _failure ??= error; }
            if (_request is { Operation.PreparationFailedBeforeNativeDispatch: true } preparation)
            {
                FailPreparation(preparation, error);
                return;
            }
            // An interrupted wait can still drain on this original worker. Unknown Submit state or
            // failed retirement cannot be retried or treated as release proof.
            if (_request is null || _request.CanDrainAfterFailure)
            {
                try { Drain(); }
                catch (Exception drainError) { lock (_sync) _failure = new AggregateException(error, drainError); }
            }
            // Otherwise quarantine with pending operation/shutdown tasks and retained native storage.
        }
        finally { RetireSignal(); }
    }

    private void Drain()
    {
        while (true)
        {
            Request? request;
            lock (_sync)
            {
                request = _request;
                if (request is null && _stopping) break;
            }
            if (request is not null && Advance(request)) continue;
            _signal.WaitOne();
        }
        Close();
    }

    private void FailPreparation(Request request, Exception error)
    {
        // Allocation has unwound without dispatch. Drain the recorder outside its admission lock.
        request.Registration.Dispose();
        // A retained BCL pin is not active payload access. Zero bytes, but retain request/API/root:
        // no returned pointer is not proof of BCL cleanup, so neither Retire nor Close is permitted.
        CryptographicOperations.ZeroMemory(request.Operation.NativeReport);
        lock (_sync) request.Completion.TrySetException(error);
        FailUnreleased(_failure ?? error); // Actual operation terminalization precedes shared shutdown.
    }

    private bool Advance(Request request)
    {
        if (!request.DispatchStarted)
        {
            lock (_sync)
            {
                if (request.CancellationRequested || _stopping)
                {
                    request.PreDispatch = true;
                }
                else request.DispatchStarted = true;
            }
            if (request.PreDispatch) { CompleteRequest(request, null); return true; }
            var submission = _reportApi.Submit(request.Operation);
            request.SubmissionError = submission.ErrorCode;
            request.SubmitReturned = true;
        }

        WindowsHidCompletion? completion;
        bool cancel;
        lock (_sync)
        {
            completion = request.NativeCompletion;
            cancel = !completion.HasValue && !request.CancelIssued && (request.CancellationRequested || _stopping);
        }
        if (request.SubmissionError != 0 || completion.HasValue)
        {
            CompleteRequest(request, completion);
            return true;
        }
        if (cancel)
        {
            request.CancelIssued = true;
            uint error = _reportApi.Cancel(request.Operation);
            if (error is not (0 or 1168))
            {
                lock (_sync) { _stopping = true; _failure ??= NativeError("CancelIoEx", error); }
            }
        }
        return false;
    }

    private void CompleteRequest(Request request, WindowsHidCompletion? completion)
    {
        // No lock is held while unregistering: a running recorder may need _sync to finish.
        request.Registration.Dispose();
        Exception? error = null;
        bool canceled = request.PreDispatch && request.CancellationToken.IsCancellationRequested;
        ReadOnlyMemory<byte> payload = default;
        if (!request.PreDispatch)
        {
            uint nativeError = request.SubmissionError != 0 ? request.SubmissionError : completion.GetValueOrDefault().ErrorCode;
            canceled = nativeError == 995 && request.CancellationToken.IsCancellationRequested;
            if (nativeError != 0 && !canceled) error = NativeError("HID report", nativeError);
            if (nativeError == 0) error = ValidateCompletion(request, completion.GetValueOrDefault());
            // Mark first, so an unexpected Retire exception never triggers a second retirement.
            request.RetirementStarted = true;
            _reportApi.Retire(request.Operation);
            if (error is null && !canceled && request.Operation.Direction == WindowsHidReportDirection.Input)
                payload = request.Operation.NativeReport.AsSpan(1).ToArray();
        }
        else if (!canceled) error = new ObjectDisposedException(nameof(WindowsFidoHidNativeOwner));
        CryptographicOperations.ZeroMemory(request.Operation.NativeReport);
        lock (_sync)
        {
            if (error is not null) _stopping = true;
            if (canceled) request.Completion.TrySetCanceled(request.CancellationToken);
            else if (error is not null) request.Completion.TrySetException(error);
            else request.Completion.TrySetResult(payload);
            _request = null; // Actual public completion and slot release share admission lock.
        }
    }

    private static Exception? ValidateCompletion(Request request, WindowsHidCompletion completion)
    {
        byte[] report = request.Operation.NativeReport;
        return completion.BytesTransferred != report.Length || report[0] != 0 || (request.Typed && report.Length != 65)
            ? new IOException("Windows HID report has an invalid byte count or report ID.") : null;
    }

    private static IOException NativeError(string operation, uint error) => new($"{operation} failed with Windows error {error}.");

    /// <summary>Rejects synchronous self-drain on the original worker.</summary>
    internal void ThrowIfWorkerThread()
    {
        if (Environment.CurrentManagedThreadId == Volatile.Read(ref _workerId))
            throw new InvalidOperationException("Cannot synchronously drain the Windows FIDO owner from its worker.");
    }

    internal void Dispose() { ThrowIfWorkerThread(); ShutdownAsync().GetAwaiter().GetResult(); }

    /// <summary>Finalizer-safe request only; no native work, wait or application continuation.</summary>
    internal void RequestShutdown()
    {
        try { _ = ShutdownAsync(); }
        catch { /* Finalization never asserts release or propagates an exception. */ }
    }

    internal Task ShutdownAsync()
    {
        lock (_sync)
        {
            _stopping = true;
            if (!_signalRetired) _signal.Set();
            return _closed.Task;
        }
    }

    private void Close()
    {
        try
        {
            var release = _reportApi.CloseOnce();
            if (!release.NativeReleaseProven)
            {
                FailUnreleased(Combine(_failure, NativeError("Close (release unproven)", release.ErrorCode)));
                return;
            }
            Exception? diagnostic = release.ErrorCode == 0 ? _failure : Combine(_failure, NativeError("Close", release.ErrorCode));
            FinishRelease(diagnostic);
        }
        catch (Exception error) { FailUnreleased(Combine(_failure, error)); }
    }

    private void FinishRelease(Exception? diagnostic)
    {
        try { _physicalClaim?.Dispose(); }
        catch (Exception error) { FailUnreleased(Combine(diagnostic, error)); return; }
        _root.Free();
        if (diagnostic is null) _closed.TrySetResult();
        else _closed.TrySetException(diagnostic);
    }

    private void FailUnreleased(Exception error)
    {
        var failure = new UnrecoveredConnectionException("Windows FIDO release was not proven; owner and physical claim retained.", error);
        try { if (_physicalClaim is not null) DeviceConnectionRegistry.TryMarkUnrecovered(_physicalClaim, failure); }
        catch (Exception markError) { failure = new UnrecoveredConnectionException(failure.Message, new AggregateException(error, markError)); }
        _closed.TrySetException(failure);
    }

    private static Exception Combine(Exception? first, Exception second) =>
        first is null ? second : new AggregateException(first, second);

    private void RetireSignal()
    {
        lock (_sync) { _signalRetired = true; _signal.Dispose(); }
    }

    private sealed class Request(WindowsFidoHidNativeOwner owner, WindowsHidOverlappedOperation operation,
        CancellationToken cancellationToken, bool typed)
    {
        internal WindowsFidoHidNativeOwner Owner { get; } = owner;
        internal WindowsHidOverlappedOperation Operation { get; } = operation;
        internal CancellationToken CancellationToken { get; } = cancellationToken;
        internal bool Typed { get; } = typed;
        internal TaskCompletionSource<ReadOnlyMemory<byte>> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationTokenRegistration Registration;
        internal WindowsHidCompletion? NativeCompletion;
        internal bool CancellationRequested, DispatchStarted, SubmitReturned, CancelIssued, PreDispatch, RetirementStarted;
        internal uint SubmissionError;
        internal bool CanDrainAfterFailure => !RetirementStarted && (!DispatchStarted || SubmitReturned);
    }
}