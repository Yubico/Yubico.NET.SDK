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

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv
{
    public class KeyTests
    {
        [Fact]
        public void TDesWeakKey()
        {
            byte[] keyData = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            using TripleDES tDesObject = CryptographyProviders.TripleDesCreator();

            bool isWeak = TripleDES.IsWeakKey(keyData);
            Assert.True(isWeak);

            _ = Assert.Throws<CryptographicException>(() => tDesObject.Key = keyData);
        }

        [Fact]
        public void TDesKey()
        {
            byte[] keyData = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            using TripleDES tDesObject = CryptographyProviders.TripleDesCreator();
            tDesObject.GenerateKey();

            bool isWeak = TripleDES.IsWeakKey(keyData);
            Assert.True(isWeak);

            byte[] oldKeyData = tDesObject.Key;

            oldKeyData[0] = 0x01;
            oldKeyData[1] = 0x02;

            byte[] keyDataAgain = tDesObject.Key;

            Assert.Equal(192, tDesObject.KeySize);
        }

        [Fact]
        public void DesWeak()
        {
            byte[] keyData = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] dataToEncrypt = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] encryptedData = new byte[8];
            byte[] encryptCipher = new byte[8];

            var desObject = TripleDES.Create();
            desObject.Mode = CipherMode.ECB;
            desObject.Padding = PaddingMode.None;
            ICryptoTransform encryptor = desObject.CreateEncryptor(keyData, null);
            int eLen = encryptor.TransformBlock(dataToEncrypt, 0, 8, encryptedData, 0);
            Assert.Equal(8, eLen);

            eLen = encryptor.TransformBlock(encryptedData, 0, 8, encryptCipher, 0);
            Assert.Equal(8, eLen);

            ICryptoTransform decryptor = desObject.CreateDecryptor(keyData, null);
            byte[] newBuf = decryptor.TransformFinalBlock(dataToEncrypt, 0, 8);
            Assert.Equal(encryptedData[0], newBuf[0]);
        }

        [Fact]
        public void DesWeak_Matching()
        {
            byte[] keyData1 = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
            };
            byte[] keyData2 = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE
            };
            byte[] keyData3 = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xE0, 0xE0, 0xE0, 0xE0, 0xF1, 0xF1, 0xF1, 0xF1
            };
            byte[] keyData4 = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x1F, 0x1F, 0x1F, 0x1F, 0x0E, 0x0E, 0x0E, 0x0E
            };
            byte[] dataToEncrypt = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] result1 = new byte[8];
            byte[] result2 = new byte[8];
            byte[] result3 = new byte[8];
            byte[] result4 = new byte[8];

            var tDesObject = TripleDES.Create();
            tDesObject.Mode = CipherMode.ECB;
            tDesObject.Padding = PaddingMode.None;

            ICryptoTransform encryptor1 = tDesObject.CreateEncryptor(keyData1, null);
            int eLen = encryptor1.TransformBlock(dataToEncrypt, 0, 8, result1, 0);
            Assert.Equal(8, eLen);

            ICryptoTransform encryptor2 = tDesObject.CreateEncryptor(keyData2, null);
            eLen = encryptor2.TransformBlock(dataToEncrypt, 0, 8, result2, 0);
            Assert.Equal(8, eLen);

            ICryptoTransform encryptor3 = tDesObject.CreateEncryptor(keyData3, null);
            eLen = encryptor3.TransformBlock(dataToEncrypt, 0, 8, result3, 0);
            Assert.Equal(8, eLen);

            ICryptoTransform encryptor4 = tDesObject.CreateEncryptor(keyData4, null);
            eLen = encryptor4.TransformBlock(dataToEncrypt, 0, 8, result4, 0);
            Assert.Equal(8, eLen);
        }

        [Fact]
        public void TDes_Double()
        {
            byte[] keyData1 = new byte[] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
            };
            byte[] keyData2 = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
            };
            byte[] keyData3 = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };
            byte[] dataToEncrypt = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] result1 = new byte[8];
            byte[] result2 = new byte[8];
            byte[] result3 = new byte[8];

            var tDesObject = TripleDES.Create();
            tDesObject.Mode = CipherMode.ECB;
            tDesObject.Padding = PaddingMode.None;
            tDesObject.KeySize = 128;

            ICryptoTransform encryptor1 = tDesObject.CreateEncryptor(keyData1, null);
            int eLen = encryptor1.TransformBlock(dataToEncrypt, 0, 8, result1, 0);
            Assert.Equal(8, eLen);

            ICryptoTransform encryptor2 = tDesObject.CreateEncryptor(keyData2, null);
            eLen = encryptor2.TransformBlock(dataToEncrypt, 0, 8, result2, 0);
            Assert.Equal(8, eLen);

            tDesObject.KeySize = 192;
            ICryptoTransform encryptor3 = tDesObject.CreateEncryptor(keyData3, null);
            eLen = encryptor3.TransformBlock(dataToEncrypt, 0, 8, result3, 0);
            Assert.Equal(8, eLen);
        }

        [Fact]
        public void DesReplace()
        {
            byte[] keyDataT = new byte[] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
            };
            byte[] keyData1 = new byte[] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0xd3, 0x90, 0xbf, 0x01, 0x9c, 0x39, 0x53, 0x70,
                0xc9, 0x7a, 0xe1, 0x8c, 0x61, 0xe3, 0x48, 0x47
            };
            byte[] keyData2 = new byte[] {
                0xd3, 0x90, 0xbf, 0x01, 0x9c, 0x39, 0x53, 0x70
            };
            byte[] keyData3 = new byte[] {
                0xc9, 0x7a, 0xe1, 0x8c, 0x61, 0xe3, 0x48, 0x47
            };
            byte[] dataToEncrypt = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] result1 = new byte[8];
            byte[] result2 = new byte[8];
            byte[] part1 = new byte[8];
            byte[] part2 = new byte[8];

            var tDesObject = TripleDES.Create();
            tDesObject.Mode = CipherMode.ECB;
            tDesObject.Padding = PaddingMode.None;

            ICryptoTransform encryptor0 = tDesObject.CreateEncryptor(keyDataT, null);
            int eLen = encryptor0.TransformBlock(dataToEncrypt, 0, 8, result1, 0);
            Assert.Equal(8, eLen);

            ICryptoTransform encryptor1 = tDesObject.CreateEncryptor(keyData1, null);
            eLen = encryptor1.TransformBlock(dataToEncrypt, 0, 8, part1, 0);
            Assert.Equal(8, eLen);

            var desObject = DES.Create();
            desObject.Mode = CipherMode.ECB;
            desObject.Padding = PaddingMode.None;

            ICryptoTransform decryptor2 = desObject.CreateDecryptor(keyData3, null);
            eLen = decryptor2.TransformBlock(part1, 0, 8, part2, 0);
            Assert.Equal(8, eLen);

            ICryptoTransform encryptor2 = desObject.CreateEncryptor(keyData2, null);
            eLen = encryptor2.TransformBlock(part2, 0, 8, result2, 0);
            Assert.Equal(8, eLen);
        }
    }
}
