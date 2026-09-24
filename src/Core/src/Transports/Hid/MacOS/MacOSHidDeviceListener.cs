// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using CFNativeMethods = Yubico.YubiKit.Core.Native.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.Native.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>macOS HID topology listener. The run-loop thread owns each generation's native lifetime.</summary>
internal sealed unsafe class MacOSHidDeviceListener : HidDeviceListener
{
    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<MacOSHidDeviceListener>();

    private readonly Lock _syncLock = new();
    private readonly IMacOSHidListenerNative _native;
    private readonly TimeSpan _stopTimeout;
    private Generation? _generation;
    private bool _disposed;

    public MacOSHidDeviceListener() : this(new MacOSHidListenerNative(), MaxDisposalWaitTime) { }

    internal MacOSHidDeviceListener(IMacOSHidListenerNative native, TimeSpan stopTimeout)
    {
        _native = native;
        _stopTimeout = stopTimeout;
    }

    public override void Start()
    {
        lock (_syncLock)
        {
            if (_disposed) return;
            if (_generation is { } previous)
            {
                if (Status == DeviceListenerStatus.Started && !previous.StopRequested && previous.Thread.IsAlive) return;
                if (previous.Thread.IsAlive || !previous.Cleaned)
                {
                    Logger.LogWarning("macOS HID listener generation has not finished cleanup; cannot restart");
                    Status = DeviceListenerStatus.Error;
                    return;
                }

                _generation = null;
            }

            StartGeneration();
        }
    }

    private void StartGeneration()
    {
        Generation? generation = null;
        try
        {
            var manager = _native.CreateManager();
            if (manager == 0) throw new InvalidOperationException("Failed to create IOHIDManager");
            generation = new Generation(this, manager);
            _generation = generation;
            _native.SetDeviceMatching(manager);
            foreach (var entryId in _native.GetInitialEntryIds(manager)) generation.KnownEntryIds.Add(entryId);
            generation.RegistrationAttempted = true;
            _native.RegisterMatching(manager, (nint)(delegate* unmanaged[Cdecl]<nint, int, nint, nint, void>)&Arrived, generation.Context);
            _native.RegisterRemoval(manager, (nint)(delegate* unmanaged[Cdecl]<nint, int, nint, nint, void>)&Removed, generation.Context);
            Status = DeviceListenerStatus.Started;
            generation.Thread.Start();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start macOS HID listener");
            Status = DeviceListenerStatus.Error;
            // A failed registration may still have installed a native callback. Keep its
            // context rooted rather than claiming quiescence we cannot establish.
            if (generation is not null && !generation.RegistrationAttempted && !generation.Thread.IsAlive) Cleanup(generation);
        }
    }

    public override void Stop()
    {
        Generation? generation;
        lock (_syncLock)
        {
            generation = _generation;
            if (generation is null) { Status = DeviceListenerStatus.Stopped; return; }
            generation.StopRequested = true;
            // Cleanup and this wake serialize under _syncLock; a stop before scheduling
            // is also observed by the run-loop polling condition.
            if (generation.RunLoop != 0 && !generation.TimedOut) _native.StopRunLoop(generation.RunLoop);
            // A callback may stop itself; external callers share one monotonic wait budget.
            if (Thread.CurrentThread == generation.Thread || generation.TimedOut || !generation.Thread.IsAlive) return;
            if (generation.WaitStartedAt == 0) generation.WaitStartedAt = Stopwatch.GetTimestamp();
        }

        WaitForExit(generation);
    }

    private void WaitForExit(Generation generation)
    {
        TimeSpan remaining = _stopTimeout - Stopwatch.GetElapsedTime(generation.WaitStartedAt);
        if (remaining > TimeSpan.Zero && generation.Thread.Join(remaining)) return;
        lock (_syncLock)
        {
            if (!generation.Cleaned && !generation.TimedOut)
            {
                generation.TimedOut = true;
                if (ReferenceEquals(_generation, generation)) Status = DeviceListenerStatus.Error;
                Logger.LogError("macOS HID listener thread did not exit within timeout; retaining native generation until callback drain");
            }
        }
    }

