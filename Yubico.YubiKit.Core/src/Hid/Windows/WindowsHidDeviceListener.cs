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

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.PlatformInterop.Windows.Cfgmgr32;

namespace Yubico.YubiKit.Core.Hid.Windows;

/// <summary>
/// Windows implementation of HID device listener using CM_Register_Notification.
/// </summary>
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

    private GCHandle _marshalableThisPtr;
    private NativeMethods.CM_NOTIFY_CALLBACK? _callbackDelegate;
    private IntPtr _notificationHandle;
    private bool _disposed;

    public WindowsHidDeviceListener()
    {
        StartListening();
    }

    private void StartListening()
    {
        try
        {
            // Keep callback delegate alive for the duration of the listener
            _callbackDelegate = NotificationCallback;

            // Pin 'this' so we can pass it to the callback
            _marshalableThisPtr = GCHandle.Alloc(this);

            // Build the notification filter for HID device interfaces
            var filterSize = Marshal.SizeOf<NativeMethods.CM_NOTIFY_FILTER>();
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
            return 0;
        }

        try
        {
            listener.HandleNotification(action, eventData);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Exception in HID notification callback");
        }

        return 0; // ERROR_SUCCESS
    }

    private void HandleNotification(NativeMethods.CM_NOTIFY_ACTION action, IntPtr eventData)
    {
        switch (action)
        {
            case NativeMethods.CM_NOTIFY_ACTION.DEVICEINTERFACEARRIVAL:
                HandleDeviceArrival(eventData);
                break;
            case NativeMethods.CM_NOTIFY_ACTION.DEVICEINTERFACEREMOVAL:
                HandleDeviceRemoval();
                break;
        }
    }

    private void HandleDeviceArrival(IntPtr eventData)
    {
        if (eventData == IntPtr.Zero)
        {
            return;
        }

        try
        {
            // The symbolic link (device path) starts at offset 24 in the event data
            var symbolicLinkPtr = IntPtr.Add(eventData, SymbolicLinkOffset);
            var devicePath = Marshal.PtrToStringUni(symbolicLinkPtr);

            if (string.IsNullOrEmpty(devicePath))
            {
                return;
            }

            Logger.LogDebug("HID device arrived: {DevicePath}", devicePath);
            OnDeviceEvent();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process device arrival");
        }
    }

    private void HandleDeviceRemoval()
    {
        OnDeviceEvent();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;

        // Unregister the notification
        if (_notificationHandle != IntPtr.Zero)
        {
            _ = NativeMethods.CM_Unregister_Notification(_notificationHandle);
            _notificationHandle = IntPtr.Zero;
        }

        // Free the GCHandle
        if (_marshalableThisPtr.IsAllocated)
        {
            _marshalableThisPtr.Free();
        }

        _callbackDelegate = null;

        base.Dispose(disposing);
    }

    ~WindowsHidDeviceListener()
    {
        Dispose(disposing: false);
    }
}
