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

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Native.Linux.Udev;
using Yubico.YubiKit.Core.Transports.Hid;
using LibcNativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;
using UdevNativeMethods = Yubico.YubiKit.Core.Native.Linux.Udev.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>
///     Linux implementation of a Human Interface Device (HID) using hidraw.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHidDevice : IHidDevice
{
    private readonly string _devNode;

    public string ReaderName => _devNode;

    /// <summary>
    /// Raw HID descriptor information as reported by the operating system.
    /// </summary>
    public HidDescriptorInfo DescriptorInfo { get; }

    /// <summary>
    /// The classified YubiKey HID interface type.
    /// </summary>
    public HidInterfaceType InterfaceType { get; }

    private LinuxHidDevice(HidDescriptorInfo descriptorInfo)
    {
        DescriptorInfo = descriptorInfo;
        InterfaceType = HidInterfaceClassifier.Classify(descriptorInfo);
        _devNode = descriptorInfo.DevicePath;
    }

    /// <summary>
    ///     Returns a list of all Yubico HID devices on the system using udev enumeration.
    /// </summary>
    /// <returns>
    ///     An enumerable list of all the supported Yubico HID devices present on the system.
    /// </returns>
    public static IReadOnlyList<IHidDevice> GetList()
    {
        using var udev = UdevNativeMethods.udev_new();
        if (udev.IsInvalid)
        {
            throw new PlatformApiException(
                nameof(UdevNativeMethods.udev_new),
                Marshal.GetLastWin32Error(),
                "Failed to create udev context.");
        }

        using var enumerate = UdevNativeMethods.udev_enumerate_new(udev);
        if (enumerate.IsInvalid)
        {
            throw new PlatformApiException(
                nameof(UdevNativeMethods.udev_enumerate_new),
                Marshal.GetLastWin32Error(),
                "Failed to create udev enumerate context.");
        }

        int result = UdevNativeMethods.udev_enumerate_add_match_subsystem(enumerate, UdevNativeMethods.UdevSubsystemName);
        if (result < 0)
        {
            throw new PlatformApiException(
                nameof(UdevNativeMethods.udev_enumerate_add_match_subsystem),
                result,
                "Failed to add subsystem match filter.");
        }

        result = UdevNativeMethods.udev_enumerate_scan_devices(enumerate);
        if (result < 0)
        {
            throw new PlatformApiException(
                nameof(UdevNativeMethods.udev_enumerate_scan_devices),
                result,
                "Failed to scan for devices.");
        }

        var devices = new List<IHidDevice>();
        var currentEntry = UdevNativeMethods.udev_enumerate_get_list_entry(enumerate);

        while (currentEntry != IntPtr.Zero)
        {
            var namePtr = UdevNativeMethods.udev_list_entry_get_name(currentEntry);
            var syspath = Marshal.PtrToStringAnsi(namePtr);

            if (syspath is not null)
            {
                using var device = UdevNativeMethods.udev_device_new_from_syspath(udev, syspath);
                if (!device.IsInvalid)
                {
                    var descriptorInfo = ParseHidDescriptor(device);

                    // Only include Yubico devices with supported interface types
                    if (descriptorInfo.VendorId == HidConstants.YubicoVendorId &&
                        HidInterfaceClassifier.IsSupported(descriptorInfo))
                    {
                        devices.Add(new LinuxHidDevice(descriptorInfo));
                    }
                }
            }

            currentEntry = UdevNativeMethods.udev_list_entry_get_next(currentEntry);
        }

        return devices;
    }

    /// <summary>
    ///     Establishes a connection capable of transmitting feature reports to a keyboard device.
    /// </summary>
    /// <returns>
    ///     An active connection object.
    /// </returns>
    public IHidConnection ConnectToFeatureReports() =>
        new LinuxHidFeatureReportConnection(ReaderName);

    /// <summary>
    ///     Establishes a connection capable of transmitting IO reports to a FIDO device.
    /// </summary>
    /// <returns>
    ///     An active connection object.
    /// </returns>
    public IHidConnection ConnectToIOReports() =>
        new LinuxHidIOReportConnection(ReaderName);

    private static HidDescriptorInfo ParseHidDescriptor(LinuxUdevDeviceSafeHandle device)
    {
        var devNodePtr = UdevNativeMethods.udev_device_get_devnode(device);
        var devNode = Marshal.PtrToStringAnsi(devNodePtr);

        if (string.IsNullOrEmpty(devNode))
        {
            return new HidDescriptorInfo { DevicePath = string.Empty };
        }

        // Get vendor and product ID from the parent USB device
        var parentDevice = UdevNativeMethods.udev_device_get_parent(device);
        if (parentDevice == IntPtr.Zero)
        {
            return new HidDescriptorInfo { DevicePath = devNode };
        }

        short vendorId = 0;
        short productId = 0;

        // Walk up the device tree to find the USB device
        var usbDevice = FindUsbParent(parentDevice);
        if (usbDevice != IntPtr.Zero)
        {
            vendorId = GetDevicePropertyAsShort(usbDevice, "idVendor");
            productId = GetDevicePropertyAsShort(usbDevice, "idProduct");
        }

        // Get usage page and usage from the HID report descriptor
        var (usagePage, usage) = GetHidUsageFromDescriptor(devNode);

        return new HidDescriptorInfo
        {
            UsagePage = usagePage,
            Usage = usage,
            DevicePath = devNode,
            VendorId = vendorId,
            ProductId = productId
        };
    }

    private static IntPtr FindUsbParent(IntPtr device)
    {
        var current = device;
        while (current != IntPtr.Zero)
        {
            var syspathPtr = UdevNativeMethods.udev_device_get_syspath(current);
            var syspath = Marshal.PtrToStringAnsi(syspathPtr);

            // USB devices have paths containing "/usb" and vendor/product attributes
            if (syspath?.Contains("/usb", StringComparison.Ordinal) == true)
            {
                // Check if this device has the vendor ID attribute
                if (GetDevicePropertyAsShort(current, "idVendor") != 0)
                {
                    return current;
                }
            }

            current = UdevNativeMethods.udev_device_get_parent(current);
        }

        return IntPtr.Zero;
    }

    private static short GetDevicePropertyAsShort(IntPtr device, string property)
    {
        // For USB devices, we need to read sysfs attributes directly
        var syspathPtr = UdevNativeMethods.udev_device_get_syspath(device);
        var syspath = Marshal.PtrToStringAnsi(syspathPtr);

        if (string.IsNullOrEmpty(syspath))
        {
            return 0;
        }

        var attributePath = Path.Combine(syspath, property);
        if (!File.Exists(attributePath))
        {
            return 0;
        }

        try
        {
            var value = File.ReadAllText(attributePath).Trim();
            if (short.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var result))
            {
                return result;
            }
        }
        catch
        {
            // Ignore read errors
        }

        return 0;
    }

    private static (ushort UsagePage, ushort Usage) GetHidUsageFromDescriptor(string devNode)
    {
        LinuxFileSafeHandle? handle = null;

        try
        {
            // Try opening with O_RDWR | O_NONBLOCK first (for descriptor reading)
            handle = LibcNativeMethods.open(devNode,
                LibcNativeMethods.OpenFlags.O_RDWR | LibcNativeMethods.OpenFlags.O_NONBLOCK);

            if (handle.IsInvalid)
            {
                // Dispose the invalid handle before trying again
                handle.Dispose();

                // O_RDWR failed, try O_RDONLY | O_NONBLOCK
                handle = LibcNativeMethods.open(devNode,
                    LibcNativeMethods.OpenFlags.O_RDONLY | LibcNativeMethods.OpenFlags.O_NONBLOCK);

                if (handle.IsInvalid)
                {
                    return (0, 0);
                }
            }

            return GetHidUsageFromHandle(handle, devNode);
        }
        finally
        {
            handle?.Dispose();
        }
    }

    private static (ushort UsagePage, ushort Usage) GetHidUsageFromHandle(LinuxFileSafeHandle handle, string devNode)
    {
        if (handle.IsInvalid)
        {
            return (0, 0);
        }

        // Get the descriptor size first
        var descSizePtr = Marshal.AllocHGlobal(LibcNativeMethods.DescriptorSizeSize);
        try
        {
            int result = LibcNativeMethods.ioctl(handle, LibcNativeMethods.HIDIOCGRDESCSIZE, descSizePtr);
            if (result < 0)
            {
                return (0, 0);
            }

            int descSize = Marshal.ReadInt32(descSizePtr);
            if (descSize <= 0 || descSize > LibcNativeMethods.DescriptorSize - LibcNativeMethods.OffsetDescValue)
            {
                return (0, 0);
            }

            // Get the actual descriptor
            var descPtr = Marshal.AllocHGlobal(LibcNativeMethods.DescriptorSize);
            try
            {
                Marshal.WriteInt32(descPtr, descSize);
                result = LibcNativeMethods.ioctl(handle, LibcNativeMethods.HIDIOCGRDESC, descPtr);
                if (result < 0)
                {
                    return (0, 0);
                }

                // Parse the descriptor to find usage page and usage
                var descriptor = new byte[descSize];
                Marshal.Copy(descPtr + LibcNativeMethods.OffsetDescValue, descriptor, 0, descSize);

                return ParseHidDescriptorBytes(descriptor);
            }
            finally
            {
                Marshal.FreeHGlobal(descPtr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(descSizePtr);
        }
    }

    internal static (ushort UsagePage, ushort Usage) ParseHidDescriptorBytes(ReadOnlySpan<byte> descriptor)
    {
        // HID descriptor parsing - looking for Usage Page and Usage items

        // IMPORTANT: We need to validate the UsagePage + Usage combination
        // Don't just return the first values found - parse both, then validate

        ushort usagePage = 0;
        ushort usage = 0;
        bool usagePageFound = false;
        bool usageFound = false;

        int position = 0;
        while (HidReportDescriptorReader.TryReadItem(descriptor, ref position, out HidReportItem item))
        {
            // Global items
            if (item.Type == HidReportDescriptorReader.TypeGlobal)
            {
                if (item.Tag == 0 && !usagePageFound) // Usage Page
                {
                    usagePage = (ushort)item.Value;
                    usagePageFound = true;
                }
            }
            // Local items
            else if (item.Type == HidReportDescriptorReader.TypeLocal)
            {
                if (item.Tag == 0 && !usageFound) // Usage
                {
                    usage = (ushort)item.Value;
                    usageFound = true;
                }
            }

            // Once we have both, we can stop (we want the first/primary usage)
            if (usagePageFound && usageFound)
            {
                break;
            }
        }

        return (usagePage, usage);
    }
}