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
using System.IO;
using System.Linq;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// Exposes methods that ensure integration tests only run on YubiKeys intended for testing.
    /// </summary>
    public sealed class IntegrationTestDeviceEnumeration
    {
        private static readonly Lazy<IntegrationTestDeviceEnumeration> _singleInstance
            = new Lazy<IntegrationTestDeviceEnumeration>(() => new IntegrationTestDeviceEnumeration());

        private static IntegrationTestDeviceEnumeration Instance => _singleInstance.Value;
        private readonly static string yubicoAppDataSubDirectory = "Yubico";
        private readonly static string blockListFileName = "BlockList.txt";
        private readonly HashSet<string> blockedSerialNumbers = new HashSet<string>();

        private IntegrationTestDeviceEnumeration()
        {
            string blockListFilePath = GetPath(yubicoAppDataSubDirectory, blockListFileName);
            blockedSerialNumbers = File.Exists(blockListFilePath)
                ? new HashSet<string>(File.ReadLines(blockListFilePath)) : new HashSet<string>();
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
                .Where(key => key.SerialNumber == null ||
                !Instance.blockedSerialNumbers.Contains(key.SerialNumber.Value.ToString()));

            return testYubiKeys.ToList();
        }
    }
}
