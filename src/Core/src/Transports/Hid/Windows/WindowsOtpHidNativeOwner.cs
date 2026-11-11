// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Owns one Windows feature handle lifetime, its original worker and transferred physical claim.</summary>
/// <remarks>
/// Rooting precedes acquisition. One ordinary slot admits GET or SET without a backlog. Synchronous
/// feature calls borrow only private framed arrays through return. Native release evidence, not task
/// completion or cancellation, permits claim release. Unknown release retains the rooted lifetime.
/// </remarks>
internal sealed class WindowsOtpHidNativeOwner
{
    private readonly IWindowsHidFeatureReportApi _reportApi;
    private readonly IDisposable? _physicalClaim;
    private readonly Lock _sync = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private GCHandle _root;
    private int _workerId;
    private Action? _work;
    private TaskCompletionSource? _workCompletion;
    private TaskCompletionSource<ReadOnlyMemory<byte>>? _reader;
    private byte[]? _outputCopy;
    private byte[]? _inputCopy;
    private Exception? _workerFailure;
    private bool _openAttempted;
    private bool _openSucceeded;
    private bool _active;
    private bool _stopping;

    private WindowsOtpHidNativeOwner(IWindowsHidFeatureReportApi reportApi, IDisposable? physicalClaim)
    {
        _reportApi = reportApi;
        _root = GCHandle.Alloc(this);
        _physicalClaim = physicalClaim;
    }

    /// <summary>Gets validated descriptor metadata; native length includes the report-ID prefix.</summary>
    internal WindowsHidFeatureReportMetadata Metadata { get; private set; }

