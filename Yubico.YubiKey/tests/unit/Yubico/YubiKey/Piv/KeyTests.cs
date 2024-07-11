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
            var keyData = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            using var tDesObject = CryptographyProviders.TripleDesCreator();

            var isWeak = TripleDES.IsWeakKey(keyData);
            Assert.True(isWeak);

            _ = Assert.Throws<CryptographicException>(() => tDesObject.Key = keyData);
        }

        [Fact]
        public void TDesKey()
        {
            var keyData = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            using var tDesObject = CryptographyProviders.TripleDesCreator();
            tDesObject.GenerateKey();

            var isWeak = TripleDES.IsWeakKey(keyData);
            Assert.True(isWeak);

            var oldKeyData = tDesObject.Key;

            oldKeyData[0] = 0x01;
            oldKeyData[1] = 0x02;

            var keyDataAgain = tDesObject.Key;

            Assert.Equal(expected: 192, tDesObject.KeySize);
        }

        [Fact]
        public void DesWeak()
        {
            var keyData = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var dataToEncrypt = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var encryptedData = new byte[8];
            var encryptCipher = new byte[8];

            var desObject = TripleDES.Create();
            desObject.Mode = CipherMode.ECB;
            desObject.Padding = PaddingMode.None;
            var encryptor = desObject.CreateEncryptor(keyData, rgbIV: null);
            var eLen = encryptor.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, encryptedData,
                outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            eLen = encryptor.TransformBlock(encryptedData, inputOffset: 0, inputCount: 8, encryptCipher,
                outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            var decryptor = desObject.CreateDecryptor(keyData, rgbIV: null);
            var newBuf = decryptor.TransformFinalBlock(dataToEncrypt, inputOffset: 0, inputCount: 8);
            Assert.Equal(encryptedData[0], newBuf[0]);
        }

        [Fact]
        public void DesWeak_Matching()
        {
            var keyData1 = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
            };
            var keyData2 = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE
            };
            var keyData3 = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xE0, 0xE0, 0xE0, 0xE0, 0xF1, 0xF1, 0xF1, 0xF1
            };
            var keyData4 = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x1F, 0x1F, 0x1F, 0x1F, 0x0E, 0x0E, 0x0E, 0x0E
            };
            var dataToEncrypt = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var result1 = new byte[8];
            var result2 = new byte[8];
            var result3 = new byte[8];
            var result4 = new byte[8];

            var tDesObject = TripleDES.Create();
            tDesObject.Mode = CipherMode.ECB;
            tDesObject.Padding = PaddingMode.None;

            var encryptor1 = tDesObject.CreateEncryptor(keyData1, rgbIV: null);
            var eLen = encryptor1.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, result1,
                outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            var encryptor2 = tDesObject.CreateEncryptor(keyData2, rgbIV: null);
            eLen = encryptor2.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, result2, outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            var encryptor3 = tDesObject.CreateEncryptor(keyData3, rgbIV: null);
            eLen = encryptor3.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, result3, outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            var encryptor4 = tDesObject.CreateEncryptor(keyData4, rgbIV: null);
            eLen = encryptor4.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, result4, outputOffset: 0);
            Assert.Equal(expected: 8, eLen);
        }

        [Fact]
        public void TDes_Double()
        {
            var keyData1 = new byte[]
            {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
            };
            var keyData2 = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
            };
            var keyData3 = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };
            var dataToEncrypt = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var result1 = new byte[8];
            var result2 = new byte[8];
            var result3 = new byte[8];

            var tDesObject = TripleDES.Create();
            tDesObject.Mode = CipherMode.ECB;
            tDesObject.Padding = PaddingMode.None;
            tDesObject.KeySize = 128;

            var encryptor1 = tDesObject.CreateEncryptor(keyData1, rgbIV: null);
            var eLen = encryptor1.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, result1,
                outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            var encryptor2 = tDesObject.CreateEncryptor(keyData2, rgbIV: null);
            eLen = encryptor2.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, result2, outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            tDesObject.KeySize = 192;
            var encryptor3 = tDesObject.CreateEncryptor(keyData3, rgbIV: null);
            eLen = encryptor3.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, result3, outputOffset: 0);
            Assert.Equal(expected: 8, eLen);
        }

        [Fact]
        public void DesReplace()
        {
            var keyDataT = new byte[]
            {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
            };
            var keyData1 = new byte[]
            {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0xd3, 0x90, 0xbf, 0x01, 0x9c, 0x39, 0x53, 0x70,
                0xc9, 0x7a, 0xe1, 0x8c, 0x61, 0xe3, 0x48, 0x47
            };
            var keyData2 = new byte[]
            {
                0xd3, 0x90, 0xbf, 0x01, 0x9c, 0x39, 0x53, 0x70
            };
            var keyData3 = new byte[]
            {
                0xc9, 0x7a, 0xe1, 0x8c, 0x61, 0xe3, 0x48, 0x47
            };
            var dataToEncrypt = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var result1 = new byte[8];
            var result2 = new byte[8];
            var part1 = new byte[8];
            var part2 = new byte[8];

            var tDesObject = TripleDES.Create();
            tDesObject.Mode = CipherMode.ECB;
            tDesObject.Padding = PaddingMode.None;

            var encryptor0 = tDesObject.CreateEncryptor(keyDataT, rgbIV: null);
            var eLen = encryptor0.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, result1,
                outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            var encryptor1 = tDesObject.CreateEncryptor(keyData1, rgbIV: null);
            eLen = encryptor1.TransformBlock(dataToEncrypt, inputOffset: 0, inputCount: 8, part1, outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            var desObject = DES.Create();
            desObject.Mode = CipherMode.ECB;
            desObject.Padding = PaddingMode.None;

            var decryptor2 = desObject.CreateDecryptor(keyData3, rgbIV: null);
            eLen = decryptor2.TransformBlock(part1, inputOffset: 0, inputCount: 8, part2, outputOffset: 0);
            Assert.Equal(expected: 8, eLen);

            var encryptor2 = desObject.CreateEncryptor(keyData2, rgbIV: null);
            eLen = encryptor2.TransformBlock(part2, inputOffset: 0, inputCount: 8, result2, outputOffset: 0);
            Assert.Equal(expected: 8, eLen);
        }
    }
}
