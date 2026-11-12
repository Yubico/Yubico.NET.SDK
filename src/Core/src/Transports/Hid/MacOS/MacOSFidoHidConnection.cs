// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using HidInputResult = Yubico.YubiKit.Core.Native.MacOS.HidInput.HidInputResult;
using HidInputTerminalReason = Yubico.YubiKit.Core.Native.MacOS.HidInput.HidInputTerminalReason;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal sealed class MacOSFidoHidConnection : IFidoHidConnection, ITerminalWakeControl
{
    private const int FidoPacketSize = 64;
    internal const int MaxInputReportLength = FidoPacketSize + 1; // Optional report ID.
    internal const int ReportCapacity = 160;
    private const int ShutdownTerminalReason = 0; // Managed wake; not a native terminal reason.

    private readonly Owner _owner;
    private MacOSFidoHidConnection(Owner owner) => _owner = owner;
    public int PacketSize => FidoPacketSize;
    internal int InputReportSize => _owner.InputReportSize;
    internal int OutputReportSize => _owner.OutputReportSize;
    public ConnectionType Type => ConnectionType.HidFido;

    internal static async Task<MacOSFidoHidConnection> OpenAsync(long entryId, IMacOSHidInputBridge inputBridge,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null, Action? beforeSendPublication = null)
    {
        Owner owner;
        try
        {
            owner = new Owner(entryId, inputBridge, physicalClaim, beforeSendPublication);
        }
        catch
        {
            physicalClaim?.Dispose();
            throw;
        }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await owner.OpenAsync(cancellationToken).ConfigureAwait(false);
            return new MacOSFidoHidConnection(owner);
        }
        catch
        {
            await owner.ShutdownAsync().ConfigureAwait(false);
            throw;
        }
    }

    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (packet.Length != FidoPacketSize)
        {
            throw new ArgumentException("FIDO packet must be exactly 64 bytes", nameof(packet));
        }
        return _owner.SendAsync(packet, cancellationToken);
    }
    internal Task SendReportAsync(ReadOnlyMemory<byte> report) => _owner.SendAsync(report, CancellationToken.None);
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(cancellationToken);
    public void RequestTerminalWake() => _owner.Terminal(ShutdownTerminalReason);
    public void Dispose()
    {
        if (_owner.IsWorkerThread)
        {
            throw new InvalidOperationException("Cannot drain the native worker from itself.");
        }
        _owner.ShutdownAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    ~MacOSFidoHidConnection() => _ = _owner.ShutdownAsync();

    private sealed class Owner
    {
        private readonly long _entryId;
        private readonly IMacOSHidInputBridge _inputBridge;
        private readonly IDisposable? _physicalClaim;
        private readonly Action? _beforeSendPublication;
        private readonly Lock _sync = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly Queue<byte[]> _reports = new();
        private readonly HashSet<Task> _publicReads = [];
        private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _shutdown;
        private GCHandle _root;
        private Thread? _worker;
        private Action? _work;
        private TaskCompletionSource? _ordinaryCompletion;
        private TaskCompletionSource<ReadOnlyMemory<byte>>? _reader;
        private nint _device;
        private nint _input;
        private bool _openAttempted;
        private bool _started;
        private bool _active;
        private bool _stopping;
        private bool _terminal;
        private int _workerId;
        internal int InputReportSize { get; private set; }
        internal int OutputReportSize { get; private set; }

        internal Owner(long entryId, IMacOSHidInputBridge inputBridge, IDisposable? physicalClaim, Action? beforeSendPublication)
        {
            _entryId = entryId;
            _inputBridge = inputBridge;
            _physicalClaim = physicalClaim;
            _beforeSendPublication = beforeSendPublication;
            _root = GCHandle.Alloc(this);
        }
        internal bool IsWorkerThread => Environment.CurrentManagedThreadId == Volatile.Read(ref _workerId);

        private void Schedule(Action action)
        {
            lock (_sync)
            {
                ScheduleLocked(action);
            }
        }

        private void ScheduleLocked(Action action)
        {
            _work = action;
            if (_worker is null)
            {
                _worker = new Thread(WorkLoop) { IsBackground = true, Name = "YubiKit macOS FIDO" };
                try { _worker.Start(); }
                catch (Exception startFailure)
                {
                    _worker = null;
                    _work = null;
                    _stopping = true;
                    Exception? releaseFailure = null;
                    try { _physicalClaim?.Dispose(); }
                    catch (Exception ex) { releaseFailure = ex; }
                    _root.Free();
                    _signal.Dispose();
                    _closed.TrySetResult();
                    if (releaseFailure is not null)
                    {
                        throw new AggregateException(startFailure, releaseFailure);
                    }
                    throw;
                }
            }
            _signal.Set();
        }

        private void WorkLoop()
        {
            Volatile.Write(ref _workerId, Environment.CurrentManagedThreadId);
            try
            {
                while (true)
                {
                    _signal.WaitOne();
                    Action? action;
                    lock (_sync)
                    {
                        action = _work;
                        _work = null;
                    }
                    action?.Invoke();
                    bool close;
                    lock (_sync)
                    {
                        close = _stopping && _work is null;
                    }
                    if (close)
                    {
                        Close();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                lock (_sync)
                {
                    _stopping = true;
                    TerminalLocked(HidInputTerminalReason.Fault);
                }
                _ordinaryCompletion?.TrySetException(ex);
                if (_device == 0)
                {
                    try { _physicalClaim?.Dispose(); }
                    catch (Exception releaseFailure) { ex = new AggregateException(ex, releaseFailure); }
                    _root.Free();
                    _closed.TrySetResult();
                }
                else
                {
                    FailUnreleased("macOS FIDO worker failed before release proof", ex);
                }
            }
            finally { _signal.Dispose(); }
        }

        internal Task OpenAsync(CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _ordinaryCompletion = completion;
            Schedule(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }
                try
                {
                    _device = _inputBridge.CreateDevice(_entryId);
                    if (_device == 0)
                    {
                        throw new InvalidOperationException("IOHIDDeviceCreate returned null");
                    }
                    var openStatus = _inputBridge.OpenDeviceResult(_device);
                    // IOHIDDeviceClass marks exclusive access as opened even though the call fails.
                    _openAttempted = openStatus is 0 or unchecked((int)0xE00002C5);
                    if (openStatus != 0)
                        throw new InvalidOperationException($"IOHIDDeviceOpen failed: {openStatus:X8}");
                    var size = _inputBridge.InputSize(_device);
                    if (size < FidoPacketSize)
                    {
                        throw new InvalidOperationException("FIDO input report size is smaller than 64");
                    }
                    InputReportSize = size;
                    OutputReportSize = _inputBridge.OutputSize(_device);
                    _input = _inputBridge.CreateInput(_device, size, Report, Terminal);
                    if (_input == 0)
                    {
                        throw new InvalidOperationException("Native_HidInputCreate returned null");
                    }
                    var status = _inputBridge.Start(_input);
                    if (status != HidInputResult.Ok)
                    {
                        throw new InvalidOperationException($"Native_HidInputStart failed: {status}");
                    }
                    _started = true;
                    completion.TrySetResult();
                }
                catch (Exception ex) { completion.TrySetException(ex); }
            });
            return completion.Task;
        }

        private void Report(ReadOnlyMemory<byte> report)
        {
            lock (_sync)
            {
                if (_terminal || _stopping)
                {
                    return;
                }
                var packet = report.Span;
                if (packet.Length == MaxInputReportLength && packet[0] == 0)
                {
                    packet = packet[1..];
                }
                if (packet.Length != FidoPacketSize)
                {
                    TerminalLocked(HidInputTerminalReason.Fault);
                    return;
                }
                if (_reader is { } reader)
                {
                    var copy = packet.ToArray();
                    _reader = null;
                    _active = false;
                    reader.TrySetResult(copy);
                }
                else if (_reports.Count < ReportCapacity)
                {
                    _reports.Enqueue(packet.ToArray());
                }
                else
                {
                    TerminalLocked(HidInputTerminalReason.Overflow);
                }
            }
        }
        internal void Terminal(int reason)
        {
            lock (_sync)
            {
                TerminalLocked(reason);
            }
        }
        private void TerminalLocked(int reason)
        {
            _terminal = true;
            while (_reports.TryDequeue(out var report))
            {
                CryptographicOperations.ZeroMemory(report);
            }
            _reader?.TrySetException(new InvalidOperationException($"FIDO input terminated: {reason}"));
            _reader = null;
        }

        private void CancelRead(TaskCompletionSource<ReadOnlyMemory<byte>> reader, CancellationToken cancellationToken)
        {
            bool detached;
            lock (_sync)
            {
                detached = ReferenceEquals(_reader, reader);
                if (detached)
                {
                    _reader = null;
                    _active = false;
                }
            }
            if (detached)
            {
                reader.TrySetCanceled(cancellationToken);
            }
        }

        private static async Task<ReadOnlyMemory<byte>> AwaitReadAsync(
            Task<ReadOnlyMemory<byte>> task, CancellationTokenRegistration registration)
        {
            try { return await task.ConfigureAwait(false); }
            finally { registration.Dispose(); }
        }

        internal Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TaskCompletionSource<ReadOnlyMemory<byte>> reader;
            CancellationTokenRegistration registration;
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_sync)
            {
                if (_terminal || _stopping)
                {
                    throw new ObjectDisposedException(nameof(MacOSFidoHidConnection));
                }
                if (_active)
                {
                    throw new InvalidOperationException("Another raw FIDO operation is active");
                }
                if (_reports.TryDequeue(out var report))
                {
                    return Task.FromResult<ReadOnlyMemory<byte>>(report);
                }
                _active = true;
                reader = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
                _reader = reader;
                // Register may invoke synchronously for an already-cancelled token.
                // The lock is reentrant, and the callback only detaches this reader.
                registration = cancellationToken.Register(static state =>
                {
                    var (owner, pending, cancellation) =
                        ((Owner, TaskCompletionSource<ReadOnlyMemory<byte>>, CancellationToken))state!;
                    owner.CancelRead(pending, cancellation);
                }, (this, reader, cancellationToken));
                _publicReads.Add(finished.Task);
            }
            // Registration disposal may wait for a callback acquiring _sync.
            var publicRead = AwaitReadAsync(reader.Task, registration);
            _ = publicRead.ContinueWith(_ =>
            {
                lock (_sync)
                {
                    _publicReads.Remove(finished.Task);
                    finished.TrySetResult();
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return publicRead;
        }

        internal Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_sync)
            {
                if (_terminal || _stopping)
                {
                    throw new ObjectDisposedException(nameof(MacOSFidoHidConnection));
                }
                if (_active)
                {
                    throw new InvalidOperationException("Another raw FIDO operation is active");
                }
                byte[] copy = packet.ToArray();
                try { _beforeSendPublication?.Invoke(); }
                catch
                {
                    CryptographicOperations.ZeroMemory(copy);
                    throw;
                }
                _active = true;
                _ordinaryCompletion = completion;
                ScheduleLocked(() =>
                {
                    Exception? failure = null;
                    bool cancelled = cancellationToken.IsCancellationRequested;
                    try
                    {
                        if (!cancelled)
                        {
                            _inputBridge.SetReport(_device, copy);
                        }
                    }
                    catch (Exception ex) { failure = ex; }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(copy);
                        lock (_sync)
                        {
                            _active = false;
                        }
                    }
                    if (cancelled) completion.TrySetCanceled(cancellationToken);
                    else if (failure is not null) completion.TrySetException(failure);
                    else completion.TrySetResult();
                });
            }
            return completion.Task;
        }

        internal Task ShutdownAsync()
        {
            lock (_sync)
            {
                if (_stopping)
                {
                    return _shutdown ?? _closed.Task;
                }
                _stopping = true;
                TerminalLocked(ShutdownTerminalReason);
                _shutdown = DrainShutdownAsync(_publicReads.ToArray());
                // The worker processes any accepted output before teardown; no ordinary backlog.
                _signal.Set();
                if (_worker is null) Schedule(static () => { });
            }
            return _shutdown;
        }

        private async Task DrainShutdownAsync(Task[] reads)
        {
            await Task.WhenAll(reads).ConfigureAwait(false);
            await _closed.Task.ConfigureAwait(false);
        }

        private void Close()
        {
            try
            {
                ReleaseInput();
                ReleaseDevice();
                CompleteClose();
            }
            catch (Exception ex)
            {
                FailUnreleased("macOS FIDO native release was not proven", ex);
            }
        }

        private void ReleaseInput()
        {
            if (_input == 0)
            {
                return;
            }
            if (_started)
            {
                AcknowledgeInputShutdown();
            }
            var result = _inputBridge.Destroy(_input);
            if (result != HidInputResult.Ok)
            {
                throw new InvalidOperationException($"FIDO input destroy failed: {result}");
            }
        }

        private void AcknowledgeInputShutdown()
        {
            _inputBridge.Cancel(_input);
            var ack = _inputBridge.WaitShutdown(_input);
            // FAULT reports a terminal input error but still proves native quiescence.
            if (ack is not (HidInputResult.Ok or HidInputResult.Fault))
            {
                throw new InvalidOperationException($"FIDO input cancel acknowledgment failed: {ack}");
            }
        }

        private void ReleaseDevice()
        {
            if (_openAttempted && !_started && !_inputBridge.CloseUnstarted(_device))
            {
                throw new InvalidOperationException("FIDO partial open close failed");
            }
            if (_device != 0)
            {
                _inputBridge.ReleaseDevice(_device);
            }
            _device = 0;
        }

        private void CompleteClose()
        {
            Exception? physicalClaimFailure = null;
            try { _physicalClaim?.Dispose(); }
            catch (Exception ex) { physicalClaimFailure = ex; }
            _root.Free();
            if (physicalClaimFailure is null) _closed.TrySetResult();
            else _closed.TrySetException(physicalClaimFailure);
        }

        private void FailUnreleased(string message, Exception cause)
        {
            // No release proof: retain the native owner, callback roots, and physical claim.
            var failure = new UnrecoveredConnectionException(message, cause);
            try
            {
                if (_physicalClaim is not null)
                {
                    DeviceConnectionRegistry.MarkUnrecovered(_physicalClaim, failure);
                }
            }
            catch (Exception observerFailure)
            {
                failure = new UnrecoveredConnectionException(message, new AggregateException(cause, observerFailure));
            }
            _closed.TrySetException(failure);
        }
    }
}