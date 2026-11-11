// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using NativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Owns the native lifetime shared by typed Linux OTP and direct synchronous feature-report access.</summary>
/// <remarks>
/// Roots itself before native acquisition and retains the original physical claim through checked
/// release. One dedicated worker performs open, feature GET/SET and close; feature ioctls never run
/// on the read reactor. Send and receive share one ordinary slot, with no backlog, replacement worker
/// or replay. Operation task completion is not close proof. Unknown native/claim release retains and
/// quarantines the rooted lifetime rather than allowing timeout-based release or a native-close retry.
/// </remarks>
internal sealed class LinuxOtpHidNativeOwner
{
    private readonly ILinuxHidReportApi _api;
    private readonly IDisposable? _claim;
    private readonly Lock _sync = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private GCHandle _root;
    private Thread? _worker;
    private int _workerId;
    private Action? _work;
    // Independent of the dispatch delegate: loop failure must terminalize accepted work.
    private TaskCompletionSource? _workCompletion;
    private TaskCompletionSource<ReadOnlyMemory<byte>>? _reader;
    private byte[]? _outputCopy;
    private byte[]? _inputCopy;
    private LinuxFileSafeHandle? _handle;
    private Exception? _workerFailure;
    private bool _active;
    private bool _stopping;

    private LinuxOtpHidNativeOwner(ILinuxHidReportApi api, IDisposable? claim)
    {
        _api = api;
        _root = GCHandle.Alloc(this); // Root before accepting the claim/acquiring native state.
        _claim = claim;
    }

    /// <summary>Creates a rooted owner and opens its descriptor on the original dedicated worker.</summary>
    /// <param name="path">hidraw path to open.</param>
    /// <param name="api">Borrowed synchronous report seam; not disposed by this owner.</param>
    /// <param name="token">Checked before/after native open; cancellation requests shutdown but cannot detach submitted open.</param>
    /// <param name="claim">Optional original physical/discovery claim transferred to this lifetime, including failed-open cleanup.</param>
    /// <returns>The opened owner; failed/canceled preparation awaits cleanup before propagating its error.</returns>
    /// <remarks>Preserves both open and cleanup failures. A held open/close can keep this task and the original claim pending indefinitely.</remarks>
    internal static async Task<LinuxOtpHidNativeOwner> OpenAsync(string path, ILinuxHidReportApi api,
        CancellationToken token, IDisposable? claim = null)
    {
        LinuxOtpHidNativeOwner owner;
        try { owner = new LinuxOtpHidNativeOwner(api, claim); }
        catch (Exception constructionError)
        {
            try { claim?.Dispose(); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
        try
        {
            using var cancellation = token.UnsafeRegister(_ => owner.RequestShutdown(), null);
            await owner.StartOpen(path, token).ConfigureAwait(false);
            return owner;
        }
        catch (Exception openError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception cleanupError)
            {
                if (ReferenceEquals(openError, cleanupError)) throw;
                throw new AggregateException(openError, cleanupError);
            }
            throw;
        }
    }

    private Task StartOpen(string path, CancellationToken token)
    {
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            _workCompletion = opened;
            _work = () => OpenOnWorker(path, token, opened);
            try
            {
                _worker = new Thread(WorkLoop) { IsBackground = true, Name = "YubiKit Linux OTP" };
                StartWithoutContext(_worker);
            }
            catch (Exception ex)
            {
                // Thread.Start failed: no worker acquired any native state.
                _stopping = true;
                _work = null;
                _workCompletion = null;
                FinishRelease(ex);
                _ = opened.TrySetException(ex);
                _signal.Dispose();
                return opened.Task;
            }
            _ = _signal.Set();
        }
        return opened.Task;
    }

    private static void StartWithoutContext(Thread worker)
    {
        if (ExecutionContext.IsFlowSuppressed()) worker.Start();
        else { using (ExecutionContext.SuppressFlow()) worker.Start(); }
    }

