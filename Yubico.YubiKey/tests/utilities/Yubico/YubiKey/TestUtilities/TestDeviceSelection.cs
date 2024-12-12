// Copyright 2023 Yubico AB
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
            const int sleepDuration = 100; //ms

            int reconnectAttempts = 0;
            do
            {
                System.Threading.Thread.Sleep(sleepDuration);

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
            var devices = yubiKeys as IYubiKeyDevice[] ?? yubiKeys.ToArray();
            if (!devices.Any())
            {
                ThrowDeviceNotFoundException("Could not find any connected Yubikeys (Transport: {transport})", devices);
            }

            var devicesVersionFiltered =
                devices.Where(d => d.FirmwareVersion >= MatchVersion(testDeviceType, minimumFirmwareVersion));

            return testDeviceType switch
            {
                StandardTestDevice.Fw3 => SelectDevice(3),
                StandardTestDevice.Fw4Fips => SelectDevice(4, isFipsSeries: true),
                StandardTestDevice.Fw5 => SelectDevice(5),
                StandardTestDevice.Fw5Fips => SelectDevice(5, isFipsSeries: true),
                StandardTestDevice.Fw5Bio => SelectDevice(5, formFactor: FormFactor.UsbABiometricKeychain),
                _ => throw new ArgumentException("Invalid test device value.", nameof(testDeviceType)),
            };

            IYubiKeyDevice SelectDevice(
                int majorVersion,
                FormFactor? formFactor = null,
                bool isFipsSeries = false)
            {
                IYubiKeyDevice device = null!;
                try
                {
                    bool MatchingDeviceSelector(
                        IYubiKeyDevice d) =>
                        d.FirmwareVersion.Major == majorVersion &&
                        (formFactor is null || d.FormFactor == formFactor) &&
                        d.IsFipsSeries == isFipsSeries;

                    device = devicesVersionFiltered.First(MatchingDeviceSelector);
                }
                catch (InvalidOperationException)
                {
                    ThrowDeviceNotFoundException($"Target test device not found ({testDeviceType}, Transport: {transport})", devices);
                }

                return device;
            }
        }

        private static FirmwareVersion MatchVersion(
            StandardTestDevice testDeviceType,
            FirmwareVersion? minimumFirmwareVersion)
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

        public static IYubiKeyDevice SelectByMinimumVersion(
            this IEnumerable<IYubiKeyDevice> yubiKeys,
            FirmwareVersion minimumFirmwareVersion)
        {
            var devices = yubiKeys as IYubiKeyDevice[] ?? yubiKeys.ToArray();
            if (!devices.Any())
            {
                throw new InvalidOperationException("Could not find any connected Yubikeys");
            }

            var device = devices.FirstOrDefault(d => d.FirmwareVersion >= minimumFirmwareVersion);
            if (device is null)
            {
                ThrowDeviceNotFoundException("No matching YubiKey found", devices);
            }

            return device;
        }

        [DoesNotReturn]
        private static void ThrowDeviceNotFoundException(
            string errorMessage,
            IYubiKeyDevice[] devices)
        {
            var connectedDevicesText = FormatConnectedDevices(devices);
            throw new DeviceNotFoundException($"{errorMessage}. {connectedDevicesText}");
        }

        private static string FormatConnectedDevices(
            IReadOnlyCollection<IYubiKeyDevice> devices)
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
