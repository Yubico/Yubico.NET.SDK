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
        public static IYubiKeyDevice RenewDeviceEnumeration(int serialNumber)
        {
            const int maxReconnectAttempts = 40;
            const int sleepDuration = 100; //ms

            int reconnectAttempts = 0;
            do
            {
                System.Threading.Thread.Sleep(sleepDuration);

                try
                {
                    return TestDev
                        .GetTestDevices()
                        .Single(d => d.SerialNumber == serialNumber);
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
        /// Thrown when <paramref name="testDevice"/> is not a recognized value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the input sequence did not contain a valid test device.
        /// </exception>
        public static IYubiKeyDevice SelectRequiredTestDevice(
            this IEnumerable<IYubiKeyDevice> yubiKeys,
            StandardTestDevice testDevice)
        {
            IEnumerable<IYubiKeyDevice> yubiKeyDevices = yubiKeys as IYubiKeyDevice[] ?? yubiKeys.ToArray();
            if (!yubiKeyDevices.Any())
            {
                throw new InvalidOperationException("Could not find any connected Yubikeys");
            }

            return testDevice switch
            {
                StandardTestDevice.Fw3 => SelectDevice(3),
                StandardTestDevice.Fw4Fips => SelectDevice(4, isFipsSeries: true),
                StandardTestDevice.Fw5 => SelectDevice(5, formFactor: null),
                StandardTestDevice.Fw5Fips => SelectDevice(5, formFactor: FormFactor.UsbAKeychain, isFipsSeries: true),
                StandardTestDevice.Fw5Bio => SelectDevice(5, formFactor: FormFactor.UsbABiometricKeychain),
                _ => throw new ArgumentException("Invalid test device value.", nameof(testDevice)),
            };

            IYubiKeyDevice SelectDevice(int majorVersion, FormFactor? formFactor = null, bool isFipsSeries = false)
            {
                try
                {
                    return yubiKeyDevices.First(d =>
                        d.FirmwareVersion.Major == majorVersion &&
                        (formFactor is null || d.FormFactor == formFactor) &&
                        d.IsFipsSeries == isFipsSeries);
                }
                catch (InvalidOperationException)
                {
                    string connectedDevices = yubiKeyDevices.Any()
                        ? "Connected devices: " + string.Join(", ",
                            yubiKeyDevices.Select(y => $"{{{y.FirmwareVersion}, {y.FormFactor}}}"))
                        : string.Empty;
                    throw new DeviceNotFoundException(
                        $"Target test device not found ({testDevice}). ({connectedDevices})");
                }
            }
        }
    }

    // Custom test exception inheriting from InvalidOperationException as some test code depends on InvalidOperationExceptions 
    public class DeviceNotFoundException : InvalidOperationException
    {
        public DeviceNotFoundException(string message) : base(message) { }
    }
}
