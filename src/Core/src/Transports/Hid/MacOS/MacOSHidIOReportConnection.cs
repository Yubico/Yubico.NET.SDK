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

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.MacOS.IOKitFramework;
using CFNativeMethods = Yubico.YubiKit.Core.Native.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.Native.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
///     macOS implementation of the FIDO IO report connection.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidIOReportConnection : IHidConnection
{
    private readonly long _entryId;
    private readonly nint _loopId;
    private readonly byte[] _readBuffer;
    private readonly IOKitNativeMethods.IOHIDCallback _removalDelegate;
    private readonly IOKitNativeMethods.IOHIDReportCallback _reportDelegate;
    private readonly ConcurrentQueue<byte[]> _reportsQueue;

    private nint _deviceHandle;
    private bool _disposed;
    private GCHandle _pinnedReportsQueue;
    private GCHandle _readHandle;

    public MacOSHidIOReportConnection(long entryId)
    {
        _entryId = entryId;

        var cstr = Encoding.UTF8.GetBytes($"fido2-loopid-{entryId}");
        _loopId = CFNativeMethods.CFStringCreateWithCString(IntPtr.Zero, cstr, 0);

        _readBuffer = new byte[64];
        _readHandle = GCHandle.Alloc(_readBuffer, GCHandleType.Pinned);

        _reportsQueue = new ConcurrentQueue<byte[]>();
        _pinnedReportsQueue = GCHandle.Alloc(_reportsQueue);

        _reportDelegate = ReportCallback;
        _removalDelegate = RemovalCallback;

        SetupConnection();

        InputReportSize = IOKitHelpers.GetIntPropertyValue(_deviceHandle, IOKitHidConstants.MaxInputReportSize);
        OutputReportSize = IOKitHelpers.GetIntPropertyValue(_deviceHandle, IOKitHidConstants.MaxOutputReportSize);

    }

    public int InputReportSize { get; }
    public int OutputReportSize { get; }

    public byte[] GetReport()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);


        if (_reportsQueue.TryDequeue(out var report))
        {
            return report;
        }

        var runLoop = CFNativeMethods.CFRunLoopGetCurrent();

        IOKitNativeMethods.IOHIDDeviceScheduleWithRunLoop(_deviceHandle, runLoop, _loopId);

        int runLoopResult;
        try
        {
            runLoopResult = CFNativeMethods.CFRunLoopRunInMode(_loopId, 6, true);
        }
        finally
        {
            // Unschedule on every path. Leaving the device scheduled on a run loop we are no longer
            // draining leaks the source and lets a later read observe this call's callbacks.
            IOKitNativeMethods.IOHIDDeviceUnscheduleFromRunLoop(_deviceHandle, runLoop, _loopId);
        }

        if (runLoopResult != CFNativeMethods.kCFRunLoopRunHandledSource)
            throw new PlatformApiException($"RunLoop returned unexpected result: {runLoopResult}");

        if (!_reportsQueue.TryDequeue(out report))
            throw new InvalidOperationException(
                "Failed to receive HID report: RunLoop completed but no report was queued. " +
                "This may indicate a timing issue with the macOS HID subsystem.");

        return report;
    }

    public void SetReport(byte[] report)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(report);


        var result = IOKitNativeMethods.IOHIDDeviceSetReport(
            _deviceHandle,
            IOKitHidConstants.kIOHidReportTypeOutput,
            0,
            report,
            report.Length);


        if (result != 0)
            throw new PlatformApiException(
                nameof(IOKitNativeMethods.IOHIDDeviceSetReport),
                result,
                "Failed to set HID report.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync() =>
        _disposed
            ? ValueTask.CompletedTask
            : new ValueTask(Task.Run(Dispose));

    public ConnectionType Type { get; } = ConnectionType.Hid;

    private void SetupConnection()
    {
        var deviceEntry = 0;
        try
        {
            var matchingDictionary = IOKitNativeMethods.IORegistryEntryIDMatching((ulong)_entryId);
            deviceEntry = IOKitNativeMethods.IOServiceGetMatchingService(0, matchingDictionary);

            if (deviceEntry == 0)
                throw new PlatformApiException("Failed to find matching device entry in IO registry.");

            _deviceHandle = IOKitNativeMethods.IOHIDDeviceCreate(IntPtr.Zero, deviceEntry);

            if (_deviceHandle == IntPtr.Zero) throw new PlatformApiException("Failed to create HID device handle.");

            // kIOHIDOptionsTypeNone (0), NOT kIOHIDOptionsTypeSeizeDevice (0x01). Seizing makes macOS refuse
            // a second open with kIOReturnExclusiveAccess (0xE00002C5), which contradicts this SDK's
            // "FIDO HID is shared" ownership contract: DeviceConnectionRegistry admits a second FIDO
            // connection, and the platform would then reject it. Both canonical implementations open
            // non-seizing on macOS — Rust yubikit enables hidapi's `macos-shared-device`
            // (hid_darwin_set_open_exclusive(0) => kIOHIDOptionsTypeNone), and python-fido2's macOS backend
            // calls IOHIDDeviceOpen(handle, 0). The OTP feature-report path here already uses 0, so this
            // also makes the two macOS HID paths consistent.
            var result = IOKitNativeMethods.IOHIDDeviceOpen(_deviceHandle, 0);

            if (result != 0)
                throw new PlatformApiException(
                    nameof(IOKitNativeMethods.IOHIDDeviceOpen),
                    result,
                    "Failed to open HID device.");

            var reportCallback = Marshal.GetFunctionPointerForDelegate(_reportDelegate);
            IOKitNativeMethods.IOHIDDeviceRegisterInputReportCallback(
                _deviceHandle,
                _readBuffer,
                _readBuffer.Length,
                reportCallback,
                GCHandle.ToIntPtr(_pinnedReportsQueue));

            var callback = Marshal.GetFunctionPointerForDelegate(_removalDelegate);
            IOKitNativeMethods.IOHIDDeviceRegisterRemovalCallback(_deviceHandle, callback, _deviceHandle);
        }
        finally
        {
            if (deviceEntry != 0) _ = IOKitNativeMethods.IOObjectRelease(deviceEntry);
        }
    }

    private static void ReportCallback(
        IntPtr context,
        int result,
        IntPtr sender,
        int type,
        int reportId,
        byte[] report,
        long reportLength)
    {

        if (result != 0 || type != IOKitHidConstants.kIOHidReportTypeInput || reportId != 0 || reportLength < 0)
        {
            return;
        }

        var reportsQueue = (ConcurrentQueue<byte[]>)GCHandle.FromIntPtr(context).Target!;
        reportsQueue.Enqueue(report);
    }

    /// <summary>
    ///     Invoked by IOKit when the device is removed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Deliberately does nothing. It previously called <c>CFRunLoopStop(context)</c>, but the
    ///         context registered for this callback is <c>_deviceHandle</c> — an <c>IOHIDDeviceRef</c>, not
    ///         a <c>CFRunLoopRef</c>. Passing it to <c>CFRunLoopStop</c> is undefined behaviour, and it
    ///         never woke the blocked read either, because the run loop it should stop is the one captured
    ///         inside <see cref="GetReport" /> and is not reachable from here.
    ///     </para>
    ///     <para>
    ///         Removing the call eliminates the undefined behaviour without changing observable behaviour:
    ///         a read in progress during removal already ran to its <c>CFRunLoopRunInMode</c> timeout, and
    ///         still does. Waking the read promptly on removal is a real improvement, but it needs the
    ///         scheduled run loop plumbed through as the callback context and hardware unplug testing to
    ///         verify, so it is left as follow-up rather than changed blind.
    ///     </para>
    /// </remarks>
    private static void RemovalCallback(IntPtr context, int result, IntPtr sender)
    {
        // Intentionally empty — see remarks.
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        IOKitNativeMethods.IOHIDDeviceRegisterInputReportCallback(
            _deviceHandle,
            _readBuffer,
            _readBuffer.Length,
            IntPtr.Zero,
            IntPtr.Zero);

        IOKitNativeMethods.IOHIDDeviceRegisterRemovalCallback(_deviceHandle, IntPtr.Zero, IntPtr.Zero);

        if (_readHandle.IsAllocated) _readHandle.Free();

        if (_pinnedReportsQueue.IsAllocated) _pinnedReportsQueue.Free();

        if (_deviceHandle != IntPtr.Zero)
        {
            _ = IOKitNativeMethods.IOHIDDeviceClose(_deviceHandle, 0);
            _deviceHandle = IntPtr.Zero;
        }

        _disposed = true;
    }

    ~MacOSHidIOReportConnection()
    {
        Dispose(false);
    }
}