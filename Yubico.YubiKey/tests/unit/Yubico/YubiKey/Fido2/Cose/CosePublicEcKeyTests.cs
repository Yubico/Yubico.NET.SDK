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

namespace Yubico.YubiKey.Fido2.Cose
{
    public class CosePublicEcKeyTests
    {
        [Fact]
        public void Encode_ReturnsCorrect()
        {
            byte[] correctValue =
            {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01,
                0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };

            var xCoord = new byte[32];
            var yCoord = new byte[32];

            Array.Copy(correctValue, sourceIndex: 11, xCoord, destinationIndex: 0, length: 32);
            Array.Copy(correctValue, sourceIndex: 46, yCoord, destinationIndex: 0, length: 32);

            var eccKey = new CoseEcPublicKey(CoseEcCurve.P256, xCoord, yCoord);

            ReadOnlyMemory<byte> encodedKey = eccKey.Encode();

            var isValid = MemoryExtensions.SequenceEqual(correctValue, encodedKey.Span);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_CorrectX()
        {
            byte[] encodedKey =
            {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01,
                0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };

            var eccKey = new CoseEcPublicKey(encodedKey);

            var correctX = new Span<byte>(encodedKey, start: 11, length: 32);

            var isValid = correctX.SequenceEqual(eccKey.XCoordinate.Span);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_CorrectY()
        {
            byte[] encodedKey =
            {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01,
                0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };

            var eccKey = new CoseEcPublicKey(encodedKey);

            var correctY = new Span<byte>(encodedKey, start: 46, length: 32);

            var isValid = correctY.SequenceEqual(eccKey.YCoordinate.Span);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_ECParameters_CorrectX()
        {
            byte[] encodedKey =
            {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01,
                0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };

            var eccKey = new CoseEcPublicKey(encodedKey);
            var ecParams = eccKey.ToEcParameters();
            Assert.NotNull(ecParams.Q.X);
            if (ecParams.Q.X is null)
            {
                return;
            }

            var xCoord = new Span<byte>(ecParams.Q.X);

            var correctX = new Span<byte>(encodedKey, start: 11, length: 32);

            var isValid = correctX.SequenceEqual(xCoord);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_ECParameters_CorrectY()
        {
            byte[] encodedKey =
            {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01,
                0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };

            var eccKey = new CoseEcPublicKey(encodedKey);
            var ecParams = eccKey.ToEcParameters();
            Assert.NotNull(ecParams.Q.Y);
            if (ecParams.Q.Y is null)
            {
                return;
            }

            var yCoord = new Span<byte>(ecParams.Q.Y);

            var correctY = new Span<byte>(encodedKey, start: 46, length: 32);

            var isValid = correctY.SequenceEqual(yCoord);

            Assert.True(isValid);
        }

        [Fact]
        public void FromECParameters_CorrectEncoding()
        {
            byte[] correctEncoding =
            {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01,
                0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };
            var encodingSpan = new Span<byte>(correctEncoding);

            var ecParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = encodingSpan.Slice(start: 11, length: 32).ToArray(),
                    Y = encodingSpan.Slice(start: 46, length: 32).ToArray()
                }
            };

            var eccKey = new CoseEcPublicKey(ecParams);
            ReadOnlyMemory<byte> encodedKey = eccKey.Encode();

            var isValid = encodingSpan.SequenceEqual(encodedKey.Span);

            Assert.True(isValid);
        }
    }
}
