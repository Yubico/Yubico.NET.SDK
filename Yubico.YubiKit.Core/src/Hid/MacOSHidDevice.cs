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

using System.Globalization;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.PlatformInterop;
using Yubico.YubiKit.Core.PlatformInterop.MacOS.CoreFoundation;
using Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework;
using CFNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
///     macOS implementation of a Human Interface Device (HID).
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidDevice : IHidDevice
{
    private readonly long _entryId;

    public string ReaderName { get; }
    public short VendorId { get; init; }
    public short ProductId { get; init; }
    public short Usage { get; init; }
    public HidUsagePage UsagePage { get; init; }

    public MacOSHidDevice(long entryId)
    {
        _entryId = entryId;
        ReaderName = entryId.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Returns a list of all HID devices on the system.
    /// </summary>
    /// <returns>
    ///     An enumerable list of all the HID devices present on the system.
    /// </returns>
    public static IReadOnlyList<MacOSHidDevice> GetList()
    {
        nint manager = 0;
        nint deviceSet = 0;

        try
        {
            manager = IOKitNativeMethods.IOHIDManagerCreate(IntPtr.Zero, 0);
            IOKitNativeMethods.IOHIDManagerSetDeviceMatching(manager, IntPtr.Zero);

            deviceSet = IOKitNativeMethods.IOHIDManagerCopyDevices(manager);

            long deviceSetCount = CFNativeMethods.CFSetGetCount(deviceSet);

            var devices = new IntPtr[deviceSetCount];
            CFNativeMethods.CFSetGetValues(deviceSet, devices);

            var result = new List<MacOSHidDevice>((int)deviceSetCount);
            foreach (var device in devices)
            {
                result.Add(new MacOSHidDevice(GetEntryId(device))
                {
                    VendorId = (short)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyVendorId) ?? 0),
                    ProductId = (short)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyProductId) ?? 0),
                    Usage = (short)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyPrimaryUsage) ?? 0),
                    UsagePage = (HidUsagePage)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyPrimaryUsagePage) ?? 0),
                });
            }

            return result;
        }
        finally
        {
            if (manager != IntPtr.Zero)
            {
                CFNativeMethods.CFRelease(manager);
            }

            if (deviceSet != IntPtr.Zero)
            {
                CFNativeMethods.CFRelease(deviceSet);
            }
        }
    }

    /// <summary>
    ///     Establishes a connection capable of transmitting feature reports to a keyboard device.
    /// </summary>
    /// <returns>
    ///     An active connection object.
    /// </returns>
    public IHidConnectionSync ConnectToFeatureReports() =>
        new MacOSHidFeatureReportConnection(_entryId);

    /// <summary>
    ///     Establishes a connection capable of transmitting IO reports to a FIDO device.
    /// </summary>
    /// <returns>
    ///     An active connection object.
    /// </returns>
    public IHidConnectionSync ConnectToIOReports() =>
        new MacOSHidIOReportConnection(_entryId);

    internal static long GetEntryId(IntPtr device)
    {
        int service = IOKitNativeMethods.IOHIDDeviceGetService(device);
        kern_return_t result = IOKitNativeMethods.IORegistryEntryGetRegistryEntryID(service, out long entryId);

        if (result != kern_return_t.KERN_SUCCESS)
        {
            throw new PlatformApiException(
                nameof(IOKitNativeMethods.IORegistryEntryGetRegistryEntryID),
                (int)result,
                "Failed to get registry entry ID for HID device.");
        }

        return entryId;
    }
}
