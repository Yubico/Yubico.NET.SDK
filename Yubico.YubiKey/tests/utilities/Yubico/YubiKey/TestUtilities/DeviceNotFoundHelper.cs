// Copyright 2024 Yubico AB
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
using System.Linq;

namespace Yubico.YubiKey.TestUtilities
{
    public static class DeviceNotFoundHelper
    {
        public static string FormatConnectedDevices(IReadOnlyCollection<IYubiKeyDevice> devices)
        {
            return devices.Any()
                ? "Connected devices: " + string.Join(", ",
                    devices.Select(y => $"{{{y.FirmwareVersion}, {y.FormFactor}, IsFipsSeries: {y.IsFipsSeries}}}"))
                : string.Empty;
        }

        public static string GetSkipReason(StandardTestDevice requiredDevice)
        {
            return IsDeviceMissing(requiredDevice, out var devices)
                ? $"Test skipped: Requested device not found: ({requiredDevice}). {FormatConnectedDevices(devices)}"
                : string.Empty;
        }

        private static bool IsDeviceMissing(StandardTestDevice testDevice,
            out IReadOnlyCollection<IYubiKeyDevice> devices)
        {
            devices = Array.Empty<IYubiKeyDevice>();

            try
            {
                devices = IntegrationTestDeviceEnumeration.GetTestDevices().ToList();
                _ = IntegrationTestDeviceEnumeration.GetTestDevice(testDevice);
                return false;
            }
            catch (DeviceNotFoundException)
            {
                // Skip because we can't find the device
                return true;
            }
        }
    }
}
