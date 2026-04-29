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
using System.Linq;
using Xunit;
using Yubico.PlatformInterop;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.PlatformInterop.Cryptography
{
    public class GcmEvpInteropTests
    {
        // NIST SP 800-38D Test Case 13: 256-bit key, empty plaintext, empty AAD
        // Reference: https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Algorithm-Validation-Program/documents/mac/gcmtestvectors.zip
        private static readonly byte[] Nist_TC13_Key = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] Nist_TC13_Nonce = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] Nist_TC13_Tag = new byte[]
        {
            0x53, 0x0F, 0x8A, 0xFB, 0xC7, 0x45, 0x36, 0xB9,
            0xA9, 0x63, 0xB4, 0xF1, 0xC4, 0xCB, 0x73, 0x8B,
        };

        // NIST SP 800-38D Test Case 14: 256-bit key, 16-byte plaintext, empty AAD
        private static readonly byte[] Nist_TC14_Key = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] Nist_TC14_Nonce = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] Nist_TC14_Plaintext = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private static readonly byte[] Nist_TC14_Ciphertext = new byte[]
        {
            0xCE, 0xA7, 0x40, 0x3D, 0x4D, 0x60, 0x6B, 0x6E,
            0x07, 0x4E, 0xC5, 0xD3, 0xBA, 0xF3, 0x9D, 0x18,
        };

        private static readonly byte[] Nist_TC14_Tag = new byte[]
        {
            0xD0, 0xD1, 0xC8, 0xA7, 0x99, 0x99, 0x6B, 0xF0,
            0x26, 0x5B, 0x98, 0xB5, 0xD4, 0x8A, 0xB9, 0x19,
        };

        [Fact]
        public void EvpCipherCtxNew_CreatesValidContext()
        {
            using SafeEvpCipherCtx ctx = NativeMethods.EvpCipherCtxNew();

            Assert.NotNull(ctx);
            Assert.False(ctx.IsInvalid);
        }

        [Fact]
        public void EvpAes256GcmInit_ValidParameters_ReturnsSuccess()
        {
            using SafeEvpCipherCtx ctx = NativeMethods.EvpCipherCtxNew();

            int result = NativeMethods.EvpAes256GcmInit(true, ctx, Nist_TC13_Key, Nist_TC13_Nonce);

            Assert.Equal(1, result);
        }

        [Fact]
        public void Encrypt_EmptyPlaintext_MatchesNistTC13Tag()
        {
            // NIST SP 800-38D Test Case 13: empty plaintext, verify tag
            using SafeEvpCipherCtx ctx = NativeMethods.EvpCipherCtxNew();

            int initResult = NativeMethods.EvpAes256GcmInit(true, ctx, Nist_TC13_Key, Nist_TC13_Nonce);
            Assert.Equal(1, initResult);

            byte[] output = new byte[16];
            int finalResult = NativeMethods.EvpFinal(ctx, output, out int outLen);
            Assert.Equal(1, finalResult);
            Assert.Equal(0, outLen); // No ciphertext for empty plaintext

            byte[] tag = new byte[16];
            int tagResult = NativeMethods.EvpCipherCtxCtrl(ctx, CtrlFlag.GetTag, 16, tag);
            Assert.Equal(1, tagResult);

            Assert.Equal(Nist_TC13_Tag, tag);
        }

        [Fact]
        public void Encrypt_16BytePlaintext_MatchesNistTC14()
        {
            // NIST SP 800-38D Test Case 14: 16-byte plaintext, verify ciphertext + tag
            using SafeEvpCipherCtx ctx = NativeMethods.EvpCipherCtxNew();

            int initResult = NativeMethods.EvpAes256GcmInit(true, ctx, Nist_TC14_Key, Nist_TC14_Nonce);
            Assert.Equal(1, initResult);

            byte[] ciphertext = new byte[16];
            int updateResult = NativeMethods.EvpUpdate(ctx, ciphertext, out int ctLen, Nist_TC14_Plaintext, Nist_TC14_Plaintext.Length);
            Assert.Equal(1, updateResult);
            Assert.Equal(16, ctLen);

            byte[] finalBuffer = new byte[16];
            int finalResult = NativeMethods.EvpFinal(ctx, finalBuffer, out int finalLen);
            Assert.Equal(1, finalResult);
            Assert.Equal(0, finalLen);

            byte[] tag = new byte[16];
            int tagResult = NativeMethods.EvpCipherCtxCtrl(ctx, CtrlFlag.GetTag, 16, tag);
            Assert.Equal(1, tagResult);

            Assert.Equal(Nist_TC14_Ciphertext, ciphertext);
            Assert.Equal(Nist_TC14_Tag, tag);
        }

        [Fact]
        public void RoundTrip_WithAAD_DecryptsSuccessfully()
        {
            byte[] key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            byte[] nonce = Enumerable.Range(0, 12).Select(i => (byte)(i * 11)).ToArray();
            byte[] plaintext = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 }; // "Hello World"
            byte[] aad = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

            // Encrypt
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag;

            using (SafeEvpCipherCtx encCtx = NativeMethods.EvpCipherCtxNew())
            {
                int initResult = NativeMethods.EvpAes256GcmInit(true, encCtx, key, nonce);
                Assert.Equal(1, initResult);

                // Add AAD (output = null → input is AAD)
                int aadResult = NativeMethods.EvpUpdate(encCtx, null, out int aadLen, aad, aad.Length);
                Assert.Equal(1, aadResult);

                // Encrypt plaintext
                int updateResult = NativeMethods.EvpUpdate(encCtx, ciphertext, out int ctLen, plaintext, plaintext.Length);
                Assert.Equal(1, updateResult);
                Assert.Equal(plaintext.Length, ctLen);

                byte[] finalBuffer = new byte[16];
                int finalResult = NativeMethods.EvpFinal(encCtx, finalBuffer, out int finalLen);
                Assert.Equal(1, finalResult);

                tag = new byte[16];
                int tagResult = NativeMethods.EvpCipherCtxCtrl(encCtx, CtrlFlag.GetTag, 16, tag);
                Assert.Equal(1, tagResult);
            }

            // Decrypt
            byte[] decrypted = new byte[ciphertext.Length];

            using (SafeEvpCipherCtx decCtx = NativeMethods.EvpCipherCtxNew())
            {
                int initResult = NativeMethods.EvpAes256GcmInit(false, decCtx, key, nonce);
                Assert.Equal(1, initResult);

                // Add AAD
                int aadResult = NativeMethods.EvpUpdate(decCtx, null, out int aadLen, aad, aad.Length);
                Assert.Equal(1, aadResult);

                // Decrypt ciphertext
                int updateResult = NativeMethods.EvpUpdate(decCtx, decrypted, out int ptLen, ciphertext, ciphertext.Length);
                Assert.Equal(1, updateResult);
                Assert.Equal(ciphertext.Length, ptLen);

                // Set expected tag before finalize
                int setTagResult = NativeMethods.EvpCipherCtxCtrl(decCtx, CtrlFlag.SetTag, tag.Length, tag);
                Assert.Equal(1, setTagResult);

                byte[] finalBuffer = new byte[16];
                int finalResult = NativeMethods.EvpFinal(decCtx, finalBuffer, out int finalLen);
                Assert.Equal(1, finalResult); // Tag verification succeeded
            }

            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void Decrypt_TamperedTag_FailsAuthentication()
        {
            byte[] key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            byte[] nonce = Enumerable.Range(0, 12).Select(i => (byte)(i * 11)).ToArray();
            byte[] plaintext = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
            byte[] aad = new byte[] { 0xAA, 0xBB };

            // Encrypt
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag;

            using (SafeEvpCipherCtx encCtx = NativeMethods.EvpCipherCtxNew())
            {
                NativeMethods.EvpAes256GcmInit(true, encCtx, key, nonce);
                NativeMethods.EvpUpdate(encCtx, null, out _, aad, aad.Length);
                NativeMethods.EvpUpdate(encCtx, ciphertext, out _, plaintext, plaintext.Length);
                byte[] finalBuffer = new byte[16];
                NativeMethods.EvpFinal(encCtx, finalBuffer, out _);

                tag = new byte[16];
                NativeMethods.EvpCipherCtxCtrl(encCtx, CtrlFlag.GetTag, 16, tag);
            }

            // Tamper with tag
            tag[0] ^= 0x01;

            // Decrypt with tampered tag
            byte[] decrypted = new byte[ciphertext.Length];

            using (SafeEvpCipherCtx decCtx = NativeMethods.EvpCipherCtxNew())
            {
                NativeMethods.EvpAes256GcmInit(false, decCtx, key, nonce);
                NativeMethods.EvpUpdate(decCtx, null, out _, aad, aad.Length);
                NativeMethods.EvpUpdate(decCtx, decrypted, out _, ciphertext, ciphertext.Length);
                NativeMethods.EvpCipherCtxCtrl(decCtx, CtrlFlag.SetTag, tag.Length, tag);

                byte[] finalBuffer = new byte[16];
                int finalResult = NativeMethods.EvpFinal(decCtx, finalBuffer, out _);

                // Tag verification must fail
                Assert.Equal(0, finalResult);
            }
        }

        [Fact]
        public void Decrypt_ModifiedAAD_FailsAuthentication()
        {
            byte[] key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            byte[] nonce = Enumerable.Range(0, 12).Select(i => (byte)(i * 11)).ToArray();
            byte[] plaintext = new byte[] { 0x48, 0x65, 0x6C };
            byte[] aad = new byte[] { 0xAA, 0xBB, 0xCC };

            // Encrypt
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag;

            using (SafeEvpCipherCtx encCtx = NativeMethods.EvpCipherCtxNew())
            {
                NativeMethods.EvpAes256GcmInit(true, encCtx, key, nonce);
                NativeMethods.EvpUpdate(encCtx, null, out _, aad, aad.Length);
                NativeMethods.EvpUpdate(encCtx, ciphertext, out _, plaintext, plaintext.Length);
                byte[] finalBuffer = new byte[16];
                NativeMethods.EvpFinal(encCtx, finalBuffer, out _);

                tag = new byte[16];
                NativeMethods.EvpCipherCtxCtrl(encCtx, CtrlFlag.GetTag, 16, tag);
            }

            // Modify AAD
            byte[] modifiedAad = (byte[])aad.Clone();
            modifiedAad[0] ^= 0x01;

            // Decrypt with modified AAD
            byte[] decrypted = new byte[ciphertext.Length];

            using (SafeEvpCipherCtx decCtx = NativeMethods.EvpCipherCtxNew())
            {
                NativeMethods.EvpAes256GcmInit(false, decCtx, key, nonce);
                NativeMethods.EvpUpdate(decCtx, null, out _, modifiedAad, modifiedAad.Length);
                NativeMethods.EvpUpdate(decCtx, decrypted, out _, ciphertext, ciphertext.Length);
                NativeMethods.EvpCipherCtxCtrl(decCtx, CtrlFlag.SetTag, tag.Length, tag);

                byte[] finalBuffer = new byte[16];
                int finalResult = NativeMethods.EvpFinal(decCtx, finalBuffer, out _);

                // AAD verification must fail
                Assert.Equal(0, finalResult);
            }
        }
    }
}
