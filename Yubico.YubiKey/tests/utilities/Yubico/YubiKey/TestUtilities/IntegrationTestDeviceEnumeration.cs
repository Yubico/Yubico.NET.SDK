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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit.Sdk;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// Exposes methods that ensure integration tests only run on YubiKeys intended for testing.
    /// Enumerates all YubiKey test devices on a system
    /// <remarks>
    /// Instructions for setting up the allow-list file:
    ///
    /// The user needs to add their Yubikeys serial numbers to the allow-list file which is located at
    /// C:\Users\&lt;username&gt;\AppData\Local\Yubico\YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt for Windows users
    /// and /Users/&lt;username&gt;/.local/share/Yubico/YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt for macOS users.
    /// The SDK attempts to create the file if it doesn't already exist.
    ///
    /// The allow-list file contains the serial numbers of YubiKeys to be allowed to run integration tests.
    /// Each serial number should be on a separate line.
    ///
    /// If you prefer to use environment variables instead, you can add the allowed serial numbers to the
    /// YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS in a colon-separated string. E.g: 1223344:3443343
    /// </remarks>
    /// </summary>
    public sealed class IntegrationTestDeviceEnumeration
    {
        public readonly HashSet<string> AllowedSerialNumbers;

        private static readonly Lazy<IntegrationTestDeviceEnumeration> SingleInstance
            = new Lazy<IntegrationTestDeviceEnumeration>(() => new IntegrationTestDeviceEnumeration());

        private static IntegrationTestDeviceEnumeration Instance => SingleInstance.Value;
        private const string YubikeyIntegrationtestAllowedKeysName = "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS";
        private readonly string _allowlistFileName = $"{YubikeyIntegrationtestAllowedKeysName}.txt";

        public IntegrationTestDeviceEnumeration(string? configDirectory = null)
        {
            var allowListFilePath = Path.Combine(
                configDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Yubico",
                _allowlistFileName);

            CreateIfMissing(allowListFilePath);

            AllowedSerialNumbers = File.Exists(allowListFilePath)
                ? new HashSet<string>(File.ReadLines(allowListFilePath))
                : new HashSet<string>();

            var allowedKeys = Environment.GetEnvironmentVariable(YubikeyIntegrationtestAllowedKeysName)
                ?.Split(':') ?? Array.Empty<string>();

            foreach (string allowedKey in allowedKeys)
            {
                _ = AllowedSerialNumbers.Add(allowedKey);
            }

            if (!AllowedSerialNumbers.Any())
            {
                throw new TestClassException(
                    "In order to prevent you from accidentally wiping your own important keys," +
                    "you must add your allowlisted Yubikeys serial number to either the environment variable " +
                    $"'{YubikeyIntegrationtestAllowedKeysName}' or to the file {_allowlistFileName} at {allowListFilePath}\n" +
                    "For the environment variable, they should be added as a colon separated string, e.g: 1232332:347233\n" +
                    "For the file, they should be added line by line, e.g: 1232332\n347233");
            }

            Debug.WriteLine("Loaded {0} keys(s) to allow list ({1})", AllowedSerialNumbers.Count,
                string.Join(",", AllowedSerialNumbers));
        }

        private static void CreateIfMissing(string allowListFilePath)
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(allowListFilePath)!);
            if (File.Exists(allowListFilePath))
            {
                return;
            }

            var file = File.Create(allowListFilePath);
            file.Close();
        }

        /// <summary>
        /// Enumerates all YubiKey test devices on a system.
        /// </summary>
        /// <returns>The allow-list filtered list of available Yubikeys</returns>
        public static IList<IYubiKeyDevice> GetTestDevices(Transport transport = Transport.All)
        {
            return YubiKeyDevice
                .FindByTransport(transport)
                .Where(IsAllowedKey).ToList();

            static bool IsAllowedKey(IYubiKeyDevice key)
                => key.SerialNumber == null ||
                   Instance.AllowedSerialNumbers.Contains(key.SerialNumber.Value.ToString());
        }

        /// <summary>
        /// Get YubiKey test device of specified type available on a system.
        /// </summary>
        /// <param name="testDeviceType">The type of the device.</param>
        /// <param name="requireSerialNumber">A boolean indicating if the caller
        /// wants to require finding YubiKeys with serial numbers only. If
        /// <c>true</c> the method will only examine YubiKeys have a visible
        /// serial number. This is the default, if no <c>requireSerialNumber</c>
        /// arg is given, then this method will require serial numbers. If
        /// <c>false</c>, the method will examine all YubiKeys, whether the
        /// serial number is visible or not.</param>
        /// <returns>The allow-list filtered YubiKey that was found.</returns>
        public static IYubiKeyDevice GetTestDevice(
            StandardTestDevice testDeviceType,
            bool requireSerialNumber = true)
        {
            var devices = GetTestDevices();
            if (requireSerialNumber)
            {
                devices = devices.Where(d => d.SerialNumber.HasValue).ToList();
            }

            return devices.SelectRequiredTestDevice(testDeviceType);
        }

        /// <summary>
        /// Get YubiKey test device of specified transport and for which the
        /// firmware version number is at least the minimum.
        /// </summary>
        /// <param name="transport">The transport the device must support.</param>
        /// <param name="minimumFirmwareVersion">The earliest version number the
        /// caller is willing to accept.</param>
        /// <returns>The allow-list filtered YubiKey that was found.</returns>
        public static IYubiKeyDevice GetTestDevice(Transport transport, FirmwareVersion minimumFirmwareVersion)
        {
            IList<IYubiKeyDevice> deviceList = GetTestDevices(transport);
            foreach (IYubiKeyDevice currentDevice in deviceList)
            {
                if (currentDevice.FirmwareVersion >= minimumFirmwareVersion)
                {
                    return currentDevice;
                }
            }

            throw new InvalidOperationException("No matching YubiKey found.");
        }
    }
}
