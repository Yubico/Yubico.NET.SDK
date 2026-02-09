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
using Yubico.YubiKit.Core.PlatformInterop.Linux.Libc;
using Yubico.YubiKit.Core.PlatformInterop.Linux.Udev;
using LibcNativeMethods = Yubico.YubiKit.Core.PlatformInterop.Linux.Libc.NativeMethods;
using UdevNativeMethods = Yubico.YubiKit.Core.PlatformInterop.Linux.Udev.NativeMethods;

namespace Yubico.YubiKit.Core.Hid.Linux;

/// <summary>
/// Linux implementation of HID device listener using udev_monitor with poll().
/// </summary>
internal sealed class LinuxHidDeviceListener : HidDeviceListener
{
    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<LinuxHidDeviceListener>();

    private LinuxUdevSafeHandle? _udevHandle;
    private LinuxUdevMonitorSafeHandle? _monitorHandle;
    private Thread? _listenerThread;
    private volatile bool _shouldStop;
    private bool _disposed;

    public LinuxHidDeviceListener()
    {
        StartListening();
    }

    private void StartListening()
    {
        try
        {
            // Create udev context
            _udevHandle = UdevNativeMethods.udev_new();
            if (_udevHandle.IsInvalid)
            {
                Logger.LogWarning("Failed to create udev context");
                Status = DeviceListenerStatus.Error;
                return;
            }

            // Create monitor for netlink events
            _monitorHandle = UdevNativeMethods.udev_monitor_new_from_netlink(_udevHandle, UdevNativeMethods.UdevMonitorName);
            if (_monitorHandle.IsInvalid)
            {
                Logger.LogWarning("Failed to create udev monitor");
                Status = DeviceListenerStatus.Error;
                return;
            }

            // Filter for hidraw subsystem
            var filterResult = UdevNativeMethods.udev_monitor_filter_add_match_subsystem_devtype(
                _monitorHandle,
                UdevNativeMethods.UdevSubsystemName,
                null);
            
            if (filterResult < 0)
            {
                Logger.LogWarning("Failed to add udev filter: {Result}", filterResult);
                Status = DeviceListenerStatus.Error;
                return;
            }

            // Enable receiving
            var enableResult = UdevNativeMethods.udev_monitor_enable_receiving(_monitorHandle);
            if (enableResult < 0)
            {
                Logger.LogWarning("Failed to enable udev receiving: {Result}", enableResult);
                Status = DeviceListenerStatus.Error;
                return;
            }

            // Start the listener thread
            _listenerThread = new Thread(ListenerThreadProc)
            {
                Name = "LinuxHidDeviceListener",
                IsBackground = true
            };
            _listenerThread.Start();

            Status = DeviceListenerStatus.Started;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start Linux HID listener");
            Status = DeviceListenerStatus.Error;
        }
    }

    private void ListenerThreadProc()
    {
        if (_monitorHandle is null || _monitorHandle.IsInvalid)
        {
            return;
        }

        try
        {
            // Get the file descriptor for the monitor
            var fd = UdevNativeMethods.udev_monitor_get_fd(_monitorHandle);
            if (fd == IntPtr.Zero || fd.ToInt32() < 0)
            {
                Logger.LogWarning("Failed to get udev monitor fd");
                Status = DeviceListenerStatus.Error;
                return;
            }

            var pollFds = new LibcNativeMethods.PollFd[1];
            pollFds[0].fd = fd.ToInt32();
            pollFds[0].events = (short)(LibcNativeMethods.POLLIN | LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP);

            while (!_shouldStop)
            {
                // Poll with timeout
                var pollResult = LibcNativeMethods.poll(pollFds, 1, (int)CheckForChangesWaitTime.TotalMilliseconds);

                if (_shouldStop)
                {
                    break;
                }

                if (pollResult < 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    // EINTR (4) is normal for interrupted system calls
                    if (error == 4)
                    {
                        continue;
                    }
                    
                    Logger.LogWarning("poll() failed with error: {Error}", error);
                    continue;
                }

                if (pollResult == 0)
                {
                    // Timeout, continue polling
                    continue;
                }

                // Check if there's data to read
                if ((pollFds[0].revents & LibcNativeMethods.POLLIN) != 0)
                {
                    ProcessUdevEvent();
                }

                // Check for errors
                if ((pollFds[0].revents & (LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP)) != 0)
                {
                    Logger.LogWarning("poll() reported error or hangup");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (!_shouldStop)
            {
                Logger.LogError(ex, "Linux HID listener thread encountered an error");
                Status = DeviceListenerStatus.Error;
            }
        }
    }

    private void ProcessUdevEvent()
    {
        if (_monitorHandle is null || _monitorHandle.IsInvalid)
        {
            return;
        }

        using var device = UdevNativeMethods.udev_monitor_receive_device(_monitorHandle);
        if (device.IsInvalid)
        {
            return;
        }

        // Get the action
        var actionPtr = UdevNativeMethods.udev_device_get_action(device);
        if (actionPtr == IntPtr.Zero)
        {
            return;
        }

        var action = Marshal.PtrToStringAnsi(actionPtr);

        switch (action)
        {
            case "add":
                HandleDeviceAdd(device);
                break;
            case "remove":
                OnRemoved(NullDevice.Instance);
                break;
        }
    }

    private void HandleDeviceAdd(LinuxUdevDeviceSafeHandle device)
    {
        try
        {
            // Get the device node path (e.g., /dev/hidraw0)
            var devNodePtr = UdevNativeMethods.udev_device_get_devnode(device);
            if (devNodePtr == IntPtr.Zero)
            {
                return;
            }

            var devNode = Marshal.PtrToStringAnsi(devNodePtr);
            if (string.IsNullOrEmpty(devNode))
            {
                return;
            }

            // Create a HID device from the devnode
            // Note: Full device creation would require opening the device and reading descriptor
            Logger.LogDebug("Device added: {DevNode}", devNode);
            
            // For now, use NullDevice - full device creation requires descriptor parsing
            // which is done in LinuxHidDevice.GetList()
            // TODO: Create proper LinuxHidDevice from udev device
            OnArrived(NullDevice.Instance);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process device add event");
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
        _shouldStop = true;

        // Wait for the listener thread to exit
        if (_listenerThread is not null && _listenerThread.IsAlive)
        {
            if (!_listenerThread.Join(MaxDisposalWaitTime))
            {
                Logger.LogWarning("Linux HID listener thread did not exit within timeout");
            }
        }

        _listenerThread = null;

        // Cleanup udev resources
        _monitorHandle?.Dispose();
        _monitorHandle = null;

        _udevHandle?.Dispose();
        _udevHandle = null;

        base.Dispose(disposing);
    }

    ~LinuxHidDeviceListener()
    {
        Dispose(disposing: false);
    }
}
