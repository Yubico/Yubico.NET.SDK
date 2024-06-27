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
using System.Security.Cryptography;
using Xunit;

namespace Yubico.Core.Cryptography
{
    public class AesGcmPrimitivesOpenSslTests
    {
        [Fact]
        public void Instantiate_Succeeds()
        {
            IAesGcmPrimitives aesObj = AesGcmPrimitives.Create();
            Assert.NotNull(aesObj);
        }

        [Fact]
        public void Encrypt_Decrypt_Succeeds()
        {
            byte[] keyData = GetKeyData(null);
            byte[] nonce = GetNonce(null);
            byte[] plaintext = GetPlaintext(null);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];
            byte[] associatedData = GetAssociatedData(plaintext.Length);
            byte[] encryptedData = GetEncryptedData();
            byte[] authTag = GetAuthTag();

            IAesGcmPrimitives aesObj = AesGcmPrimitives.Create();
            aesObj.EncryptAndAuthenticate(keyData, nonce, plaintext, ciphertext, tag, associatedData);

            bool isValid = encryptedData.AsSpan().SequenceEqual(ciphertext.AsSpan());
            Assert.True(isValid);
            isValid = authTag.AsSpan().SequenceEqual(tag.AsSpan());
            Assert.True(isValid);

            byte[] decryptedData = new byte[ciphertext.Length];
            bool isVerified = aesObj.DecryptAndVerify(keyData, nonce, ciphertext, tag, decryptedData, associatedData);
            Assert.True(isVerified);

            isValid = plaintext.AsSpan().SequenceEqual(decryptedData.AsSpan());
            Assert.True(isValid);
        }

        [SkippableFact(typeof(System.PlatformNotSupportedException))]
        public void Encrypt_Decrypt_Succeeds_RandomValues_Succeed()
        {
            var random = RandomNumberGenerator.Create();
            byte[] keyData = GetKeyData(random);
            byte[] nonce = GetNonce(random);
            byte[] plaintext = GetPlaintext(random, 50);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] ciphertextS = new byte[plaintext.Length];
            byte[] tag = new byte[16];
            byte[] tagS = new byte[16];
            byte[] associatedData = GetAssociatedData(plaintext.Length);

            IAesGcmPrimitives aesObj = AesGcmPrimitives.Create();
            aesObj.EncryptAndAuthenticate(keyData, nonce, plaintext, ciphertext, tag, associatedData);

            var aesGcm = new AesGcm(keyData);
            aesGcm.Encrypt(nonce, plaintext, ciphertextS, tagS, associatedData);

            bool isValid = ciphertextS.AsSpan().SequenceEqual(ciphertext.AsSpan());
            Assert.True(isValid);
            isValid = tagS.AsSpan().SequenceEqual(tag.AsSpan());
            Assert.True(isValid);

            byte[] decryptedData = new byte[ciphertext.Length];
            bool isVerified = aesObj.DecryptAndVerify(keyData, nonce, ciphertext, tag, decryptedData, associatedData);
            Assert.True(isVerified);

            byte[] decryptedDataS = new byte[ciphertextS.Length];
            aesGcm.Decrypt(nonce, ciphertextS, tag, decryptedDataS, associatedData);

            isValid = decryptedDataS.AsSpan().SequenceEqual(decryptedData.AsSpan());
            Assert.True(isValid);
            isValid = plaintext.AsSpan().SequenceEqual(decryptedData.AsSpan());
            Assert.True(isValid);
        }

        private static byte[] GetKeyData(RandomNumberGenerator? random)
        {
            byte[] keyBytes = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            random?.GetBytes(keyBytes);

            return keyBytes;
        }

        private static byte[] GetNonce(RandomNumberGenerator? random)
        {
            byte[] nonceBytes = new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x41, 0x42, 0x43, 0x44
            };

            random?.GetBytes(nonceBytes);

            return nonceBytes;
        }

        private static byte[] GetPlaintext(RandomNumberGenerator? random, int dataLength = 18)
        {
            byte[] dataToEncrypt;
            if (dataLength == 18)
            {
                dataToEncrypt = new byte[] {
                    0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                    0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                    0x51, 0x52
                };
            }
            else
            {
                dataToEncrypt = new byte[dataLength];
            }

            random?.GetBytes(dataToEncrypt);

            return dataToEncrypt;
        }

        private static byte[] GetAssociatedData(int originalSize)
        {
            byte[] associatedData = new byte[] {
                0x62, 0x6c, 0x6f, 0x62,
                (byte)originalSize,
                (byte)(originalSize >>  8),
                (byte)(originalSize >> 16),
                (byte)(originalSize >> 24), 0, 0, 0, 0
            };

            return associatedData;
        }

        private static byte[] GetEncryptedData()
        {
            byte[] encryptedData = new byte[] {
                0xea, 0x6a, 0x01, 0x13, 0x8d, 0x78, 0xa6, 0xa7,
                0xec, 0x57, 0x91, 0x13, 0xbe, 0xe1, 0xcd, 0x75,
                0xba, 0x87
            };

            return encryptedData;
        }

        private static byte[] GetAuthTag()
        {
            byte[] authTag = new byte[] {
                0xba, 0x13, 0x8f, 0x68, 0xaf, 0xc7, 0xff, 0x26,
                0x5f, 0x75, 0x25, 0xb2, 0xcc, 0xe9, 0x6b, 0xae
            };

            return authTag;
        }
    }
}
