// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal sealed class MacOSOtpHidConnection : IOtpHidConnection
{
    private readonly Owner _owner;
    private MacOSOtpHidConnection(Owner owner) => _owner = owner;
    public ConnectionType Type => ConnectionType.HidOtp;
    public int FeatureReportSize => 8;

    internal static async Task<MacOSOtpHidConnection> OpenAsync(long entryId, IIOKitDeviceLifetime lifetime,
        CancellationToken cancellationToken, IDisposable? registration = null)
    {
        Owner owner;
        try { owner = new Owner(entryId, lifetime, registration); }
        catch { registration?.Dispose(); throw; }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await owner.OpenAsync(cancellationToken).ConfigureAwait(false);
            return new MacOSOtpHidConnection(owner);
        }
        catch (Exception openFailure)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (UnrecoveredConnectionException cleanupFailure)
            {
                throw new UnrecoveredConnectionException("macOS OTP open and native cleanup failed",
                    new AggregateException(openFailure, cleanupFailure));
            }
            catch (Exception cleanupFailure) { throw new AggregateException(openFailure, cleanupFailure); }
            throw;
        }
    }

    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
        _owner.SendAsync(report, cancellationToken);
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(cancellationToken);
    public void Dispose()
    {
        if (_owner.IsWorkerThread)
            throw new InvalidOperationException("Cannot drain the OTP native worker from itself.");
        _owner.ShutdownAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
    public async ValueTask DisposeAsync()
    {
        if (_owner.IsWorkerThread)
            throw new InvalidOperationException("Cannot drain the OTP native worker from itself.");
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    ~MacOSOtpHidConnection() => _ = _owner.ShutdownAsync();

    private sealed class Owner
    {
        private readonly long _entryId;
        private readonly IIOKitDeviceLifetime _lifetime;
        private readonly IDisposable? _registration;
        private readonly Lock _sync = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private GCHandle _root;
        private Thread? _worker;
        private Action? _work;
        private Action<Exception>? _pendingFailure;
        private nint _device;
        private bool _opened;
        private bool _active;
        private bool _stopping;
        private int _workerId;

        internal Owner(long entryId, IIOKitDeviceLifetime lifetime, IDisposable? registration)
        {
            _entryId = entryId;
            _lifetime = lifetime;
            _registration = registration;
            _root = GCHandle.Alloc(this);
        }

        internal bool IsWorkerThread => Environment.CurrentManagedThreadId == Volatile.Read(ref _workerId);

        private void ScheduleLocked(Action action, Action<Exception>? onFailure = null)
        {
            _work = action;
            _pendingFailure = onFailure;
            if (_worker is null)
            {
                _worker = new Thread(WorkLoop) { IsBackground = true, Name = "YubiKit macOS OTP" };
                try { _worker.Start(); }
                catch (Exception startFailure)
                {
                    _worker = null;
                    _work = null;
                    _pendingFailure = null;
                    _stopping = true;
                    Exception? releaseFailure = null;
                    try { _registration?.Dispose(); }
                    catch (Exception ex) { releaseFailure = ex; }
                    _root.Free();
                    _signal.Dispose();
                    var failure = releaseFailure is null ? startFailure : new AggregateException(startFailure, releaseFailure);
                    _closed.TrySetException(failure);
                    throw failure;
                }
            }
            _signal.Set();
        }

        private void WorkLoop()
        {
            Volatile.Write(ref _workerId, Environment.CurrentManagedThreadId);
            Action<Exception>? runningFailure = null;
            try
            {
                while (true)
                {
                    _signal.WaitOne();
                    Action? action;
                    lock (_sync)
                    {
                        action = _work;
                        runningFailure = _pendingFailure;
                        _work = null;
                        _pendingFailure = null;
                    }
                    action?.Invoke();
                    runningFailure = null;
                    lock (_sync)
                    {
                        if (!_stopping || _work is not null) continue;
                    }
                    Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                Action<Exception>? queuedFailure;
                lock (_sync)
                {
                    _stopping = true;
                    queuedFailure = _pendingFailure;
                    _pendingFailure = null;
                    _work = null;
                }
                try { runningFailure?.Invoke(ex); }
                catch (Exception completionFailure) { ex = new AggregateException(ex, completionFailure); }
                try { queuedFailure?.Invoke(ex); }
                catch (Exception completionFailure) { ex = new AggregateException(ex, completionFailure); }
                if (_device != 0) FailUnreleased(ex);
                else
                {
                    try { _registration?.Dispose(); }
                    catch (Exception releaseFailure) { ex = new AggregateException(ex, releaseFailure); }
                    _root.Free();
                    _closed.TrySetException(ex);
                }
            }
            finally { _signal.Dispose(); }
        }

        internal Task OpenAsync(CancellationToken token)
        {
            var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_sync)
            {
                try { ScheduleLocked(() =>
                {
                    if (token.IsCancellationRequested) { result.TrySetCanceled(token); return; }
                    try
                    {
                        _device = _lifetime.CreateDevice(_entryId);
                        if (_device == 0) throw new InvalidOperationException("IOHIDDeviceCreate returned null.");
                        var status = _lifetime.OpenDeviceResult(_device);
                        // Apple's IOHIDDeviceClass marks both success and exclusive-access as opened.
                        _opened = status == 0 || status == unchecked((int)0xE00002C5);
                        if (status != 0)
                            throw new PlatformApiException("IOHIDDeviceOpen", status, "Failed to open OTP HID device.");
                        result.TrySetResult();
                    }
                    catch (Exception ex) { result.TrySetException(ex); }
                }, ex => result.TrySetException(ex)); }
                catch (Exception ex) { result.TrySetException(ex); }
            }
            return result.Task;
        }

        internal Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (report.Length != 8) throw new ArgumentException("OTP feature report must be exactly 8 bytes.", nameof(report));
            var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_sync)
            {
                Admit();
                byte[] copy = report.ToArray(); // The caller can mutate/zero its buffer after dispatch.
                _active = true;
                try
                {
                    ScheduleLocked(() =>
                    {
                        Exception? error = null;
                        bool cancelled = token.IsCancellationRequested;
                        try
                        {
                            if (!cancelled)
                            {
                                var status = _lifetime.SetFeatureReport(_device, copy);
                                if (status != 0) throw new PlatformApiException("IOHIDDeviceSetReport", status, "Failed to set OTP feature report.");
                            }
                        }
                        catch (Exception ex) { error = ex; }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(copy);
                            lock (_sync) _active = false;
                        }
                        if (cancelled) result.TrySetCanceled(token);
                        else if (error is not null) result.TrySetException(error);
                        else result.TrySetResult();
                    }, ex =>
                    {
                        CryptographicOperations.ZeroMemory(copy);
                        result.TrySetException(ex);
                    });
                }
                catch
                {
                    CryptographicOperations.ZeroMemory(copy);
                    _active = false;
                    throw;
                }
            }
            return result.Task;
        }

        internal Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var result = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
            byte[]? report = null;
            lock (_sync)
            {
                Admit();
                _active = true;
                try { ScheduleLocked(() =>
                {
                    Exception? error = null;
                    bool cancelled = token.IsCancellationRequested;
                    try
                    {
                        if (!cancelled)
                        {
                            report = new byte[8];
                            long length = report.Length;
                            var status = _lifetime.GetFeatureReport(_device, report, ref length);
                            if (status != 0) throw new PlatformApiException("IOHIDDeviceGetReport", status, "Failed to get OTP feature report.");
                            if (length != 8) throw new InvalidOperationException($"Expected 8-byte OTP feature report, got {length} bytes.");
                        }
                    }
                    catch (Exception ex) { error = ex; }
                    finally
                    {
                        if (error is not null && report is not null)
                            CryptographicOperations.ZeroMemory(report);
                        lock (_sync) _active = false;
                    }
                    if (cancelled) result.TrySetCanceled(token);
                    else if (error is not null) result.TrySetException(error);
                    else if (report is null) result.TrySetException(new InvalidOperationException("Native GET returned no report."));
                    else result.TrySetResult(report);
                }, ex =>
                {
                    if (report is not null) CryptographicOperations.ZeroMemory(report);
                    result.TrySetException(ex);
                }); }
                catch
                {
                    _active = false;
                    throw;
                }
            }
            return result.Task;
        }

        private void Admit()
        {
            if (_stopping) throw new ObjectDisposedException(nameof(MacOSOtpHidConnection));
            if (_active) throw new InvalidOperationException("Another raw OTP operation is active.");
        }

        internal Task ShutdownAsync()
        {
            lock (_sync)
            {
                if (!_stopping)
                {
                    _stopping = true;
                    if (_worker is null) ScheduleLocked(static () => { });
                    else _signal.Set();
                }
                return _closed.Task;
            }
        }

        private void Close()
        {
            try
            {
                if (_device != 0)
                {
                    if (_opened && !_lifetime.CloseDeviceChecked(_device))
                        throw new InvalidOperationException("OTP native close was not proven.");
                    _lifetime.ReleaseCFObject(_device);
                    _device = 0;
                }
                Exception? releaseError = null;
                try { _registration?.Dispose(); }
                catch (Exception ex) { releaseError = ex; }
                _root.Free();
                if (releaseError is null) _closed.TrySetResult();
                else _closed.TrySetException(releaseError);
            }
            catch (Exception ex) { FailUnreleased(ex); }
        }

        private void FailUnreleased(Exception error)
        {
            var failure = new UnrecoveredConnectionException("macOS OTP native release was not proven", error);
            try
            {
                if (_registration is not null) DeviceConnectionRegistry.MarkUnrecovered(_registration, failure);
            }
            catch (Exception observerError)
            {
                failure = new UnrecoveredConnectionException("macOS OTP release observer failed",
                    new AggregateException(error, observerError));
            }
            _closed.TrySetException(failure); // Keep the native owner and claim rooted; never retry uncertain close.
        }
    }
}
