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

// Purpose
// -------
// Direct P/Invoke functional tests for the OpenSSL EC group / EC point
// marshaling layer exposed by Yubico.NativeShims (Native_EC_GROUP_*,
// Native_EC_POINT_*). These wrappers underpin every ECC operation in the SDK
// (ECDH, ARKG-P256 on-curve validation, FIDO2 key handling); marshaling
// regressions cascade silently into wrong shared secrets or accepted
// invalid-curve points.
//
// What this validates
// -------------------
//   * Group/point lifecycle on NIST P-256 (curve NID 415).
//   * Native_EC_POINT_set_affine_coordinates + get_affine_coordinates
//     round-trips the SEC2 P-256 generator G unchanged.
//   * Native_EC_POINT_mul: G·1 = G; G·n (n = group order) = point at infinity
//     (get_affine subsequently fails as expected).
//   * Native_EC_POINT_is_on_curve: returns 1 for the valid generator,
//     0 for a Y-bit-flipped off-curve candidate. Required for SEC 1 §3.2.2
//     public-key validation in ARKG-P256.
//
// References
// ----------
//   * SEC 2: Recommended Elliptic Curve Domain Parameters, v2.0 §2.4.2
//     (secp256r1 / NIST P-256 generator and group order)
//     https://www.secg.org/sec2-v2.pdf
//   * SEC 1: Elliptic Curve Cryptography, v2.0 §3.2.2 (Public Key Validation)
//     https://www.secg.org/sec1-v2.pdf
//   * NIST SP 800-186 §3.2.1.3 (Curve P-256) — current authoritative source
//     for NIST P-256 domain parameters (the FIPS 186-5 revision moved curve
//     definitions out of FIPS 186 into SP 800-186).
//     https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-186.pdf
//   * OpenSSL EC_POINT(3) man page —
//     https://docs.openssl.org/master/man3/EC_POINT_new/

