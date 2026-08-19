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
    private readonly IIOKitDeviceLifetime _lifetime;
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
        : this(entryId, IOKitDeviceLifetime.Instance)
    {
    }

    /// <summary>
    ///     Test seam. Lets the constructor-failure and disposal paths be exercised without macOS hardware.
    /// </summary>
    internal MacOSHidIOReportConnection(long entryId, IIOKitDeviceLifetime lifetime)
    {
        _entryId = entryId;
        _lifetime = lifetime;

        _readBuffer = new byte[64];
        _reportsQueue = new ConcurrentQueue<byte[]>();

        _reportDelegate = ReportCallback;
        _removalDelegate = RemovalCallback;

        // Everything from here acquires something that must be handed back. A throw past this point would
        // otherwise strand a CFStringRef, two GCHandles, and an IOHIDDeviceRef with no owner: the object
        // never finishes construction, so the caller has nothing to dispose.
        try
        {
            _loopId = _lifetime.CreateRunLoopMode($"fido2-loopid-{entryId}");

            _readHandle = GCHandle.Alloc(_readBuffer, GCHandleType.Pinned);
            _pinnedReportsQueue = GCHandle.Alloc(_reportsQueue);

            SetupConnection();

            InputReportSize = _lifetime.GetIntProperty(_deviceHandle, IOKitHidConstants.MaxInputReportSize);
            OutputReportSize = _lifetime.GetIntProperty(_deviceHandle, IOKitHidConstants.MaxOutputReportSize);
        }
        catch
        {
            Dispose(false);
            GC.SuppressFinalize(this);
            throw;
        }
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
        _deviceHandle = _lifetime.CreateDevice(_entryId);
        _lifetime.OpenDevice(_deviceHandle);

        var reportCallback = Marshal.GetFunctionPointerForDelegate(_reportDelegate);
        _lifetime.RegisterInputReportCallback(
            _deviceHandle,
            _readBuffer,
            _readBuffer.Length,
            reportCallback,
            GCHandle.ToIntPtr(_pinnedReportsQueue));

        var callback = Marshal.GetFunctionPointerForDelegate(_removalDelegate);
        _lifetime.RegisterRemovalCallback(_deviceHandle, callback, _deviceHandle);
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
        // Set first: this runs from the failing-constructor path as well as from Dispose and the finalizer,
        // and every CoreFoundation object below must be released exactly once. Over-releasing corrupts a
        // retain count just as surely as never releasing leaks.
        if (_disposed) return;
        _disposed = true;

        // Guard the handle. A constructor that failed at device creation leaves this zero, and the finalizer
        // still runs on the partially-constructed object. Handing a NULL IOHIDDeviceRef to IOKit is undefined
        // behaviour on the finalizer thread, not a harmless no-op.
        if (_deviceHandle != IntPtr.Zero)
        {
            _lifetime.RegisterInputReportCallback(
                _deviceHandle,
                _readBuffer,
                _readBuffer.Length,
                IntPtr.Zero,
                IntPtr.Zero);

            _lifetime.RegisterRemovalCallback(_deviceHandle, IntPtr.Zero, IntPtr.Zero);

            _lifetime.CloseDevice(_deviceHandle);

            // IOHIDDeviceCreate returns a retained CoreFoundation object. Closing it is not releasing it.
            _lifetime.ReleaseCFObject(_deviceHandle);
            _deviceHandle = IntPtr.Zero;
        }

        // Free the GCHandles only AFTER the device is unregistered and closed above, never before.
        // _pinnedReportsQueue is the context IOKit hands back to ReportCallback, which dereferences it; a
        // callback in flight against a freed handle is a use-after-free at the native boundary rather than
        // a managed exception. Ordering is the whole mitigation here — the device is only scheduled on a
        // run loop for the duration of GetReport, so outside that window no callback can be dispatched, and
        // by the time these run the device is closed. Do not hoist these above the block above.
        if (_readHandle.IsAllocated) _readHandle.Free();

        if (_pinnedReportsQueue.IsAllocated) _pinnedReportsQueue.Free();

        // CFStringCreateWithCString also returns a retained object, and this one leaked on the success path
        // too: every FIDO connection created one and no path ever gave it back.
        if (_loopId != IntPtr.Zero) _lifetime.ReleaseCFObject(_loopId);
    }

    ~MacOSHidIOReportConnection()
    {
        Dispose(false);
    }
}