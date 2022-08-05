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

namespace Yubico.YubiKey.Fido2
{
    public class Fido2EccKeyTests
    {
        [Fact]
        public void Encode_ReturnsCorrect()
        {
            byte[] correctValue = new byte[] {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01, 0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };

            byte[] xCoord = new byte[32];
            byte[] yCoord = new byte[32];

            Array.Copy(correctValue, 11, xCoord, 0, 32);
            Array.Copy(correctValue, 46, yCoord, 0, 32);

            var eccKey = new Fido2EccPublicKey(CoseAlgorithmIdentifier.ES256, xCoord, yCoord);

            ReadOnlyMemory<byte> encodedKey = eccKey.GetEncodedKey();

            bool isValid = MemoryExtensions.SequenceEqual(correctValue, encodedKey.Span);

            Assert.True(isValid);
        }
    }
}
