// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.MacOS.IOKitFramework;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using HidInputInterop = Yubico.YubiKit.Core.Native.MacOS.HidInput.NativeMethods;
using HidInputResult = Yubico.YubiKit.Core.Native.MacOS.HidInput.HidInputResult;
using HidInputTerminalReason = Yubico.YubiKit.Core.Native.MacOS.HidInput.HidInputTerminalReason;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal interface IHidInputBridge
{
    nint CreateDevice(long entryId);
    void OpenDevice(nint device);
    int OpenDeviceResult(nint device)
    {
        OpenDevice(device);
        return 0;
    }
    int InputSize(nint device);
    bool CloseUnstarted(nint device);
    void ReleaseDevice(nint device);
    nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal);
    int Start(nint owner);
    void Cancel(nint owner);
    int WaitShutdown(nint owner);
    int Destroy(nint owner);
    void SetReport(nint device, byte[] report);
}

internal sealed unsafe class NativeHidInputBridge : IHidInputBridge
{
    private GCHandle _context;
    private nint _owner;

    // Native callbacks borrow the context until destroy succeeds.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnNativeReport(nint ctx, nint data, nuint length)
    {
        try
        {
            ((CallbackState)GCHandle.FromIntPtr(ctx).Target!).Report(data, length);
        }
        catch { /* Never throw through the C callback ABI. */ }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnNativeTerminal(nint ctx, int reason)
    {
        try
        {
            ((CallbackState)GCHandle.FromIntPtr(ctx).Target!).Terminal(reason);
        }
        catch { /* Never throw through the C callback ABI. */ }
    }

    internal sealed class CallbackState(Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
    {
        internal void Report(nint data, nuint length)
        {
            try
            {
                if (length is 0 or > MacOSFidoHidConnection.MaxInputReportLength)
                {
                    Terminal(HidInputTerminalReason.Fault);
                    return;
                }
                var copy = new byte[(int)length];
                try
                {
                    Marshal.Copy(data, copy, 0, copy.Length);
                    Deliver(copy);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(copy);
                }
            }
            catch { Terminal(HidInputTerminalReason.Fault); }
        }

        internal void Deliver(ReadOnlyMemory<byte> packet)
        {
            try { report(packet); }
            catch { Terminal(HidInputTerminalReason.Fault); }
        }

        internal void Terminal(int reason)
        {
            try { terminal(reason); }
            catch { /* Callbacks cannot escape into C, even if terminal delivery faults. */ }
        }
    }

    public nint CreateDevice(long entryId) => IOKitDeviceLifetime.Instance.CreateDevice(entryId);
    public void OpenDevice(nint device) => IOKitDeviceLifetime.Instance.OpenDevice(device);
    public int OpenDeviceResult(nint device) => IOKitDeviceLifetime.Instance.OpenDeviceResult(device);
    public int InputSize(nint device) => IOKitDeviceLifetime.Instance.GetIntProperty(device, IOKitHidConstants.MaxInputReportSize);
    public bool CloseUnstarted(nint device) => NativeMethods.IOHIDDeviceClose(device, 0) == 0;
    public void ReleaseDevice(nint device) => IOKitDeviceLifetime.Instance.ReleaseCFObject(device);
    public void SetReport(nint device, byte[] report)
    {
        var status = NativeMethods.IOHIDDeviceSetReport(device, IOKitHidConstants.kIOHidReportTypeOutput, 0, report, report.Length);
        if (status != 0)
        {
            throw new InvalidOperationException($"IOHIDDeviceSetReport failed: {status}");
        }
    }

    public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
    {
        if (_owner != 0 || _context.IsAllocated)
        {
            throw new InvalidOperationException("This native input bridge already owns a device.");
        }
        _context = GCHandle.Alloc(new CallbackState(report, terminal));
        try
        {
            // Native and managed delivery each have a separately bounded queue.
            _owner = HidInputInterop.HidInputCreate(device, (nuint)size, MacOSFidoHidConnection.ReportCapacity,
                &OnNativeReport, &OnNativeTerminal,
                GCHandle.ToIntPtr(_context));
        }
        catch
        {
            _context.Free();
            throw;
        }
        if (_owner != 0)
        {
            return _owner;
        }
        _context.Free();
        throw new InvalidOperationException("Native_HidInputCreate failed");
    }
    public int Start(nint owner) => HidInputInterop.HidInputStart(owner);
    public void Cancel(nint owner) => HidInputInterop.HidInputCancel(owner);
    public int WaitShutdown(nint owner) => HidInputInterop.HidInputWaitShutdown(owner, uint.MaxValue);
    public int Destroy(nint owner)
    {
        var result = HidInputInterop.HidInputDestroy(owner);
        if (result == HidInputResult.Ok)
        {
            _context.Free();
            _owner = 0;
        }
        return result;
    }
}

internal sealed class MacOSFidoHidConnection : IFidoHidConnection, ITerminalWakeControl
{
    private const int FidoPacketSize = 64;
    internal const int MaxInputReportLength = FidoPacketSize + 1; // Optional report ID.
    internal const int ReportCapacity = 160;
    private const int ShutdownTerminalReason = 0; // Managed wake; not a native terminal reason.

    private readonly Owner _owner;
    private MacOSFidoHidConnection(Owner owner) => _owner = owner;
    public int PacketSize => FidoPacketSize;
    public ConnectionType Type => ConnectionType.HidFido;

    internal static async Task<MacOSFidoHidConnection> OpenAsync(long entryId, IHidInputBridge bridge,
        CancellationToken cancellationToken, IDisposable? registration = null, Action? beforeSendPublication = null)
    {
        Owner owner;
        try
        {
            owner = new Owner(entryId, bridge, registration, beforeSendPublication);
        }
        catch
        {
            registration?.Dispose();
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

    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
        _owner.SendAsync(packet, cancellationToken);
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
        private readonly IHidInputBridge _bridge;
        private readonly IDisposable? _registration;
        private readonly Action? _beforeSendPublication;
        private readonly Lock _sync = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly Queue<byte[]> _reports = new();
        private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
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

        internal Owner(long entryId, IHidInputBridge bridge, IDisposable? registration, Action? beforeSendPublication)
        {
            _entryId = entryId;
            _bridge = bridge;
            _registration = registration;
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
                    try { _registration?.Dispose(); }
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
                    try { _registration?.Dispose(); }
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

        internal Task OpenAsync(CancellationToken token)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _ordinaryCompletion = completion;
            Schedule(() =>
            {
                if (token.IsCancellationRequested)
                {
                    completion.TrySetCanceled(token);
                    return;
                }
                try
                {
                    _device = _bridge.CreateDevice(_entryId);
                    if (_device == 0)
                    {
                        throw new InvalidOperationException("IOHIDDeviceCreate returned null");
                    }
                    var openStatus = _bridge.OpenDeviceResult(_device);
                    // IOHIDDeviceClass marks exclusive access as opened even though the call fails.
                    _openAttempted = openStatus is 0 or unchecked((int)0xE00002C5);
                    if (openStatus != 0)
                        throw new InvalidOperationException($"IOHIDDeviceOpen failed: {openStatus:X8}");
                    var size = _bridge.InputSize(_device);
                    if (size < FidoPacketSize)
                    {
                        throw new InvalidOperationException("FIDO input report size is smaller than 64");
                    }
                    _input = _bridge.CreateInput(_device, size, Report, Terminal);
                    if (_input == 0)
                    {
                        throw new InvalidOperationException("Native_HidInputCreate returned null");
                    }
                    var status = _bridge.Start(_input);
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

        internal Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
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
                _reader = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
                return _reader.Task;
            }
        }

        internal Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (packet.Length != FidoPacketSize)
            {
                throw new ArgumentException("FIDO packet must be exactly 64 bytes", nameof(packet));
            }
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
                    bool cancelled = token.IsCancellationRequested;
                    try
                    {
                        if (!cancelled)
                        {
                            _bridge.SetReport(_device, copy);
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
                    if (cancelled) completion.TrySetCanceled(token);
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
                    return _closed.Task;
                }
                _stopping = true;
                TerminalLocked(ShutdownTerminalReason);
                // The worker processes any accepted output before teardown; no ordinary backlog.
                _signal.Set();
                if (_worker is null) Schedule(static () => { });
            }
            return _closed.Task;
        }

        private void Close()
        {
            try
            {
                if (_input != 0)
                {
                    if (_started)
                    {
                        _bridge.Cancel(_input);
                        var ack = _bridge.WaitShutdown(_input);
                        // FAULT reports a terminal input error but still proves native quiescence.
                        if (ack is not (HidInputResult.Ok or HidInputResult.Fault))
                        {
                            throw new InvalidOperationException($"FIDO input cancel acknowledgment failed: {ack}");
                        }
                    }
                    var result = _bridge.Destroy(_input);
                    if (result != HidInputResult.Ok)
                    {
                        throw new InvalidOperationException($"FIDO input destroy failed: {result}");
                    }
                }
                if (_openAttempted && !_started && !_bridge.CloseUnstarted(_device))
                {
                    throw new InvalidOperationException("FIDO partial open close failed");
                }
                if (_device != 0)
                {
                    _bridge.ReleaseDevice(_device);
                }
                _device = 0;
                Exception? registrationFailure = null;
                try { _registration?.Dispose(); }
                catch (Exception ex) { registrationFailure = ex; }
                _root.Free();
                if (registrationFailure is null) _closed.TrySetResult();
                else _closed.TrySetException(registrationFailure);
            }
            catch (Exception ex)
            {
                FailUnreleased("macOS FIDO native release was not proven", ex);
            }
        }

        private void FailUnreleased(string message, Exception cause)
        {
            // No release proof: retain the native owner, callback roots, and physical claim.
            var failure = new UnrecoveredConnectionException(message, cause);
            try
            {
                if (_registration is not null)
                {
                    DeviceConnectionRegistry.MarkUnrecovered(_registration, failure);
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
