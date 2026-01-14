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

        #region ECC P-256
        public static readonly byte[] DefaultEccP256PrivateKey =
            new byte[32] {0x54, 0x9d, 0x2a, 0x8a, 0x03, 0xe6, 0x2d, 0xc8, 0x29, 0xad, 0xe4, 0xd6, 0x85, 0x0d, 0xb9, 0x56, 0x84, 0x75, 0x14, 0x7c, 0x59, 0xef, 0x23, 0x8f, 0x12, 0x2a, 0x08, 0xcf, 0x55, 0x7c, 0xdb, 0x91};

        public static readonly byte[] AlternateEccP256PrivateKey =
            new byte[32] { 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16,
                          15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };

        public static readonly byte[] DefaultEccP256PublicKey = new byte[65] 
        { 
            0x04, 0x68, 0x01, 0x03, 0xf0, 0x7e, 0xbe, 0x8e, 0x9f, 0x8c, 0x56, 0xa3, 0x9b, 0xa9, 0x6c, 0xc7, 
            0xa0, 0xf2, 0x36, 0xd9, 0x4f, 0x68, 0x41, 0x0a, 0x05, 0xc6, 0x2a, 0x16, 0x75, 0xc1, 0x47, 0x12, 
            0x07, 0x9a, 0x93, 0x45, 0x3f, 0x9a, 0x52, 0xf7, 0x6e, 0xb8, 0x7e, 0x75, 0xc5, 0xa0, 0xd6, 0x00, 
            0xad, 0x88, 0xc8, 0x43, 0xb2, 0x60, 0x82, 0x0a, 0x6d, 0xf9, 0x78, 0x20, 0x5b, 0x2b, 0x38, 0x8b, 
            0xac
        };

        public static readonly byte[] AlternateEccP256PublicKey = new byte[65] 
        { 
            0x04, // Uncompressed prefix
            31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16,
            15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, // X-coordinate
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  // Y-coordinate (placeholder)
        };

        public static readonly byte [] cardCryptoDefault = new byte[16] 
        {
            0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE, 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE
        };

        /// <summary>
        /// Returns an ECC P-256 credential with values from the default set.
        /// </summary>
        public static readonly EccP256CredentialWithSecrets DefaultEccP256Cred =
            new EccP256CredentialWithSecrets(
                DefaultCredPassword,
                DefaultEccP256PrivateKey,
                DefaultCredLabel,
                DefaultCredTouchRequired);

        /// <summary>
        /// Returns an ECC P-256 credential with values from the alternate set.
        /// </summary>
        public static readonly EccP256CredentialWithSecrets AlternateEccP256Cred =
            new EccP256CredentialWithSecrets(
                AlternateCredPassword,
                AlternateEccP256PrivateKey,
                AlternateCredLabel,
                AlternateCredTouchRequired);
        #endregion

        /// <summary>
        /// Finds a standard device and puts the device
        /// into a known "control" state for performing integration
        /// tests with the YubiHSM Auth application.
        /// </summary>
        public static IYubiKeyDevice GetCleanDevice(StandardTestDevice testDeviceType = StandardTestDevice.Fw5)
        {
            var testDevice = DeviceReset.EnableAllCapabilities(IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType));
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

        /// <summary>
        /// Adds an ECC P-256 credential (with values from the default set) to the
        /// YubiHSM Auth application.
        /// </summary>
        public static void AddDefaultEccP256Credential(IYubiKeyDevice testDevice)
        {
            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.AddCredential(DefaultMgmtKey, DefaultEccP256Cred);
            }
        }

        /// <summary>
        /// Adds an ECC P-256 credential (with values from the alternate set) to the
        /// YubiHSM Auth application. It uses the default management key.
        /// </summary>
        public static void AddAlternateEccP256Credential(IYubiKeyDevice testDevice)
        {
            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.AddCredential(DefaultMgmtKey, AlternateEccP256Cred);
            }
        }
    }
}
