﻿// Copyright 2022 Yubico AB
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

using System.Collections.Generic;
using System.Linq;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// This class provides the most values and methods used during
    /// integration testing of the YubiHSM Auth application.
    /// </summary>
    /// <seealso cref="SimpleKeyCollector"/>
    public class YhaTestUtilities
    {
        private static readonly FirmwareVersion MinimumFirmwareVersion = new FirmwareVersion(5, 4, 3);

        #region default
        public static readonly byte[] DefaultMgmtKey =
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public static readonly byte[] DefaultCredPassword =
            new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        public static readonly byte[] DefaultCredEncKey =
            new byte[16] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };
        public static readonly byte[] DefaultCredMacKey =
            new byte[16] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 };

        public static readonly string DefaultCredLabel = "abc";
        public static readonly bool DefaultCredTouchRequired = false;

        public static readonly byte[] DefaultHostChallenge =
            new byte[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
        public static readonly byte[] DefaultHsmDeviceChallenge =
            new byte[8] { 0, 2, 4, 6, 8, 10, 12, 14 };
        #endregion

        #region alternate
        public static readonly byte[] AlternateMgmtKey =
            new byte[16] { 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4 };

        public static readonly byte[] AlternateCredPassword =
            new byte[16] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        public static readonly byte[] AlternateCredEncKey =
            new byte[16] { 30, 28, 26, 24, 22, 20, 18, 16, 14, 12, 10, 8, 6, 4, 2, 0 };
        public static readonly byte[] AlternateCredMacKey =
            new byte[16] { 31, 29, 27, 25, 23, 21, 19, 17, 15, 13, 11, 9, 7, 5, 3, 1 };

        public static readonly string AlternateCredLabel = "xyz";
        public static readonly bool AlternateCredTouchRequired = true;

        public static readonly byte[] AlternateHostChallenge =
            new byte[8] { 7, 6, 5, 4, 3, 2, 1, 0 };
        public static readonly byte[] AlternateHsmDeviceChallenge =
            new byte[8] { 14, 12, 10, 8, 6, 4, 2, 0 };
        #endregion

        /// <summary>
        /// Returns an AES-128 credential with values from the default set.
        /// </summary>
        public static readonly Aes128CredentialWithSecrets DefaultAes128Cred =
            new Aes128CredentialWithSecrets(
                DefaultCredPassword,
                DefaultCredEncKey,
                DefaultCredMacKey,
                DefaultCredLabel,
                DefaultCredTouchRequired);

        /// <summary>
        /// Returns an AES-128 credential with values from the alternate set.
        /// </summary>
        public static readonly Aes128CredentialWithSecrets AlternateAes128Cred =
            new Aes128CredentialWithSecrets(
                AlternateCredPassword,
                AlternateCredEncKey,
                AlternateCredMacKey,
                AlternateCredLabel,
                AlternateCredTouchRequired);

        /// <summary>
        /// Finds a device with the minimum firmware version (see <see cref="MinimumFirmwareVersion"/>),
        /// and puts the device into a known "control" state for performing integration
        /// tests with the YubiHSM Auth application.
        /// </summary>
        public static IYubiKeyDevice GetCleanDevice() => GetCleanDevice(MinimumFirmwareVersion);

        /// <summary>
        /// Finds a device with the matching FirmwareVersion, and puts the device
        /// into a known "control" state for performing integration
        /// tests with the YubiHSM Auth application.
        /// </summary>
        private static IYubiKeyDevice GetCleanDevice(FirmwareVersion fwVersion)
        {
            IList<IYubiKeyDevice>? testDevices = IntegrationTestDeviceEnumeration.GetTestDevices();
            IYubiKeyDevice testDevice = testDevices
                .First(d =>
                    d.FirmwareVersion >= fwVersion &&
                    d.SerialNumber.HasValue);

            testDevice = DeviceReset.EnableAllCapabilities(testDevice);

            return DeviceReset.ResetYubiHsmAuth(testDevice);
        }

        /// <summary>
        /// Adds an AES-128 credential (with values from the default set) to the
        /// YubiHSM Auth application.
        /// </summary>
        public static void AddDefaultAes128Credential(IYubiKeyDevice testDevice)
        {
            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.AddCredential(DefaultMgmtKey, DefaultAes128Cred);
            }
        }

        /// <summary>
        /// Adds an AES-128 credential (with values from the alternate set) to the
        /// YubiHSM Auth application. It uses the default management key.
        /// </summary>
        public static void AddAlternateAes128Credential(IYubiKeyDevice testDevice)
        {
            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.AddCredential(DefaultMgmtKey, AlternateAes128Cred);
            }
        }
    }
}
