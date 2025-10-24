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
using System.Linq;
using TestDev = Yubico.YubiKey.TestUtilities.IntegrationTestDeviceEnumeration;

namespace Yubico.YubiKey.TestUtilities
{
    public static class TestDeviceSelection
    {
        /// <summary>
        /// Persistent enumeration to find a specific YubiKey test device by serial number.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the test device could not be found.
        /// </exception>
        public static IYubiKeyDevice RenewDeviceEnumeration(
            int serialNumber)
        {
            const int maxReconnectAttempts = 40;
            var sleepDurationMs = TimeSpan.FromMilliseconds(200);

            int reconnectAttempts = 0;
            do
            {
                System.Threading.Thread.Sleep(sleepDurationMs);

                try
                {
                    return TestDev.GetBySerial(serialNumber);
                }
                catch (InvalidOperationException)
                {
                    //
                }
            } while (reconnectAttempts++ < maxReconnectAttempts);

            throw new InvalidOperationException($"Could not find test device s/n \"{serialNumber}\".");
        }

        /// <summary>
        /// Retrieves a single <see cref="IYubiKeyDevice"/> based on test device requirements.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="testDeviceType"/> is not a recognized value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the input sequence did not contain a valid test device.
        /// </exception>
        /// <param name="testDeviceType">The type of the device.</param>
        /// <param name="minimumFirmwareVersion">The earliest version number the
        /// caller is willing to accept. Defaults to the minimum version for the given device.</param>
        /// <param name="yubiKeys">The list of yubikeys to select from</param>
        /// <param name="transport">The desired transport</param>
        /// <returns>The allow-list filtered YubiKey that was found.</returns>
        public static IYubiKeyDevice SelectByStandardTestDevice(
            this IEnumerable<IYubiKeyDevice> yubiKeys,
            StandardTestDevice testDeviceType,
            FirmwareVersion? minimumFirmwareVersion = null,
            Transport transport = Transport.All
            )
        {
            if (!yubiKeys.Any())
            {
                ThrowDeviceNotFoundException($"Could not find any connected Yubikeys (Transport: {transport})", yubiKeys);
            }

            var minVersion = GetMinVersion(testDeviceType, minimumFirmwareVersion);
            var filteredDevices = yubiKeys.Where(d => d.FirmwareVersion >= minVersion);

            return testDeviceType switch
            {
                StandardTestDevice.Fw4Fips => SelectDevice(filteredDevices, isFipsSeries: true),
                StandardTestDevice.Fw5Fips => SelectDevice(filteredDevices, isFipsSeries: true),
                StandardTestDevice.Fw5Bio => SelectDevice(filteredDevices, [FormFactor.UsbCBiometricKeychain, FormFactor.UsbABiometricKeychain]),
                _ => SelectDevice(filteredDevices)
            };

            IYubiKeyDevice SelectDevice(
                IEnumerable<IYubiKeyDevice> devices,
                IEnumerable<FormFactor>? formFactors = null,
                bool isFipsSeries = false)
            {
                IYubiKeyDevice device = null!;
                try
                {
                    device = devices.First( d => (formFactors is null || formFactors.Contains(d.FormFactor)) &&
                        d.IsFipsSeries == isFipsSeries);
                }
                catch (InvalidOperationException)
                {
                    ThrowDeviceNotFoundException($"Target test device not found ({testDeviceType}, Transport: {transport})", devices);
                }

                return device;
            }
        }

        public static IYubiKeyDevice SelectByMinimumVersion(
            this IEnumerable<IYubiKeyDevice> yubiKeys,
            FirmwareVersion minimumFirmwareVersion)
        {
            if (!yubiKeys.Any())
            {
                throw new InvalidOperationException("Could not find any connected Yubikeys");
            }

            var device = yubiKeys.FirstOrDefault(d => d.FirmwareVersion >= minimumFirmwareVersion);
            if (device is null)
            {
                ThrowDeviceNotFoundException("No matching YubiKey found", yubiKeys);
            }

            return device;
        }

        private static FirmwareVersion GetMinVersion(StandardTestDevice testDeviceType, FirmwareVersion? minimumFirmwareVersion)
        {
            if (minimumFirmwareVersion is { })
            {
                return minimumFirmwareVersion;
            }

            return testDeviceType switch
            {
                StandardTestDevice.Fw3 => FirmwareVersion.V3_1_0,
                StandardTestDevice.Fw4Fips => FirmwareVersion.V4_0_0,
                _ => FirmwareVersion.V5_0_0,
            };
        }

        [DoesNotReturn]
        private static void ThrowDeviceNotFoundException(
            string errorMessage,
            IEnumerable<IYubiKeyDevice> devices)
        {
            var connectedDevicesText = FormatConnectedDevices(devices);
            throw new DeviceNotFoundException($"{errorMessage}. {connectedDevicesText}");
        }

        private static string FormatConnectedDevices(
            IEnumerable<IYubiKeyDevice> devices)
        {
            var deviceText =
                devices.Select(y => $"{{{y.FirmwareVersion}, {y.FormFactor}, IsFipsSeries: {y.IsFipsSeries}}}");

            return devices.Any()
                ? $"Connected devices: {string.Join(", ", deviceText)}"
                : string.Empty;
        }
    }

    // Custom test exception inheriting from InvalidOperationException as some test code depends on InvalidOperationExceptions 
    public class DeviceNotFoundException : InvalidOperationException
    {
        public DeviceNotFoundException(
            string message) : base(message)
        {
        }
    }
}
