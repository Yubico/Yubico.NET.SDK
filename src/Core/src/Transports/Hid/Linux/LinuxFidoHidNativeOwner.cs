// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using NativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

// One native lifetime, one worker, one ordinary operation slot. No replacement worker,
// command queue, cancellation timeout, or cross-thread close can manufacture release proof.
internal sealed class LinuxFidoHidNativeOwner
{
    internal const int ReportCapacity = 160;
    private static long _nextGeneration;
    [ThreadStatic] private static bool _inReadCallback;
    private readonly long _generation = Interlocked.Increment(ref _nextGeneration);
    private readonly ILinuxHidReportApi _api;
    private readonly ILinuxHidReadReactor _reactor;
    private readonly IDisposable? _claim;
    private readonly Lock _sync = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Queue<byte[]> _reports = new();
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private GCHandle _root;
    private Thread? _worker;
    private int _workerId;
    private Action? _work;
    // Retain accepted work independently of its delegate until public completion.
    // A loop fault can occur before dispatch, or after the delegate was removed.
    private TaskCompletionSource? _workCompletion;
    private byte[]? _outputCopy;
    private Exception? _workerFailure;
    private LinuxFileSafeHandle? _handle;
    private ILinuxHidReadRegistration? _registration;
    private TaskCompletionSource<ReadOnlyMemory<byte>>? _reader;
    private CancellationTokenRegistration _readerCancellation;
    private bool _typedReader;
    private bool _active;
    private bool _stopping;
    private Exception? _terminal;
    private Exception? _removalError;

    private LinuxFidoHidNativeOwner(ILinuxHidReportApi api, ILinuxHidReadReactor reactor, IDisposable? claim)
    {
        _api = api;
        _reactor = reactor;
        _root = GCHandle.Alloc(this); // Root before accepting the claim or acquiring native state.
        _claim = claim;
    }

    internal int InputReportSize { get; private set; }
    internal int OutputReportSize { get; private set; }

