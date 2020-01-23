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
            const int maxReconnectAttempts = 10;
            const int sleepDuration = 100; //ms

            int reconnectAttempts = 0;
            do
            {
                System.Threading.Thread.Sleep(sleepDuration);

                try
                {
                    return
                        TestDev.GetTestDevices()
                        .Where(d => d.SerialNumber == serialNumber)
                        .Single();
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
        /// <seealso cref="SelectRequiredTestDevices(IEnumerable{IYubiKeyDevice}, int, bool)"/>
        public static IYubiKeyDevice SelectRequiredTestDevice(
            this IEnumerable<IYubiKeyDevice> yubiKeys,
            StandardTestDevice testDevice)
            => testDevice switch
            {
                StandardTestDevice.Fw3 => yubiKeys.SelectRequiredTestDevice(3, FormFactor.Unknown, false),
                StandardTestDevice.Fw4Fips => yubiKeys.SelectRequiredTestDevice(4, FormFactor.Unknown, true),
                StandardTestDevice.Fw5 => yubiKeys.SelectRequiredTestDevice(5, FormFactor.UsbAKeychain, false),
                StandardTestDevice.Fw5ci => yubiKeys.SelectRequiredTestDevice(5, FormFactor.UsbCLightning, false),
                StandardTestDevice.Fw5Fips => yubiKeys.SelectRequiredTestDevice(5, FormFactor.UsbAKeychain, true),
                _ => throw new ArgumentException("Invalid test device value.", nameof(testDevice)),
            };

        /// <summary>
        /// Retrieves a single <see cref="IYubiKeyDevice"/> based on test device requirements.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the input sequence did not contain a valid test device.
        /// </exception>
        public static IYubiKeyDevice SelectRequiredTestDevice(
            this IEnumerable<IYubiKeyDevice> yubiKeys,
            int? majorVersion,
            FormFactor? formFactor,
            bool? fipsSeries)
            => yubiKeys
                .Where(d =>
                    (majorVersion is null || d.FirmwareVersion.Major == majorVersion)
                    && (formFactor is null || d.FormFactor == formFactor)
                    && (fipsSeries is null || d.IsFipsSeries == fipsSeries))
                .First();
    }
}
