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
    /// %LOCALAPPDATA%\Yubico\YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt for Windows users
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

        private const string YUBIKEY_INTEGRATIONTEST_ALLOWED_KEYS_VAR_NAME = "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS";
        private static readonly Lazy<IntegrationTestDeviceEnumeration> LazyInstance =
            new Lazy<IntegrationTestDeviceEnumeration>(() => new IntegrationTestDeviceEnumeration());

        private readonly string _allowlistFileName = $"{YUBIKEY_INTEGRATIONTEST_ALLOWED_KEYS_VAR_NAME}.txt";
        private readonly string? _configDirectory;
        private string SetupMessage => "In order to prevent you from accidentally wiping your own important keys," +
                    "you must add your allow-listed Yubikeys serial number to either the environment variable " +
                    $"'{YUBIKEY_INTEGRATIONTEST_ALLOWED_KEYS_VAR_NAME}' or to the file {_allowlistFileName} at {AllowListFilePath}\n" +
                    "For the environment variable, they should be added as a colon separated string, e.g: 1232332:347233\n" +
                    "For the file, they should be added line by line, e.g: 1232332\n347233";

        private string AllowListFilePath => Path.Combine(_configDirectory ?? DefaultDirectory, _allowlistFileName);
        private static IntegrationTestDeviceEnumeration Instance => LazyInstance.Value;
        private static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yubico");
        public readonly HashSet<string> AllowedSerialNumbers;

        public IntegrationTestDeviceEnumeration(string? configDirectory = null)
        {
            _configDirectory = configDirectory;

            Debug.WriteLine(SetupMessage);
            CreateAllowListFileIfMissing(AllowListFilePath);

            // Load the allow-listed serial numbers from the file
            AllowedSerialNumbers = File.Exists(AllowListFilePath)
                ? new HashSet<string>(File.ReadLines(AllowListFilePath))
                : new HashSet<string>();

            // Load the allow-listed serial numbers from the environment variable
            var allowedSerialNumbersFromEnv = Environment
                .GetEnvironmentVariable(YUBIKEY_INTEGRATIONTEST_ALLOWED_KEYS_VAR_NAME)?
                .Split(':') ?? Array.Empty<string>();

            // Add the serial numbers from the environment variable to the allow-list
            foreach (var allowedSerialNumber in allowedSerialNumbersFromEnv)
            {
                _ = AllowedSerialNumbers.Add(allowedSerialNumber);
            }

            if (!AllowedSerialNumbers.Any())
            {
                throw new TestClassException(SetupMessage);
            }

            Debug.WriteLine("Loaded {0} keys(s) to allow list ({1})", AllowedSerialNumbers.Count,
                string.Join(",", AllowedSerialNumbers));
        }

        /// <summary>
        /// Gets a Yubikey device by its serial number
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        public static IYubiKeyDevice GetBySerial(
            int serialNumber)
            => GetTestDevices().Single(d => d.SerialNumber == serialNumber);

        /// <summary>
        /// Enumerates all YubiKey test devices on a system.
        /// </summary>
        /// <returns>The allow-list filtered list of available Yubikeys</returns>
        public static IList<IYubiKeyDevice> GetTestDevices(
            Transport transport = Transport.All)
        {
            var devices = YubiKeyDevice
                .FindByTransport(transport)
                .Where(IsAllowedKey)
                .ToList();
            
            return devices;
            
            static bool IsAllowedKey(
                IYubiKeyDevice key)
                => key.SerialNumber == null ||
                   Instance.AllowedSerialNumbers.Contains(key.SerialNumber.Value.ToString());
        }

        /// <summary>
        /// Get YubiKey test device of specified type available on a system.
        /// </summary>
        /// <param name="testDeviceType">The type of the device.</param>
        /// <param name="transport">The transport the device must support.</param>
        /// <param name="minimumFirmwareVersion">The earliest version number the
        /// caller is willing to accept. Defaults to the minimum version for the given device.</param>
        /// <returns>The allow-list filtered YubiKey that was found.</returns>
        public static IYubiKeyDevice GetTestDevice(
            StandardTestDevice testDeviceType = StandardTestDevice.Fw5,
            Transport transport = Transport.All,
            FirmwareVersion? minimumFirmwareVersion = null)
            => GetTestDevices(transport)
                .SelectByStandardTestDevice(testDeviceType, minimumFirmwareVersion, transport);

        /// <summary>
        /// Get YubiKey test device of specified transport and for which the
        /// firmware version number is at least the minimum.
        /// </summary>
        /// <param name="transport">The transport the device must support.</param>
        /// <param name="minimumFirmwareVersion">The earliest version number the
        /// caller is willing to accept.</param>
        /// <returns>The allow-list filtered YubiKey that was found.</returns>
        public static IYubiKeyDevice GetTestDevice(
            Transport transport,
            FirmwareVersion minimumFirmwareVersion)
            => GetTestDevices(transport)
                .SelectByMinimumVersion(minimumFirmwareVersion);

        private static void CreateAllowListFileIfMissing(string allowListFilePath)
        {
            if (File.Exists(allowListFilePath))
            {
                return;
            }

            _ = Directory.CreateDirectory(Path.GetDirectoryName(allowListFilePath)!);

            var file = File.Create(allowListFilePath);
            file.Close();
        }
    }
}
