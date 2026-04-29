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
// Direct P/Invoke functional tests for the AES-128-CMAC (Cipher-based MAC)
// EVP MAC wrappers exposed by Yubico.NativeShims (Native_CMAC_EVP_MAC_*).
// CMAC is consumed by SCP03 / PIV / OATH session authentication paths; an
// off-by-one in update chunking or a wrong subkey derivation results in
// silent authentication failures against real YubiKeys.
//
// What this validates
// -------------------
//   * MAC context lifecycle: Native_CMAC_EVP_MAC_CTX_new / Native_EVP_MAC_CTX_free.
//   * Native_CMAC_EVP_MAC_init binds the AES-128 key.
//   * Native_CMAC_EVP_MAC_update accepts variable-length chunks.
//   * Native_CMAC_EVP_MAC_final emits the 16-byte tag.
//   * RFC 4493 §4 published test vectors (AES-128) for messages of length
//     0, 16, 40, and 64 bytes — pins the wire-level contract.
//   * Multi-update equivalence: update(A) followed by update(B) produces the
//     same tag as update(A || B). Catches buffer-management regressions in
//     the C side.
//
// References
// ----------
//   * RFC 4493 — The AES-CMAC Algorithm, §2 (Specification), §4 (Test Vectors).
//     https://datatracker.ietf.org/doc/html/rfc4493
//   * NIST SP 800-38B — Recommendation for Block Cipher Modes of Operation:
//     The CMAC Mode for Authentication.
//     https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-38B.pdf
//   * FIPS 197 — Advanced Encryption Standard (AES).
//     https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.197.pdf
//   * OpenSSL EVP_MAC(3) — https://docs.openssl.org/master/man3/EVP_MAC/

