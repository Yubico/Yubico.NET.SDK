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

using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
///     The macOS twins of the Windows constructor-leak bug fixed in <c>bbf07e8e</c>, plus the CoreFoundation
///     release discipline neither macOS connection had.
/// </summary>
/// <remarks>
///     <para>
///         Two distinct defects are pinned here. First, both connections call <c>SetupConnection()</c> bare in
///         the constructor, so a failure leaves an already-created <c>IOHIDDeviceRef</c> — and, for the IO
///         connection, an already-created run-loop mode <c>CFStringRef</c> — with no owner able to release
///         them. Second, neither connection ever released those CoreFoundation objects even on the success
///         path: closing a device with <c>IOHIDDeviceClose</c> is not the same as releasing it.
///     </para>
///     <para>
///         The finalizer pin is the sharpest of the set. Both types declare a finalizer, so a constructor that
///         throws still produces a finalizable object; the IO connection's disposal then registers callbacks
///         against whatever <c>_deviceHandle</c> holds. When creation is what failed, that value is
///         <c>IntPtr.Zero</c>, and passing a NULL <c>IOHIDDeviceRef</c> to IOKit on the finalizer thread risks
///         taking the process down rather than merely leaking.
///     </para>
///     <para>
///         The IOKit layer is faked through <see cref="IIOKitDeviceLifetime" />, so these run on every platform
///         and need no macOS hardware.
///     </para>
/// </remarks>
public class MacOSHidConnectionLifetimeTests
{
    private const long EntryId = 4242;

    [Fact]
    public void FeatureReportConnection_WhenOpenThrows_ReleasesTheDevice()
    {
        var lifetime = new RecordingLifetime { ThrowOnOpen = true };

        _ = Assert.Throws<PlatformApiException>(() => new MacOSHidFeatureReportConnection(EntryId, lifetime));

        Assert.Contains(
            lifetime.CreatedDevice,
            lifetime.Released);
    }

    [Fact]
    public void IOReportConnection_WhenOpenThrows_ReleasesTheDeviceAndTheRunLoopMode()
    {
        var lifetime = new RecordingLifetime { ThrowOnOpen = true };

        _ = Assert.Throws<PlatformApiException>(() => new MacOSHidIOReportConnection(EntryId, lifetime));

        Assert.Contains(lifetime.CreatedDevice, lifetime.Released);
        Assert.Contains(lifetime.CreatedRunLoopMode, lifetime.Released);
    }

    /// <summary>
    ///     The earliest failure point: nothing to close, but the run-loop mode string was already created and
    ///     is owned by no one once the constructor unwinds.
    /// </summary>
    [Fact]
    public void IOReportConnection_WhenCreateThrows_ReleasesTheRunLoopMode()
    {
        var lifetime = new RecordingLifetime { ThrowOnCreate = true };

        _ = Assert.Throws<PlatformApiException>(() => new MacOSHidIOReportConnection(EntryId, lifetime));

        Assert.Contains(lifetime.CreatedRunLoopMode, lifetime.Released);
    }

    /// <summary>
    ///     The crash pin. A constructor that fails at device creation leaves <c>_deviceHandle</c> at
    ///     <c>IntPtr.Zero</c>; the finalizer must not hand that to IOKit.
    /// </summary>
    [Fact]
    public void IOReportConnection_WhenCreateThrows_NeverRegistersCallbacksOnANullDeviceHandle()
    {
        var lifetime = new RecordingLifetime { ThrowOnCreate = true };

        _ = Assert.Throws<PlatformApiException>(() => new MacOSHidIOReportConnection(EntryId, lifetime));

        // Force the finalizer of the abandoned, partially-constructed instance to run.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(
            lifetime.RegisteredCallbackOnNullDevice,
            "a NULL IOHIDDeviceRef was passed to IOKit callback registration on the finalizer thread");
    }

    [Fact]
    public void FeatureReportConnection_WhenDisposed_ReleasesTheDevice()
    {
        var lifetime = new RecordingLifetime();
        var connection = new MacOSHidFeatureReportConnection(EntryId, lifetime);

        connection.Dispose();

        Assert.Contains(lifetime.CreatedDevice, lifetime.Closed);
        Assert.Contains(lifetime.CreatedDevice, lifetime.Released);
    }

    [Fact]
    public void IOReportConnection_WhenDisposed_ReleasesTheDeviceAndTheRunLoopMode()
    {
        var lifetime = new RecordingLifetime();
        var connection = new MacOSHidIOReportConnection(EntryId, lifetime);

        connection.Dispose();

        Assert.Contains(lifetime.CreatedDevice, lifetime.Closed);
        Assert.Contains(lifetime.CreatedDevice, lifetime.Released);
        Assert.Contains(lifetime.CreatedRunLoopMode, lifetime.Released);
    }

    /// <summary>
    ///     Releasing twice is as much a defect as never releasing: CoreFoundation over-release corrupts the
    ///     retain count of an object that may already have been handed to another owner.
    /// </summary>
    [Fact]
    public void IOReportConnection_WhenDisposedTwice_ReleasesEachObjectOnlyOnce()
    {
        var lifetime = new RecordingLifetime();
        var connection = new MacOSHidIOReportConnection(EntryId, lifetime);

        connection.Dispose();
        connection.Dispose();

        Assert.Single(lifetime.Released, handle => handle == lifetime.CreatedDevice);
        Assert.Single(lifetime.Released, handle => handle == lifetime.CreatedRunLoopMode);
    }

    private sealed class RecordingLifetime : IIOKitDeviceLifetime
    {
        private nint _nextHandle = 0x1000;

        public bool ThrowOnCreate { get; init; }
        public bool ThrowOnOpen { get; init; }

        public nint CreatedDevice { get; private set; }
        public nint CreatedRunLoopMode { get; private set; }

        public List<nint> Released { get; } = [];
        public List<nint> Closed { get; } = [];
        public bool RegisteredCallbackOnNullDevice { get; private set; }

        public nint CreateDevice(long entryId)
        {
            if (ThrowOnCreate) throw new PlatformApiException("simulated device creation failure");

            CreatedDevice = _nextHandle++;
            return CreatedDevice;
        }

        public void OpenDevice(nint device)
        {
            if (ThrowOnOpen) throw new PlatformApiException("simulated device open failure");
        }

        public void CloseDevice(nint device) => Closed.Add(device);

        public void ReleaseCFObject(nint cfObject) => Released.Add(cfObject);

        public nint CreateRunLoopMode(string name)
        {
            CreatedRunLoopMode = _nextHandle++;
            return CreatedRunLoopMode;
        }

        public int GetIntProperty(nint device, string propertyName) => 64;

        public void RegisterInputReportCallback(
            nint device,
            byte[] buffer,
            int bufferLength,
            nint callback,
            nint context)
        {
            if (device == IntPtr.Zero) RegisteredCallbackOnNullDevice = true;
        }

        public void RegisterRemovalCallback(nint device, nint callback, nint context)
        {
            if (device == IntPtr.Zero) RegisteredCallbackOnNullDevice = true;
        }
    }
}