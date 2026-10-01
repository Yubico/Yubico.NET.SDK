using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal static class ListenerDrainScenario
{
    internal static async Task<int> RunAsync()
    {
        var native = new RecordingListenerNative();
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromSeconds(4));
        try
        {
            Console.WriteLine("READY_LISTENER: wait up to 5 seconds for real matching callback; no device operation requested");
            listener.Start();
            if (listener.Status != DeviceListenerStatus.Started)
                throw new InvalidOperationException($"Listener did not start: {listener.Status}");
            if (!native.Entered.Wait(TimeSpan.FromSeconds(5)))
            {
                Console.Error.WriteLine("BLOCKED: no IOHIDManager matching callback arrived; coordinate topology change with operator");
                return 2;
            }
            Console.WriteLine($"PHASE_NATIVE_MATCHING_ENTERED entryId={native.EntryId}");
            Task<DrainSnapshot> stopping = Task.Run(() =>
            {
                listener.Stop();
                return native.Snapshot(listener.Status);
            });
            if (!native.StopRequested.Wait(TimeSpan.FromSeconds(3)))
                throw new InvalidOperationException("Stop never requested native run-loop stop during held callback");
            if (stopping.IsCompleted || native.Closed != 0 || native.Unscheduled != 0 || native.ManagerReleased != 0)
                throw new InvalidOperationException("Listener cleaned up while native callback was held");
            native.ReleaseCallback.Set();
            DrainSnapshot snapshot = await stopping.WaitAsync(TimeSpan.FromSeconds(5));
            snapshot.AssertDrained();
            Console.WriteLine($"PASS real matching callback: entryId={snapshot.EntryId} openResult={native.OpenResult} stopRequested={snapshot.StopOrder} callbackExit={snapshot.CallbackExit} closed={snapshot.Closed} unscheduled={snapshot.Unscheduled} managerReleased={snapshot.ManagerReleased} stopReturned={snapshot.StopReturn}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL listener drain: {ex}");
            return 1;
        }
        finally { native.ReleaseCallback.Set(); }
    }

    internal static int RunLateDrain()
    {
        var native = new RecordingListenerNative();
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromMilliseconds(150));
        try
        {
            Console.WriteLine("READY_LISTENER_LATE: waiting up to 5 seconds for real initial matching callback; no device operation requested");
            listener.Start();
            if (listener.Status != DeviceListenerStatus.Started)
                throw new InvalidOperationException($"Listener did not start: {listener.Status}");
            if (!native.Entered.Wait(TimeSpan.FromSeconds(5)))
            {
                Console.Error.WriteLine("BLOCKED: no real IOHIDManager matching callback arrived");
                return 2;
            }
            Console.WriteLine($"PHASE_NATIVE_MATCHING_ENTERED entryId={native.EntryId}");
            Task<DrainSnapshot> stopping = Task.Run(() =>
            {
                listener.Stop();
                return native.Snapshot(listener.Status);
            });
            DrainSnapshot timedOut = stopping.WaitAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            timedOut.AssertTimedOut();
            listener.Start();
            DrainSnapshot rejected = native.Snapshot(listener.Status);
            rejected.AssertRestartRejected(timedOut, native.ManagersCreated);
            Console.WriteLine($"PHASE_STOP_TIMED_OUT entryId={timedOut.EntryId} stopRequested={timedOut.StopOrder} stopReturned={timedOut.StopReturn} callbackExit={timedOut.CallbackExit} unscheduled={timedOut.Unscheduled} managerReleased={timedOut.ManagerReleased} restartStatus={rejected.Status} managersCreated={native.ManagersCreated}");
            native.ReleaseCallback.Set();
            if (!native.Cleaned.Wait(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("Native cleanup did not complete after callback release");
            if (!SpinWait.SpinUntil(() => listener.Status == DeviceListenerStatus.Stopped, TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("Native cleanup did not transition listener to Stopped");
            DrainSnapshot snapshot = native.Snapshot(listener.Status);
            snapshot.AssertLateDrained(timedOut);
            if (!SpinWait.SpinUntil(() => { listener.Start(); return native.ManagersCreated == 2; }, TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("Restart failed after cleanup");
            if (listener.Status != DeviceListenerStatus.Started || native.ManagersCreated != 2)
                throw new InvalidOperationException("Restart failed after cleanup");
            listener.Stop();
            if (listener.Status != DeviceListenerStatus.Stopped)
                throw new InvalidOperationException("Restarted listener failed to stop");
            if (native.ManagersReleased != 2)
                throw new InvalidOperationException($"Restarted manager not released: {native.ManagersReleased}");
            Console.WriteLine($"PASS real late callback drain: entryId={snapshot.EntryId} stopRequested={snapshot.StopOrder} stopReturned={timedOut.StopReturn} callbackExit={snapshot.CallbackExit} unscheduled={snapshot.Unscheduled} managerReleased={snapshot.ManagerReleased} restarted=2");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL listener late drain: {ex}");
            return 1;
        }
        finally
        {
            native.ReleaseCallback.Set();
            listener.Stop();
            if (native.ManagersCreated != 0 && !native.Cleaned.Wait(TimeSpan.FromSeconds(5)))
                Console.Error.WriteLine("BLOCKED: listener cleanup not observed after callback release");
        }
    }

    internal static async Task<int> RunRemovalAsync(int serial)
    {
        var native = new RecordingListenerNative { HoldRemoval = true };
        MacOSHidDeviceListener? listener = null;
        try
        {
            long? target = await ResolveRemovalTargetAsync(serial);
            if (target is null) return 2;
            long entryId = target.Value;
            native.RemovalTarget = entryId;
            listener = new MacOSHidDeviceListener(native, TimeSpan.FromSeconds(4));
            var delivered = new TaskCompletionSource<HidDeviceRescanHint>(TaskCreationOptions.RunContinuationsAsynchronously);
            listener.DeviceEvent = hint =>
            {
                if (hint.ChangeKind == HidDeviceChangeKind.Removed &&
                    hint.PlatformDeviceId == entryId.ToString(CultureInfo.InvariantCulture))
                    delivered.TrySetResult(hint);
            };
            listener.Start();
            if (listener.Status != DeviceListenerStatus.Started)
                throw new InvalidOperationException($"Listener did not start: {listener.Status}");
            if (!native.Scheduled.Wait(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("Listener did not schedule");
            if (!native.OpenReturned.Wait(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("Listener did not return from manager open");
            if (!native.InitialEntryIds.Contains(entryId))
            {
                Console.Error.WriteLine($"BLOCKED: selected serial={serial} entryId={entryId} absent from listener initial devices");
                return 2;
            }
            return await DrainRemovalAsync(serial, entryId, listener, native, delivered.Task);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL listener removal serial={serial}: {ex}");
            return 1;
        }
        finally
        {
            native.ReleaseCallback.Set();
            CleanupRemovalListener(listener, native);
            await YubiKeyManager.ShutdownAsync();
        }
    }

    private static async Task<int> DrainRemovalAsync(int serial, long entryId,
        MacOSHidDeviceListener listener, RecordingListenerNative native, Task<HidDeviceRescanHint> delivered)
    {
        Console.WriteLine($"READY_LISTENER_REMOVE serial={serial} entryId={entryId}: operator may remove this selected key; waiting up to 180 seconds");
        if (!native.Entered.Wait(TimeSpan.FromSeconds(180)))
        {
            Console.Error.WriteLine($"BLOCKED: no real removal callback for selected serial={serial} entryId={entryId}");
            return 2;
        }
        Console.WriteLine($"PHASE_NATIVE_REMOVAL_ENTERED serial={serial} entryId={native.EntryId}");
        HidDeviceRescanHint hint = await delivered.WaitAsync(TimeSpan.FromSeconds(3));
        if (hint.ChangeKind != HidDeviceChangeKind.Removed || hint.PlatformDeviceId != entryId.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException($"Removal callback did not pass through as selected rescan hint: {hint}");
        Task<DrainSnapshot> stopping = Task.Run(() =>
        {
            listener.Stop();
            return native.Snapshot(listener.Status);
        });
        if (!native.StopRequested.Wait(TimeSpan.FromSeconds(3)))
            throw new InvalidOperationException("Stop did not request native run-loop stop");
        if (stopping.IsCompleted || native.Closed != 0 || native.Unscheduled != 0 || native.ManagerReleased != 0)
            throw new InvalidOperationException("Listener cleaned up while removal callback was held");
        native.ReleaseCallback.Set();
        DrainSnapshot snapshot = await stopping.WaitAsync(TimeSpan.FromSeconds(5));
        snapshot.AssertDrained(entryId);
        Console.WriteLine($"PASS real removal callback matched serial={serial} entryId={snapshot.EntryId} hint={hint.ChangeKind} hintEntryId={hint.PlatformDeviceId} stopRequested={snapshot.StopOrder} callbackExit={snapshot.CallbackExit} closeReturned={snapshot.Closed} closeResult=0x{snapshot.CloseResult:X8} unscheduled={snapshot.Unscheduled} managerReleased={snapshot.ManagerReleased} stopReturned={snapshot.StopReturn}");
        return 0;
    }

    private static async Task<long?> ResolveRemovalTargetAsync(int serial)
    {
        IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync();
        IYubiKey[] selected = devices.Where(d => d.SerialNumber == serial && d.SupportsConnection(ConnectionType.HidFido)).ToArray();
        if (selected.Length != 1)
        {
            Console.Error.WriteLine($"BLOCKED: serial={serial} matched {selected.Length} FIDO HID devices");
            return null;
        }
        // macOS HidConnectionSlot IDs are hid:<registry entry ID>:<usage>.
        string interfaceId = DeviceConnectionRegistry.ResolveInterfaceId(selected[0], ConnectionType.HidFido);
        string[] parts = interfaceId.Split(':');
        if (parts.Length == 3 && parts[0] == "hid" &&
            long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long entryId) && entryId > 0)
            return entryId;
        Console.Error.WriteLine($"BLOCKED: cannot bind serial={serial} to native HID registry entry; interface={interfaceId}");
        return null;
    }

    private static void CleanupRemovalListener(MacOSHidDeviceListener? listener, RecordingListenerNative native)
    {
        if (listener is null) return;
        listener.Stop();
        if (native.ManagersCreated != 0 && !native.Cleaned.Wait(TimeSpan.FromSeconds(5)))
            Console.Error.WriteLine("BLOCKED: removal listener cleanup not observed after callback release");
        listener.Dispose();
    }

    private sealed record DrainSnapshot(long EntryId, DeviceListenerStatus Status, string? Error,
        int StopOrder, int CallbackExit, int Closed, int CloseResult, int Unscheduled, int ManagerReleased, int StopReturn)
    {
        internal void AssertDrained(long? expectedEntryId = null)
        {
            if (Error is not null || Status != DeviceListenerStatus.Stopped || EntryId == 0 ||
                (expectedEntryId is not null && EntryId != expectedEntryId) ||
                 StopOrder == 0 || CallbackExit <= StopOrder || Closed <= CallbackExit || Unscheduled <= Closed ||
                 ManagerReleased <= Unscheduled || StopReturn <= ManagerReleased)
                throw new InvalidOperationException($"Native callback drain/order unproven: status={Status}, error={Error}, stop={StopOrder}, callbackExit={CallbackExit}, unschedule={Unscheduled}, managerRelease={ManagerReleased}, stopReturn={StopReturn}");
        }

        internal void AssertTimedOut()
        {
            if (Error is not null || Status != DeviceListenerStatus.Error || EntryId == 0 ||
                StopOrder == 0 || StopReturn <= StopOrder || CallbackExit != 0 ||
                 Closed != 0 || Unscheduled != 0 || ManagerReleased != 0)
                throw new InvalidOperationException($"Stop timeout did not retain held callback: {this}");
        }

        internal void AssertRestartRejected(DrainSnapshot timeout, int managersCreated)
        {
            if (Error is not null || Status != DeviceListenerStatus.Error || managersCreated != 1 ||
                 CallbackExit != 0 || Closed != 0 || Unscheduled != 0 || ManagerReleased != 0 || StopReturn <= timeout.StopReturn)
                throw new InvalidOperationException($"Restart admitted undrained callback: {this}, managers={managersCreated}");
        }

        internal void AssertLateDrained(DrainSnapshot timeout)
        {
            if (Error is not null || Status != DeviceListenerStatus.Stopped || EntryId != timeout.EntryId ||
                 CallbackExit <= timeout.StopReturn || Closed <= CallbackExit || Unscheduled <= Closed ||
                ManagerReleased <= Unscheduled)
                throw new InvalidOperationException($"Late native drain unproven: {this}, timeout={timeout}");
        }
    }

    private sealed unsafe class RecordingListenerNative : IMacOSHidListenerNative
    {
        private readonly MacOSHidListenerNative _real = new();
        private GCHandle _root;
        private nint _callback;
        private nint _context;
        private nint _removalCallback;
        private nint _removalContext;
        private nint _manager;
        private int _first;
        private int _created;
        private int _released;
        private int _order;
        private int _stopOrder;
        private int _callbackExit;
        private int _unscheduled;
        private int _closed;
        private int _closeResult = int.MinValue;
        private int _openResult = int.MinValue;
        private int _managerReleased;
        private long _entryId;
        private string? _error;
        internal readonly ManualResetEventSlim Entered = new();
        internal readonly ManualResetEventSlim ReleaseCallback = new();
        internal readonly ManualResetEventSlim StopRequested = new();
        internal readonly ManualResetEventSlim Scheduled = new();
        internal readonly ManualResetEventSlim OpenReturned = new();
        internal readonly ManualResetEventSlim Cleaned = new();
        internal bool HoldRemoval { get; init; }
        internal long RemovalTarget { get; set; }
        internal long[] InitialEntryIds { get; private set; } = [];
        internal int ManagersCreated => Volatile.Read(ref _created);
        internal int ManagersReleased => Volatile.Read(ref _released);
        internal long EntryId => Interlocked.Read(ref _entryId);
        internal int StopOrder => Volatile.Read(ref _stopOrder);
        internal int CallbackExit => Volatile.Read(ref _callbackExit);
        internal int Unscheduled => Volatile.Read(ref _unscheduled);
        internal int Closed => Volatile.Read(ref _closed);
        internal int OpenResult => Volatile.Read(ref _openResult);
        internal int ManagerReleased => Volatile.Read(ref _managerReleased);
        internal DrainSnapshot Snapshot(DeviceListenerStatus status) => new(EntryId, status, _error,
            StopOrder, CallbackExit, Closed, Volatile.Read(ref _closeResult), Unscheduled, ManagerReleased,
            Interlocked.Increment(ref _order));

        public nint CreateManager()
        {
            Interlocked.Increment(ref _created);
            return _manager = _real.CreateManager();
        }
        public void SetDeviceMatching(nint manager, int vendorId) => _real.SetDeviceMatching(manager, vendorId);
        public long[] GetInitialEntryIds(nint manager) => InitialEntryIds = _real.GetInitialEntryIds(manager);
        public long GetEntryId(nint device) => _real.GetEntryId(device);
        public void RegisterMatching(nint manager, nint callback, nint context)
        {
            _callback = callback;
            _context = context;
            _root = GCHandle.Alloc(this);
            _real.RegisterMatching(manager, (nint)(delegate* unmanaged[Cdecl]<nint, int, nint, nint, void>)&ForwardMatching,
                GCHandle.ToIntPtr(_root));
        }
        public void RegisterRemoval(nint manager, nint callback, nint context)
        {
            _removalCallback = callback;
            _removalContext = context;
            _real.RegisterRemoval(manager, (nint)(delegate* unmanaged[Cdecl]<nint, int, nint, nint, void>)&ForwardRemoval,
                GCHandle.ToIntPtr(_root));
        }
        public nint RetainCurrentRunLoop() => _real.RetainCurrentRunLoop();
        public nint CreateRunLoopMode() => _real.CreateRunLoopMode();
        public void Schedule(nint manager, nint loop, nint mode)
        {
            _real.Schedule(manager, loop, mode);
            Scheduled.Set();
        }
        public int Open(nint manager)
        {
            int result = _real.Open(manager);
            Volatile.Write(ref _openResult, result);
            OpenReturned.Set();
            return result;
        }
        public int Close(nint manager)
        {
            int result = _real.Close(manager);
            Volatile.Write(ref _closeResult, result);
            Volatile.Write(ref _closed, Interlocked.Increment(ref _order));
            return result;
        }
        public int Run(nint mode, double seconds) => _real.Run(mode, seconds);
        public void StopRunLoop(nint loop)
        {
            _real.StopRunLoop(loop);
            Volatile.Write(ref _stopOrder, Interlocked.Increment(ref _order));
            StopRequested.Set();
        }
        public void Unschedule(nint manager, nint loop, nint mode)
        {
            _real.Unschedule(manager, loop, mode);
            Volatile.Write(ref _unscheduled, Interlocked.Increment(ref _order));
        }
        public void Release(nint handle)
        {
            _real.Release(handle);
            if (handle != _manager) return;
            Volatile.Write(ref _managerReleased, Interlocked.Increment(ref _order));
            Interlocked.Increment(ref _released);
            _root.Free();
            Cleaned.Set();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void ForwardMatching(nint context, int result, nint sender, nint device)
        {
            RecordingListenerNative? owner = null;
            try
            {
                owner = (RecordingListenerNative?)GCHandle.FromIntPtr(context).Target;
                owner?.DeliverMatching(result, sender, device);
            }
            catch (Exception ex) { if (owner is not null) owner._error = ex.GetType().Name; }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void ForwardRemoval(nint context, int result, nint sender, nint device)
        {
            RecordingListenerNative? owner = null;
            try
            {
                owner = (RecordingListenerNative?)GCHandle.FromIntPtr(context).Target;
                owner?.DeliverRemoval(result, sender, device);
            }
            catch (Exception ex) { if (owner is not null) owner._error = ex.GetType().Name; }
        }

        private void DeliverRemoval(int result, nint sender, nint device)
        {
            long id = 0;
            try { if (device != 0) id = _real.GetEntryId(device); }
            catch (Exception ex) { _error = ex.GetType().Name; }
            bool selected = HoldRemoval && id == RemovalTarget && Interlocked.CompareExchange(ref _first, 1, 0) == 0;
            try
            {
                if (selected)
                {
                    Interlocked.Exchange(ref _entryId, id);
                    ((delegate* unmanaged[Cdecl]<nint, int, nint, nint, void>)_removalCallback)(_removalContext, result, sender, device);
                    Entered.Set();
                    if (!ReleaseCallback.Wait(TimeSpan.FromSeconds(8))) _error = "CallbackGateTimeout";
                }
                else ((delegate* unmanaged[Cdecl]<nint, int, nint, nint, void>)_removalCallback)(_removalContext, result, sender, device);
            }
            catch (Exception ex) { _error = ex.GetType().Name; }
            finally
            {
                if (selected) Volatile.Write(ref _callbackExit, Interlocked.Increment(ref _order));
            }
        }

        private void DeliverMatching(int result, nint sender, nint device)
        {
            bool first = !HoldRemoval && Interlocked.CompareExchange(ref _first, 1, 0) == 0;
            try
            {
                if (first)
                {
                    try { Interlocked.Exchange(ref _entryId, _real.GetEntryId(device)); }
                    catch (Exception ex) { _error = ex.GetType().Name; }
                    Entered.Set();
                    if (!ReleaseCallback.Wait(TimeSpan.FromSeconds(8))) _error = "CallbackGateTimeout";
                }
                ((delegate* unmanaged[Cdecl]<nint, int, nint, nint, void>)_callback)(_context, result, sender, device);
            }
            catch (Exception ex) { _error = ex.GetType().Name; }
            finally
            {
                if (first) Volatile.Write(ref _callbackExit, Interlocked.Increment(ref _order));
            }
        }
    }
}
