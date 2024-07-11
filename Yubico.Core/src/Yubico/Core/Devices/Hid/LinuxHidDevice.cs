// Copyright 2021 Yubico AB
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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    /// This class represents Linux HID device.
    /// </summary>
    internal class LinuxHidDevice : HidDevice
    {
        private const int UsagePageTag = 4;
        private const int UsageTag = 8;
        private const int UsagePageGeneric = 1;
        private const int UsagePageFido = 0x0000F1D0;
        private const int UsageKeyboard = 6;
        private const int UsageU2FDevice = 1;

        private readonly string _devnode;

        private readonly Logger _log = Log.GetLogger();

        /// <summary>
        /// Gets a list of all the HIDs on the system (not just YubiKeys).
        /// </summary>
        /// <returns>A list of <see cref="HidDevice"/> objects.</returns>
        public static IEnumerable<HidDevice> GetList()
        {
            // Build an object to search for "hidraw" devices.
            using var scanObject = new LinuxUdevScan();
            scanObject.EnumerateAddMatchSubsystem(NativeMethods.UdevSubsystemName);
            scanObject.EnumerateScanDevices();

            return scanObject.GetLinuxHidDeviceList();
        }

        // Build a new LinuxHidDevice from a device handle.
        internal LinuxHidDevice(LinuxUdevDeviceSafeHandle deviceHandle)
            : this(DeviceGetPath(deviceHandle), DeviceGetDevnode(deviceHandle), GetParentDevicePath(deviceHandle))
        {
        }

        internal LinuxHidDevice(string path, string devnode, string? parentPath) :
            base(path)
        {
            _log.LogInformation(
                "Creating new instance of LinuxHidDevice based on path [{Path}], devnode [{DevNode}]",
                path, devnode);

            VendorId = 0;
            ProductId = 0;
            Usage = 0;
            UsagePage = HidUsagePage.Unknown;
            _devnode = devnode;
            ParentDeviceId = parentPath;

            // If this call fails, the handle will be < 0. If so, the following
            // function calls will do nothing.
            using LinuxFileSafeHandle handle = NativeMethods.open(
                devnode, NativeMethods.OpenFlags.O_RDWR | NativeMethods.OpenFlags.O_NONBLOCK);

            if (handle.IsInvalid)
            {
                _log.LogWarning(
                    "Could not open [{Path}]. Errno = {errno} {errorstring}",
                    path, Marshal.GetLastWin32Error(), LibcHelpers.GetErrnoString());
            }

            GetVendorProductIds(handle);
            GetUsageProperties(handle);
        }

        private static string DeviceGetPath(IntPtr udevDevice)
        {
            IntPtr pathPtr = NativeMethods.udev_device_get_syspath(udevDevice);
            return Marshal.PtrToStringAnsi(pathPtr);
        }

        private static string DeviceGetPath(LinuxUdevDeviceSafeHandle udevDevice)
        {
            IntPtr pathPtr = NativeMethods.udev_device_get_syspath(udevDevice);
            return Marshal.PtrToStringAnsi(pathPtr);
        }

        // Get the devnode from the device.
        private static string DeviceGetDevnode(LinuxUdevDeviceSafeHandle udevDevice)
        {
            IntPtr devnodePtr = NativeMethods.udev_device_get_devnode(udevDevice);
            return Marshal.PtrToStringAnsi(devnodePtr);
        }

        private static string? GetParentDevicePath(LinuxUdevDeviceSafeHandle udevDevice)
        {
            // It's quite a few hops to the composite device parent that we're looking for. I'm no udev expert,
            // but my guess is that we start with the HIDRAW device, we then get the USB endpoint (parent1),
            // then the USB interface (parent2), and then finally the composite device (parent3).
            IntPtr parentDev = NativeMethods.udev_device_get_parent(udevDevice);
            parentDev = NativeMethods.udev_device_get_parent(parentDev);
            parentDev = NativeMethods.udev_device_get_parent(parentDev);

            if (parentDev == IntPtr.Zero)
            {
                return null;
            }

            return DeviceGetPath(parentDev);
        }

        // Get the devinfo out of the handle. The VendorId and ProductId are in
        // that struct.
        // Then set the VendorId and ProductId properties in this object.
        private void GetVendorProductIds(LinuxFileSafeHandle handle)
        {
            IntPtr infoStructData = Marshal.AllocHGlobal(NativeMethods.InfoSize);
            int status = NativeMethods.ioctl(handle, NativeMethods.HIDIOCGRAWINFO, infoStructData);
            if (status >= 0)
            {
                VendorId = Marshal.ReadInt16(infoStructData, NativeMethods.OffsetInfoVendor);
                ProductId = Marshal.ReadInt16(infoStructData, NativeMethods.OffsetInfoProduct);
            }
            else
            {
                _log.LogWarning("IOCTL failed. {error}", LibcHelpers.GetErrnoString());
            }

            Marshal.FreeHGlobal(infoStructData);
        }

        // Get the descriptor info struct out of the handle, that struct contains
        // the descriptor. Parse the descriptor to get the Usage and UsagePage.
        private void GetUsageProperties(LinuxFileSafeHandle handle)
        {
            // Get the descriptor length before getting the descriptor. It is not
            // possible to get the descriptor out unless the length is set in the
            // struct.
            int descriptorLength = GetDescriptorLength(handle);
            IntPtr descriptorStructData = Marshal.AllocHGlobal(NativeMethods.DescriptorSize);
            Marshal.WriteInt32(descriptorStructData, NativeMethods.OffsetDescSize, descriptorLength);
            int status = NativeMethods.ioctl(handle, NativeMethods.HIDIOCGRDESC, descriptorStructData);
            if (status >= 0)
            {
                byte[] descriptor = new byte[NativeMethods.DescriptorSize];
                Marshal.Copy(descriptorStructData, descriptor, 0, NativeMethods.DescriptorSize);
                ParseUsageProperties(descriptor, NativeMethods.OffsetDescValue, descriptorLength);
            }
            else
            {
                _log.LogWarning("IOCTL failed. {error}", LibcHelpers.GetErrnoString());
            }

            Marshal.FreeHGlobal(descriptorStructData);
        }

        private static int GetDescriptorLength(LinuxFileSafeHandle handle)
        {
            int returnValue = 0;

            IntPtr descSize = Marshal.AllocHGlobal(NativeMethods.DescriptorSizeSize);
            int status = NativeMethods.ioctl(handle, NativeMethods.HIDIOCGRDESCSIZE, descSize);
            if (status >= 0)
            {
                returnValue = Marshal.ReadInt32(descSize, NativeMethods.OffsetDescSize);
            }
            else
            {
                Log.GetLogger().LogWarning("IOCTL failed. {error}", LibcHelpers.GetErrnoString());
            }

            Marshal.FreeHGlobal(descSize);

            return returnValue;
        }

        // The descriptor begins at offset. Parse the TLVs to get the Usage and
        // UsagePage.
        // This will search for specific combos. If it finds something that is
        // not a supported combo, it will leave UsagePage at Unknown and return.
        // Acceptable combos are
        //   USAGE PAGE : Generic (value = 1)      USAGE : Keyboard (value = 6)
        //   USAGE PAGE : FIDO (value = 0xF1D0)    USAGE : U2FHID (value = 1)
        // If this method does not find the Usage and UsagePage, it will leave
        // those properties at Unknown/0 and return.
        private void ParseUsageProperties(byte[] descriptor, int offset, int descriptorLength)
        {
            int currentOffset = offset;
            int usagePageValue = 0;

            bool usagePageFound = false;
            bool usageFound = false;

            while (currentOffset < descriptorLength)
            {
                currentOffset = ReadTagAndLength(
                    descriptor, currentOffset, descriptorLength, out int tag, out int length);

                currentOffset = ReadValue(descriptor, currentOffset, descriptorLength, length, out int value);

                // If the tag is 4, the value is the USAGE PAGE. If we already
                // have a USAGE PAGE, ignore this one.
                if (tag == UsagePageTag && !usagePageFound)
                {
                    usagePageValue = value;
                    usagePageFound = true;
                }

                // If the tag is 8, the value is the USAGE. Go ahead and set the
                // Usage property in this object. If we already have a USAGE,
                // ignore this one.
                else if (tag == UsageTag && !usageFound)
                {
                    Usage = (short)value;
                    usageFound = true;
                }

                // Once we have both, stop looking.
                if (usagePageFound && usageFound)
                {
                    break;
                }
            }

            // If it is not a valid combo, don't set the UsagePage.
            // We set the Usage even though it might be one we don't support,
            // because if the UsagePage is Unknown, the Usage won't matter.
            if (usagePageValue == UsagePageGeneric && Usage == UsageKeyboard)
            {
                UsagePage = HidUsagePage.Keyboard;
            }
            else if (usagePageValue == UsagePageFido && Usage == UsageU2FDevice)
            {
                UsagePage = HidUsagePage.Fido;
            }
        }

        // The byte at offset is the tag and length, or it is the tag for long
        // form.
        // The tag returned will actually be the bTag + bType
        // Return the new offset (the offset into the buffer of the next byte
        // beyond the tag and length).
        // This method assumes that the offset is valid. If the offset is beyond
        // the end of the buffer, don't call this method.
        // If this is long form, the method will read the next two bytes (the
        // actual tag and length), but will verify there are two bytes to read.
        // If not, it will return an offset beyond the tag and length octets.
        private static int ReadTagAndLength(
            byte[] descriptor,
            int offset,
            int descriptorLength,
            out int tag,
            out int length)
        {
            int newOffset = offset + 1;

            // The length is the least significant 2 bits. In addition, the only
            // valid lengths are 0, 1, 2, and 4. So if the length is 3, it's
            // really 4.
            tag = (int)descriptor[offset];
            length = tag & 3;
            if (length == 3)
            {
                length = 4;
            }

            // The tag is made up of bits 2 - 7.
            // If the tag is FC, this is long form.
            tag &= 0xFC;
            if (tag == 0xFC)
            {
                newOffset += 2;
                if (newOffset < descriptorLength)
                {
                    tag = (int)descriptor[offset + 2] & 0xFF;
                    length = (int)descriptor[offset + 1] & 0xFF;
                }
            }

            return newOffset;
        }

        // Read the value beginning at offset.
        // If the length is more than 4, don't bother (this can happen with the
        // long form).
        // Return the offset to the byte beyond the value.
        // The length of the value can be 0, in which case the return value is
        // offset.
        private static int ReadValue(byte[] descriptor, int offset, int descriptorLength, int length, out int value)
        {
            value = 0;

            int newOffset = offset + length;

            if (length <= 4 && length + offset <= descriptorLength)
            {
                for (int index = 0; index < length; index++)
                {
                    value += (int)descriptor[offset + index] << (8 * index);
                }
            }

            return newOffset;
        }

        /// <summary>
        /// Return an implementation of IHidConnection that will already have a
        /// connection to the Linux HID device, and will be able to Get and Set Feature Reports.
        /// </summary>
        /// <returns>An open <see cref="IHidConnection"/>.</returns>
        public override IHidConnection ConnectToFeatureReports()
        {
            return new LinuxHidFeatureReportConnection(this, _devnode);
        }

        /// <summary>
        /// Opens an active connection to the Linux HID device.
        /// </summary>
        /// <returns>An open <see cref="IHidConnection"/>.</returns>
        public override IHidConnection ConnectToIOReports()
        {
            return new LinuxHidIOReportConnection(this, _devnode);
        }

        public void LogDeviceAccessTime()
        {
            LastAccessed = DateTime.Now;
            _log.LogInformation(
                "Updating last used for {Device} to {LastAccessed:hh:mm:ss.fffffff}", this, LastAccessed);
        }
    }
}
