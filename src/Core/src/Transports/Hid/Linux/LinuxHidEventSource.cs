// Copyright 2026 Yubico AB
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
/// Outcome of a single blocking wait on the Linux HID event source.
/// </summary>
internal enum LinuxHidPollOutcome
{
    /// <summary>No actionable outcome (EINTR or spurious wake); poll again.</summary>
    Retry = 0,

    /// <summary>The udev monitor has at least one event ready to receive.</summary>
    Event,

    /// <summary>The shutdown event fd was signaled; exit the loop cleanly.</summary>
    ShutdownSignaled,

    /// <summary>The shutdown event fd reported an error condition; the loop cannot be woken reliably.</summary>
    ShutdownFdError,

    /// <summary>The udev monitor fd reported an error condition; events can no longer be received.</summary>
    MonitorFdError,

    /// <summary>poll() failed with a non-EINTR error.</summary>
    PollFailed
}

/// <summary>
/// A udev event received from the Linux HID event source, reduced to the fields the
/// listener needs to construct a <see cref="HidDeviceRescanHint"/>.
/// </summary>
/// <param name="Action">The udev action string ("add", "remove", ...), or null when unavailable.</param>
/// <param name="StableIdentity">The stable udev/sysfs identity (parent HID device syspath), or null.</param>
/// <param name="DevNode">The /dev/hidrawN node path, diagnostic only, or null.</param>
internal sealed record LinuxHidUdevEvent(string? Action, string? StableIdentity, string? DevNode);

/// <summary>
/// Abstraction over the native udev monitor, poll(), and shutdown eventfd used by
/// <see cref="LinuxHidDeviceListener"/>. Enables no-hardware fault-injection tests of the
/// listener's loop policy (bounded exit, error transitions, hint emission).
/// </summary>
/// <remarks>
/// Ownership: the listener session's thread owns the source and disposes it when the loop
/// exits. <see cref="SignalShutdown"/> may be called concurrently from other threads and
/// must tolerate a source that is concurrently disposing.
/// </remarks>
internal interface ILinuxHidEventSource : IDisposable
{
    /// <summary>
    /// Creates the udev context, monitor (hidraw subsystem filter, receiving enabled), and
    /// shutdown event fd. Returns false when any native step fails; the caller disposes the
    /// source in that case.
    /// </summary>
    bool Initialize();

    /// <summary>
    /// Blocks until the monitor has an event, the shutdown fd is signaled, or an error occurs.
    /// </summary>
    LinuxHidPollOutcome WaitForEvent();

    /// <summary>
    /// Receives one pending udev event. Returns null when the receive fails — which is most
    /// likely during event storms (for example ENOBUFS after the kernel dropped notifications),
    /// exactly when a removal may have been lost.
    /// </summary>
    LinuxHidUdevEvent? ReceiveEvent();

    /// <summary>
    /// Wakes a thread blocked in <see cref="WaitForEvent"/> so it can observe cancellation.
    /// Safe to call from any thread, including concurrently with <see cref="IDisposable.Dispose"/>.
    /// </summary>
    void SignalShutdown();

    /// <summary>
    /// Returns true when the hidraw device node exists and can be opened, indicating the node
    /// is ready for enumeration by discovery rescans.
    /// </summary>
    bool IsHidrawReady(string? devNode);
}

