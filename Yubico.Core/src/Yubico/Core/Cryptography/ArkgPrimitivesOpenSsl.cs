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
using System.Security;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Diagnostics;
using Yubico.PlatformInterop;

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// OpenSSL-backed implementation of <see cref="IArkgPrimitives"/> for ARKG-P256.
    /// </summary>
    /// <remarks>
    /// Provides the security-critical primitives required by the ARKG-P256
    /// algorithm: on-curve point validation, ECDH shared-secret computation,
    /// and the full draft-bradleylundberg-cfrg-arkg-09 derivation. Point math
    /// goes through Yubico.NativeShims (OpenSSL); scalar reduction uses
    /// <see cref="BigInteger"/>.
    /// </remarks>
    internal sealed class ArkgPrimitivesOpenSsl : IArkgPrimitives
    {
        private const int P256CoordinateLength = 32;
        private const int Sec1UncompressedLength = 1 + (2 * P256CoordinateLength);
        private const byte Sec1UncompressedTag = 0x04;

        // Cross-ref: python-fido2 arkg.py:248,289,360,371; draft-bradleylundberg-cfrg-arkg-09 §5
        private const string DstExt = "ARKG-P256";

        // P-256 group order N (SEC2 v2 §2.4.2). Used for scalar reduction mod N.
        private static readonly BigInteger N = BigInteger.Parse(
            "00FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551",
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);

        // 2^256 mod N — used by the wide reduction in HashToScalar.
        // Bytes (BE): 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00
        //             43 19 05 52 58 E8 61 7B 0C 46 35 3D 03 9C DA AF
        private static readonly BigInteger TwoPow256ModN = BigInteger.Parse(
            "0000000000FFFFFFFF00000000000000004319055258E8617B0C46353D039CDAAF",
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public bool IsPointOnCurve(ReadOnlySpan<byte> point)
        {
            if (point.Length != Sec1UncompressedLength || point[0] != Sec1UncompressedTag)
            {
                return false;
            }

            // Extract coordinates using span slicing, then convert to array for P/Invoke
            byte[] xBytes = point.Slice(1, P256CoordinateLength).ToArray();
            byte[] yBytes = point.Slice(1 + P256CoordinateLength, P256CoordinateLength).ToArray();

            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(
                ECCurve.NamedCurves.nistP256.ToSslCurveId());
            using SafeEcPoint sslPoint = NativeMethods.EcPointNew(group);
            using SafeBigNum xBn = NativeMethods.BnBinaryToBigNum(xBytes);
            using SafeBigNum yBn = NativeMethods.BnBinaryToBigNum(yBytes);

            // EC_POINT_set_affine_coordinates returns 0 for an off-curve point on
            // most modern OpenSSL builds; fall through to the explicit on-curve
            // check so the answer is unambiguous regardless of OpenSSL version.
            int setResult = NativeMethods.EcPointSetAffineCoordinates(group, sslPoint, xBn, yBn);
            if (setResult != 1)
            {
                return false;
            }

            int onCurve = NativeMethods.EcPointIsOnCurve(group, sslPoint);
            return onCurve == 1;
        }

        /// <inheritdoc />
        public byte[] ComputeEcdhSharedSecret(ReadOnlySpan<byte> privateScalar, ReadOnlySpan<byte> publicPoint)
        {
            Guard.HasSizeGreaterThan(privateScalar, 0, nameof(privateScalar));
            Guard.HasSizeGreaterThan(publicPoint, 0, nameof(publicPoint));

            if (publicPoint.Length != Sec1UncompressedLength || publicPoint[0] != Sec1UncompressedTag)
            {
                throw new ArgumentException(
                    "Public point must be a 65-byte SEC1 uncompressed P-256 point.",
                    nameof(publicPoint));
            }

            // Extract coordinates using span slicing
            byte[] x = publicPoint.Slice(1, P256CoordinateLength).ToArray();
            byte[] y = publicPoint.Slice(1 + P256CoordinateLength, P256CoordinateLength).ToArray();

            var publicKey = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = x, Y = y }
            };

            // Reject points that lie outside the curve before doing any scalar
            // multiplication. Defends against invalid-curve attacks on the KEM
            // public key carried in the previewSign generated-key blob.
            if (!IsPointOnCurve(publicPoint))
            {
                throw new SecurityException("Public point is not on the P-256 curve.");
            }

            return EcdhPrimitives.Create().ComputeSharedSecret(publicKey, privateScalar.ToArray());
        }

        /// <inheritdoc />
        public (byte[] derivedPk, byte[] arkgKeyHandle) Derive(
            ReadOnlySpan<byte> pkBl,
            ReadOnlySpan<byte> pkKem,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> ctx)
        {
            Guard.HasSizeGreaterThan(pkBl, 0, nameof(pkBl));
            Guard.HasSizeGreaterThan(pkKem, 0, nameof(pkKem));
            Guard.HasSizeGreaterThan(ikm, 0, nameof(ikm));
            Guard.HasSizeLessThanOrEqualTo(ctx, 64, nameof(ctx));

            if (!IsPointOnCurve(pkBl))
            {
                throw new ArgumentException("pkBl is not on the P-256 curve.", nameof(pkBl));
            }

            if (!IsPointOnCurve(pkKem))
            {
                throw new ArgumentException("pkKem is not on the P-256 curve.", nameof(pkKem));
            }

            byte[] ctxPrime = new byte[1 + ctx.Length];
            ctxPrime[0] = (byte)ctx.Length;
            ctx.CopyTo(ctxPrime.AsSpan(1));

            byte[] ctxKem = Concat(Encoding.ASCII.GetBytes("ARKG-Derive-Key-KEM."), ctxPrime);
            byte[] ctxBl = Concat(Encoding.ASCII.GetBytes("ARKG-Derive-Key-BL."), ctxPrime);

            (byte[] ikmTau, byte[] cipher) = HmacKemEncaps(pkKem, ikm, ctxKem);
            BigInteger tau;
            try
            {
                tau = BlPrf(ikmTau, ctxBl);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ikmTau);
            }

            byte[] derivedPk = BlBlindPublicKey(pkBl, tau);
            return (derivedPk, cipher);
        }

        // ---------------------------------------------------------------------
        // ARKG-BL: blinding-key arithmetic
        // ---------------------------------------------------------------------

        private static BigInteger BlPrf(ReadOnlySpan<byte> ikmTau, ReadOnlySpan<byte> ctx)
        {
            byte[] dst = Concat(
                Encoding.ASCII.GetBytes("ARKG-BL-EC."),
                Encoding.ASCII.GetBytes(DstExt),
                ctx.ToArray());
            return HashToScalar(ikmTau, dst);
        }

        private static byte[] BlBlindPublicKey(ReadOnlySpan<byte> pkBl, BigInteger tau)
        {
            byte[] tauBytes = ScalarToBytes(tau);
            try
            {
                using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(
                    ECCurve.NamedCurves.nistP256.ToSslCurveId());
                using SafeEcPoint pkBlPoint = SecToPoint(group, pkBl);
                using SafeEcPoint result = NativeMethods.EcPointNew(group);
                using SafeBigNum tauBn = NativeMethods.BnBinaryToBigNum(tauBytes);
                using SafeBigNum oneBn = NativeMethods.BnBinaryToBigNum([0x01]);

                // r = tau*G + 1*pkBl  =>  r = pkBl + tau*G.
                int rc = NativeMethods.EcPointMul(
                    group,
                    result,
                    tauBn.DangerousGetHandle(),
                    pkBlPoint.DangerousGetHandle(),
                    oneBn.DangerousGetHandle());
                if (rc != 1)
                {
                    throw new CryptographicException("EC_POINT_mul failed in BlBlindPublicKey.");
                }

                return PointToSec(group, result);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(tauBytes);
            }
        }

        // ---------------------------------------------------------------------
        // ARKG-KEM: ECDH-KEM with HMAC wrapper
        // ---------------------------------------------------------------------

        private (byte[] shared, byte[] ciphertext) HmacKemEncaps(ReadOnlySpan<byte> pkKem, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> ctx)
        {
            byte[] dstAug = Encoding.ASCII.GetBytes("ARKG-ECDH.ARKG-P256");

            // Generate ephemeral keypair from IKM (deterministic, matches Rust reference).
            (byte[] ephPk, BigInteger ephSk) = KemDeriveKeypair(ikm);

            byte[] ephSkBytes = ScalarToBytes(ephSk);
            byte[] kPrime;
            try
            {
                kPrime = ComputeEcdhSharedSecret(ephSkBytes, pkKem);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ephSkBytes);
            }

            byte[] macInfo = Concat(
                Encoding.ASCII.GetBytes("ARKG-KEM-HMAC-mac."),
                dstAug,
                ctx.ToArray());
            byte[] mk = HkdfUtilities.DeriveKey(kPrime, salt: ReadOnlySpan<byte>.Empty, contextInfo: macInfo, length: 32).ToArray();

            byte[] tag;
            try
            {
                using HMACSHA256 hmac = new HMACSHA256(mk);
                byte[] full = hmac.ComputeHash(ephPk);
                tag = full.AsSpan(0, 16).ToArray();
                CryptographicOperations.ZeroMemory(full);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(mk);
            }

            byte[] sharedInfo = Concat(
                Encoding.ASCII.GetBytes("ARKG-KEM-HMAC-shared."),
                dstAug,
                ctx.ToArray());
            byte[] shared = HkdfUtilities.DeriveKey(kPrime, salt: ReadOnlySpan<byte>.Empty, contextInfo: sharedInfo, length: kPrime.Length).ToArray();
            CryptographicOperations.ZeroMemory(kPrime);

            // Ciphertext = MAC tag || ephemeral public key.
            byte[] ciphertext = new byte[tag.Length + ephPk.Length];
            Span<byte> ciphertextSpan = ciphertext;
            tag.CopyTo(ciphertextSpan);
            ephPk.CopyTo(ciphertextSpan[tag.Length..]);

            return (shared, ciphertext);
        }

        private static (byte[] pk, BigInteger sk) KemDeriveKeypair(ReadOnlySpan<byte> ikm)
        {
            byte[] dst = Concat(
                Encoding.ASCII.GetBytes("ARKG-KEM-ECDH-KG.ARKG-ECDH."),
                Encoding.ASCII.GetBytes(DstExt));
            BigInteger sk = HashToScalar(ikm, dst);
            byte[] pk = ScalarMulGenerator(sk);
            return (pk, sk);
        }

        private static byte[] ScalarMulGenerator(BigInteger sk)
        {
            byte[] skBytes = ScalarToBytes(sk);
            try
            {
                using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(
                    ECCurve.NamedCurves.nistP256.ToSslCurveId());
                using SafeEcPoint pkPoint = NativeMethods.EcPointNew(group);
                using SafeBigNum skBn = NativeMethods.BnBinaryToBigNum(skBytes);

                int rc = NativeMethods.EcPointMul(
                    group,
                    pkPoint,
                    skBn.DangerousGetHandle(),
                    IntPtr.Zero,
                    IntPtr.Zero);
                if (rc != 1)
                {
                    throw new CryptographicException("EC_POINT_mul failed in ScalarMulGenerator.");
                }

                return PointToSec(group, pkPoint);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(skBytes);
            }
        }

        // ---------------------------------------------------------------------
        // RFC 9380 hash-to-curve helpers (scalar variant only)
        // ---------------------------------------------------------------------

        // RFC 9380 §5.4 hash_to_field (scalar variant)
        private static BigInteger HashToScalar(ReadOnlySpan<byte> msg, ReadOnlySpan<byte> dst)
        {
            // P256_L = 48 = ceil((ceil(log2(p)) + k) / 8) with k=128.
            const int L = 48;
            byte[] uniform = ExpandMessageXmd(msg, dst, L);

            // Wide reduction: split into high(16) || low(32), each interpreted big-endian,
            // then result = high * (2^256 mod N) + low (mod N).
            BigInteger high = BytesToBigIntBE(uniform.AsSpan(0, 16));
            BigInteger low = BytesToBigIntBE(uniform.AsSpan(16, 32));
            return Mod((high * TwoPow256ModN) + low, N);
        }

        private static byte[] ExpandMessageXmd(ReadOnlySpan<byte> msg, ReadOnlySpan<byte> dst, int lenInBytes)
        {
            const int BInBytes = 32;
            const int SInBytes = 64;

            int ell = (lenInBytes + BInBytes - 1) / BInBytes;
            if (ell > 255 || lenInBytes > 65535 || dst.Length > 255)
            {
                throw new ArgumentException("expand_message_xmd parameter out of range.");
            }

            byte[] dstPrime = new byte[dst.Length + 1];
            dst.CopyTo(dstPrime);
            dstPrime[dst.Length] = (byte)dst.Length;

            byte[] zPad = new byte[SInBytes];
            byte[] lIBStr = [(byte)((lenInBytes >> 8) & 0xFF), (byte)(lenInBytes & 0xFF)];

            byte[] msgPrime = Concat(zPad, msg.ToArray(), lIBStr, [0x00], dstPrime);

            byte[][] bVals = new byte[ell + 1][];
            using (SHA256 sha = SHA256.Create())
            {
                bVals[0] = sha.ComputeHash(msgPrime);
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] input = Concat(bVals[0], [0x01], dstPrime);
                bVals[1] = sha.ComputeHash(input);
            }

            Span<byte> xored = stackalloc byte[BInBytes];
            for (int i = 2; i <= ell; i++)
            {
                for (int j = 0; j < BInBytes; j++)
                {
                    xored[j] = (byte)(bVals[0][j] ^ bVals[i - 1][j]);
                }

                using SHA256 sha = SHA256.Create();
                byte[] input = Concat(xored.ToArray(), [(byte)i], dstPrime);
                bVals[i] = sha.ComputeHash(input);
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

        // ---------------------------------------------------------------------
        // Conversion helpers
        // ---------------------------------------------------------------------

        private static byte[] ScalarToBytes(BigInteger scalar)
        {
            // BigInteger.ToByteArray is little-endian and includes a sign byte
            // when the high bit would otherwise read as negative — strip it,
            // then left-pad to 32 bytes big-endian.
            byte[] le = scalar.ToByteArray();
            int len = le.Length;
            if (len > 1 && le[len - 1] == 0)
            {
                len--;
            }

            byte[] be = new byte[P256CoordinateLength];
            int copy = Math.Min(len, P256CoordinateLength);
            for (int i = 0; i < copy; i++)
            {
                be[P256CoordinateLength - 1 - i] = le[i];
            }

            return be;
        }

        private static BigInteger BytesToBigIntBE(ReadOnlySpan<byte> bytes)
        {
            byte[] padded = new byte[bytes.Length + 1];
            for (int i = 0; i < bytes.Length; i++)
            {
                padded[bytes.Length - 1 - i] = bytes[i];
            }

            // padded[bytes.Length] = 0 by default — explicit positive sign.
            return new BigInteger(padded);
        }

        private static BigInteger Mod(BigInteger value, BigInteger modulus)
        {
            BigInteger r = value % modulus;
            if (r.Sign < 0)
            {
                r += modulus;
            }

            return r;
        }

        private static SafeEcPoint SecToPoint(SafeEcGroup group, ReadOnlySpan<byte> sec1)
        {
            byte[] x = sec1.Slice(1, P256CoordinateLength).ToArray();
            byte[] y = sec1.Slice(1 + P256CoordinateLength, P256CoordinateLength).ToArray();

            using SafeBigNum xBn = NativeMethods.BnBinaryToBigNum(x);
            using SafeBigNum yBn = NativeMethods.BnBinaryToBigNum(y);
            SafeEcPoint point = NativeMethods.EcPointNew(group);
            int rc = NativeMethods.EcPointSetAffineCoordinates(group, point, xBn, yBn);
            if (rc != 1)
            {
                point.Dispose();
                throw new CryptographicException("EC_POINT_set_affine_coordinates failed.");
            }

            return point;
        }

        private static byte[] PointToSec(SafeEcGroup group, SafeEcPoint point)
        {
            using SafeBigNum xBn = NativeMethods.BnNew();
            using SafeBigNum yBn = NativeMethods.BnNew();
            int rc = NativeMethods.EcPointGetAffineCoordinates(group, point, xBn, yBn);
            if (rc != 1)
            {
                throw new CryptographicException("EC_POINT_get_affine_coordinates failed.");
            }

            byte[] xBytes = new byte[P256CoordinateLength];
            byte[] yBytes = new byte[P256CoordinateLength];
            _ = NativeMethods.BnBigNumToBinaryWithPadding(xBn, xBytes);
            _ = NativeMethods.BnBigNumToBinaryWithPadding(yBn, yBytes);

            byte[] sec1 = new byte[Sec1UncompressedLength];
            Span<byte> sec1Span = sec1;
            sec1[0] = Sec1UncompressedTag;
            xBytes.CopyTo(sec1Span[1..]);
            yBytes.CopyTo(sec1Span[(1 + P256CoordinateLength)..]);
            return sec1;
        }

        private static byte[] Concat(params byte[][] parts)
        {
            int total = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                total += parts[i].Length;
            }

            byte[] result = new byte[total];
            int offset = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                Buffer.BlockCopy(parts[i], 0, result, offset, parts[i].Length);
                offset += parts[i].Length;
            }

            return result;
        }
    }
}
