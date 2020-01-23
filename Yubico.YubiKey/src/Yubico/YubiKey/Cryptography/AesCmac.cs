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

// `DoubleLu` and `ShiftLeft` are from BouncyCastle:
// Copyright (c) 2000 - 2020 The Legion of the Bouncy Castle Inc. (https://www.bouncycastle.org)
// See LICENSE.txt in the project root for full license text.
//
// Source: https://github.com/bcgit/bc-csharp/blob/bb8fafb833412453d062abbba6f22b22426286d6/crypto/src/crypto/macs/CMac.cs

using System;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    internal static class Cmac
    {
        private const byte Constant128 = (byte)0x87;

        private static readonly byte[] Zeroes = {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };


        private static int ShiftLeft(Span<byte> output, ReadOnlySpan<byte> block)
        {
            int i = block.Length;
            uint bit = 0;
            while (--i >= 0)
            {
                uint b = block[i];
                output[i] = (byte)((b << 1) | bit);
                bit = (b >> 7) & 1;
            }

            return (int)bit;
        }

        private static void DoubleLu(Span<byte> output, ReadOnlySpan<byte> input)
        {
            int carry = ShiftLeft(output, input);
            int xor = Constant128;

            // NOTE: This construction is an attempt at a constant-time implementation.
            output[input.Length - 1] ^= (byte)(xor >> ((1 - carry) << 3));
        }

        private static void AesCmacPadding(Span<byte> output, ReadOnlySpan<byte> input, int length)
        {
            for (int j = 0; j < 16; j++)
            {
                output[j] = j switch
                {
                    _ when j < length   => input[j],
                    _ when j == length  => 0x80,
                    _                   => 0x00,
                };
            }
        }

        private static void XorBytesInto(Span<byte> output, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            for (int i = 0; i < 16; i++)
            {
                output[i] = (byte)(a[i] ^ b[i]);
            }
        }

        /// <summary>
        /// Computes AES128-CMAC, a 'message authentication code' based on AES.
        /// </summary>
        /// <remarks>
        /// Per RFC 4493, this is a secure MAC using AES-128.
        /// </remarks>
        /// <param name="key">16-byte AES128 key</param>
        /// <param name="input">Input to compute the MAC over</param>
        /// <returns></returns>
        public static byte[] AesCmac(byte[] key, ReadOnlySpan<byte> input)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key.Length != 16)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectAesKeyLength, nameof(key));
            }

            if (input.Length == 0)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectPlaintextLength, nameof(input));
            }

            // output buffer, heap allocation required
            byte[] X = new byte[16];

            Span<byte> Y = stackalloc byte[16];
            Span<byte> M_last = stackalloc byte[16];

            // derive subkeys
            byte[] L = AesUtilities.BlockCipher(key, Zeroes);
            Span<byte> K1 = stackalloc byte[16];
            Span<byte> K2 = stackalloc byte[16];
            DoubleLu(K1, L);
            DoubleLu(K2, K1);
            CryptographicOperations.ZeroMemory(L);

            int length = input.Length;

            bool flag;

            // length calculation
            int n = checked((length + 15) / 16);

            if (n == 0)
            {
                n = 1;
                flag = false;
            }
            else
            {
                flag = (length % 16) == 0;
            }

            // padding, if necessary
            // this branch is only length-dependent
            if (flag)
            {
                XorBytesInto(M_last, K1, input[(16 * (n - 1))..(16 * n)]);
            }
            else
            {
                AesCmacPadding(M_last, input[(16 * (n - 1))..], length % 16);
                XorBytesInto(M_last, M_last, K2);
            }

            // actual CMAC calculation
            for (int i = 0; i < n - 1; i++)
            {
                XorBytesInto(Y, X, input[(16 * i)..((16 * i) + 16)]);
                CryptographicOperations.ZeroMemory(X);
                X = AesUtilities.BlockCipher(key, Y);
            }

            XorBytesInto(Y, X, M_last);
            CryptographicOperations.ZeroMemory(X);
            X = AesUtilities.BlockCipher(key, Y);

            CryptographicOperations.ZeroMemory(Y);
            CryptographicOperations.ZeroMemory(M_last);

            return X;
        }
    }
}