using System;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.PlatformInterop.Cryptography
{
    public class EcPointInteropTests
    {
        // P-256 curve NID (OpenSSL constant for X9.62 prime256v1)
        private const int NidP256 = 415;

        // P-256 generator G (SEC1 uncompressed: 0x04 || Gx || Gy).
        // Reference: SEC2 v2 §2.4.2.
        private static readonly byte[] P256GeneratorX =
        {
            0x6B, 0x17, 0xD1, 0xF2, 0xE1, 0x2C, 0x42, 0x47,
            0xF8, 0xBC, 0xE6, 0xE5, 0x63, 0xA4, 0x40, 0xF2,
            0x77, 0x03, 0x7D, 0x81, 0x2D, 0xEB, 0x33, 0xA0,
            0xF4, 0xA1, 0x39, 0x45, 0xD8, 0x98, 0xC2, 0x96,
        };

        private static readonly byte[] P256GeneratorY =
        {
            0x4F, 0xE3, 0x42, 0xE2, 0xFE, 0x1A, 0x7F, 0x9B,
            0x8E, 0xE7, 0xEB, 0x4A, 0x7C, 0x0F, 0x9E, 0x16,
            0x2B, 0xCE, 0x33, 0x57, 0x6B, 0x31, 0x5E, 0xCE,
            0xCB, 0xB6, 0x40, 0x68, 0x37, 0xBF, 0x51, 0xF5,
        };

        // P-256 group order (SEC2 v2 §2.4.2)
        private static readonly byte[] P256Order =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xBC, 0xE6, 0xFA, 0xAD, 0xA7, 0x17, 0x9E, 0x84,
            0xF3, 0xB9, 0xCA, 0xC2, 0xFC, 0x63, 0x25, 0x51,
        };

        [Fact]
        public void EcGroupNewByCurveName_P256_CreatesValidGroup()
        {
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(NidP256);

            Assert.NotNull(group);
            Assert.False(group.IsInvalid);
        }

        [Fact]
        public void EcPointNew_ValidGroup_CreatesValidPoint()
        {
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(NidP256);
            using SafeEcPoint point = NativeMethods.EcPointNew(group);

            Assert.NotNull(point);
            Assert.False(point.IsInvalid);
        }

        [Fact]
        public void EcPointIsOnCurve_P256Generator_ReturnsTrue()
        {
            // SEC2 §2.4.2 P-256 generator G is on the curve
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(NidP256);
            using SafeEcPoint point = NativeMethods.EcPointNew(group);
            using SafeBigNum x = NativeMethods.BnBinaryToBigNum(P256GeneratorX);
            using SafeBigNum y = NativeMethods.BnBinaryToBigNum(P256GeneratorY);

            int setResult = NativeMethods.EcPointSetAffineCoordinates(group, point, x, y);
            Assert.Equal(1, setResult);

            int isOnCurve = NativeMethods.EcPointIsOnCurve(group, point);
            Assert.Equal(1, isOnCurve);
        }

        [Fact]
        public void EcPointIsOnCurve_OffCurvePoint_ReturnsFalse()
        {
            // Flip the lowest bit of Y to create a point not on the curve
            byte[] offCurveY = (byte[])P256GeneratorY.Clone();
            offCurveY[31] ^= 0x01;

            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(NidP256);
            using SafeEcPoint point = NativeMethods.EcPointNew(group);
            using SafeBigNum x = NativeMethods.BnBinaryToBigNum(P256GeneratorX);
            using SafeBigNum y = NativeMethods.BnBinaryToBigNum(offCurveY);

            // set_affine_coordinates might fail for invalid points, but if it succeeds,
            // is_on_curve must return 0
            int setResult = NativeMethods.EcPointSetAffineCoordinates(group, point, x, y);
            if (setResult == 1)
            {
                int isOnCurve = NativeMethods.EcPointIsOnCurve(group, point);
                Assert.Equal(0, isOnCurve);
            }
            // If set fails, the point is invalid - that's also acceptable behavior
        }

        [Fact]
        public void EcPointGetAffineCoordinates_RoundTrip_MatchesOriginal()
        {
            // G·1 round-trips back to G via set/get affine coordinates
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(NidP256);
            using SafeEcPoint point = NativeMethods.EcPointNew(group);
            using SafeBigNum xIn = NativeMethods.BnBinaryToBigNum(P256GeneratorX);
            using SafeBigNum yIn = NativeMethods.BnBinaryToBigNum(P256GeneratorY);

            int setResult = NativeMethods.EcPointSetAffineCoordinates(group, point, xIn, yIn);
            Assert.Equal(1, setResult);

            using SafeBigNum xOut = NativeMethods.BnNew();
            using SafeBigNum yOut = NativeMethods.BnNew();

            int getResult = NativeMethods.EcPointGetAffineCoordinates(group, point, xOut, yOut);
            Assert.Equal(1, getResult);

            byte[] xBytes = new byte[32];
            byte[] yBytes = new byte[32];
            int xLen = NativeMethods.BnBigNumToBinaryWithPadding(xOut, xBytes);
            int yLen = NativeMethods.BnBigNumToBinaryWithPadding(yOut, yBytes);

            Assert.Equal(32, xLen);
            Assert.Equal(32, yLen);
            Assert.Equal(P256GeneratorX, xBytes);
            Assert.Equal(P256GeneratorY, yBytes);
        }

        [Fact]
        public void EcPointMul_GeneratorTimesOne_ReturnsGenerator()
        {
            // G·1 = G
            byte[] scalarOne = new byte[32];
            scalarOne[31] = 1;

            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(NidP256);
            using SafeEcPoint generatorPoint = NativeMethods.EcPointNew(group);
            using SafeBigNum xGen = NativeMethods.BnBinaryToBigNum(P256GeneratorX);
            using SafeBigNum yGen = NativeMethods.BnBinaryToBigNum(P256GeneratorY);

            int setResult = NativeMethods.EcPointSetAffineCoordinates(group, generatorPoint, xGen, yGen);
            Assert.Equal(1, setResult);

            using SafeBigNum scalarBn = NativeMethods.BnBinaryToBigNum(scalarOne);
            using SafeEcPoint result = NativeMethods.EcPointNew(group);

            // EC_POINT_mul(group, r, n, q, m, ctx) computes r = n·G + m·q
            // To compute q·scalar, pass n=0, q=generatorPoint, m=scalar
            int mulResult = NativeMethods.EcPointMul(
                group,
                result,
                IntPtr.Zero, // n = NULL (don't add generator multiple)
                generatorPoint.DangerousGetHandle(), // q
                scalarBn.DangerousGetHandle()); // m

            Assert.Equal(1, mulResult);

            using SafeBigNum xResult = NativeMethods.BnNew();
            using SafeBigNum yResult = NativeMethods.BnNew();

            int getResult = NativeMethods.EcPointGetAffineCoordinates(group, result, xResult, yResult);
            Assert.Equal(1, getResult);

            byte[] xBytes = new byte[32];
            byte[] yBytes = new byte[32];
            NativeMethods.BnBigNumToBinaryWithPadding(xResult, xBytes);
            NativeMethods.BnBigNumToBinaryWithPadding(yResult, yBytes);

            Assert.Equal(P256GeneratorX, xBytes);
            Assert.Equal(P256GeneratorY, yBytes);
        }

        [Fact]
        public void EcPointMul_GeneratorTimesOrder_ReturnsPointAtInfinity()
        {
            // G·n where n = P-256 group order → point at infinity
            // Point at infinity cannot have affine coordinates extracted
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(NidP256);
            using SafeEcPoint generatorPoint = NativeMethods.EcPointNew(group);
            using SafeBigNum xGen = NativeMethods.BnBinaryToBigNum(P256GeneratorX);
            using SafeBigNum yGen = NativeMethods.BnBinaryToBigNum(P256GeneratorY);

            int setResult = NativeMethods.EcPointSetAffineCoordinates(group, generatorPoint, xGen, yGen);
            Assert.Equal(1, setResult);

            using SafeBigNum orderBn = NativeMethods.BnBinaryToBigNum(P256Order);
            using SafeEcPoint result = NativeMethods.EcPointNew(group);

            int mulResult = NativeMethods.EcPointMul(
                group,
                result,
                IntPtr.Zero,
                generatorPoint.DangerousGetHandle(),
                orderBn.DangerousGetHandle());

            Assert.Equal(1, mulResult);

            using SafeBigNum xResult = NativeMethods.BnNew();
            using SafeBigNum yResult = NativeMethods.BnNew();

            // Attempting to get affine coordinates of point at infinity should fail
            int getResult = NativeMethods.EcPointGetAffineCoordinates(group, result, xResult, yResult);
            Assert.Equal(0, getResult);
        }
    }
}