    private void OpenOnWorker(string path, CancellationToken token, TaskCompletionSource opened)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            _handle = _api.Open(path, NativeMethods.OpenFlags.O_RDWR | NativeMethods.OpenFlags.O_CLOEXEC);
            if (_handle.IsInvalid)
            {
                // An invalid SafeHandle owns no descriptor; Dispose cannot invoke native close.
                _handle.Dispose();
                _handle = null;
                throw new IOException("Linux OTP open returned an invalid descriptor.");
            }
            token.ThrowIfCancellationRequested();
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_stopping, this);
                _ = opened.TrySetResult();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { _ = opened.TrySetCanceled(token); }
        catch (Exception ex) { _ = opened.TrySetException(ex); }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_workCompletion, opened)) _workCompletion = null;
            }
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
        catch (Exception ex)
        {
            StopAfterWorkerFailure(ex);
            Close(); // Still on the original worker; try checked cleanup, never replace it.
        }
        finally
        {
            // Stopping and every setter share this lock with event retirement.
            lock (_sync) _signal.Dispose();
        }
    }

    private void StopAfterWorkerFailure(Exception failure)
    {
        lock (_sync)
        {
            _stopping = true;
            _workerFailure ??= failure;
            _work = null;
            if (_outputCopy is not null) CryptographicOperations.ZeroMemory(_outputCopy);
            if (_inputCopy is not null) CryptographicOperations.ZeroMemory(_inputCopy);
            _outputCopy = null;
            _inputCopy = null;
            _active = false;
            _ = (_workCompletion?.TrySetException(failure));
            _ = (_reader?.TrySetException(failure));
            _workCompletion = null;
            _reader = null;
        }
    }

    /// <summary>Admits one feature SET and owns a payload copy before returning its actual operation task.</summary>
    /// <param name="report">Caller-owned payload (0..16382 bytes); typed callers separately enforce eight bytes.</param>
    /// <param name="token">Checked at entry and dispatch; cannot detach or abort a submitted ioctl.</param>
    /// <returns>A task terminal after owned-copy clearing and native return, or pre-dispatch cancellation/failure.</returns>
    /// <remarks>
    /// Admission takes the owner lock and rejects overlap. Caller source storage is never cleared by
    /// the owner. Native SET is attempted at most once, without replay or suffix retry; success may
    /// still succeed after token cancellation. The worker borrows only the owned copy.
    /// </remarks>
    internal Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(report.Length, 16382);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            Admit();
            byte[] copy;
            try { copy = report.ToArray(); } // Escapes to worker; never borrow caller storage.
            catch { _active = false; throw; }
            _workCompletion = completion;
            _outputCopy = copy;
            _work = () => Send(copy, token, completion);
            _ = _signal.Set();
        }
        return completion.Task;
    }

    private void Send(byte[] copy, CancellationToken token, TaskCompletionSource completion)
    {
        Exception? error = null;
        bool cancelled = token.IsCancellationRequested;
        try { if (!cancelled) _api.SetFeature(Handle, copy); }
        catch (Exception ex) { error = ex; }
        finally { CryptographicOperations.ZeroMemory(copy); }
        lock (_sync)
        {
            _outputCopy = null;
            _workCompletion = null;
            _active = false;
            if (cancelled) _ = completion.TrySetCanceled(token);
            else if (error is not null) _ = completion.TrySetException(error);
            else _ = completion.TrySetResult();
        }
    }

    /// <summary>Admits one feature GET into worker-allocated eight-byte storage and returns its actual completion task.</summary>
    /// <param name="token">Checked at entry and dispatch; cancellation cannot detach a submitted GET or free its operation slot.</param>
    /// <returns>Caller-owned exact eight-byte array storage exposed through a read-only view after a complete native GET.</returns>
    /// <remarks>
    /// Failed/partial staging is cleared before failure. Successfully returned storage is transferred
    /// to the caller and never cleared by this owner, including at shutdown. The view provides no
    /// disposal/zeroing API; sensitive output requires caller-managed backing-storage cleanup, and a
    /// copy alone does not erase the original. A successful GET can complete successfully despite
    /// cancellation after dispatch. Admission is locked, overlap is rejected, and continuations run asynchronously.
    /// </remarks>
    internal Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var reader = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            Admit();
            _reader = reader;
            _work = () => Receive(token, reader);
            _ = _signal.Set();
        }
        return reader.Task;
    }

    private void Receive(CancellationToken token, TaskCompletionSource<ReadOnlyMemory<byte>> reader)
    {
        Exception? error = null;
        bool cancelled = token.IsCancellationRequested;
        try
        {
            if (!cancelled)
            {
                _inputCopy = new byte[8];
                _api.GetFeature(Handle, _inputCopy); // L1 rejects incomplete native transfers.
            }
        }
        catch (Exception ex) { error = ex; }
        lock (_sync)
        {
            var report = _inputCopy;
            if (error is not null && report is not null) CryptographicOperations.ZeroMemory(report);
            _inputCopy = null;
            _reader = null;
            _active = false;
            if (cancelled) _ = reader.TrySetCanceled(token);
            else if (error is not null) _ = reader.TrySetException(error);
            else if (report is not null) _ = reader.TrySetResult(report);
            else _ = reader.TrySetException(new IOException("Linux OTP GET returned no report."));
        }
    }

    private void Admit()
    {
        ObjectDisposedException.ThrowIf(_stopping, this);
        if (_active) throw new InvalidOperationException("Another raw Linux OTP operation is active.");
        _active = true;
    }

    private LinuxFileSafeHandle Handle => _handle ?? throw new InvalidOperationException("No Linux OTP handle.");

    /// <summary>Synchronously drains the same checked shutdown outcome used by asynchronous disposal.</summary>
    /// <remarks>
    /// Retained for the synchronous expert facade and IDisposable compatibility. Prefer awaiting
    /// <see cref="ShutdownAsync"/>; submitted native work or close can block this indefinitely. Calls
    /// on the owner worker are rejected to prevent self-drain. Do not hold resources required by the
    /// operation being drained. Returning with an exception is not proof of descriptor/claim release.
    /// </remarks>
    internal void Dispose()
    {
        if (Environment.CurrentManagedThreadId == Volatile.Read(ref _workerId))
            throw new InvalidOperationException("Cannot synchronously drain a Linux OTP owner from its worker.");
        ShutdownAsync().GetAwaiter().GetResult();
    }

    /// <summary>Requests shutdown for facade finalization without waiting for native work or propagating exceptions.</summary>
    /// <remarks>Takes the admission lock; does not promise lock-free invocation or acknowledge release.</remarks>
    internal void RequestShutdown()
    {
        try { _ = ShutdownAsync(); }
        catch { /* Nonthrowing finalizer backstop with no drain wait; never assert native release. */ }
    }

    /// <summary>Stops admission and returns the shared result of original-worker drain and checked release.</summary>
    /// <remarks>
    /// An admitted GET/SET reaches native return and public task terminality before checked close.
    /// Only proven native release permits original-claim disposal/root removal. Linux EINTR/EIO still
    /// report diagnostics after releasing the claim; unknown release or failed claim disposal retains
    /// and quarantines the lifetime. There is no timeout, native abort or close retry, and repeated
    /// calls share the same outcome. A hung native call may leave shutdown pending indefinitely.
    /// </remarks>
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
            if (_handle is not null)
            {
                var outcome = _api.CloseOnce(_handle);
                if (!outcome.Released) throw new IOException($"Linux OTP release unproven: executed={outcome.Executed}, errno={outcome.Error}.");
                if (outcome.Error != 0)
                    diagnostic = Combine(diagnostic, new PlatformApiException("close", outcome.Error, "Linux OTP descriptor released with a close diagnostic."));
            }
            FinishRelease(diagnostic);
        }
        catch (Exception ex) { FailUnreleased(Combine(_workerFailure, ex) ?? ex); }
    }

    private static Exception? Combine(Exception? first, Exception? second) =>
        first is null ? second : second is null ? first : new AggregateException(first, second);

    private void FinishRelease(Exception? diagnostic)
    {
        try { _claim?.Dispose(); }
        catch (Exception ex) { FailUnreleased(Combine(diagnostic, ex) ?? ex); return; }
        _root.Free();
        if (diagnostic is null) _ = _closed.TrySetResult();
        else _ = _closed.TrySetException(diagnostic);
    }

    private void FailUnreleased(Exception cause)
    {
        var failure = new UnrecoveredConnectionException("Linux OTP release was not proven; original owner and claim retained.", cause);
        try { if (_claim is not null) DeviceConnectionRegistry.TryMarkUnrecovered(_claim, failure); }
        catch (Exception ex) { failure = new UnrecoveredConnectionException(failure.Message, new AggregateException(cause, ex)); }
        _ = _closed.TrySetException(failure); // Retain root on unknown native or claim release; never retry.
    }
}