    /// <summary>Transfers the claim before acquisition and opens on a context-suppressed dedicated worker.</summary>
    /// <remarks>Failed/canceled open awaits checked cleanup, preserving both primary and cleanup errors.</remarks>
    internal static Task<WindowsOtpHidNativeOwner> OpenAsync(string devicePath, IWindowsHidFeatureReportApi reportApi,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null, Func<Task, Task>? beforeOpenCompletionAwait = null)
    {
        WindowsOtpHidNativeOwner owner;
        try { owner = new WindowsOtpHidNativeOwner(reportApi, physicalClaim); }
        catch (Exception constructionError)
        {
            try { physicalClaim?.Dispose(); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
        return owner.OpenAndCleanupAsync(devicePath, cancellationToken, beforeOpenCompletionAwait);
    }

    private async Task<WindowsOtpHidNativeOwner> OpenAndCleanupAsync(string devicePath, CancellationToken cancellationToken,
        Func<Task, Task>? beforeOpenCompletionAwait)
    {
        try
        {
            using var cancellation = cancellationToken.UnsafeRegister(_ => CancelOpen(), null);
            var opening = StartOpen(devicePath, cancellationToken);
            // Per-open test seam: hold the consumer before observing worker publication, without changing its scheduler.
            if (beforeOpenCompletionAwait is not null) await beforeOpenCompletionAwait(opening).ConfigureAwait(false);
            await opening.ConfigureAwait(false);
            return this;
        }
        catch (Exception openError)
        {
            try { await ShutdownAsync().ConfigureAwait(false); }
            catch (Exception cleanupError)
            {
                if (ReferenceEquals(openError, cleanupError)) throw;
                throw new AggregateException(openError, cleanupError);
            }
            throw;
        }
    }

    private Task StartOpen(string devicePath, CancellationToken cancellationToken)
    {
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            _workCompletion = opened;
            _work = () => OpenOnWorker(devicePath, cancellationToken, opened);
            try
            {
                var worker = new Thread(WorkLoop) { IsBackground = true, Name = "YubiKit Windows OTP" };
                if (ExecutionContext.IsFlowSuppressed()) worker.Start();
                else { using (ExecutionContext.SuppressFlow()) worker.Start(); }
            }
            catch (Exception error)
            {
                // No worker started, so no native resources could have been acquired.
                _stopping = true;
                _work = null;
                _workCompletion = null;
                _ = opened.TrySetException(error);
                FinishRelease(error);
                _signal.Dispose();
                return opened.Task;
            }
            _ = _signal.Set();
        }
        return opened.Task;
    }

    private void OpenOnWorker(string devicePath, CancellationToken cancellationToken, TaskCompletionSource opened)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _openAttempted = true; // Even a throwing Open may have acquired resources.
            Metadata = _reportApi.Open(devicePath);
            // Preserve the Windows signed-short descriptor boundary, and require a payload.
            if (Metadata.FeatureReportByteLength is < 2 or > (ushort)short.MaxValue)
                throw new IOException("Windows OTP feature descriptor length is invalid.");
            lock (_sync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_stopping, this);
                // Retire creation-token authority atomically with success, before any consumer can resume.
                _openSucceeded = true;
                _ = opened.TrySetResult();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { _ = opened.TrySetCanceled(cancellationToken); }
        catch (Exception error) { _ = opened.TrySetException(error); }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_workCompletion, opened)) _workCompletion = null;
            }
        }
    }

    private void CancelOpen()
    {
        lock (_sync)
        {
            if (!_openSucceeded) RequestShutdown();
        }
    }

    private void WorkLoop()
    {
        Volatile.Write(ref _workerId, Environment.CurrentManagedThreadId);
        try
        {
            while (true)
            {
                _ = _signal.WaitOne();
                Action? work;
                lock (_sync) { work = _work; _work = null; }
                work?.Invoke();
                lock (_sync) { if (!_stopping || _work is not null) continue; }
                Close();
                return;
            }
        }
        catch (Exception error)
        {
            StopAfterWorkerFailure(error);
            Close(); // The original worker attempts checked cleanup; never create a replacement.
        }
        finally { lock (_sync) _signal.Dispose(); }
    }

    private void StopAfterWorkerFailure(Exception error)
    {
        lock (_sync)
        {
            _stopping = true;
            _workerFailure ??= error;
            _work = null;
            if (_outputCopy is not null) CryptographicOperations.ZeroMemory(_outputCopy);
            if (_inputCopy is not null) CryptographicOperations.ZeroMemory(_inputCopy);
            _outputCopy = null;
            _inputCopy = null;
            _active = false;
            _ = _workCompletion?.TrySetException(error);
            _ = _reader?.TrySetException(error);
            _workCompletion = null;
            _reader = null;
        }
    }

    /// <summary>Copies one descriptor-sized payload with a zero report ID before publishing SET.</summary>
    /// <remarks>Returns the actual asynchronous-continuation TCS task. Cancellation after dispatch does not detach native work.</remarks>
    internal Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (report.Length != Metadata.FeatureReportByteLength - 1)
            throw new ArgumentException("The Windows feature payload must match its descriptor length.", nameof(report));
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            Admit();
            byte[] copy;
            try
            {
                copy = new byte[Metadata.FeatureReportByteLength];
                report.Span.CopyTo(copy.AsSpan(1));
            }
            catch { _active = false; throw; }
            _outputCopy = copy;
            _workCompletion = completion;
            _work = () => Send(copy, cancellationToken, completion);
            _ = _signal.Set();
        }
        return completion.Task;
    }

    private void Send(byte[] copy, CancellationToken cancellationToken, TaskCompletionSource completion)
    {
        Exception? error = null;
        bool cancelled = cancellationToken.IsCancellationRequested;
        try { if (!cancelled) CheckResult(_reportApi.SetFeature(copy), "HidD_SetFeature"); }
        catch (Exception failure) { error = failure; }
        finally { CryptographicOperations.ZeroMemory(copy); }
        lock (_sync)
        {
            _outputCopy = null;
            _workCompletion = null;
            _active = false;
            if (cancelled) _ = completion.TrySetCanceled(cancellationToken);
            else if (error is not null) _ = completion.TrySetException(error);
            else _ = completion.TrySetResult();
        }
    }

    /// <summary>Gets a full native feature report and transfers a separate payload array after clearing native staging.</summary>
    /// <remarks>
    /// No actual-count proof is available from HidD. The caller owns successful payload storage and
    /// its sensitive-data cleanup. Failed staging is cleared. Accepted native success wins late cancellation.
    /// </remarks>
    internal Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reader = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            Admit();
            _reader = reader;
            _work = () => Receive(cancellationToken, reader);
            _ = _signal.Set();
        }
        return reader.Task;
    }

    private void Receive(CancellationToken cancellationToken, TaskCompletionSource<ReadOnlyMemory<byte>> reader)
    {
        Exception? error = null;
        byte[]? payload = null;
        bool cancelled = cancellationToken.IsCancellationRequested;
        try
        {
            if (!cancelled)
            {
                _inputCopy = new byte[Metadata.FeatureReportByteLength];
                CheckResult(_reportApi.GetFeature(_inputCopy), "HidD_GetFeature");
                if (_inputCopy[0] != 0) throw new IOException("Windows OTP returned a numbered feature report.");
                payload = _inputCopy.AsSpan(1).ToArray(); // Escapes to caller independently of native staging.
            }
        }
        catch (Exception failure) { error = failure; }
        finally { if (_inputCopy is not null) CryptographicOperations.ZeroMemory(_inputCopy); }
        lock (_sync)
        {
            _inputCopy = null;
            _reader = null;
            _active = false;
            if (cancelled) _ = reader.TrySetCanceled(cancellationToken);
            else if (error is not null) _ = reader.TrySetException(error);
            else if (payload is not null) _ = reader.TrySetResult(payload);
            else _ = reader.TrySetException(new IOException("Windows OTP GET returned no report."));
        }
    }

    private static void CheckResult(WindowsHidFeatureResult result, string operation)
    {
        if (result.ErrorCode != 0) throw new Win32Exception(unchecked((int)result.ErrorCode), $"{operation} failed.");
    }

    private void Admit()
    {
        ObjectDisposedException.ThrowIf(_stopping, this);
        if (_active) throw new InvalidOperationException("Another raw Windows OTP operation is active.");
        _active = true;
    }

    /// <summary>Synchronously drains the shared shutdown; rejects worker self-wait.</summary>
    internal void Dispose()
    {
        if (Environment.CurrentManagedThreadId == Volatile.Read(ref _workerId))
            throw new InvalidOperationException("Cannot synchronously drain a Windows OTP owner from its worker.");
        ShutdownAsync().GetAwaiter().GetResult();
    }

    /// <summary>Requests nonblocking, nonthrowing finalizer shutdown without acknowledging release.</summary>
    internal void RequestShutdown()
    {
        try { _ = ShutdownAsync(); }
        catch { /* Finalization requests only; never infer release from an exception. */ }
    }

    /// <summary>Stops admission and returns the shared task for accepted-work drain and checked close.</summary>
    /// <remarks>No timeout, close retry, native abort or worker replacement. A held call can keep shutdown pending indefinitely.</remarks>
    internal Task ShutdownAsync()
    {
        lock (_sync)
        {
            if (_stopping) return _closed.Task;
            _stopping = true;
            _ = _signal.Set();
            return _closed.Task;
        }
    }

    private void Close()
    {
        try
        {
            Exception? diagnostic = _workerFailure;
            if (_openAttempted)
            {
                var release = _reportApi.CloseOnce();
                if (!release.NativeReleaseProven) throw new IOException($"Windows OTP release unproven: error={release.ErrorCode}.");
                if (release.ErrorCode != 0)
                    diagnostic = Combine(diagnostic, new Win32Exception(unchecked((int)release.ErrorCode), "Windows OTP close diagnostic."));
            }
            FinishRelease(diagnostic);
        }
        catch (Exception error) { FailUnreleased(Combine(_workerFailure, error) ?? error); }
    }

    private static Exception? Combine(Exception? first, Exception? second) =>
        first is null ? second : second is null ? first : new AggregateException(first, second);

    private void FinishRelease(Exception? diagnostic)
    {
        try { _physicalClaim?.Dispose(); }
        catch (Exception error) { FailUnreleased(Combine(diagnostic, error) ?? error); return; }
        _root.Free();
        if (diagnostic is null) _ = _closed.TrySetResult();
        else _ = _closed.TrySetException(diagnostic);
    }

    private void FailUnreleased(Exception cause)
    {
        var failure = new UnrecoveredConnectionException("Windows OTP release was not proven; original owner and claim retained.", cause);
        try { if (_physicalClaim is not null) DeviceConnectionRegistry.TryMarkUnrecovered(_physicalClaim, failure); }
        catch (Exception error) { failure = new UnrecoveredConnectionException(failure.Message, new AggregateException(cause, error)); }
        _ = _closed.TrySetException(failure); // Retain root and original claim. No retry.
    }
}