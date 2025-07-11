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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    /// This class represents Windows HID device.
    /// </summary>
    internal class WindowsHidDevice : HidDevice
    {
        private readonly ILogger _log = Logging.Log.GetLogger<WindowsHidDevice>();

        /// <summary>
        /// Gets the list of Windows HID devices available to the system.
        /// </summary>
        /// <returns>List of <see cref="HidDevice"/> objects.</returns>
        public static IEnumerable<HidDevice> GetList() => CmDevice
            .GetList(CmInterfaceGuid.Hid)
            .Where(cmDevice => cmDevice.InterfacePath != null)
            .Select(cmDevice => new WindowsHidDevice(
                cmDevice.InterfacePath!, // Null forgiveness as compiler isn't aware of previous null check
                cmDevice.ContainerId,
                cmDevice.HidUsageId,
                (HidUsagePage)cmDevice.HidUsagePage));

        private WindowsHidDevice(string instancePath, Guid containerId, short usage, HidUsagePage usagePage) :
            base(instancePath)
        {
            ResolveIdsFromInstancePath(instancePath);
            ParentDeviceId = containerId.ToString();

            Usage = usage;
            UsagePage = usagePage;
        }

        /// <summary>
        /// Constructs a <see cref="WindowsHidDevice"/>.
        /// </summary>
        internal WindowsHidDevice(CmDevice device) :
            base(device.InterfacePath!)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            ResolveIdsFromInstancePath(device.InterfacePath!);

            Usage = device.HidUsageId;
            UsagePage = (HidUsagePage)device.HidUsagePage;
        }

        [SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Method needs to compile for both netstandard 2.0 and 2.1")]
        private void ResolveIdsFromInstancePath(string instancePath)
        {
            // \\?\HID#VID_1050&PID_0407&MI_00#7
            // 012345678901234567890123456789
            //             ^---     ^---

            // Disable string comparison warning for this method as it needs to compile for both net47, netstandard 2.0 and 2.1
#pragma warning disable CA1862
            if (instancePath.ToUpperInvariant().Contains("VID") && instancePath.ToUpperInvariant().Contains("HID"))
#pragma warning restore CA1862
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

        [SuppressMessage("Performance", "CA1846:Prefer \'AsSpan\' over \'Substring\'", Justification = "Method needs to compile for both netstandard 2.0 and 2.1")]
        private static bool TryGetHexShort(string s, int offset, int length, out ushort result) =>
            ushort.TryParse(s.Substring(offset, length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);

        /// <summary>
        /// Opens an active connection to the Windows HID device.
        /// </summary>
        /// <returns>An open <see cref="IHidConnection"/>.</returns>
        public override IHidConnection ConnectToFeatureReports() =>
            new WindowsHidFeatureReportConnection(this, Path);

        /// <summary>
        /// Opens an active connection to the Windows HID device.
        /// </summary>
        /// <returns>An open <see cref="IHidConnection"/>.</returns>
        public override IHidConnection ConnectToIOReports() =>
            new WindowsHidIOReportConnection(this, Path);

        public void LogDeviceAccessTime()
        {
            LastAccessed = DateTime.Now;
            _log.LogInformation("Updating last used for {Device} to {LastAccessed:hh:mm:ss.fffffff}", this, LastAccessed);
        }
    }
}
