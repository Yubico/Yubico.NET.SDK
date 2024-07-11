// Copyright 2022 Yubico AB
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
using System.Collections.ObjectModel;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.PinProtocols
{
    public class Protocol2Tests
    {
        [Fact]
        public void Encapsulate_Correct()
        {
            byte[] encodedKey = GetEncodedPublicKey();

            var pubKey = new CoseEcPublicKey(encodedKey);
            Assert.Equal(CoseEcCurve.P256, pubKey.Curve);

            var p2 = new PinUvAuthProtocolTwo();
            Assert.Equal(PinUvAuthProtocol.ProtocolTwo, p2.Protocol);

            p2.Encapsulate(pubKey);

            Assert.NotNull(p2.AuthenticatorPublicKey);
            Assert.NotNull(p2.PlatformPublicKey);
            _ = Assert.NotNull(p2.EncryptionKey);
            _ = Assert.NotNull(p2.AuthenticationKey);
        }

        [Fact]
        public void Encrypt_Correct()
        {
            byte[] dataToEncrypt = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] encodedKey = GetEncodedPublicKey();
            var pubKey = new CoseEcPublicKey(encodedKey);

            var p2 = new PinUvAuthProtocolTwo();
            p2.Encapsulate(pubKey);

            byte[] encryptedData = p2.Encrypt(dataToEncrypt, 0, dataToEncrypt.Length);
            byte[] decryptedData = p2.Decrypt(encryptedData, 0, encryptedData.Length);

            bool isValid = MemoryExtensions.SequenceEqual(dataToEncrypt.AsSpan<byte>(), decryptedData.AsSpan<byte>());

            Assert.True(isValid);
        }

        [Fact]
        public void Authenticate_Correct()
        {
            byte[] dataToAuthenticate = new byte[] {
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f,
                0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f
            };
            byte[] encodedKey = GetEncodedPublicKey();
            var pubKey = new CoseEcPublicKey(encodedKey);

            var p2 = new PinUvAuthProtocolTwo();
            p2.Encapsulate(pubKey);

            byte[] authValue = p2.Authenticate(dataToAuthenticate);

            Assert.Equal(32, authValue.Length);
        }

        private byte[] GetEncodedPublicKey()
        {
            byte[] encodedKey = new byte[] {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01, 0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };

            byte[] returnValue = new byte[encodedKey.Length];
            Array.Copy(encodedKey, 0, returnValue, 0, encodedKey.Length);

            return returnValue;
        }
    }
}
