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
using System.IO;
using System.Linq;
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
        private readonly static string YubicoAppDataSubDirectory = "Yubico";
        private const string BlockListFileName = "BlockList.txt";
        private readonly HashSet<string> _blockedSerialNumbers;
        private readonly HashSet<string> _allowedSerialNumbers;

        private IntegrationTestDeviceEnumeration()
        {
            var blockListFilePath = GetPath(YubicoAppDataSubDirectory, BlockListFileName);
            _blockedSerialNumbers = File.Exists(blockListFilePath)
                ? new HashSet<string>(File.ReadLines(blockListFilePath)) 
                : new HashSet<string>();

            var allowedKeys = Environment.GetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS")
                ?.Split(':') ?? Array.Empty<string>();
            _allowedSerialNumbers = allowedKeys.Any() 
                ? new HashSet<string>(allowedKeys) 
                : new HashSet<string>();
        }

        private static string GetPath(string appDataSubDirectory, string filename) =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appDataSubDirectory,
                filename);

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
            IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindByTransport(transport);

            IEnumerable<IYubiKeyDevice> testYubiKeys = yubiKeyList
                .Where(IsNotBlockedKey)
                .Where(IsAllowedKey);
            
            return testYubiKeys.ToList();
            
            static bool IsNotBlockedKey(IYubiKeyDevice key) => key.SerialNumber == null || !Instance._blockedSerialNumbers.Contains(key.SerialNumber.Value.ToString());
            static bool IsAllowedKey(IYubiKeyDevice key) => key.SerialNumber == null || Instance._allowedSerialNumbers.Contains(key.SerialNumber.Value.ToString());
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
        public static IYubiKeyDevice GetTestDevice(StandardTestDevice testDeviceType, bool requireSerialNumber = true)
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

        /// <summary>
        /// Get YubiKey test device connected using SCP03 (with the default
        /// static keys). Find the first one, regardless of the type (Fw5, Fw5C,
        /// Bio, etc.).
        /// </summary>
        /// <remarks>
        /// Note that SCP03 is available on 5.3 and later YubiKeys.
        /// </remarks>
        /// <param name="testDeviceType">The type of the device.</param>
        /// <returns>A YubiKey that was found.</returns>
        public static IYubiKeyDevice GetScp03TestDevice()
        {
            return GetScp03TestDevice(new StaticKeys());
        }

        /// <summary>
        /// Get YubiKey test device connected using SCP03 and the given key set.
        /// </summary>
        public static IYubiKeyDevice GetScp03TestDevice(StaticKeys staticKeys)
        {
            IList<IYubiKeyDevice> deviceList = GetTestDevices(Transport.SmartCard);
            foreach (IYubiKeyDevice currentDevice in deviceList)
            {
                if (currentDevice.FirmwareVersion >= FirmwareVersion.V5_3_0)
                {
                    if (currentDevice is YubiKeyDevice ykDevice)
                    {
#pragma warning disable CS0618 // Specifically testing this soon-to-be-deprecated feature
                        return ykDevice.WithScp03(staticKeys);
#pragma warning restore CS0618
                    }
                }
            }

            throw new InvalidOperationException("No matching YubiKey found.");
        }
    }
}