/// <summary>
/// Production <see cref="ILinuxHidEventSource"/> backed by libudev and libc P/Invoke.
/// </summary>
internal sealed class LinuxUdevHidEventSource : ILinuxHidEventSource
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<LinuxUdevHidEventSource>();

    // eventfd writes are nonblocking. Four total attempts tolerate a short interruption burst
    // while guaranteeing Stop cannot hot-loop forever under persistent EINTR.
    private const int MaxShutdownWriteAttempts = 4;

    private LinuxUdevSafeHandle? _udevHandle;
    private LinuxUdevMonitorSafeHandle? _monitorHandle;
    private LinuxEventFdSafeHandle? _shutdownEventHandle;
    private LibcNativeMethods.PollFd[]? _pollFds;
    private bool _disposed;

    /// <inheritdoc />
    public bool Initialize()
    {
        _udevHandle = UdevNativeMethods.udev_new();
        if (_udevHandle.IsInvalid)
        {
            Logger.LogWarning("Failed to create udev context");
            return false;
        }

        _monitorHandle = UdevNativeMethods.udev_monitor_new_from_netlink(_udevHandle, UdevNativeMethods.UdevMonitorName);
        if (_monitorHandle.IsInvalid)
        {
            Logger.LogWarning("Failed to create udev monitor");
            return false;
        }

        var filterResult = UdevNativeMethods.udev_monitor_filter_add_match_subsystem_devtype(
            _monitorHandle,
            UdevNativeMethods.UdevSubsystemName,
            null);
        if (filterResult < 0)
        {
            Logger.LogWarning("Failed to add udev filter: {Result}", filterResult);
            return false;
        }

        var enableResult = UdevNativeMethods.udev_monitor_enable_receiving(_monitorHandle);
        if (enableResult < 0)
        {
            Logger.LogWarning("Failed to enable udev receiving: {Result}", enableResult);
            return false;
        }

        _shutdownEventHandle = LibcNativeMethods.eventfd(
            0,
            LibcNativeMethods.EFD_CLOEXEC | LibcNativeMethods.EFD_NONBLOCK);
        if (_shutdownEventHandle.IsInvalid)
        {
            Logger.LogWarning(
                "Failed to create Linux HID listener shutdown event fd: {Error}",
                Marshal.GetLastWin32Error());
            return false;
        }

        var monitorFd = UdevNativeMethods.udev_monitor_get_fd(_monitorHandle);
        if (!IsValidFileDescriptor(monitorFd))
        {
            Logger.LogWarning("Failed to get udev monitor fd");
            return false;
        }

        var shutdownFd = _shutdownEventHandle.DangerousGetHandle().ToInt32();
        if (shutdownFd < 0)
        {
            Logger.LogWarning("Invalid Linux HID listener shutdown fd");
            return false;
        }

        var pollFds = new LibcNativeMethods.PollFd[2];
        pollFds[0].fd = monitorFd;
        pollFds[0].events = (short)(LibcNativeMethods.POLLIN | LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP);
        pollFds[1].fd = shutdownFd;
        pollFds[1].events = (short)(LibcNativeMethods.POLLIN | LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP);
        _pollFds = pollFds;

        return true;
    }

    /// <inheritdoc />
    public LinuxHidPollOutcome WaitForEvent()
    {
        var pollFds = _pollFds ?? throw new InvalidOperationException("The event source is not initialized.");

        var pollResult = LibcNativeMethods.poll(pollFds, pollFds.Length, -1);
        if (pollResult < 0)
        {
            var error = Marshal.GetLastWin32Error();
            if (error == LibcNativeMethods.EINTR)
            {
                return LinuxHidPollOutcome.Retry;
            }

            Logger.LogWarning("poll() failed with error: {Error}", error);
            return LinuxHidPollOutcome.PollFailed;
        }

        if ((pollFds[1].revents & LibcNativeMethods.POLLIN) != 0)
        {
            DrainShutdownEvent();
            return LinuxHidPollOutcome.ShutdownSignaled;
        }

        if ((pollFds[1].revents & (LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP | LibcNativeMethods.POLLNVAL)) != 0)
        {
            Logger.LogWarning("Linux HID listener shutdown fd reported error: {Revents}", pollFds[1].revents);
            return LinuxHidPollOutcome.ShutdownFdError;
        }

        if ((pollFds[0].revents & LibcNativeMethods.POLLIN) != 0)
        {
            return LinuxHidPollOutcome.Event;
        }

        if ((pollFds[0].revents & (LibcNativeMethods.POLLERR | LibcNativeMethods.POLLHUP | LibcNativeMethods.POLLNVAL)) != 0)
        {
            Logger.LogWarning("udev monitor fd reported error: {Revents}", pollFds[0].revents);
            return LinuxHidPollOutcome.MonitorFdError;
        }

        return LinuxHidPollOutcome.Retry;
    }

    /// <inheritdoc />
    public LinuxHidUdevEvent? ReceiveEvent()
    {
        if (_monitorHandle is null || _monitorHandle.IsInvalid)
        {
            return null;
        }

        using var device = UdevNativeMethods.udev_monitor_receive_device(_monitorHandle);
        if (device.IsInvalid)
        {
            return null;
        }

        var action = PtrToString(UdevNativeMethods.udev_device_get_action(device));
        var stableIdentity = GetStableUdevIdentity(device);
        var devNode = PtrToString(UdevNativeMethods.udev_device_get_devnode(device));

        return new LinuxHidUdevEvent(action, stableIdentity, devNode);
    }

    /// <inheritdoc />
    public void SignalShutdown()
    {
        var handle = _shutdownEventHandle;
        if (handle is null || handle.IsInvalid)
        {
            return;
        }

        try
        {
            var signal = BitConverter.GetBytes(1UL);
            WriteShutdownSignal(
                () => LibcNativeMethods.write(handle, signal, signal.Length),
                Marshal.GetLastWin32Error,
                (result, error) =>
                {
                    if (result < 0)
                    {
                        Logger.LogDebug("Failed to signal Linux HID listener shutdown fd: {Error}", error);
                    }
                    else
                    {
                        Logger.LogDebug(
                            "Failed to signal Linux HID listener shutdown fd: wrote {BytesWritten} bytes instead of {ExpectedBytes}",
                            result,
                            sizeof(ulong));
                    }
                });
        }
        catch (ObjectDisposedException)
        {
            // The owning thread disposed the source while exiting — nothing left to wake.
        }
    }

    /// <inheritdoc />
    public bool IsHidrawReady(string? devNode)
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pollFds = null;

        _shutdownEventHandle?.Dispose();
        _shutdownEventHandle = null;

        _monitorHandle?.Dispose();
        _monitorHandle = null;

        _udevHandle?.Dispose();
        _udevHandle = null;
    }

    /// <summary>
    /// Trims the hidraw-specific suffix from a hidraw syspath so the remaining prefix
    /// identifies the parent HID device stably across hidraw node renumbering.
    /// </summary>
    internal static string TrimHidrawSyspath(string syspath)
    {
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

    internal static bool IsValidFileDescriptor(int fileDescriptor) => fileDescriptor >= 0;

    internal static void WriteShutdownSignal(
        Func<int> write,
        Func<int> getLastError,
        Action<int, int> reportFailure)
    {
        for (var attempt = 1; attempt <= MaxShutdownWriteAttempts; attempt++)
        {
            var result = write();
            if (result == sizeof(ulong))
            {
                return;
            }

            if (result >= 0)
            {
                reportFailure(result, 0);
                return;
            }

            var error = getLastError();
            if (error == LibcNativeMethods.EINTR)
            {
                if (attempt < MaxShutdownWriteAttempts)
                {
                    continue;
                }

                reportFailure(result, error);
                return;
            }

            if (error != LibcNativeMethods.EAGAIN)
            {
                reportFailure(result, error);
            }

            return;
        }
    }

    private void DrainShutdownEvent()
    {
        var handle = _shutdownEventHandle;
        if (handle is null || handle.IsInvalid)
        {
            return;
        }

        var buffer = new byte[sizeof(ulong)];
        while (true)
        {
            var result = LibcNativeMethods.read(handle, buffer, buffer.Length);
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

    private static string? GetStableUdevIdentity(LinuxUdevDeviceSafeHandle device)
    {
        var parent = UdevNativeMethods.udev_device_get_parent(device);
        var syspath = parent == IntPtr.Zero
            ? PtrToString(UdevNativeMethods.udev_device_get_syspath(device))
            : PtrToString(UdevNativeMethods.udev_device_get_syspath(parent));

        return string.IsNullOrEmpty(syspath) ? null : TrimHidrawSyspath(syspath);
    }

    private static string? PtrToString(IntPtr value) =>
        value == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(value);
}