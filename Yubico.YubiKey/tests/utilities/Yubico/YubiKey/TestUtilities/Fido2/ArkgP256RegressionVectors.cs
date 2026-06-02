// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
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
using System.Text;

namespace Yubico.YubiKey.TestUtilities.Fido2
{
    /// <summary>
    /// ARKG-P256 expected-output vectors used by the SDK's derivation regression tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are internal SDK regression fixtures, not published ARKG draft
    /// vectors. They detect unintended regressions in ARKG-P256 key derivation
    /// across refactors and cover three scenarios:
    /// <list type="bullet">
    ///   <item><description>A: baseline derivation with canonical IKM and context.</description></item>
    ///   <item><description>B: distinct IKM (same context) to verify input isolation.</description></item>
    ///   <item><description>C: distinct context (same IKM) to verify context separation.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class ArkgP256RegressionVectors
    {
        public static byte[] BlindingPublicKey => HexToBytes(
            "046d3bdf31d0db48988f16d47048fdd24123cd286e42d0512daa9f726b4ecf18df" +
            "65ed42169c69675f936ff7de5f9bd93adbc8ea73036b16e8d90adbfabdaddba7");

        public static byte[] KemPublicKey => HexToBytes(
            "04c38bbdd7286196733fa177e43b73cfd3d6d72cd11cc0bb2c9236cf85a42dcff5" +
            "dfa339c1e07dfcdfda8d7be2a5a3c7382991f387dfe332b1dd8da6e0622cfb35");

        public static byte[] IkmA => HexToBytes(
            "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f");

        public static byte[] CtxA => Encoding.ASCII.GetBytes("ARKG-P256.test vectors");

        public static byte[] ExpectedDerivedA => HexToBytes(
            "04572a111ce5cfd2a67d56a0f7c684184b16ccd212490dc9c5b579df749647d107" +
            "dac2a1b197cc10d2376559ad6df6bc107318d5cfb90def9f4a1f5347e086c2cd");

        public static byte[] ExpectedHandleA => HexToBytes(
            "27987995f184a44cfa548d104b0a461d" +
            "0487fc739dbcdabc293ac5469221da91b220e04c681074ec4692a76ffacb9043" +
            "dec2847ea9060fd42da267f66852e63589f0c00dc88f290d660c65a65a50c86361");

        public static byte[] IkmB => HexToBytes(
            "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f");

        public static byte[] ExpectedDerivedB => HexToBytes(
            "04aed80c70cc9e2fa6b2d22db62285e6e3af7dc7426ce9846a500723d82aa60cd0" +
            "98168e98c4f437fc5d45986afaed5d5ce6e39de46fe4f61ae88541cb37687f8d");

        public static byte[] IkmAdditionalB => HexToBytes(
            "a0a1a2a3a4a5a6a7a8a9aaabacadaeafb0b1b2b3b4b5b6b7b8b9babbbcbdbebf");

        public static byte[] ExpectedDerivedAdditionalB => HexToBytes(
            "04ea7d962c9f44ffe8b18f1058a471f394ef81b674948eefc1865b5c021cf858f" +
            "577f9632b84220e4a1444a20b9430b86731c37e4dcb285eda38d76bf758918d86");

        public static byte[] CtxC => Encoding.ASCII.GetBytes("ARKG-P256.alt context");

        public static byte[] ExpectedDerivedC => HexToBytes(
            "04ccfc29c2d0f438642dae5153ccb4eda6be6ec8a0e654a009f2953ab4b52dc1eb" +
            "3ffbbf91b3e46e8e68a3c38c7268b2ca42f6d19c44dd5ee15fa0d30e0c9eb326");

        public static byte[] CtxAdditionalC => Encoding.ASCII.GetBytes("ARKG-P256.test vectors.0");

        public static byte[] ExpectedDerivedAdditionalC => HexToBytes(
            "04b79b65d6bbb419ff97006a1bd52e3f4ad53042173992423e06e52987a037cb61" +
            "dd82b126b162e4e7e8dc5c9fd86e82769d402a1968c7c547ef53ae4f96e10b0e");

        private static byte[] HexToBytes(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
    }
}
