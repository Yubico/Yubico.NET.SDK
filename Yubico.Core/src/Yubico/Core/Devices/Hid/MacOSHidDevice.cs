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
using System.Globalization;
using System.Linq;
using Yubico.PlatformInterop;

using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    /// macOS implementation of a Human Interface Device (HID)
    /// </summary>
    internal class MacOSHidDevice : HidDevice
    {
        private readonly long _entryId;

        private MacOSHidDevice(long entryId) :
            base(entryId.ToString(CultureInfo.InvariantCulture))
        {
            _entryId = entryId;
        }

        /// <summary>
        /// Return a list of all HID devices on the system. No filtering is done here.
        /// </summary>
        /// <returns>
        /// An enumerable list of all the HID devices present on the system.
        /// </returns>
        public static IEnumerable<HidDevice> GetList()
        {
            IntPtr manager = IntPtr.Zero;
            IntPtr deviceSet = IntPtr.Zero;

            try
            {
                manager = IOHIDManagerCreate(IntPtr.Zero, 0);
                IOHIDManagerSetDeviceMatching(manager, IntPtr.Zero);

                deviceSet = IOHIDManagerCopyDevices(manager);

                long deviceSetCount = CFSetGetCount(deviceSet);
                var devices = new IntPtr[deviceSetCount];

                CFSetGetValues(deviceSet, devices);

                return devices
                    .Select(device => new MacOSHidDevice(GetEntryId(device))
                    {
                        VendorId = (short)IOKitHelpers.GetIntPropertyValue(device, IOKitHidConstants.DevicePropertyVendorId),
                        ProductId = (short)IOKitHelpers.GetIntPropertyValue(device, IOKitHidConstants.DevicePropertyProductId),
                        Usage = (short)IOKitHelpers.GetIntPropertyValue(device, IOKitHidConstants.DevicePropertyPrimaryUsage),
                        UsagePage = (HidUsagePage)IOKitHelpers.GetIntPropertyValue(device, IOKitHidConstants.DevicePropertyPrimaryUsagePage)
                    })
                    .ToList();
            }
            finally
            {
                if (manager != IntPtr.Zero)
                {
                    CFRelease(manager);
                }

                if (deviceSet != IntPtr.Zero)
                {
                    CFRelease(deviceSet);
                }
            }
        }

        /// <summary>
        /// Establishes a connection capable of transmitting feature reports to a keyboard device.
        /// </summary>
        /// <returns>
        /// An active connection object.
        /// </returns>
        public override IHidConnection ConnectToFeatureReports() =>
            new MacOSHidFeatureReportConnection(_entryId);

        /// <summary>
        /// Establishes a connection capable of transmitting IO reports to a FIDO device.
        /// </summary>
        /// <returns>
        /// An active connection object.
        /// </returns>
        public override IHidConnection ConnectToIOReports() =>
            new MacOSHidIOReportConnection(_entryId);

        private static long GetEntryId(IntPtr device)
        {
            int service = IOHIDDeviceGetService(device);
            kern_return_t result = IORegistryEntryGetRegistryEntryID(service, out long entryId);

            if (result != kern_return_t.KERN_SUCCESS)
            {
                throw new PlatformApiException(
                    nameof(IORegistryEntryGetRegistryEntryID),
                    (int)result,
                    ExceptionMessages.IOKitRegistryEntryNotFound);
            }

            return entryId;
        }
    }
}
