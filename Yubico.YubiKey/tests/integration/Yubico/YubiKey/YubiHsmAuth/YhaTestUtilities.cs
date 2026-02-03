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

using System.IO;
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
        public static readonly byte[] TestingCredPassword = "helloworldenders"u8.ToArray();
            //new byte[16] { 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        public static readonly byte[] DefaultCredEncKey =
            new byte[16] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };
        public static readonly byte[] DefaultCredMacKey =
            new byte[16] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 };

        public static readonly string DefaultCredLabel = "abc";
        public static readonly bool DefaultCredTouchRequired = false;

        public static readonly byte[] DefaultHostChallenge =
            new byte[65] { 0x04, 0x68, 0x01, 0x03, 0xf0, 0x7e, 0xbe, 0x8e, 0x9f, 0x8c, 0x56, 0xa3, 0x9b, 0xa9, 0x6c, 0xc7, 
            0xa0, 0xf2, 0x36, 0xd9, 0x4f, 0x68, 0x41, 0x0a, 0x05, 0xc6, 0x2a, 0x16, 0x75, 0xc1, 0x47, 0x12, 
            0x07, 0x9a, 0x93, 0x45, 0x3f, 0x9a, 0x52, 0xf7, 0x6e, 0xb8, 0x7e, 0x75, 0xc5, 0xa0, 0xd6, 0x00, 
            0xad, 0x88, 0xc8, 0x43, 0xb2, 0x60, 0x82, 0x0a, 0x6d, 0xf9, 0x78, 0x20, 0x5b, 0x2b, 0x38, 0x8b, 
            0xac}; //context
        public static readonly byte[] DefaultHsmDeviceChallenge =
            new byte[65] { 0x04, 0x68, 0x01, 0x03, 0xf0, 0x7e, 0xbe, 0x8e, 0x9f, 0x8c, 0x56, 0xa3, 0x9b, 0xa9, 0x6c, 0xc7, 
            0xa0, 0xf2, 0x36, 0xd9, 0x4f, 0x68, 0x41, 0x0a, 0x05, 0xc6, 0x2a, 0x16, 0x75, 0xc1, 0x47, 0x12, 
            0x07, 0x9a, 0x93, 0x45, 0x3f, 0x9a, 0x52, 0xf7, 0x6e, 0xb8, 0x7e, 0x75, 0xc5, 0xa0, 0xd6, 0x00, 
            0xad, 0x88, 0xc8, 0x43, 0xb2, 0x60, 0x82, 0x0a, 0x6d, 0xf9, 0x78, 0x20, 0x5b, 0x2b, 0x38, 0x8b, 
            0xac }; //context
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
            new byte[32] {
                0x54, 0x9d, 0x2a, 0x8a, 0x03, 0xe6, 0x2d, 0xc8,
                0x29, 0xad, 0xe4, 0xd6, 0x85, 0x0d, 0xb9, 0x56, 
                0x84, 0x75, 0x14, 0x7c, 0x59, 0xef, 0x23, 0x8f, 
                0x12, 0x2a, 0x08, 0xcf, 0x55, 0x7c, 0xdb, 0x91};
        public static readonly byte[] TestingEccP256PrivateKey =
            new byte[32] {
                0x87, 0x68, 0x8E, 0x27, 0x6E, 0x91, 0x94, 0xFF, 
                0x7E, 0xB9, 0x25, 0xFC, 0x38, 0x70, 0x0F, 0xA8, 
                0xD3, 0x20, 0x05, 0x2A, 0x8F, 0x67, 0x37, 0x96, 
                0x4E, 0x41, 0x2D, 0xEB, 0xBE, 0x48, 0x52, 0x5F};

        public static readonly byte[] AlternateEccP256PrivateKey =
            new byte[32] {
                0x87, 0x68, 0x8E, 0x27, 0x6E, 0x91, 0x94, 0xFF, 
                0x7E, 0xB9, 0x25, 0xFC, 0x38, 0x70, 0x0F, 0xA8, 
                0xD3, 0x20, 0x05, 0x2A, 0x8F, 0x67, 0x37, 0x96, 
                0x4E, 0x41, 0x2D, 0xEB, 0xBE, 0x48, 0x52, 0x5F
            };

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
            0x04, 0x22, 0x3F, 0x87, 0x59, 0xD8, 0xAF, 0xE7, 0x86, 0x9F, 0x7F, 0x4C, 0xDB, 0xFD, 0xC3, 0x01, 
            0xB4, 0x2E, 0x4B, 0x39, 0x86, 0x43, 0xBB, 0x56, 0xA1, 0xB8, 0x0B, 0x98, 0xE1, 0xC2, 0x6F, 0x56, 
            0x74, 0x1E, 0x87, 0x17, 0x5C, 0x12, 0xCD, 0x34, 0x41, 0x39, 0xEB, 0x6D, 0x7F, 0x1E, 0x15, 0x03, 
            0xFC, 0x91, 0x63, 0x11, 0x27, 0x23, 0x52, 0x25, 0x54, 0x55, 0x10, 0x98, 0x9E, 0xAB, 0x66, 0x16, 
            0xCF
        };

        public static readonly byte [] cardCryptoDefault = new byte[16] 
        {
            0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE, 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE
        };



        // For verification purposes
        public static readonly byte [] TestingCryptogram = new byte[16] 
        {
            0x5b, 0xbf, 0x53, 0xad, 0xc4, 0x0b, 0x71, 0x3e, 0xc5, 0xd5, 0x1d, 0x00, 0xf7, 0x30, 0x32, 0x44
        };

        public static readonly byte [] EPK_OCE = new byte[65] 
        {
            0x04, 0x6f, 0x45, 0xe9, 0x3b, 0xb7, 0x14, 0xf9, 0x88, 0x56, 0xb9, 0xa1, 0x05, 0xda, 0x45, 0x62,
            0x70, 0x0e, 0xb8, 0x2e, 0x92, 0xa2, 0xc6, 0x42, 0x08, 0xed, 0x78, 0x90, 0x94, 0x7f, 0xd4, 0x78,
            0x20, 0xa5, 0x67, 0xb2, 0xed, 0x4c, 0x48, 0x96, 0x8d, 0x92, 0xb5, 0x32, 0x6c, 0xb2, 0x67, 0xc5,
            0xb9, 0x87, 0xe0, 0x4a, 0x34, 0xb3, 0x3d, 0xa1, 0x74, 0xe0, 0x2f, 0xd9, 0x2e, 0x8e, 0x71, 0xdf,
            0x2c
        };

        public static readonly byte [] EPK_SD = new byte[65] 
        {
            0x04, 0x78, 0xef, 0xb8, 0x00, 0x54, 0xa8, 0xbd, 0x72, 0x72, 0x88, 0xb7, 0x87, 0xf5, 0x19, 0xe4,
            0xf0, 0x5e, 0x02, 0x2b, 0x2e, 0xf9, 0x77, 0x04, 0x35, 0x2e, 0x2b, 0x2c, 0x28, 0x75, 0xbf, 0xbc,
            0x21, 0x3b, 0x51, 0x2f, 0x9f, 0xa4, 0x0d, 0x7e, 0x46, 0xc3, 0x55, 0x33, 0x0a, 0x74, 0x4f, 0x9c,
            0x9c, 0xaf, 0x71, 0x8d, 0xf7, 0xbf, 0x78, 0x22, 0x43, 0x53, 0x34, 0x43, 0xb0, 0x69, 0x7d, 0x8e,
            0xf7
        };

        public static readonly byte [] HSMChallenge = new byte[16] 
        {
            0xef, 0xb8, 0x00, 0x54, 0xa8, 0xbd, 0x72, 0x72, 0x88, 0xb7, 0x87, 0xf5, 0x19, 0xe4, 0xf0, 0x5e
        };

        public static readonly byte [] YubiKeyChallenge = new byte[16] 
        {
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public static readonly byte [] HSMPublicKey = new byte[65]
        {
            0x04, 0x81, 0xff, 0x96, 0x5e, 0xc9, 0xee, 0xbd, 0x94, 0xcf, 0xe5, 0xf1, 0xa2, 0xe5, 0xc3, 0x51,
            0xee, 0x2b, 0x7d, 0xc0, 0xe9, 0xe9, 0x0a, 0x12, 0x36, 0xfa, 0x5f, 0xed, 0xab, 0x5c, 0x28, 0x7e,
            0xd4, 0xdb, 0x82, 0xd0, 0xf0, 0x64, 0x5e, 0xb7, 0xe5, 0x38, 0xbd, 0x67, 0x4b, 0x1f, 0x16, 0x18,
            0x81, 0x1a, 0x59, 0x93, 0x03, 0xca, 0xe3, 0x0f, 0xb2, 0xa2, 0x34, 0x61, 0x07, 0x2e, 0xe5, 0x0c,
            0x73
        };
        

        /// <summary>
        /// Returns an ECC P-256 credential with values from the default set.
        /// </summary>
        public static readonly EccP256CredentialWithSecrets DefaultEccP256Cred =
            new EccP256CredentialWithSecrets(
                TestingCredPassword,
                TestingEccP256PrivateKey,
                DefaultCredLabel,
                DefaultCredTouchRequired);

        public static readonly EccP256CredentialWithSecrets TestingEccP256Cred =
            new EccP256CredentialWithSecrets(
                TestingCredPassword,
                TestingEccP256PrivateKey,
                DefaultCredLabel,
                DefaultCredTouchRequired);

        /// <summary>
        /// Returns an ECC P-256 credential with values from the alternate set.
        /// </summary>
        public static readonly EccP256CredentialWithSecrets AlternateEccP256Cred =
            new EccP256CredentialWithSecrets(
                AlternateCredPassword,
                AlternateEccP256PrivateKey,
                DefaultCredLabel,
                DefaultCredTouchRequired);
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

        public static void AddTestingEccP256Credential(IYubiKeyDevice testDevice)
        {
            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                yubiHsmAuthSession.AddCredential(DefaultMgmtKey, TestingEccP256Cred);
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

        public static byte [] CreateHostChallengeEccP256(IYubiKeyDevice testDevice)
        {
            using (var yubiHsmAuthSession = new YubiHsmAuthSession(testDevice))
            {
                return yubiHsmAuthSession.CreateHostChallengeEccP256(DefaultEccP256Cred);
            }
        }
    }
}
