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
using System.Globalization;
using System.Numerics;
using static Yubico.Core.Cryptography.ArkgByteUtilities;

namespace Yubico.Core.Cryptography
{
    internal static class ArkgP256Scalar
    {
        private const int P256ScalarLength = 32;

        // DST_ext for ARKG-P256, draft-bradleylundberg-cfrg-arkg-10 section 4.1:
        // https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html#name-arkg-p256
        internal const string DstExt = "ARKG-P256";

        // P-256 group order N (SEC 2 v2, section 2.4.2). Used for scalar reduction mod N.
        private static readonly BigInteger N = BigInteger.Parse(
            "00FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551",
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);

        internal static BigInteger HashToScalar(ReadOnlySpan<byte> msg, ReadOnlySpan<byte> dst)
        {
            const int L = 48;
            byte[] uniform = ExpandMessageXmdSha256(msg, dst, L);

            // Follows RFC 9380 section 5.2 hash_to_field reduction with the
            // P256_XMD:SHA-256_SSWU_RO_ section 8.2 parameters (m=1, L=48,
            // expand_message_xmd/SHA-256), except ARKG-P256 hashes to a
            // scalar by reducing modulo the P-256 group order N.
            return Os2Ip(uniform) % N;
        }

        internal static byte[] ToFixedWidthBytes(BigInteger scalar)
        {
            // BigInteger is signed little-endian, while OpenSSL BN input
            // expects unsigned big-endian.
            byte[] littleEndian = scalar.ToByteArray();
            int length = littleEndian.Length;
            if (length > 1 && littleEndian[length - 1] == 0)
            {
                length--;
            }

            byte[] bigEndian = new byte[P256ScalarLength];
            int copyLength = Math.Min(length, P256ScalarLength);
            for (int i = 0; i < copyLength; i++)
            {
                bigEndian[P256ScalarLength - 1 - i] = littleEndian[i];
            }

            return bigEndian;
        }

        // RFC 9380 expand_message_xmd instantiated with SHA-256 (b_in_bytes=32,
        // s_in_bytes=64), as used by the P256_XMD:SHA-256 suites.
        private static byte[] ExpandMessageXmdSha256(ReadOnlySpan<byte> msg, ReadOnlySpan<byte> dst, int lenInBytes)
        {
            const int BInBytes = 32;
            const int SInBytes = 64;

            // Equivalent to ceil(len_in_bytes / b_in_bytes) from RFC 9380.
            int ell = (lenInBytes + BInBytes - 1) / BInBytes;
            if (ell > 255 || lenInBytes > 65535 || dst.Length > 255)
            {
                throw new ArgumentException("expand_message_xmd parameter out of range.");
            }

            byte[] dstPrime = new byte[dst.Length + 1];
            dst.CopyTo(dstPrime);
            dstPrime[dst.Length] = (byte)dst.Length;

            byte[] zPad = new byte[SInBytes];
            // RFC 9380 uses I2OSP(len_in_bytes, 2); the range check above
            // keeps the value representable in exactly two octets.
            byte[] lIBStr = [(byte)((lenInBytes >> 8) & 0xFF), (byte)(lenInBytes & 0xFF)];

            byte[] msgPrime = Concat(zPad, msg.ToArray(), lIBStr, [0x00], dstPrime);

            byte[][] bVals = new byte[ell + 1][];
            bVals[0] = Sha256(msgPrime);
            bVals[1] = Sha256(Concat(bVals[0], [0x01], dstPrime));

            Span<byte> xored = stackalloc byte[BInBytes];
            for (int i = 2; i <= ell; i++)
            {
                for (int j = 0; j < BInBytes; j++)
                {
                    xored[j] = (byte)(bVals[0][j] ^ bVals[i - 1][j]);
                }

                byte[] input = Concat(xored.ToArray(), [(byte)i], dstPrime);
                bVals[i] = Sha256(input);
            }

            byte[] result = new byte[lenInBytes];
            Span<byte> resultSpan = result;
            int offset = 0;
            for (int i = 1; i <= ell && offset < lenInBytes; i++)
            {
                int copy = Math.Min(BInBytes, lenInBytes - offset);
                bVals[i].AsSpan(0, copy).CopyTo(resultSpan[offset..]);
                offset += copy;
            }

            return result;
        }

        // RFC 8017 OS2IP: interpret an octet string as a non-negative integer.
        private static BigInteger Os2Ip(ReadOnlySpan<byte> bytes)
        {
            byte[] padded = new byte[bytes.Length + 1];
            for (int i = 0; i < bytes.Length; i++)
            {
                padded[bytes.Length - 1 - i] = bytes[i];
            }

            // padded[bytes.Length] = 0 by default - explicit positive sign.
            return new BigInteger(padded);
        }
    }
}