    private void Listen(Generation generation)
    {
        try
        {
            var runLoop = _native.RetainCurrentRunLoop();
            lock (_syncLock) generation.RunLoop = runLoop;
            if (generation.RunLoop == 0) throw new InvalidOperationException("Failed to retain macOS HID run loop");
            generation.Mode = _native.CreateRunLoopMode();
            if (generation.Mode == 0) throw new InvalidOperationException("Failed to create macOS HID run loop mode");
            _native.Schedule(generation.Manager, generation.RunLoop, generation.Mode);
            generation.Scheduled = true;
            Run(generation);
        }
        catch (Exception ex)
        {
            LogWorkerFailure(ex, "macOS HID listener thread encountered an error");
            lock (_syncLock)
            {
                if (ReferenceEquals(_generation, generation) && !generation.StopRequested) Status = DeviceListenerStatus.Error;
            }
        }
        finally
        {
            lock (_syncLock)
            {
                Cleanup(generation);
                if (ReferenceEquals(_generation, generation) && generation.Cleaned)
                    Status = generation.StopRequested ? DeviceListenerStatus.Stopped : DeviceListenerStatus.Error;
            }
        }
    }

    private void Run(Generation generation)
    {
        while (!generation.StopRequested)
        {
            var result = _native.Run(generation.Mode, CheckForChangesWaitTime.TotalSeconds);
            if (result is CFNativeMethods.kCFRunLoopRunStopped or CFNativeMethods.kCFRunLoopRunFinished) break;
            if (result is not CFNativeMethods.kCFRunLoopRunTimedOut and not CFNativeMethods.kCFRunLoopRunHandledSource)
            {
                Logger.LogDebug("CFRunLoopRunInMode returned unexpected result: {Result}", result);
            }
        }
    }

