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
using Yubico.YubiKit.Core.PlatformInterop;
using Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework;
using CFNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
///     macOS implementation of the FIDO IO report connection.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidIOReportConnection : IHidConnectionSync
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

        if (_reportsQueue.TryDequeue(out var report)) return report;

        var runLoop = CFNativeMethods.CFRunLoopGetCurrent();

        IOKitNativeMethods.IOHIDDeviceScheduleWithRunLoop(_deviceHandle, runLoop, _loopId);

        var runLoopResult = CFNativeMethods.CFRunLoopRunInMode(_loopId, 6, true);

        if (runLoopResult != CFNativeMethods.kCFRunLoopRunHandledSource)
            throw new PlatformApiException($"RunLoop returned unexpected result: {runLoopResult}");

        IOKitNativeMethods.IOHIDDeviceUnscheduleFromRunLoop(_deviceHandle, runLoop, _loopId);

        _ = _reportsQueue.TryDequeue(out report);

        return report!;
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

            var result = IOKitNativeMethods.IOHIDDeviceOpen(_deviceHandle, 0x01);

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
        if (result != 0 || type != IOKitHidConstants.kIOHidReportTypeInput || reportId != 0 || reportLength < 0) return;

        var reportsQueue = (ConcurrentQueue<byte[]>)GCHandle.FromIntPtr(context).Target!;
        reportsQueue.Enqueue(report);
    }

    private static void RemovalCallback(IntPtr context, int result, IntPtr sender) =>
        CFNativeMethods.CFRunLoopStop(context);

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