    internal static async Task<LinuxFidoHidNativeOwner> OpenAsync(string path, ILinuxHidReportApi api,
        ILinuxHidReadReactor reactor, CancellationToken token, IDisposable? claim = null)
    {
        LinuxFidoHidNativeOwner owner;
        try { owner = new LinuxFidoHidNativeOwner(api, reactor, claim); }
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
            try
            {
                _workCompletion = opened;
                _work = () => OpenOnWorker(path, token, opened);
                _worker = new Thread(WorkLoop) { IsBackground = true, Name = "YubiKit Linux FIDO" };
                // Do not retain the opening caller's ambient context on an idle native worker.
                StartWithoutContext(_worker);
                _ = _signal.Set();
            }
            catch (Exception ex)
            {
                _stopping = true;
                _work = null;
                _workCompletion = null;
                FinishRelease(null);
                _ = opened.TrySetException(ex);
                _signal.Dispose();
            }
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
            _handle = _api.Open(path, NativeMethods.OpenFlags.O_RDWR | NativeMethods.OpenFlags.O_NONBLOCK | NativeMethods.OpenFlags.O_CLOEXEC);
            (InputReportSize, OutputReportSize) = _api.GetReportSizes(_handle);
            RequireSupportedReportSize(InputReportSize);
            RequireSupportedReportSize(OutputReportSize);
            lock (_sync)
            {
                _registration = _reactor.Register(_handle, _generation, OnReady, Terminal);
                if (_stopping || _terminal is not null) RequestRemovalLocked();
            }
            // This is the existing native worker, never the caller or reactor. Store
            // the token first: activation failure is not quiescence/release evidence.
            var activationError = _registration.Activation.GetAwaiter().GetResult();
            token.ThrowIfCancellationRequested();
            if (activationError is not null) throw activationError;
            lock (_sync)
            {
                ThrowIfUnavailable();
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

    private static void RequireSupportedReportSize(int size)
    {
        if (size is < 1 or > 4096) throw new IOException("Unsupported Linux HID report size.");
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
            // This original worker is still available. Drain the actual registration
            // and inspect checked close, rather than abandoning provably releasable state.
            Close();
        }
        finally
        {
            // Every setter is under the same admission lock. Stopping is visible before
            // retirement, so neither a concurrent caller nor a finalizer can Set a
            // disposed event. No replacement worker is started after a loop failure.
            lock (_sync) { _signal.Dispose(); }
        }
    }

    private void StopAfterWorkerFailure(Exception failure)
    {
        lock (_sync)
        {
            _stopping = true;
            _workerFailure ??= failure;
            TerminalLocked(failure); // Completes the actual public reader and requests removal.
            _work = null;
            if (_outputCopy is not null) CryptographicOperations.ZeroMemory(_outputCopy);
            _outputCopy = null;
            _active = false;
            _ = (_workCompletion?.TrySetException(failure));
            _workCompletion = null;
        }
    }

    internal Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // Bound the escaping copy too, not only L1's temporary native framing.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(report.Length, 16383);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            Admit();
            // Escapes to the native worker; never borrow caller storage after publication.
            byte[] copy;
            try { copy = report.ToArray(); }
            catch { _active = false; throw; }
            try
            {
                _workCompletion = completion;
                _outputCopy = copy;
                _work = () => Send(copy, token, completion);
                _ = _signal.Set();
            }
            catch
            {
                CryptographicOperations.ZeroMemory(copy);
                _work = null;
                _workCompletion = null;
                _outputCopy = null;
                _active = false;
                throw;
            }
            return completion.Task;
        }
    }

    private void Send(byte[] copy, CancellationToken token, TaskCompletionSource completion)
    {
        Exception? error = null;
        bool cancelled = token.IsCancellationRequested;
        try { if (!cancelled) _api.Write(Handle, copy); }
        catch (Exception ex) { error = ex; }
        finally { CryptographicOperations.ZeroMemory(copy); }
        lock (_sync)
        {
            _outputCopy = null;
            _workCompletion = null;
            _active = false;
            // The public task itself is terminal before shutdown may pass the worker.
            if (cancelled) _ = completion.TrySetCanceled(token);
            else if (error is not null) _ = completion.TrySetException(error);
            else _ = completion.TrySetResult();
        }
    }

    internal Task<ReadOnlyMemory<byte>> ReceiveAsync(bool typed, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        lock (_sync)
        {
            Admit();
            var reader = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _reader = reader;
            _typedReader = typed;
            if (_reports.TryDequeue(out var report)) CompleteReadLocked(report);
            else
            {
                // May invoke immediately; identity check detaches only this reader. No
                // async wrapper: this TCS task IS the public completion/drain barrier.
                _readerCancellation = token.UnsafeRegister(_ => CancelRead(reader, token), null);
                if (_reader is null) _ = _readerCancellation.Unregister();
            }
            return reader.Task;
        }
    }

    private void CancelRead(TaskCompletionSource<ReadOnlyMemory<byte>> reader, CancellationToken token)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_reader, reader)) return;
            _reader = null;
            _active = false;
            _ = _readerCancellation.Unregister();
            _ = reader.TrySetCanceled(token);
        }
    }

    private void CompleteReadLocked(byte[] report)
    {
        if (_typedReader && report.Length != 64)
        {
            CryptographicOperations.ZeroMemory(report);
            TerminalLocked(new IOException("FIDO input must contain exactly 64 bytes."));
            return;
        }
        var reader = _reader ?? throw new InvalidOperationException("Missing admitted reader.");
        _reader = null;
        _active = false;
        _ = _readerCancellation.Unregister(); // Nonblocking, unlike registration.Dispose under this lock.
        _ = reader.TrySetResult(report);
    }

    private void Admit()
    {
        ThrowIfUnavailable();
        if (_active) throw new InvalidOperationException("Another raw Linux HID operation is active.");
        _active = true;
    }

    private void ThrowIfUnavailable()
    {
        ObjectDisposedException.ThrowIf(_stopping, this);
        if (_terminal is not null) throw new IOException("Linux HID input terminated.", _terminal);
    }

    private LinuxFileSafeHandle Handle => _handle ?? throw new InvalidOperationException("No Linux HID handle.");

    private void OnReady(long generation, short events)
    {
        if (generation != _generation) return;
        _inReadCallback = true;
        byte[]? rental = null;
        Span<byte> staging = InputReportSize <= 512 ? stackalloc byte[InputReportSize] : (rental = ArrayPool<byte>.Shared.Rent(InputReportSize)).AsSpan(0, InputReportSize);
        try { ReadBatch(staging); }
        catch (Exception ex) { Terminal(ex); }
        finally
        {
            CryptographicOperations.ZeroMemory(staging);
            if (rental is not null) ArrayPool<byte>.Shared.Return(rental);
            _inReadCallback = false;
        }
    }

    private void ReadBatch(Span<byte> staging)
    {
        int interrupted = 0;
        for (int i = 0; i < 32; i++)
        {
            lock (_sync) { if (_terminal is not null || _stopping) return; }
            var result = _api.TryRead(Handle, staging);
            if (result.Disposition == LinuxReadDisposition.WouldBlock) return;
            if (result.Disposition == LinuxReadDisposition.Interrupted && ++interrupted < 4) continue;
            if (result.Disposition != LinuxReadDisposition.Data || result.ActualBytes < 1 || result.ActualBytes > staging.Length)
                throw new IOException($"Linux HID read terminated: {result.Disposition}, errno {result.Error}.");
            Publish(staging[..result.ActualBytes]);
        }
    }

    private void Publish(ReadOnlySpan<byte> report)
    {
        lock (_sync)
        {
            if (_stopping || _terminal is not null) return;
            if (_reader is not null) CompleteReadLocked(report.ToArray());
            else if (_reports.Count < ReportCapacity) _reports.Enqueue(report.ToArray());
            else TerminalLocked(new IOException("Linux HID input queue overflow."));
        }
    }

    internal void Terminal(Exception error)
    {
        lock (_sync) { TerminalLocked(error); }
    }

    private void TerminalLocked(Exception error)
    {
        _terminal ??= error;
        while (_reports.TryDequeue(out var report)) CryptographicOperations.ZeroMemory(report);
        // A terminal owner must stop readiness too; leaving unread data registered would
        // turn level-triggered poll into a hot loop. Removal is not release proof.
        RequestRemovalLocked();
        if (_reader is null) return;
        var reader = _reader;
        _reader = null;
        _active = false;
        _ = _readerCancellation.Unregister();
        _ = reader.TrySetException(_terminal);
    }

    internal void Dispose()
    {
        if (_inReadCallback || LinuxHidReadReactor.IsReactorThread || Environment.CurrentManagedThreadId == Volatile.Read(ref _workerId))
            throw new InvalidOperationException("Cannot synchronously drain a Linux HID owner from its worker/read callback.");
        ShutdownAsync().GetAwaiter().GetResult();
    }

    // Finalizers only publish shutdown; they never wait for the worker/reactor/native call.
    internal void RequestShutdown()
    {
        try { _ = ShutdownAsync(); }
        catch { /* Finalizer backstop: never throw or manufacture native release proof. */ }
    }

    internal Task ShutdownAsync()
    {
        lock (_sync)
        {
            if (_stopping) return _closed.Task;
            _stopping = true;
            TerminalLocked(new ObjectDisposedException(nameof(LinuxFidoHidNativeOwner)));
            RequestRemovalLocked(); // Prompt even when open/write occupies the worker.
            _ = _signal.Set();
            return _closed.Task;
        }
    }

    private void RequestRemovalLocked()
    {
        if (_registration is null) return;
        try { _removalError ??= _registration.RequestRemoval(); }
        catch (Exception ex) { _removalError ??= ex; }
    }

    private void Close()
    {
        try
        {
            if (_registration is not null)
            {
                _registration.Quiescence.GetAwaiter().GetResult(); // Only actual drain permits reference release.
                _registration.ReleaseReference(); // On this original worker, never on a reactor.
            }
            Exception? diagnostic = Combine(_workerFailure, _removalError);
            if (_handle is not null)
            {
                var outcome = _api.CloseOnce(_handle);
                if (!outcome.Released) throw new IOException($"Linux HID release unproven: executed={outcome.Executed}, errno={outcome.Error}.");
                if (outcome.Error != 0)
                {
                    var closeError = new PlatformApiException("close", outcome.Error, "Linux HID descriptor released with a close diagnostic.");
                    diagnostic = diagnostic is null ? closeError : new AggregateException(diagnostic, closeError);
                }
            }
            FinishRelease(diagnostic);
        }
        catch (Exception ex) { FailUnreleased(Combine(Combine(_workerFailure, _removalError), ex) ?? ex); }
    }

    private static Exception? Combine(Exception? first, Exception? second) =>
        first is null ? second : second is null ? first : new AggregateException(first, second);

    private void FinishRelease(Exception? diagnostic)
    {
        try { _claim?.Dispose(); }
        catch (Exception ex) { FailUnreleased(diagnostic is null ? ex : new AggregateException(diagnostic, ex)); return; }
        _root.Free();
        if (diagnostic is null) _ = _closed.TrySetResult();
        else _ = _closed.TrySetException(diagnostic);
    }

    private void FailUnreleased(Exception cause)
    {
        var failure = new UnrecoveredConnectionException("Linux FIDO lifetime release was not proven; original owner and claim retained.", cause);
        try { if (_claim is not null) DeviceConnectionRegistry.TryMarkUnrecovered(_claim, failure); }
        catch (Exception ex) { failure = new UnrecoveredConnectionException(failure.Message, new AggregateException(cause, ex)); }
        _ = _closed.TrySetException(failure);
    }
}