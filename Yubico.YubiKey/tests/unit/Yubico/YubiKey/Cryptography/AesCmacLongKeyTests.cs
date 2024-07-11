// Copyright 2023 Yubico AB
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
using Xunit;
using Yubico.Core.Cryptography;
using Yubico.PlatformInterop;

namespace Yubico.YubiKey.Cryptography
{
    public class AesCmacLongKeyTests
    {
        [Fact]
        public void Aes192Cmac_NistVector_CalculatesCorrectly()
        {
            byte[] keyData = new byte[]
            {
                0x8e, 0x73, 0xb0, 0xf7, 0xda, 0x0e, 0x64, 0x52, 0xc8, 0x10, 0xf3, 0x2b, 0x80, 0x90, 0x79, 0xe5,
                0x62, 0xf8, 0xea, 0xd2, 0x52, 0x2c, 0x6b, 0x7b
            };
            byte[] message = new byte[]
            {
                0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96, 0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a,
                0xae, 0x2d, 0x8a, 0x57, 0x1e, 0x03, 0xac, 0x9c, 0x9e, 0xb7, 0x6f, 0xac, 0x45, 0xaf, 0x8e, 0x51,
                0x30, 0xc8, 0x1c, 0x46, 0xa3, 0x5c, 0xe4, 0x11
            };
            byte[] expectedResult = new byte[]
            {
                0x8a, 0x1d, 0xe5, 0xbe, 0x2e, 0xb3, 0x1a, 0xad, 0x08, 0x9a, 0x82, 0xe6, 0xee, 0x90, 0x8b, 0x0e
            };
            byte[] cmac = new byte[16];

            using ICmacPrimitives cmacObj =
                CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes192);
            cmacObj.CmacInit(keyData);
            cmacObj.CmacUpdate(message);
            cmacObj.CmacFinal(cmac);

            bool isValid = MemoryExtensions.SequenceEqual(cmac.AsSpan(), expectedResult.AsSpan());
            Assert.True(isValid);
        }

        [Fact]
        public void Aes256Cmac_NistVector_CalculatesCorrectly()
        {
            byte[] keyData = new byte[]
            {
                0x60, 0x3d, 0xeb, 0x10, 0x15, 0xca, 0x71, 0xbe, 0x2b, 0x73, 0xae, 0xf0, 0x85, 0x7d, 0x77, 0x81,
                0x1f, 0x35, 0x2c, 0x07, 0x3b, 0x61, 0x08, 0xd7, 0x2d, 0x98, 0x10, 0xa3, 0x09, 0x14, 0xdf, 0xf4
            };
            byte[] message = new byte[]
            {
                0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96, 0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a,
                0xae, 0x2d, 0x8a, 0x57, 0x1e, 0x03, 0xac, 0x9c, 0x9e, 0xb7, 0x6f, 0xac, 0x45, 0xaf, 0x8e, 0x51,
                0x30, 0xc8, 0x1c, 0x46, 0xa3, 0x5c, 0xe4, 0x11, 0xe5, 0xfb, 0xc1, 0x19, 0x1a, 0x0a, 0x52, 0xef,
                0xf6, 0x9f, 0x24, 0x45, 0xdf, 0x4f, 0x9b, 0x17, 0xad, 0x2b, 0x41, 0x7b, 0xe6, 0x6c, 0x37, 0x10
            };
            byte[] expectedResult = new byte[]
            {
                0xe1, 0x99, 0x21, 0x90, 0x54, 0x9f, 0x6e, 0xd5, 0x69, 0x6a, 0x2c, 0x05, 0x6c, 0x31, 0x54, 0x10
            };
            byte[] cmac = new byte[16];

            using ICmacPrimitives cmacObj =
                CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes256);
            cmacObj.CmacInit(keyData);
            cmacObj.CmacUpdate(message);
            cmacObj.CmacFinal(cmac);

            bool isValid = MemoryExtensions.SequenceEqual(cmac.AsSpan(), expectedResult.AsSpan());
            Assert.True(isValid);
        }
    }
}
