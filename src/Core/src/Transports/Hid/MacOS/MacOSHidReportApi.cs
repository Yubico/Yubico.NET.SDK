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
using Yubico.YubiKit.Core.Native.MacOS.IOKitFramework;
using CFNativeMethods = Yubico.YubiKit.Core.Native.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.Native.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
///     The production <see cref="IMacOSHidReportApi" /> implementation, combining IOKit and CoreFoundation
///     call forwarding with small managed compositions and status handling.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidReportApi : IMacOSHidReportApi
{
    public static readonly MacOSHidReportApi Instance = new();

    private MacOSHidReportApi() { }

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
        // prevent other non-seizing clients from opening the interface. The OTP path also passes zero.
        // Process-local admission and OS access permissions remain separate policies.
        var result = OpenDeviceResult(device);

        if (result != 0)
            throw new PlatformApiException(
                nameof(IOKitNativeMethods.IOHIDDeviceOpen),
                result,
                "Failed to open HID device.");
    }

    public int OpenDeviceResult(nint device) => IOKitNativeMethods.IOHIDDeviceOpen(device, 0);

    public void CloseDevice(nint device) => _ = IOKitNativeMethods.IOHIDDeviceClose(device, 0);

    public bool CloseDeviceChecked(nint device) => IOKitNativeMethods.IOHIDDeviceClose(device, 0) == 0;

    public int GetFeatureReport(nint device, byte[] buffer, ref long length) =>
        IOKitNativeMethods.IOHIDDeviceGetReport(device, IOKitHidConstants.kIOHidReportTypeFeature, 0, buffer, ref length);

    public int SetFeatureReport(nint device, byte[] buffer) =>
        IOKitNativeMethods.IOHIDDeviceSetReport(device, IOKitHidConstants.kIOHidReportTypeFeature, 0, buffer, buffer.Length);

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