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
using System.Text;
using Xunit;

namespace Yubico.YubiKey.Fido2.Arkg
{
    /// <summary>
    /// KAT (Known Answer Tests) for ARKG-P256 derivation.
    /// Vectors generated from the Rust reference implementation in
    /// cnh-authenticator-rs-extension/native/crates/hid-test/src/arkg.rs
    /// (see /tmp/gen_kat for the generator program). Vector A also matches the
    /// embedded `arkg.rs::tests::test_arkg_derive_key` expected output.
    /// </summary>
    public class ArkgP256Tests
    {
        // Both vectors share these seeds, derived from ikm_bl=0x00..1F and
        // ikm_kem=0x20..3F via the Rust reference's BL/KEM key-generation paths.
        private static readonly byte[] PkBl = HexToBytes(
            "046d3bdf31d0db48988f16d47048fdd24123cd286e42d0512daa9f726b4ecf18df" +
            "65ed42169c69675f936ff7de5f9bd93adbc8ea73036b16e8d90adbfabdaddba7");

        private static readonly byte[] PkKem = HexToBytes(
            "04c38bbdd7286196733fa177e43b73cfd3d6d72cd11cc0bb2c9236cf85a42dcff5" +
            "dfa339c1e07dfcdfda8d7be2a5a3c7382991f387dfe332b1dd8da6e0622cfb35");

        // Vector A — matches arkg.rs::tests::test_arkg_derive_key.
        private static readonly byte[] IkmA = HexToBytes(
            "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f");
        private static readonly byte[] CtxA = Encoding.ASCII.GetBytes("ARKG-P256.test vectors");
        private static readonly byte[] ExpectedDerivedA = HexToBytes(
            "04572a111ce5cfd2a67d56a0f7c684184b16ccd212490dc9c5b579df749647d107" +
            "dac2a1b197cc10d2376559ad6df6bc107318d5cfb90def9f4a1f5347e086c2cd");
        private static readonly byte[] ExpectedHandleA = HexToBytes(
            "27987995f184a44cfa548d104b0a461d" + // 16-byte HMAC tag
            "0487fc739dbcdabc293ac5469221da91b220e04c681074ec4692a76ffacb9043" +
            "dec2847ea9060fd42da267f66852e63589f0c00dc88f290d660c65a65a50c86361");

        // Vector B — different IKM, same ctx as A.
        private static readonly byte[] IkmB = HexToBytes(
            "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f");
        private static readonly byte[] ExpectedDerivedB = HexToBytes(
            "04aed80c70cc9e2fa6b2d22db62285e6e3af7dc7426ce9846a500723d82aa60cd0" +
            "98168e98c4f437fc5d45986afaed5d5ce6e39de46fe4f61ae88541cb37687f8d");

        // Vector C — same IKM as A, different ctx.
        private static readonly byte[] CtxC = Encoding.ASCII.GetBytes("ARKG-P256.alt context");
        private static readonly byte[] ExpectedDerivedC = HexToBytes(
            "04ccfc29c2d0f438642dae5153ccb4eda6be6ec8a0e654a009f2953ab4b52dc1eb" +
            "3ffbbf91b3e46e8e68a3c38c7268b2ca42f6d19c44dd5ee15fa0d30e0c9eb326");

        [Fact]
        public void DerivePublicKey_AgainstRustKAT_ProducesExpectedPublicKey()
        {
            (byte[] derivedPk, byte[] arkgKeyHandle) = ArkgP256.DerivePublicKey(PkBl, PkKem, IkmA, CtxA);

            Assert.Equal(ExpectedDerivedA, derivedPk);
            Assert.Equal(ExpectedHandleA, arkgKeyHandle);
        }

        [Fact]
        public void DerivePublicKey_DifferentIkm_ProducesDifferentKeys()
        {
            (byte[] derivedA, _) = ArkgP256.DerivePublicKey(PkBl, PkKem, IkmA, CtxA);
            (byte[] derivedB, _) = ArkgP256.DerivePublicKey(PkBl, PkKem, IkmB, CtxA);

            Assert.NotEqual(derivedA, derivedB);
            Assert.Equal(ExpectedDerivedA, derivedA);
            Assert.Equal(ExpectedDerivedB, derivedB);
        }

        [Fact]
        public void DerivePublicKey_DifferentCtx_ProducesDifferentKeys()
        {
            (byte[] derivedA, _) = ArkgP256.DerivePublicKey(PkBl, PkKem, IkmA, CtxA);
            (byte[] derivedC, _) = ArkgP256.DerivePublicKey(PkBl, PkKem, IkmA, CtxC);

            Assert.NotEqual(derivedA, derivedC);
            Assert.Equal(ExpectedDerivedC, derivedC);
        }

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
