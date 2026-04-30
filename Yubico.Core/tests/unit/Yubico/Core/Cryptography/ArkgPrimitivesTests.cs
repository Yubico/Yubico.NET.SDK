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

using System;
using System.Security;
using System.Security.Cryptography;
using Xunit;

namespace Yubico.Core.Cryptography
{
    public class ArkgPrimitivesTests
    {
        // P-256 generator G (SEC1 uncompressed: 0x04 || Gx || Gy). Authoritative
        // reference: SEC2 v2 §2.4.2.
        private static readonly byte[] P256Generator =
        {
            0x04,
            0x6B, 0x17, 0xD1, 0xF2, 0xE1, 0x2C, 0x42, 0x47,
            0xF8, 0xBC, 0xE6, 0xE5, 0x63, 0xA4, 0x40, 0xF2,
            0x77, 0x03, 0x7D, 0x81, 0x2D, 0xEB, 0x33, 0xA0,
            0xF4, 0xA1, 0x39, 0x45, 0xD8, 0x98, 0xC2, 0x96,
            0x4F, 0xE3, 0x42, 0xE2, 0xFE, 0x1A, 0x7F, 0x9B,
            0x8E, 0xE7, 0xEB, 0x4A, 0x7C, 0x0F, 0x9E, 0x16,
            0x2B, 0xCE, 0x33, 0x57, 0x6B, 0x31, 0x5E, 0xCE,
            0xCB, 0xB6, 0x40, 0x68, 0x37, 0xBF, 0x51, 0xF5,
        };

        [Fact]
        public void IsPointOnCurve_ValidP256Generator_ReturnsTrue()
        {
            IArkgPrimitives primitives = ArkgPrimitives.Create();

            Assert.True(primitives.IsPointOnCurve(P256Generator));
        }

        [Fact]
        public void IsPointOnCurve_OffCurvePoint_ReturnsFalse()
        {
            byte[] offCurve = (byte[])P256Generator.Clone();
            offCurve[64] ^= 0x01; // Flip the lowest bit of Y → no longer on curve.

            IArkgPrimitives primitives = ArkgPrimitives.Create();

            Assert.False(primitives.IsPointOnCurve(offCurve));
        }

        [Fact]
        public void IsPointOnCurve_MalformedLength_ReturnsFalse()
        {
            IArkgPrimitives primitives = ArkgPrimitives.Create();

            Assert.False(primitives.IsPointOnCurve(new byte[10]));
        }

        [Fact]
        public void IsPointOnCurve_WrongTagByte_ReturnsFalse()
        {
            byte[] compressedTag = (byte[])P256Generator.Clone();
            compressedTag[0] = 0x02;

            IArkgPrimitives primitives = ArkgPrimitives.Create();

            Assert.False(primitives.IsPointOnCurve(compressedTag));
        }

        [Fact]
        public void IsPointOnCurve_NullPoint_Throws()
        {
            IArkgPrimitives primitives = ArkgPrimitives.Create();

            _ = Assert.Throws<ArgumentNullException>(() => primitives.IsPointOnCurve(null!));
        }

        [Fact]
        public void ComputeEcdhSharedSecret_RoundTrip_MatchesPeer()
        {
            using var alice = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var bob = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            ECParameters aliceParams = alice.ExportParameters(includePrivateParameters: true);
            ECParameters bobParams = bob.ExportParameters(includePrivateParameters: true);

            byte[] alicePub = ToSec1(bobParams);
            byte[] bobPub = ToSec1(aliceParams);

            IArkgPrimitives primitives = ArkgPrimitives.Create();
            byte[] secretFromAlice = primitives.ComputeEcdhSharedSecret(aliceParams.D!, alicePub);
            byte[] secretFromBob = primitives.ComputeEcdhSharedSecret(bobParams.D!, bobPub);

            Assert.Equal(secretFromAlice, secretFromBob);
            Assert.Equal(32, secretFromAlice.Length);
        }

        [Fact]
        public void ComputeEcdhSharedSecret_OffCurvePublicPoint_Throws()
        {
            byte[] offCurve = (byte[])P256Generator.Clone();
            offCurve[64] ^= 0x01;

            IArkgPrimitives primitives = ArkgPrimitives.Create();

            _ = Assert.Throws<SecurityException>(
                () => primitives.ComputeEcdhSharedSecret(new byte[32] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, offCurve));
        }

        [Fact]
        public void ComputeEcdhSharedSecret_MalformedPublicPoint_Throws()
        {
            IArkgPrimitives primitives = ArkgPrimitives.Create();

            _ = Assert.Throws<ArgumentException>(
                () => primitives.ComputeEcdhSharedSecret(new byte[32], new byte[10]));
        }

        private static byte[] ToSec1(ECParameters p)
        {
            byte[] sec1 = new byte[65];
            sec1[0] = 0x04;
            Buffer.BlockCopy(p.Q.X!, 0, sec1, 1, 32);
            Buffer.BlockCopy(p.Q.Y!, 0, sec1, 33, 32);
            return sec1;
        }
    }
}