    private void Cleanup(Generation generation)
    {
        try
        {
            // Unschedule only after the run loop has returned from every admitted callback.
            if (generation.Scheduled) _native.Unschedule(generation.Manager, generation.RunLoop, generation.Mode);
            if (generation.Mode != 0)
            {
                _native.Release(generation.Mode);
                generation.Mode = 0;
            }
            if (generation.RunLoop != 0)
            {
                _native.Release(generation.RunLoop);
                generation.RunLoop = 0;
            }
            _native.Release(generation.Manager);
            generation.Cleaned = true;
            generation.Root.Free();
        }
        catch (Exception ex)
        {
            // Retain the root and handles if native quiescence/release cannot be proved.
            LogWorkerFailure(ex, "macOS HID listener cleanup failed; generation remains quarantined");
            if (ReferenceEquals(_generation, generation)) Status = DeviceListenerStatus.Error;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void Arrived(nint context, int result, nint sender, nint device)
    {
        try { var generation = (Generation)GCHandle.FromIntPtr(context).Target!; generation.Owner.OnNativeEvent(generation, HidDeviceChangeKind.Added, device); }
        catch (Exception ex) { LogCallbackFailure(ex, "Failed to process macOS device arrival"); }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void Removed(nint context, int result, nint sender, nint device)
    {
        try { var generation = (Generation)GCHandle.FromIntPtr(context).Target!; generation.Owner.OnNativeEvent(generation, HidDeviceChangeKind.Removed, device); }
        catch (Exception ex) { LogCallbackFailure(ex, "Failed to process macOS device removal"); }
    }

    private static void LogCallbackFailure(Exception ex, string message)
    {
        // Even a user-supplied logger must not throw through the unmanaged callback frame.
        try { Logger.LogWarning(ex, "{Message}", message); }
        catch (Exception) { }
    }

    private static void LogWorkerFailure(Exception ex, string message)
    {
        try { Logger.LogError(ex, "{Message}", message); }
        catch (Exception) { }
    }

    private void OnNativeEvent(Generation generation, HidDeviceChangeKind kind, nint device)
    {
        if (generation.StopRequested) return;
        if (kind == HidDeviceChangeKind.Added && device == 0) return;
        try
        {
            var entryId = device == 0 ? (long?)null : _native.GetEntryId(device);
            if (entryId is not null && !TrackEntry(generation, kind, entryId.Value)) return;

            Deliver(generation, new HidDeviceRescanHint(kind, entryId?.ToString(CultureInfo.InvariantCulture)));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process macOS device change");
            Deliver(generation, new HidDeviceRescanHint(kind));
        }
    }

    private void Deliver(Generation generation, HidDeviceRescanHint hint)
    {
        if (!generation.StopRequested) OnDeviceEvent(hint);
    }

    private static bool TrackEntry(Generation generation, HidDeviceChangeKind kind, long entryId)
    {
        lock (generation.KnownEntryIds)
        {
            if (kind == HidDeviceChangeKind.Added) return generation.KnownEntryIds.Add(entryId);
            generation.KnownEntryIds.Remove(entryId);
            return true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        bool first;
        lock (_syncLock)
        {
            first = !_disposed;
            _disposed = true;
            if (!disposing && _generation is { } generation)
            {
                // A live native callback roots this generation; finalization cannot free it.
                generation.StopRequested = true;
            }
        }

        if (disposing) Stop();
        if (first) base.Dispose(disposing);
    }

    ~MacOSHidDeviceListener() => Dispose(disposing: false);

    private sealed class Generation
    {
        public readonly MacOSHidDeviceListener Owner;
        public readonly nint Manager;
        public readonly Thread Thread;
        public readonly GCHandle Root;
        public readonly HashSet<long> KnownEntryIds = [];
        public readonly nint Context;
        public nint RunLoop;
        public nint Mode;
        public bool Scheduled;
        public bool RegistrationAttempted;
        public volatile bool StopRequested;
        public bool TimedOut;
        public long WaitStartedAt;
        public bool Cleaned;

        public Generation(MacOSHidDeviceListener owner, nint manager)
        {
            Owner = owner;
            Manager = manager;
            Root = GCHandle.Alloc(this);
            Context = GCHandle.ToIntPtr(Root);
            Thread = new Thread(() => owner.Listen(this)) { Name = "MacOSHidDeviceListener", IsBackground = true };
        }
    }
}

// Only the manager/run-loop operations are substituted by the no-hardware lifetime tests.
internal interface IMacOSHidListenerNative
{
    nint CreateManager();
    void SetDeviceMatching(nint manager);
    long[] GetInitialEntryIds(nint manager);
    long GetEntryId(nint device);
    void RegisterMatching(nint manager, nint callback, nint context);
    void RegisterRemoval(nint manager, nint callback, nint context);
    nint RetainCurrentRunLoop();
    nint CreateRunLoopMode();
    void Schedule(nint manager, nint loop, nint mode);
    int Run(nint mode, double seconds);
    void StopRunLoop(nint loop);
    void Unschedule(nint manager, nint loop, nint mode);
    void Release(nint handle);
}

internal sealed class MacOSHidListenerNative : IMacOSHidListenerNative
{
    public nint CreateManager() => IOKitNativeMethods.IOHIDManagerCreate(0, 0);
    public void SetDeviceMatching(nint manager) => IOKitNativeMethods.IOHIDManagerSetDeviceMatching(manager, 0);
    public void RegisterMatching(nint manager, nint callback, nint context) => IOKitNativeMethods.IOHIDManagerRegisterDeviceMatchingCallback(manager, callback, context);
    public void RegisterRemoval(nint manager, nint callback, nint context) => IOKitNativeMethods.IOHIDManagerRegisterDeviceRemovalCallback(manager, callback, context);
    public long GetEntryId(nint device) => MacOSHidInterface.GetEntryId(device);
    public nint RetainCurrentRunLoop() => CFNativeMethods.CFRetain(CFNativeMethods.CFRunLoopGetCurrent());
    public nint CreateRunLoopMode() => CoreFoundationString.Create("kCFRunLoopDefaultMode");
    public void Schedule(nint manager, nint loop, nint mode) => IOKitNativeMethods.IOHIDManagerScheduleWithRunLoop(manager, loop, mode);
    public int Run(nint mode, double seconds) => CFNativeMethods.CFRunLoopRunInMode(mode, seconds, returnAfterSourceHandled: false);
    public void StopRunLoop(nint loop) => CFNativeMethods.CFRunLoopStop(loop);
    public void Unschedule(nint manager, nint loop, nint mode) => IOKitNativeMethods.IOHIDManagerUnscheduleFromRunLoop(manager, loop, mode);
    public void Release(nint handle) => CFNativeMethods.CFRelease(handle);

    public long[] GetInitialEntryIds(nint manager)
    {
        var deviceSet = IOKitNativeMethods.IOHIDManagerCopyDevices(manager);
        if (deviceSet == 0) return [];
        try
        {
            var count = CFNativeMethods.CFSetGetCount(deviceSet);
            if (count <= 0) return [];
            var devices = new nint[count];
            CFNativeMethods.CFSetGetValues(deviceSet, devices);
            List<long> ids = [];
            foreach (var device in devices)
            {
                try { if (device != 0) ids.Add(GetEntryId(device)); }
                catch (Exception ex) { MacOSHidListenerNativeLogger.Log(ex); }
            }

            return [.. ids];
        }
        finally { CFNativeMethods.CFRelease(deviceSet); }
    }
}

internal static class MacOSHidListenerNativeLogger
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<MacOSHidListenerNative>();
    internal static void Log(Exception ex) => Logger.LogDebug(ex, "Failed to read macOS HID registry entry ID");
}
