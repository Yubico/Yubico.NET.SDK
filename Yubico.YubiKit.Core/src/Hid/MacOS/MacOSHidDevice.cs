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
using Yubico.YubiKit.Core.Hid.Constants;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop;
using Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework;
using CFNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.PlatformInterop.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Hid.MacOS;

/// <summary>
///     macOS implementation of a Human Interface Device (HID).
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOSHidDevice : IHidDevice
{
    private readonly long _entryId;
    
    public string ReaderName => _entryId.ToString(CultureInfo.InvariantCulture);
    
    /// <summary>
    /// Raw HID descriptor information as reported by the operating system.
    /// </summary>
    public HidDescriptorInfo DescriptorInfo { get; }
    
    /// <summary>
    /// The classified YubiKey HID interface type.
    /// </summary>
    public YubiKeyHidInterfaceType InterfaceType { get; }
    
    // Obsolete properties for backward compatibility
    #pragma warning disable CS0618 // Type or member is obsolete
    public short VendorId => DescriptorInfo.VendorId;
    public short ProductId => DescriptorInfo.ProductId;
    public short Usage => (short)DescriptorInfo.Usage;
    
    public HidUsagePage UsagePage
    {
        get
        {
            // Map new types back to old enum for backward compatibility
            return InterfaceType switch
            {
                YubiKeyHidInterfaceType.Fido => HidUsagePage.Fido,
                YubiKeyHidInterfaceType.Otp => HidUsagePage.Keyboard,
                _ => HidUsagePage.Unknown
            };
        }
    }
    #pragma warning restore CS0618

    internal MacOSHidDevice(long entryId, HidDescriptorInfo descriptorInfo)
    {
        _entryId = entryId;
        DescriptorInfo = descriptorInfo;
        InterfaceType = HidInterfaceClassifier.Classify(descriptorInfo);
    }

    /// <summary>
    ///     Returns a list of all Yubico HID devices on the system.
    /// </summary>
    /// <returns>
    ///     An enumerable list of all the supported Yubico HID devices present on the system.
    /// </returns>
    public static IReadOnlyList<IHidDevice> GetList()
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

            var result = new List<IHidDevice>((int)deviceSetCount);
            foreach (var device in devices)
            {
                var descriptorInfo = new HidDescriptorInfo
                {
                    VendorId = (short)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyVendorId) ?? 0),
                    ProductId = (short)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyProductId) ?? 0),
                    Usage = (ushort)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyPrimaryUsage) ?? 0),
                    UsagePage = (ushort)(IOKitHelpers.GetNullableIntPropertyValue(device, IOKitHidConstants.DevicePropertyPrimaryUsagePage) ?? 0),
                    DevicePath = GetEntryId(device).ToString(CultureInfo.InvariantCulture)
                };
                
                // Only include Yubico devices with supported interface types
                if (descriptorInfo.VendorId == 0x1050 && 
                    HidInterfaceClassifier.IsSupported(descriptorInfo))
                {
                    result.Add(new MacOSHidDevice(GetEntryId(device), descriptorInfo));
                }
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
