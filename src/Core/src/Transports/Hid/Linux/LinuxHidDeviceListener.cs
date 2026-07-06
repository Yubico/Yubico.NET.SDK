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
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Native.Linux.Udev;
using LibcNativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;
using UdevNativeMethods = Yubico.YubiKit.Core.Native.Linux.Udev.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>
/// Linux implementation of HID device listener using udev_monitor with poll().
/// </summary>
/// <remarks>
/// The listener does not auto-start. Call <see cref="Start"/> after setting up <see cref="DeviceEvent"/>
/// callback.
/// </remarks>
internal sealed class LinuxHidDeviceListener : HidDeviceListener
{
    private static readonly TimeSpan HidrawReadinessFallbackDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<LinuxHidDeviceListener>();

    private readonly Lock _syncLock = new();
    private readonly HashSet<string> _knownDeviceIds = new(StringComparer.Ordinal);
    private LinuxUdevSafeHandle? _udevHandle;
    private LinuxUdevMonitorSafeHandle? _monitorHandle;
    private LinuxEventFdSafeHandle? _shutdownEventHandle;
    private Thread? _listenerThread;
    private volatile bool _shouldStop;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance. The listener does not start automatically - call <see cref="Start"/>
    /// after setting up the <see cref="DeviceEvent"/> callback.
    /// </summary>
    public LinuxHidDeviceListener()
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
                // Create udev context
                _udevHandle = UdevNativeMethods.udev_new();
                if (_udevHandle.IsInvalid)
                {
                    Logger.LogWarning("Failed to create udev context");
                    Status = DeviceListenerStatus.Error;
                    ReleaseNativeHandles();
                    return;
                }

                // Create monitor for netlink events
                _monitorHandle = UdevNativeMethods.udev_monitor_new_from_netlink(_udevHandle, UdevNativeMethods.UdevMonitorName);
                if (_monitorHandle.IsInvalid)
                {
                    Logger.LogWarning("Failed to create udev monitor");
                    Status = DeviceListenerStatus.Error;
                    ReleaseNativeHandles();
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
                    ReleaseNativeHandles();
                    return;
                }

                // Enable receiving
                var enableResult = UdevNativeMethods.udev_monitor_enable_receiving(_monitorHandle);
                if (enableResult < 0)
                {
                    Logger.LogWarning("Failed to enable udev receiving: {Result}", enableResult);
                    Status = DeviceListenerStatus.Error;
                    ReleaseNativeHandles();
                    return;
                }

                _shutdownEventHandle = LibcNativeMethods.eventfd(
                    0,
                    LibcNativeMethods.EFD_CLOEXEC | LibcNativeMethods.EFD_NONBLOCK);
                if (_shutdownEventHandle.IsInvalid)
                {
                    Logger.LogWarning("Failed to create Linux HID listener shutdown event fd: {Error}", Marshal.GetLastWin32Error());
                    Status = DeviceListenerStatus.Error;
                    ReleaseNativeHandles();
                    return;
                }

                _shouldStop = false;
                Status = DeviceListenerStatus.Started;

                // Start the listener thread
                _listenerThread = new Thread(ListenerThreadProc)
                {
                    Name = "LinuxHidDeviceListener",
                    IsBackground = true
                };
                _listenerThread.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start Linux HID listener");
                Status = DeviceListenerStatus.Error;
                ReleaseNativeHandles();
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
            SignalShutdownEvent();

            // Wait for the listener thread to exit
            if (_listenerThread is not null && _listenerThread.IsAlive)
            {
                if (!_listenerThread.Join(MaxDisposalWaitTime))
                {
                    Logger.LogWarning("Linux HID listener thread did not exit within timeout");
                }
            }

            _listenerThread = null;

            ReleaseNativeHandles();
            _knownDeviceIds.Clear();

