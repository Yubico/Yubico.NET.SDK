﻿// Copyright 2023 Yubico AB
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
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// Exposes methods that ensure integration tests only run on YubiKeys intended for testing.
    /// </summary>
    public sealed class IntegrationTestDeviceEnumeration
    {
        private static readonly Lazy<IntegrationTestDeviceEnumeration> SingleInstance
            = new Lazy<IntegrationTestDeviceEnumeration>(() => new IntegrationTestDeviceEnumeration());

        private static IntegrationTestDeviceEnumeration Instance => SingleInstance.Value;
        private const string YubikeyIntegrationtestAllowedKeysName = "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS";
        public readonly HashSet<string> AllowedSerialNumbers;

        public IntegrationTestDeviceEnumeration(string? configDirectory = null)
        {
            string whitelistFileName = $"{YubikeyIntegrationtestAllowedKeysName}.txt";
            var whiteListFilePath = Path.Combine(configDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yubico", whitelistFileName);

            CreateIfMissing(whiteListFilePath);

            AllowedSerialNumbers = File.Exists(whiteListFilePath)
                ? new HashSet<string>(File.ReadLines(whiteListFilePath))
                : new HashSet<string>();

            var allowedKeys = Environment.GetEnvironmentVariable(YubikeyIntegrationtestAllowedKeysName)
                ?.Split(':') ?? Array.Empty<string>();

            foreach (string allowedKey in allowedKeys)
            {
                _ = AllowedSerialNumbers.Add(allowedKey);
            }

            if (!AllowedSerialNumbers.Any())
            {
                throw new TestClassException("In order to prevent you from accidentally wiping your own important keys," +
                                             "you must add your whitelisted Yubikeys serial number to either the environment variable " +
                                             $"'{YubikeyIntegrationtestAllowedKeysName}' or to the file {whitelistFileName} at {whiteListFilePath}\n" +
                                             "For the environment variable, they should be added as a colon separated string, e.g: 1232332:347233\n" +
                                             "For the file, they should be added line by line, e.g: 1232332\n347233");
            }

            Debug.WriteLine("Loaded {0} keys(s) to allow list ({1})", AllowedSerialNumbers.Count, string.Join(",", AllowedSerialNumbers));
        }

        private static void CreateIfMissing(string whiteListFilePath)
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(whiteListFilePath)!);
            if (!File.Exists(whiteListFilePath))
            {
                var file = File.Create(whiteListFilePath);
                file.Close();
            }
        }

        /// <summary>
        /// Enumerates all YubiKey test devices on a system, using a provided block list
        /// </summary>
        /// <remarks>
        /// Instructions of setting up the blocklist.txt.
        ///
        /// The user needs to go to the LocalApplicationData location and create a folder
        /// called "Yubico" and a text file called "BlockList.txt" inside the Yubico Folder
        /// (Both case-insensitive).
        ///
        /// The block list text file should be in the C:\Users\<username>\AppData\Local\Yubico
        /// and /Users/<username>/.local/share/Yubico for mac user.
        ///
        /// The block list file contains the serial numbers of YubiKeys to be blocked.
        /// Each serial number should be on a separate line.
        /// </remarks>
        /// <returns>The allow list for Yubikey</returns>
        public static IList<IYubiKeyDevice> GetTestDevices(Transport transport = Transport.All)
        {
            return YubiKeyDevice
                .FindByTransport(transport)
                .Where(IsAllowedKey).ToList();

            static bool IsAllowedKey(IYubiKeyDevice key) => key.SerialNumber == null || Instance.AllowedSerialNumbers.Contains(key.SerialNumber.Value.ToString());
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
        /// <returns>A YubiKey that was found.</returns>
        public static IYubiKeyDevice GetTestDevice(
            StandardTestDevice testDeviceType,
            bool requireSerialNumber = true)
        {
            if (requireSerialNumber)
            {
                return GetTestDevices()
                    .Where(d => d.SerialNumber.HasValue)
                    .SelectRequiredTestDevice(testDeviceType);
            }

            return GetTestDevices()
                .SelectRequiredTestDevice(testDeviceType);
        }

        /// <summary>
        /// Get YubiKey test device of specified transport and for which the
        /// firmware version number is at least the minimum.
        /// </summary>
        /// <param name="transport">The transport the device must support.</param>
        /// <param name="minimumFirmwareVersion">The earliest version number the
        /// caller is willing to accept.</param>
        /// <returns>A YubiKey that was found.</returns>
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
