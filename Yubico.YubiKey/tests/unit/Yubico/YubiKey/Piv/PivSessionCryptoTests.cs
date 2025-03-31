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
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionCryptoTests
    {
        [Fact]
        public void Sign_InvalidSlot_Exception()
        {
            byte[] dataToSign = new byte[128];
            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            random.GetBytes(dataToSign);
            dataToSign[0] &= 0x7F;

            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentException>(() => pivSession.Sign(0x81, dataToSign));
            }
        }

        [Fact]
        public void Sign_InvalidDataLength_Exception()
        {
            byte[] dataToSign = new byte[127];
            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            random.GetBytes(dataToSign);

            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentException>(() => pivSession.Sign(0x9a, dataToSign));
            }
        }

        [Fact]
        public void Decrypt_InvalidSlot_Exception()
        {
            byte[] dataToDecrypt = new byte[256];
            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            random.GetBytes(dataToDecrypt);
            dataToDecrypt[0] &= 0x7F;

            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentException>(() => pivSession.Decrypt(0xf9, dataToDecrypt));
            }
        }

        [Fact]
        public void Decrypt_InvalidDataLength_Exception()
        {
            byte[] dataToDecrypt = new byte[255];
            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            random.GetBytes(dataToDecrypt);

            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentException>(() => pivSession.Decrypt(0x9a, dataToDecrypt));
            }
        }
        [Fact]
        public void KeyAgree_NullPublicKey_Exception()
        {
            var yubiKey = new HollowYubiKeyDevice();
            using var pivSession = new PivSession(yubiKey);
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => pivSession.KeyAgree(0x9a, (PivPublicKey)null!));
            _ = Assert.Throws<ArgumentNullException>(() => pivSession.KeyAgree(0x9a, (IPublicKeyParameters)null!));
#pragma warning restore CS8625 // Testing null input.
        }

        [Fact]
        public void KeyAgree_EmptyPublicKey_Exception()
        {
            var yubiKey = new HollowYubiKeyDevice();
            var pivPublicKey = new PivPublicKey();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentException>(() => pivSession.KeyAgree(0x9a, pivPublicKey));
            }
        }

        [Fact]
        public void KeyAgree_InvalidPublicKey_Exception()
        {
            var yubiKey = new HollowYubiKeyDevice();

            _ = SampleKeyPairs.GetKeysAndCertPem(KeyType.RSA1024, false, out _, out var publicKeyPem, out _);
            var publicKey = new KeyConverter(publicKeyPem!.Replace("\n", "").ToCharArray());
            PivPublicKey pivPublicKey = publicKey.GetPivPublicKey();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentException>(() => pivSession.KeyAgree(0x9a, pivPublicKey));
            }
        }
    }
}
