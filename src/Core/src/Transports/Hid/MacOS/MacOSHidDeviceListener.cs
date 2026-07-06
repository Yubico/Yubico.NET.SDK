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
using System.Globalization;
using System.Text;
using Yubico.YubiKit.Core.Native.MacOS.CoreFoundation;
using CFNativeMethods = Yubico.YubiKit.Core.Native.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.Native.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
/// macOS implementation of HID device listener using IOHIDManager callbacks.
/// </summary>
/// <remarks>
/// The listener does not auto-start. Call <see cref="Start"/> after setting up <see cref="DeviceEvent"/>
/// callback.
/// </remarks>
internal sealed class MacOSHidDeviceListener : HidDeviceListener
{
    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<MacOSHidDeviceListener>();

    private readonly Lock _syncLock = new();
    private readonly Lock _cacheLock = new();
    private readonly HashSet<long> _knownEntryIds = [];
    private IntPtr _hidManager;
    private IntPtr _runLoop;
    private IntPtr _runLoopMode;
    private Thread? _listenerThread;
    private volatile bool _shouldStop;
    private volatile bool _managerOpened;
    private bool _disposed;

    // Keep callback delegates alive to prevent GC
    private IOKitNativeMethods.IOHIDDeviceCallback? _arrivedCallbackDelegate;
    private IOKitNativeMethods.IOHIDDeviceCallback? _removedCallbackDelegate;

    /// <summary>
    /// Creates a new instance. The listener does not start automatically - call <see cref="Start"/>
    /// after setting up the <see cref="DeviceEvent"/> callback.
    /// </summary>
    public MacOSHidDeviceListener()
    {
        // Lazy start - do nothing in constructor
    }

    /// <inheritdoc />
    public override void Start()
    {
        lock (_syncLock)
        {
            if (Status == DeviceListenerStatus.Started)
            {
                return;
            }

            try
            {
                // Create the HID Manager
                _hidManager = IOKitNativeMethods.IOHIDManagerCreate(IntPtr.Zero, 0);
                if (_hidManager == IntPtr.Zero)
                {
                    Logger.LogWarning("Failed to create IOHIDManager");
                    Status = DeviceListenerStatus.Error;
                    return;
                }

                // Set device matching to all HID devices
                IOKitNativeMethods.IOHIDManagerSetDeviceMatching(_hidManager, IntPtr.Zero);
                PopulateInitialKnownEntryIds();

                // Keep callback delegates alive
                _arrivedCallbackDelegate = DeviceArrivedCallback;
                _removedCallbackDelegate = DeviceRemovedCallback;

                // Register callbacks
                IOKitNativeMethods.IOHIDManagerRegisterDeviceMatchingCallback(
                    _hidManager,
                    _arrivedCallbackDelegate,
                    IntPtr.Zero);

                IOKitNativeMethods.IOHIDManagerRegisterDeviceRemovalCallback(
                    _hidManager,
                    _removedCallbackDelegate,
                    IntPtr.Zero);

                _shouldStop = false;
                Status = DeviceListenerStatus.Started;

                // Start the listener thread
                _listenerThread = new Thread(ListenerThreadProc)
                {
                    Name = "MacOSHidDeviceListener",
                    IsBackground = true
                };
                _listenerThread.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start macOS HID listener");
                Status = DeviceListenerStatus.Error;
            }
        }
    }

    /// <inheritdoc />
    public override void Stop()
    {
        lock (_syncLock)
        {
            if (Status == DeviceListenerStatus.Stopped)
            {
                return;
            }

            _shouldStop = true;

            // Stop the run loop
            if (_runLoop != IntPtr.Zero)
            {
                CFNativeMethods.CFRunLoopStop(_runLoop);
            }

            // Wait for the listener thread to exit
            if (_listenerThread is not null && _listenerThread.IsAlive)
            {
                if (!_listenerThread.Join(MaxDisposalWaitTime))
                {
                    Logger.LogWarning("macOS HID listener thread did not exit within timeout");
                }
            }

            _listenerThread = null;

            // Release the run loop mode string
            if (_runLoopMode != IntPtr.Zero)
            {
                CFNativeMethods.CFRelease(_runLoopMode);
                _runLoopMode = IntPtr.Zero;
            }

            // Release the HID manager
            if (_hidManager != IntPtr.Zero)
            {
                CFNativeMethods.CFRelease(_hidManager);
                _hidManager = IntPtr.Zero;
            }

            _arrivedCallbackDelegate = null;
            _removedCallbackDelegate = null;
            lock (_cacheLock)
            {
                _knownEntryIds.Clear();
            }

            Status = DeviceListenerStatus.Stopped;
        }
    }

