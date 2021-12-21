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

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    /// This class represents Windows HID device.
    /// </summary>
    public class WindowsHidDevice : HidDevice
    {
        /// <summary>
        /// Event for Windows HID device arrival.
        /// </summary>
        public static event EventHandler<CmDeviceEventArgs>? HidDeviceArrived;

        /// <summary>
        /// Event for Windows HID device removal.
        /// </summary>
        public static event EventHandler<CmDeviceEventArgs>? HidDeviceRemoved;

        internal static readonly CmDeviceListener cmListener = 
            new CmDeviceListener(CmInterfaceGuid.Hid, OnDeviceArrived, OnDeviceRemoved);

        /// <summary>
        /// Raises event on Windows HID device arrival.
        /// </summary>
        private static void OnDeviceArrived(CmDeviceEventArgs e) => HidDeviceArrived?.Invoke(typeof(WindowsHidDevice), e);

        /// <summary>
        /// Raises event on Windows HID device removal.
        /// </summary>
        private static void OnDeviceRemoved(CmDeviceEventArgs e) => HidDeviceRemoved?.Invoke(typeof(WindowsHidDevice), e);

        /// <summary>
        /// Gets the list of Windows HID devices available to the system.
        /// </summary>
        /// <returns>List of <see cref="HidDevice"/> objects.</returns>
        public static IEnumerable<HidDevice> GetList() => CmDevice
            .GetList(CmInterfaceGuid.Hid)
            .Where(cmDevice => cmDevice.InterfacePath != null)
            .Select(cmDevice => new WindowsHidDevice(
                cmDevice.InterfacePath!, // Null forgiveness as compiler isn't aware of previous null check
                cmDevice.HidUsageId,
                (HidUsagePage)cmDevice.HidUsagePage));

        private WindowsHidDevice(string instancePath, short usage, HidUsagePage usagePage) :
            base(instancePath)
        {
            ResolveIdsFromInstancePath(instancePath);

            Usage = usage;
            UsagePage = usagePage;
        }

        private void ResolveIdsFromInstancePath(string instancePath)
        {
            // \\?\HID#VID_1050&PID_0407&MI_00#7
            // 012345678901234567890123456789
            //             ^---     ^---

            if (instancePath.ToUpperInvariant().Contains("VID") && instancePath.ToUpperInvariant().Contains("HID"))
            {
                // If this fails, vendorId will be 0.
                _ = TryGetHexShort(instancePath, 12, 4, out ushort vendorId);
                VendorId = (short)vendorId;

                // If this fails, productId will be 0.
                _ = TryGetHexShort(instancePath, 21, 4, out ushort productId);
                ProductId = (short)productId;
            }
            else
            {
                VendorId = 0;
                ProductId = 0;
            }
        }

        private static bool TryGetHexShort(string s, int offset, int length, out ushort result) =>
            ushort.TryParse(s.Substring(offset, length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);

        /// <summary>
        /// Opens an active connection to the Windows HID device.
        /// </summary>
        /// <returns>An open <see cref="IHidConnection"/>.</returns>
        public override IHidConnection ConnectToFeatureReports() =>
            new WindowsHidFeatureReportConnection(Path);

        /// <summary>
        /// Opens an active connection to the Windows HID device.
        /// </summary>
        /// <returns>An open <see cref="IHidConnection"/>.</returns>
        public override IHidConnection ConnectToIOReports() =>
            new WindowsHidIOReportConnection(Path);
    }
}