            Status = DeviceListenerStatus.Stopped;
        }
    }

    private void ListenerThreadProc()
    {
        if (_monitorHandle is null || _monitorHandle.IsInvalid || _shutdownEventHandle is null || _shutdownEventHandle.IsInvalid)
        {
            if (!_shouldStop)
            {
                Status = DeviceListenerStatus.Error;
            }

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

            var shutdownFd = _shutdownEventHandle.DangerousGetHandle().ToInt32();
            if (shutdownFd < 0)
            {
                Logger.LogWarning("Invalid Linux HID listener shutdown fd");
                Status = DeviceListenerStatus.Error;
                return;
            }

            var pollFds = new LibcNativeMethods.PollFd[2];
            pollFds[0].fd = fd.ToInt32();
            pollFds[0].events = (short)(LibcNativeMethods.POLLIN | LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP);
            pollFds[1].fd = shutdownFd;
            pollFds[1].events = (short)(LibcNativeMethods.POLLIN | LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP);

            while (!_shouldStop)
            {
                var pollResult = LibcNativeMethods.poll(pollFds, pollFds.Length, -1);

                if (_shouldStop)
                {
                    break;
                }

                if (pollResult < 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == LibcNativeMethods.EINTR)
                    {
                        continue;
                    }

                    Logger.LogWarning("poll() failed with error: {Error}", error);
                    Status = DeviceListenerStatus.Error;
                    break;
                }

                if (pollResult == 0)
                {
                    continue;
                }

                if ((pollFds[1].revents & LibcNativeMethods.POLLIN) != 0)
                {
                    DrainShutdownEvent();
                    break;
                }

                if ((pollFds[1].revents & (LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP | LibcNativeMethods.POLLNVAL)) != 0)
                {
                    Logger.LogWarning("Linux HID listener shutdown fd reported error: {Revents}", pollFds[1].revents);
                    if (!_shouldStop)
                    {
                        Status = DeviceListenerStatus.Error;
                    }

                    break;
                }

                if ((pollFds[0].revents & LibcNativeMethods.POLLIN) != 0)
                {
                    ProcessUdevEvent();
                }

                if ((pollFds[0].revents & (LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP | LibcNativeMethods.POLLNVAL)) != 0)
                {
                    Logger.LogWarning("udev monitor fd reported error: {Revents}", pollFds[0].revents);
                    Status = DeviceListenerStatus.Error;
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
                HandleDeviceRemove(device);
                break;
        }
    }

    private void HandleDeviceAdd(LinuxUdevDeviceSafeHandle device)
    {
        var hint = CreateHint(HidDeviceChangeKind.Added, device);
        if (hint.PlatformDeviceId is not null)
        {
            _knownDeviceIds.Add(hint.PlatformDeviceId);
        }

        OnDeviceEvent(hint);

        if (!IsHidrawReady(hint.DevicePath))
        {
            QueueReadinessFallback(hint);
        }
    }

    private void HandleDeviceRemove(LinuxUdevDeviceSafeHandle device)
    {
        var hint = CreateHint(HidDeviceChangeKind.Removed, device);
        if (hint.PlatformDeviceId is not null)
        {
            _knownDeviceIds.Remove(hint.PlatformDeviceId);
        }

        OnDeviceEvent(hint);
    }

    private HidDeviceRescanHint CreateHint(HidDeviceChangeKind changeKind, LinuxUdevDeviceSafeHandle device)
    {
        var stableIdentity = GetStableUdevIdentity(device);
        var devNode = GetDevNode(device);
        return new HidDeviceRescanHint(changeKind, stableIdentity, devNode);
    }

    private void QueueReadinessFallback(HidDeviceRescanHint hint)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(HidrawReadinessFallbackDelay).ConfigureAwait(false);

                if (_shouldStop || Status != DeviceListenerStatus.Started)
                {
                    return;
                }

                OnDeviceEvent(hint);
            }
            catch (Exception ex)
            {
                Logger.LogTrace(ex, "Ignored delayed hidraw readiness rescan hint");
            }
        });
    }

    private static bool IsHidrawReady(string? devNode)
    {
        if (string.IsNullOrEmpty(devNode) || !File.Exists(devNode))
        {
            return false;
        }

        using var handle = LibcNativeMethods.open(
            devNode,
            LibcNativeMethods.OpenFlags.O_RDONLY | LibcNativeMethods.OpenFlags.O_NONBLOCK);
        return !handle.IsInvalid;
    }

    private static string? GetStableUdevIdentity(LinuxUdevDeviceSafeHandle device)
    {
        var parent = UdevNativeMethods.udev_device_get_parent(device);
        var syspath = parent == IntPtr.Zero
            ? PtrToString(UdevNativeMethods.udev_device_get_syspath(device))
            : PtrToString(UdevNativeMethods.udev_device_get_syspath(parent));

        if (string.IsNullOrEmpty(syspath))
        {
            return null;
        }

        const string hidrawSegment = "/hidraw/";
        var hidrawIndex = syspath.LastIndexOf(hidrawSegment, StringComparison.Ordinal);
        if (hidrawIndex >= 0)
        {
            return syspath[..hidrawIndex];
        }

        const string hidrawDirectorySuffix = "/hidraw";
        return syspath.EndsWith(hidrawDirectorySuffix, StringComparison.Ordinal)
            ? syspath[..^hidrawDirectorySuffix.Length]
            : syspath;
    }

    private static string? GetDevNode(LinuxUdevDeviceSafeHandle device) =>
        PtrToString(UdevNativeMethods.udev_device_get_devnode(device));

    private static string? PtrToString(IntPtr value) =>
        value == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(value);

    private void SignalShutdownEvent()
    {
        if (_shutdownEventHandle is null || _shutdownEventHandle.IsInvalid)
        {
            return;
        }

        var signal = BitConverter.GetBytes(1UL);
        var result = LibcNativeMethods.write(_shutdownEventHandle, signal, signal.Length);
        if (result < 0)
        {
            var error = Marshal.GetLastWin32Error();
            if (error != LibcNativeMethods.EAGAIN)
            {
                Logger.LogDebug("Failed to signal Linux HID listener shutdown fd: {Error}", error);
            }
        }
    }

    private void DrainShutdownEvent()
    {
        if (_shutdownEventHandle is null || _shutdownEventHandle.IsInvalid)
        {
            return;
        }

        var buffer = new byte[sizeof(ulong)];
        while (true)
        {
            var result = LibcNativeMethods.read(_shutdownEventHandle, buffer, buffer.Length);
            if (result > 0)
            {
                continue;
            }

            if (result == 0)
            {
                return;
            }

            var error = Marshal.GetLastWin32Error();
            if (error != LibcNativeMethods.EAGAIN && error != LibcNativeMethods.EINTR)
            {
                Logger.LogDebug("Failed to drain Linux HID listener shutdown fd: {Error}", error);
            }

            return;
        }
    }

    private void ReleaseNativeHandles()
    {
        _shutdownEventHandle?.Dispose();
        _shutdownEventHandle = null;

        _monitorHandle?.Dispose();
        _monitorHandle = null;

        _udevHandle?.Dispose();
        _udevHandle = null;
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
            SignalShutdownEvent();
            ReleaseNativeHandles();
        }

        base.Dispose(disposing);
    }

    ~LinuxHidDeviceListener()
    {
        Dispose(disposing: false);
    }
}