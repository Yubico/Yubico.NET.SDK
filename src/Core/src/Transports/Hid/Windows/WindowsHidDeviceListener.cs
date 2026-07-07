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
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Windows.Cfgmgr32;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>
/// Windows implementation of HID device listener using CM_Register_Notification.
/// </summary>
/// <remarks>
/// The listener does not auto-start. Call <see cref="Start"/> after setting up <see cref="DeviceEvent"/>
/// callback.
/// </remarks>
internal sealed class WindowsHidDeviceListener : HidDeviceListener
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<WindowsHidDeviceListener>();

    /// <summary>
    /// GUID for the HID device interface class.
    /// </summary>
    private static readonly Guid GuidDevinterfaceHid = new("4D1E55B2-F16F-11CF-88CB-001111000030");

    /// <summary>
    /// Offset in CM_NOTIFY_EVENT_DATA where the SymbolicLink string begins (after FilterType, Reserved, ClassGuid).
    /// </summary>
    private const int SymbolicLinkOffset = 24;

    private readonly Lock _syncLock = new();
    private GCHandle _marshalableThisPtr;
    private NativeMethods.CM_NOTIFY_CALLBACK? _callbackDelegate;
    private IntPtr _notificationHandle;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance. The listener does not start automatically - call <see cref="Start"/>
    /// after setting up the <see cref="DeviceEvent"/> callback.
    /// </summary>
    public WindowsHidDeviceListener()
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
                // Keep callback delegate alive for the duration of the listener
                _callbackDelegate = NotificationCallback;

                // Deliberately Weak, not Normal: a strong handle would self-root the listener,
                // making the finalizer unreachable while registered — an owner that drops the
                // listener without Dispose() would leak the native registration forever and keep
                // pumping events into an abandoned object graph. The Weak handle keeps the
                // finalizer reachable so it can unregister (draining in-flight callbacks) and
                // free this handle. During normal operation the owner strongly roots the
                // listener, so callbacks never observe a collected target.
                _marshalableThisPtr = GCHandle.Alloc(this, GCHandleType.Weak);

                // Build the notification filter for HID device interfaces
                var filterSize = Marshal.SizeOf<NativeMethods.CM_NOTIFY_FILTER>();
                Debug.Assert(filterSize == NativeMethods.CmNotifyFilterSize);
                var filter = new NativeMethods.CM_NOTIFY_FILTER
                {
                    cbSize = filterSize,
                    Flags = 0,
                    FilterType = NativeMethods.CM_NOTIFY_FILTER_TYPE.DEVINTERFACE,
                    ClassGuid = GuidDevinterfaceHid
                };

                var filterPtr = Marshal.AllocHGlobal(filterSize);
                try
                {
                    Marshal.StructureToPtr(filter, filterPtr, false);

                    var result = NativeMethods.CM_Register_Notification(
                        filterPtr,
                        GCHandle.ToIntPtr(_marshalableThisPtr),
                        _callbackDelegate,
                        out _notificationHandle);

                    if (result != NativeMethods.CmErrorCode.CR_SUCCESS)
                    {
                        Logger.LogWarning("Failed to register HID notification: {Result}", result);
                        Status = DeviceListenerStatus.Error;
                        ClearRegistrationState();
                        return;
                    }

                    Status = DeviceListenerStatus.Started;
                }
                finally
                {
                    Marshal.FreeHGlobal(filterPtr);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start Windows HID listener");
                Status = DeviceListenerStatus.Error;
                ClearRegistrationState();
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

            // Unregister the notification
            if (_notificationHandle != IntPtr.Zero)
            {
                _ = NativeMethods.CM_Unregister_Notification(_notificationHandle);
                _notificationHandle = IntPtr.Zero;
            }

            ClearRegistrationState();

            Status = DeviceListenerStatus.Stopped;
        }
    }

    private static int NotificationCallback(IntPtr hNotify, IntPtr context, NativeMethods.CM_NOTIFY_ACTION action, IntPtr eventData, int eventDataSize)
    {
        // Recover the listener instance from the GCHandle
        if (context == IntPtr.Zero)
        {
            return 0;
        }

        var handle = GCHandle.FromIntPtr(context);
        if (!handle.IsAllocated || handle.Target is not WindowsHidDeviceListener listener)
        {
            // Weak handle target collected (listener dropped without Dispose); the finalizer
            // will unregister the native notification. Drop the hint until then.
            Logger.LogDebug("HID notification received for a collected listener; ignoring");
            return 0;
        }

        try
        {
            listener.HandleNotification(action, eventData, eventDataSize);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Exception in HID notification callback");
        }

        return 0; // ERROR_SUCCESS
    }

    private void HandleNotification(NativeMethods.CM_NOTIFY_ACTION action, IntPtr eventData, int eventDataSize)
    {
        switch (action)
        {
            case NativeMethods.CM_NOTIFY_ACTION.DEVICEINTERFACEARRIVAL:
                HandleDeviceArrival(eventData, eventDataSize);
                break;
            case NativeMethods.CM_NOTIFY_ACTION.DEVICEINTERFACEREMOVAL:
                HandleDeviceRemoval(eventData, eventDataSize);
                break;
        }
    }

    private void HandleDeviceArrival(IntPtr eventData, int eventDataSize)
    {
        try
        {
            var devicePath = ReadSymbolicLink(eventData, eventDataSize);
            if (devicePath is null)
            {
                OnDeviceEvent(new HidDeviceRescanHint(HidDeviceChangeKind.Added));
                return;
            }

            Logger.LogDebug("HID device arrived: {DevicePath}", devicePath);
            OnDeviceEvent(CreateHint(HidDeviceChangeKind.Added, devicePath));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process device arrival");
            OnDeviceEvent(new HidDeviceRescanHint(HidDeviceChangeKind.Added));
        }
    }

    private void HandleDeviceRemoval(IntPtr eventData, int eventDataSize)
    {
        try
        {
            var devicePath = ReadSymbolicLink(eventData, eventDataSize);
            if (devicePath is null)
            {
                OnDeviceEvent(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));
                return;
            }

            Logger.LogDebug("HID device removed: {DevicePath}", devicePath);
            OnDeviceEvent(CreateHint(HidDeviceChangeKind.Removed, devicePath));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process device removal");
            OnDeviceEvent(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));
        }
    }

    // Internal for unit testing: pure memory parsing with no P/Invoke, so the
    // bounds behavior is verifiable on any platform.
    internal static string? ReadSymbolicLink(IntPtr eventData, int eventDataSize)
    {
        if (eventData == IntPtr.Zero)
        {
            return null;
        }

        if (eventDataSize < SymbolicLinkOffset + sizeof(char))
        {
            Logger.LogDebug(
                "HID notification event data too small for symbolic link: {EventDataSize}",
                eventDataSize);
            return null;
        }

        // The symbolic link (device path) starts at offset 24 in CM_NOTIFY_EVENT_DATA.
        // Windows NUL-terminates the string within eventDataSize, but never trust native
        // sizes blindly: bound the read to the event payload and trim at the first NUL.
        var symbolicLinkPtr = IntPtr.Add(eventData, SymbolicLinkOffset);
        var maxChars = (eventDataSize - SymbolicLinkOffset) / sizeof(char);
        var buffer = Marshal.PtrToStringUni(symbolicLinkPtr, maxChars);
        var nulIndex = buffer.IndexOf('\0');
        var devicePath = nulIndex >= 0 ? buffer[..nulIndex] : buffer;
        return string.IsNullOrEmpty(devicePath) ? null : devicePath;
    }

    private static HidDeviceRescanHint CreateHint(HidDeviceChangeKind changeKind, string devicePath) =>
        new(changeKind, NormalizeSymbolicLink(devicePath), devicePath);

    private static string NormalizeSymbolicLink(string devicePath) =>
        devicePath.ToUpperInvariant();

    private void ClearRegistrationState()
    {
        if (_marshalableThisPtr.IsAllocated)
        {
            _marshalableThisPtr.Free();
        }

        _callbackDelegate = null;
        _notificationHandle = IntPtr.Zero;
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
            if (_notificationHandle != IntPtr.Zero)
            {
                _ = NativeMethods.CM_Unregister_Notification(_notificationHandle);
                _notificationHandle = IntPtr.Zero;
            }

            if (_marshalableThisPtr.IsAllocated)
            {
                _marshalableThisPtr.Free();
            }
        }

        base.Dispose(disposing);
    }

    ~WindowsHidDeviceListener()
    {
        Dispose(disposing: false);
    }
}