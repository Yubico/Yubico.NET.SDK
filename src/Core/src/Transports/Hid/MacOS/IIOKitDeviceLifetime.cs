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

using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Native;
using CFNativeMethods = Yubico.YubiKit.Core.Native.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.Native.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
///     The IOKit and CoreFoundation calls that govern the <em>lifetime</em> of a macOS HID connection:
///     creating the device, opening and closing it, creating and releasing CoreFoundation objects, and
///     registering or clearing the IOKit callbacks.
/// </summary>
/// <remarks>
///     <para>
///         This is a seam, not an abstraction layer. It exists so the constructor-failure and disposal
///         paths of <see cref="MacOSHidFeatureReportConnection" /> and
///         <see cref="MacOSHidIOReportConnection" /> can be tested without macOS hardware — the same
///         reason <c>IHidDDevice</c> exists on the Windows side. It deliberately mirrors the native calls
///         one-for-one and adds no policy.
///     </para>
///     <para>
///         Report I/O (<c>IOHIDDeviceGetReport</c>/<c>IOHIDDeviceSetReport</c>) and run-loop scheduling are
///         <em>not</em> part of this seam. They never run during construction or disposal, so widening the
///         seam to cover them would add surface without adding coverage.
///     </para>
/// </remarks>
[SupportedOSPlatform("macos")]
internal interface IIOKitDeviceLifetime
{
    /// <summary>
    ///     Resolves an IO registry entry ID to a retained <c>IOHIDDeviceRef</c>.
    /// </summary>
    /// <remarks>
    ///     The returned handle is a CoreFoundation object owned by the caller and must be passed to
    ///     <see cref="ReleaseCFObject" /> when it is no longer needed. Closing it with
    ///     <see cref="CloseDevice" /> is not a substitute for releasing it.
    /// </remarks>
    /// <exception cref="PlatformApiException">
    ///     No registry entry matched, or the device handle could not be created.
    /// </exception>
    nint CreateDevice(long entryId);

    /// <summary>
    ///     Opens the device non-seizing (<c>kIOHIDOptionsTypeNone</c>).
    /// </summary>
    /// <exception cref="PlatformApiException">The open failed.</exception>
    void OpenDevice(nint device);

    /// <summary>
    ///     Closes the device. Does not release it; see <see cref="ReleaseCFObject" />.
    /// </summary>
    void CloseDevice(nint device);

    /// <summary>
    ///     Releases a CoreFoundation object previously created by <see cref="CreateDevice" /> or
    ///     <see cref="CreateRunLoopMode" />.
    /// </summary>
    void ReleaseCFObject(nint cfObject);

    /// <summary>
    ///     Creates a retained <c>CFStringRef</c> used as a run-loop mode name. The caller owns the result
    ///     and must pass it to <see cref="ReleaseCFObject" />.
    /// </summary>
    nint CreateRunLoopMode(string name);

    /// <summary>
    ///     Reads an integer-typed IOKit property from the device.
    /// </summary>
    int GetIntProperty(nint device, string propertyName);

    /// <summary>
    ///     Registers, or with zero <paramref name="callback" /> and <paramref name="context" /> clears, the
    ///     input report callback.
    /// </summary>
    void RegisterInputReportCallback(nint device, byte[] buffer, int bufferLength, nint callback, nint context);

    /// <summary>
    ///     Registers, or with zero <paramref name="callback" /> and <paramref name="context" /> clears, the
    ///     device removal callback.
    /// </summary>
    void RegisterRemovalCallback(nint device, nint callback, nint context);
}

/// <summary>
///     The production <see cref="IIOKitDeviceLifetime" />: a direct pass-through to IOKit and
///     CoreFoundation.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class IOKitDeviceLifetime : IIOKitDeviceLifetime
{
    public static readonly IOKitDeviceLifetime Instance = new();

    private IOKitDeviceLifetime() { }

    public nint CreateDevice(long entryId)
    {
        var deviceEntry = 0;
        try
        {
            var matchingDictionary = IOKitNativeMethods.IORegistryEntryIDMatching((ulong)entryId);
            deviceEntry = IOKitNativeMethods.IOServiceGetMatchingService(0, matchingDictionary);

            if (deviceEntry == 0)
                throw new PlatformApiException("Failed to find matching device entry in IO registry.");

            var device = IOKitNativeMethods.IOHIDDeviceCreate(IntPtr.Zero, deviceEntry);

            if (device == IntPtr.Zero) throw new PlatformApiException("Failed to create HID device handle.");

            return device;
        }
        finally
        {
            if (deviceEntry != 0) _ = IOKitNativeMethods.IOObjectRelease(deviceEntry);
        }
    }

    public void OpenDevice(nint device)
    {
        // kIOHIDOptionsTypeNone (0), NOT kIOHIDOptionsTypeSeizeDevice (0x01). SDK exclusivity is enforced
        // by DeviceConnectionRegistry before native open; seizing would add a platform-wide policy and
        // prevent other non-seizing clients from opening the interface. Both canonical implementations open
        // non-seizing on macOS - Rust yubikit enables hidapi's `macos-shared-device`
        // (hid_darwin_set_open_exclusive(0) => kIOHIDOptionsTypeNone), and python-fido2's macOS backend
        // calls IOHIDDeviceOpen(handle, 0). The OTP feature-report path uses 0 as well, so both macOS
        // HID paths stay consistent.
        var result = IOKitNativeMethods.IOHIDDeviceOpen(device, 0);

        if (result != 0)
            throw new PlatformApiException(
                nameof(IOKitNativeMethods.IOHIDDeviceOpen),
                result,
                "Failed to open HID device.");
    }

    public void CloseDevice(nint device) => _ = IOKitNativeMethods.IOHIDDeviceClose(device, 0);

    public void ReleaseCFObject(nint cfObject) => CFNativeMethods.CFRelease(cfObject);

    public nint CreateRunLoopMode(string name) => CoreFoundationString.Create(name);

    public int GetIntProperty(nint device, string propertyName) =>
        IOKitHelpers.GetIntPropertyValue(device, propertyName);

    public void RegisterInputReportCallback(
        nint device,
        byte[] buffer,
        int bufferLength,
        nint callback,
        nint context) =>
        IOKitNativeMethods.IOHIDDeviceRegisterInputReportCallback(device, buffer, bufferLength, callback, context);

    public void RegisterRemovalCallback(nint device, nint callback, nint context) =>
        IOKitNativeMethods.IOHIDDeviceRegisterRemovalCallback(device, callback, context);
}