    private void ListenerThreadProc()
    {
        try
        {
            // Get the current run loop for this thread
            _runLoop = CFNativeMethods.CFRunLoopGetCurrent();

            // Create the run loop mode string
            var modeBytes = Encoding.UTF8.GetBytes("kCFRunLoopDefaultMode");
            _runLoopMode = CFNativeMethods.CFStringCreateWithCString(
                IntPtr.Zero,
                [.. modeBytes, 0],
                0x08000100); // kCFStringEncodingUTF8

            // Schedule the HID manager with this run loop
            IOKitNativeMethods.IOHIDManagerScheduleWithRunLoop(
                _hidManager,
                _runLoop,
                _runLoopMode);

            var openResult = IOKitNativeMethods.IOHIDManagerOpen(_hidManager, 0);
            if (openResult != 0)
            {
                Logger.LogWarning("IOHIDManagerOpen failed: 0x{Result:X8}", openResult);
                Status = DeviceListenerStatus.Error;
                return;
            }

            _managerOpened = true;

            // Run the loop until stopped
            while (!_shouldStop)
            {
                var result = CFNativeMethods.CFRunLoopRunInMode(
                    _runLoopMode,
                    CheckForChangesWaitTime.TotalSeconds,
                    returnAfterSourceHandled: false);

                // Break if the run loop was stopped or finished
                if (result == NativeMethods.kCFRunLoopRunStopped ||
                    result == NativeMethods.kCFRunLoopRunFinished)
                {
                    break;
                }

                // Continue on timeout or source handled
                if (result != NativeMethods.kCFRunLoopRunTimedOut &&
                    result != NativeMethods.kCFRunLoopRunHandledSource)
                {
                    Logger.LogDebug("CFRunLoopRunInMode returned unexpected result: {Result}", result);
                }
            }
        }
        catch (Exception ex)
        {
            if (!_shouldStop)
            {
                Logger.LogError(ex, "macOS HID listener thread encountered an error");
                Status = DeviceListenerStatus.Error;
            }
        }
        finally
        {
            if (_managerOpened && _hidManager != IntPtr.Zero)
            {
                var closeResult = IOKitNativeMethods.IOHIDManagerClose(_hidManager, 0);
                if (!_shouldStop && closeResult != 0)
                {
                    Logger.LogWarning("IOHIDManagerClose failed: 0x{Result:X8}", closeResult);
                }

                _managerOpened = false;
            }

            // Cleanup run loop resources
            if (_hidManager != IntPtr.Zero && _runLoop != IntPtr.Zero && _runLoopMode != IntPtr.Zero)
            {
                IOKitNativeMethods.IOHIDManagerUnscheduleFromRunLoop(
                    _hidManager,
                    _runLoop,
                    _runLoopMode);
            }
        }
    }

    private void DeviceArrivedCallback(IntPtr context, int result, IntPtr sender, IntPtr deviceRef)
    {
        try
        {
            if (deviceRef == IntPtr.Zero)
            {
                return;
            }

            var hint = CreateHint(HidDeviceChangeKind.Added, deviceRef);
            if (hint.PlatformDeviceId is not null && long.TryParse(hint.PlatformDeviceId, CultureInfo.InvariantCulture, out var entryId))
            {
                if (!TryRegisterArrivedEntryId(entryId))
                {
                    Logger.LogTrace("Suppressing initial macOS HID matching callback for entry ID {EntryId}", entryId);
                    return;
                }
            }

            OnDeviceEvent(hint);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process macOS device arrival");
            OnDeviceEvent(new HidDeviceRescanHint(HidDeviceChangeKind.Added));
        }
    }

    private void DeviceRemovedCallback(IntPtr context, int result, IntPtr sender, IntPtr deviceRef)
    {
        try
        {
            var hint = CreateHint(HidDeviceChangeKind.Removed, deviceRef);
            if (hint.PlatformDeviceId is not null && long.TryParse(hint.PlatformDeviceId, CultureInfo.InvariantCulture, out var entryId))
            {
                lock (_cacheLock)
                {
                    _knownEntryIds.Remove(entryId);
                }
            }

            OnDeviceEvent(hint);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process macOS device removal");
            OnDeviceEvent(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));
        }
    }

    private void PopulateInitialKnownEntryIds()
    {
        if (_hidManager == IntPtr.Zero)
        {
            return;
        }

        var deviceSet = IOKitNativeMethods.IOHIDManagerCopyDevices(_hidManager);
        if (deviceSet == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var deviceSetCount = CFNativeMethods.CFSetGetCount(deviceSet);
            if (deviceSetCount <= 0)
            {
                return;
            }

            var devices = new IntPtr[deviceSetCount];
            CFNativeMethods.CFSetGetValues(deviceSet, devices);

            lock (_cacheLock)
            {
                foreach (var device in devices)
                {
                    if (TryGetEntryId(device, out var entryId))
                    {
                        _knownEntryIds.Add(entryId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to populate initial macOS HID listener baseline");
        }
        finally
        {
            CFNativeMethods.CFRelease(deviceSet);
        }
    }

    private bool TryRegisterArrivedEntryId(long entryId)
    {
        lock (_cacheLock)
        {
            return _knownEntryIds.Add(entryId);
        }
    }

    private static HidDeviceRescanHint CreateHint(HidDeviceChangeKind changeKind, IntPtr deviceRef)
    {
        try
        {
            if (deviceRef == IntPtr.Zero)
            {
                return new HidDeviceRescanHint(changeKind);
            }

            var entryId = MacOSHidDevice.GetEntryId(deviceRef).ToString(CultureInfo.InvariantCulture);
            return new HidDeviceRescanHint(changeKind, entryId, entryId);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to read macOS HID registry entry ID");
            return new HidDeviceRescanHint(changeKind);
        }
    }

    private static bool TryGetEntryId(IntPtr deviceRef, out long entryId)
    {
        try
        {
            if (deviceRef == IntPtr.Zero)
            {
                entryId = 0;
                return false;
            }

            entryId = MacOSHidDevice.GetEntryId(deviceRef);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to read macOS HID registry entry ID");
            entryId = 0;
            return false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;

        if (disposing)
        {
            Stop();
        }
        else
        {
            // Finalizer path - minimal cleanup
            _shouldStop = true;
            if (_runLoop != IntPtr.Zero)
            {
                CFNativeMethods.CFRunLoopStop(_runLoop);
            }
            if (_runLoopMode != IntPtr.Zero)
            {
                CFNativeMethods.CFRelease(_runLoopMode);
            }
            if (_hidManager != IntPtr.Zero)
            {
                CFNativeMethods.CFRelease(_hidManager);
            }
        }

        base.Dispose(disposing);
    }

    ~MacOSHidDeviceListener()
    {
        Dispose(disposing: false);
    }
}