using System;
using System.Linq;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.PlatformInterop.Cryptography
{
    public class CmacInteropTests
    {
        // Algorithm constants (from Cmac.Interop.cs comment)
        private const int Aes128Cbc = 1;
        private const int Aes192Cbc = 2;
        private const int Aes256Cbc = 3;

        // RFC 4493 §4 test vectors for AES-128-CMAC
        // Reference: https://www.rfc-editor.org/rfc/rfc4493.html#section-4
        private static readonly byte[] RFC4493_Key = new byte[]
        {
            0x2b, 0x7e, 0x15, 0x16, 0x28, 0xae, 0xd2, 0xa6,
            0xab, 0xf7, 0x15, 0x88, 0x09, 0xcf, 0x4f, 0x3c,
        };

        // Example 1: Empty message
        private static readonly byte[] RFC4493_Example1_Message = Array.Empty<byte>();
        private static readonly byte[] RFC4493_Example1_MAC = new byte[]
        {
            0xbb, 0x1d, 0x69, 0x29, 0xe9, 0x59, 0x37, 0x28,
            0x7f, 0xa3, 0x7d, 0x12, 0x9b, 0x75, 0x67, 0x46,
        };

        // Example 2: 16-byte message
        private static readonly byte[] RFC4493_Example2_Message = new byte[]
        {
            0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96,
            0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a,
        };
        private static readonly byte[] RFC4493_Example2_MAC = new byte[]
        {
            0x07, 0x0a, 0x16, 0xb4, 0x6b, 0x4d, 0x41, 0x44,
            0xf7, 0x9b, 0xdd, 0x9d, 0xd0, 0x4a, 0x28, 0x7c,
        };

        // Example 3: 40-byte message
        private static readonly byte[] RFC4493_Example3_Message = new byte[]
        {
            0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96,
            0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a,
            0xae, 0x2d, 0x8a, 0x57, 0x1e, 0x03, 0xac, 0x9c,
            0x9e, 0xb7, 0x6f, 0xac, 0x45, 0xaf, 0x8e, 0x51,
            0x30, 0xc8, 0x1c, 0x46, 0xa3, 0x5c, 0xe4, 0x11,
        };
        private static readonly byte[] RFC4493_Example3_MAC = new byte[]
        {
            0xdf, 0xa6, 0x67, 0x47, 0xde, 0x9a, 0xe6, 0x30,
            0x30, 0xca, 0x32, 0x61, 0x14, 0x97, 0xc8, 0x27,
        };

        // Example 4: 64-byte message
        private static readonly byte[] RFC4493_Example4_Message = new byte[]
        {
            0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96,
            0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a,
            0xae, 0x2d, 0x8a, 0x57, 0x1e, 0x03, 0xac, 0x9c,
            0x9e, 0xb7, 0x6f, 0xac, 0x45, 0xaf, 0x8e, 0x51,
            0x30, 0xc8, 0x1c, 0x46, 0xa3, 0x5c, 0xe4, 0x11,
            0xe5, 0xfb, 0xc1, 0x19, 0x1a, 0x0a, 0x52, 0xef,
            0xf6, 0x9f, 0x24, 0x45, 0xdf, 0x4f, 0x9b, 0x17,
            0xad, 0x2b, 0x41, 0x7b, 0xe6, 0x6c, 0x37, 0x10,
        };
        private static readonly byte[] RFC4493_Example4_MAC = new byte[]
        {
            0x51, 0xf0, 0xbe, 0xbf, 0x7e, 0x3b, 0x9d, 0x92,
            0xfc, 0x49, 0x74, 0x17, 0x79, 0x36, 0x3c, 0xfe,
        };

        [Fact]
        public void CmacEvpMacCtxNew_CreatesValidContext()
        {
            using SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew();

            Assert.NotNull(ctx);
            Assert.False(ctx.IsInvalid);
        }

        [Fact]
        public void CmacEvpMacInit_ValidParameters_ReturnsSuccess()
        {
            using SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew();

            int result = NativeMethods.CmacEvpMacInit(ctx, Aes128Cbc, RFC4493_Key, RFC4493_Key.Length);

            Assert.Equal(1, result);
        }

        [Fact]
        public void Cmac_RFC4493_Example1_EmptyMessage_MatchesExpectedMAC()
        {
            // RFC 4493 §4 Example 1: empty message
            using SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew();

            int initResult = NativeMethods.CmacEvpMacInit(ctx, Aes128Cbc, RFC4493_Key, RFC4493_Key.Length);
            Assert.Equal(1, initResult);

            byte[] mac = new byte[16];
            int finalResult = NativeMethods.CmacEvpMacFinal(ctx, mac, mac.Length, out int macLen);
            Assert.Equal(1, finalResult);
            Assert.Equal(16, macLen);

            Assert.Equal(RFC4493_Example1_MAC, mac);
        }

        [Fact]
        public void Cmac_RFC4493_Example2_16ByteMessage_MatchesExpectedMAC()
        {
            // RFC 4493 §4 Example 2: 16-byte message (one block)
            using SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew();

            int initResult = NativeMethods.CmacEvpMacInit(ctx, Aes128Cbc, RFC4493_Key, RFC4493_Key.Length);
            Assert.Equal(1, initResult);

            int updateResult = NativeMethods.CmacEvpMacUpdate(ctx, RFC4493_Example2_Message, RFC4493_Example2_Message.Length);
            Assert.Equal(1, updateResult);

            byte[] mac = new byte[16];
            int finalResult = NativeMethods.CmacEvpMacFinal(ctx, mac, mac.Length, out int macLen);
            Assert.Equal(1, finalResult);
            Assert.Equal(16, macLen);

            Assert.Equal(RFC4493_Example2_MAC, mac);
        }

        [Fact]
        public void Cmac_RFC4493_Example3_40ByteMessage_MatchesExpectedMAC()
        {
            // RFC 4493 §4 Example 3: 40-byte message (non-block-aligned)
            using SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew();

            int initResult = NativeMethods.CmacEvpMacInit(ctx, Aes128Cbc, RFC4493_Key, RFC4493_Key.Length);
            Assert.Equal(1, initResult);

            int updateResult = NativeMethods.CmacEvpMacUpdate(ctx, RFC4493_Example3_Message, RFC4493_Example3_Message.Length);
            Assert.Equal(1, updateResult);

            byte[] mac = new byte[16];
            int finalResult = NativeMethods.CmacEvpMacFinal(ctx, mac, mac.Length, out int macLen);
            Assert.Equal(1, finalResult);
            Assert.Equal(16, macLen);

            Assert.Equal(RFC4493_Example3_MAC, mac);
        }

        [Fact]
        public void Cmac_RFC4493_Example4_64ByteMessage_MatchesExpectedMAC()
        {
            // RFC 4493 §4 Example 4: 64-byte message (block-aligned, multiple blocks)
            using SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew();

            int initResult = NativeMethods.CmacEvpMacInit(ctx, Aes128Cbc, RFC4493_Key, RFC4493_Key.Length);
            Assert.Equal(1, initResult);

            int updateResult = NativeMethods.CmacEvpMacUpdate(ctx, RFC4493_Example4_Message, RFC4493_Example4_Message.Length);
            Assert.Equal(1, updateResult);

            byte[] mac = new byte[16];
            int finalResult = NativeMethods.CmacEvpMacFinal(ctx, mac, mac.Length, out int macLen);
            Assert.Equal(1, finalResult);
            Assert.Equal(16, macLen);

            Assert.Equal(RFC4493_Example4_MAC, mac);
        }

        [Fact]
        public void Cmac_MultiUpdate_EquivalentToSingleUpdate()
        {
            // update(A) + update(B) should equal update(A||B)
            byte[] messageA = RFC4493_Example4_Message.Take(32).ToArray();
            byte[] messageB = RFC4493_Example4_Message.Skip(32).ToArray();

            // Single update
            byte[] macSingle;
            using (SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew())
            {
                NativeMethods.CmacEvpMacInit(ctx, Aes128Cbc, RFC4493_Key, RFC4493_Key.Length);
                NativeMethods.CmacEvpMacUpdate(ctx, RFC4493_Example4_Message, RFC4493_Example4_Message.Length);
                macSingle = new byte[16];
                NativeMethods.CmacEvpMacFinal(ctx, macSingle, macSingle.Length, out _);
            }

            // Multi update
            byte[] macMulti;
            using (SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew())
            {
                NativeMethods.CmacEvpMacInit(ctx, Aes128Cbc, RFC4493_Key, RFC4493_Key.Length);
                NativeMethods.CmacEvpMacUpdate(ctx, messageA, messageA.Length);
                NativeMethods.CmacEvpMacUpdate(ctx, messageB, messageB.Length);
                macMulti = new byte[16];
                NativeMethods.CmacEvpMacFinal(ctx, macMulti, macMulti.Length, out _);
            }

            Assert.Equal(macSingle, macMulti);
            Assert.Equal(RFC4493_Example4_MAC, macMulti);
        }

        [Fact]
        public void Cmac_MultiUpdate_ThreeChunks_MatchesRFC()
        {
            // Verify multi-update with three arbitrary chunks of Example 3 (40 bytes)
            byte[] chunk1 = RFC4493_Example3_Message.Take(10).ToArray();
            byte[] chunk2 = RFC4493_Example3_Message.Skip(10).Take(20).ToArray();
            byte[] chunk3 = RFC4493_Example3_Message.Skip(30).ToArray();

            using SafeEvpCmacCtx ctx = NativeMethods.CmacEvpMacCtxNew();

            NativeMethods.CmacEvpMacInit(ctx, Aes128Cbc, RFC4493_Key, RFC4493_Key.Length);
            NativeMethods.CmacEvpMacUpdate(ctx, chunk1, chunk1.Length);
            NativeMethods.CmacEvpMacUpdate(ctx, chunk2, chunk2.Length);
            NativeMethods.CmacEvpMacUpdate(ctx, chunk3, chunk3.Length);

            byte[] mac = new byte[16];
            NativeMethods.CmacEvpMacFinal(ctx, mac, mac.Length, out int macLen);

            Assert.Equal(16, macLen);
            Assert.Equal(RFC4493_Example3_MAC, mac);
        }
    }
}
