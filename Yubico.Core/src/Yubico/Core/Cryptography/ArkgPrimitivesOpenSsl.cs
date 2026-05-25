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
    /// and ARKG-P256 derivation. Point math
    /// goes through Yubico.NativeShims (OpenSSL); scalar reduction uses
    /// <see cref="BigInteger"/>.
    /// </remarks>
    internal sealed class ArkgPrimitivesOpenSsl : IArkgPrimitives
    {
        private const int P256CoordinateLength = 32;
        private const int Sec1UncompressedLength = 1 + (2 * P256CoordinateLength);
        private const byte Sec1UncompressedTag = 0x04;

        // DST_ext for ARKG-P256, draft-bradleylundberg-cfrg-arkg-10 section 4.1:
        // https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html#name-arkg-p256
        private const string DstExt = "ARKG-P256";

        // P-256 group order N (SEC 2 v2, section 2.4.2). Used for scalar reduction mod N.
        private static readonly BigInteger N = BigInteger.Parse(
            "00FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551",
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);

        private enum PointValidationResult
        {
            Valid,
            Malformed,
            OffCurve,
        }

        /// <inheritdoc />
        public bool IsPointOnCurve(ReadOnlySpan<byte> point)
            => ValidatePoint(point) == PointValidationResult.Valid;

        /// <inheritdoc />
        public byte[] ComputeEcdhSharedSecret(ReadOnlySpan<byte> privateScalar, ReadOnlySpan<byte> publicPoint)
        {
            Guard.HasSizeGreaterThan(privateScalar, 0, nameof(privateScalar));
            Guard.HasSizeGreaterThan(publicPoint, 0, nameof(publicPoint));

            switch (ValidatePoint(publicPoint))
            {
                case PointValidationResult.Valid:
                    break;

                case PointValidationResult.Malformed:
                    throw new ArgumentException(
                        "Public point must be a 65-byte SEC1 uncompressed P-256 point.",
                        nameof(publicPoint));

                default:
                    throw new SecurityException("Public point is not on the P-256 curve.");
            }

            byte[] x = publicPoint.Slice(1, P256CoordinateLength).ToArray();
            byte[] y = publicPoint.Slice(1 + P256CoordinateLength, P256CoordinateLength).ToArray();

            var publicKey = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = x, Y = y }
            };

            return EcdhPrimitives.Create().ComputeSharedSecret(publicKey, privateScalar);
        }

        private static PointValidationResult ValidatePoint(ReadOnlySpan<byte> point)
        {
            if (point.Length != Sec1UncompressedLength || point[0] != Sec1UncompressedTag)
            {
                return PointValidationResult.Malformed;
            }

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
                return PointValidationResult.OffCurve;
            }

            int onCurve = NativeMethods.EcPointIsOnCurve(group, sslPoint);
            return onCurve == 1
                ? PointValidationResult.Valid
                : PointValidationResult.OffCurve;
        }

        /// <inheritdoc />
        public (byte[] derivedPublicKey, byte[] arkgKeyHandle) DerivePublicKey(
            ReadOnlySpan<byte> blindingPublicKey,
            ReadOnlySpan<byte> kemPublicKey,
            ReadOnlySpan<byte> inputKeyingMaterial,
            ReadOnlySpan<byte> context)
        {
            Guard.HasSizeGreaterThan(blindingPublicKey, 0, nameof(blindingPublicKey));
            Guard.HasSizeGreaterThan(kemPublicKey, 0, nameof(kemPublicKey));
            Guard.HasSizeGreaterThan(inputKeyingMaterial, 0, nameof(inputKeyingMaterial));
            Guard.HasSizeLessThanOrEqualTo(context, 64, nameof(context));

            if (!IsPointOnCurve(blindingPublicKey))
            {
                throw new ArgumentException("blindingPublicKey is not on the P-256 curve.", nameof(blindingPublicKey));
            }

            if (!IsPointOnCurve(kemPublicKey))
            {
                throw new ArgumentException("kemPublicKey is not on the P-256 curve.", nameof(kemPublicKey));
            }

            byte[] contextPrime = new byte[1 + context.Length];
            contextPrime[0] = (byte)context.Length;
            context.CopyTo(contextPrime.AsSpan(1));

            byte[] contextKem = Concat(Encoding.ASCII.GetBytes("ARKG-Derive-Key-KEM."), contextPrime);
            byte[] contextBl = Concat(Encoding.ASCII.GetBytes("ARKG-Derive-Key-BL."), contextPrime);

            (byte[] ikmTau, byte[] cipher) = HmacKemEncaps(kemPublicKey, inputKeyingMaterial, contextKem);
            BigInteger tau;
            try
            {
                tau = BlPrf(ikmTau, contextBl);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ikmTau);
            }

            byte[] derivedPublicKey = BlBlindPublicKey(blindingPublicKey, tau);
            return (derivedPublicKey, cipher);
        }

        // ---------------------------------------------------------------------
        // ARKG-BL: blinding-key arithmetic
        // ---------------------------------------------------------------------

        private static BigInteger BlPrf(ReadOnlySpan<byte> inputKeyingMaterialTau, ReadOnlySpan<byte> context)
        {
            byte[] dst = Concat(
                Encoding.ASCII.GetBytes("ARKG-BL-EC."),
                Encoding.ASCII.GetBytes(DstExt),
                context.ToArray());
            return HashToScalar(inputKeyingMaterialTau, dst);
        }

        private static byte[] BlBlindPublicKey(ReadOnlySpan<byte> blindingPublicKey, BigInteger tau)
        {
            byte[] tauBytes = ScalarToP256FixedWidthBytes(tau);
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(
                ECCurve.NamedCurves.nistP256.ToSslCurveId());
            using SafeEcPoint blindingPublicKeyPoint = Sec1ToPoint(group, blindingPublicKey);
            using SafeEcPoint result = NativeMethods.EcPointNew(group);
            using SafeBigNum tauBn = NativeMethods.BnBinaryToBigNum(tauBytes);
            using SafeBigNum oneBn = NativeMethods.BnBinaryToBigNum([0x01]);

            // r = tau*G + 1*blindingPublicKey.
            int rc = NativeMethods.EcPointMul(
                group,
                result,
                tauBn.DangerousGetHandle(),
                blindingPublicKeyPoint.DangerousGetHandle(),
                oneBn.DangerousGetHandle());
            if (rc != 1)
            {
                throw new CryptographicException("EC_POINT_mul failed in BlBlindPublicKey.");
            }

            return PointToSec1(group, result);
        }

        // ---------------------------------------------------------------------
        // ECDH with HMAC wrapper
        // ---------------------------------------------------------------------

        private (byte[] shared, byte[] ciphertext) HmacKemEncaps(
            ReadOnlySpan<byte> kemPublicKey,
            ReadOnlySpan<byte> inputKeyingMaterial,
            ReadOnlySpan<byte> context)
        {
            byte[] dstAug = Encoding.ASCII.GetBytes("ARKG-ECDH.ARKG-P256");

            (byte[] ephemeralPublicKey, BigInteger ephemeralSecretScalar) = KemDeriveKeypair(inputKeyingMaterial);

            byte[] ephemeralSecretScalarBytes = ScalarToP256FixedWidthBytes(ephemeralSecretScalar);
            byte[] kPrime = ComputeEcdhSharedSecret(ephemeralSecretScalarBytes, kemPublicKey);

            byte[] macInfo = Concat(
                Encoding.ASCII.GetBytes("ARKG-KEM-HMAC-mac."),
                dstAug,
                context.ToArray());
            byte[] mk = HkdfUtilities.DeriveKey(kPrime, salt: ReadOnlySpan<byte>.Empty, contextInfo: macInfo, length: 32).ToArray();

            byte[] tag;
            try
            {
                using HMACSHA256 hmac = new HMACSHA256(mk);
                byte[] full = hmac.ComputeHash(ephemeralPublicKey);
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
                context.ToArray());
            byte[] shared = HkdfUtilities.DeriveKey(kPrime, salt: ReadOnlySpan<byte>.Empty, contextInfo: sharedInfo, length: kPrime.Length).ToArray();
            CryptographicOperations.ZeroMemory(kPrime);

            // Ciphertext = MAC tag || ephemeral public key.
            byte[] ciphertext = new byte[tag.Length + ephemeralPublicKey.Length];
            Span<byte> ciphertextSpan = ciphertext;
            tag.CopyTo(ciphertextSpan);
            ephemeralPublicKey.CopyTo(ciphertextSpan[tag.Length..]);

            return (shared, ciphertext);
        }

        private static (byte[] publicKey, BigInteger secretScalar) KemDeriveKeypair(ReadOnlySpan<byte> inputKeyingMaterial)
        {
            byte[] dst = Concat(
                Encoding.ASCII.GetBytes("ARKG-KEM-ECDH-KG.ARKG-ECDH."),
                Encoding.ASCII.GetBytes(DstExt));
            BigInteger secretScalar = HashToScalar(inputKeyingMaterial, dst);
            byte[] publicKey = ScalarMulGenerator(secretScalar);
            return (publicKey, secretScalar);
        }

        private static byte[] ScalarMulGenerator(BigInteger scalar)
        {
            byte[] scalarBytes = ScalarToP256FixedWidthBytes(scalar);
            using SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(
                ECCurve.NamedCurves.nistP256.ToSslCurveId());
            using SafeEcPoint publicKeyPoint = NativeMethods.EcPointNew(group);
            using SafeBigNum scalarBn = NativeMethods.BnBinaryToBigNum(scalarBytes);

            int rc = NativeMethods.EcPointMul(
                group,
                publicKeyPoint,
                scalarBn.DangerousGetHandle(),
                IntPtr.Zero,
                IntPtr.Zero);
            if (rc != 1)
            {
                throw new CryptographicException("EC_POINT_mul failed in ScalarMulGenerator.");
            }

            return PointToSec1(group, publicKeyPoint);
        }

        // ---------------------------------------------------------------------
        // Hash-to-scalar helpers
        // ---------------------------------------------------------------------

        private static BigInteger HashToScalar(ReadOnlySpan<byte> msg, ReadOnlySpan<byte> dst)
        {
            const int L = 48;
            byte[] uniform = ExpandMessageXmdSha256(msg, dst, L);

            // Follows RFC 9380 section 5.2 hash_to_field reduction with the
            // P256_XMD:SHA-256_SSWU_RO_ section 8.2 parameters (m=1, L=48,
            // expand_message_xmd/SHA-256), except ARKG-P256 hashes to a
            // scalar by reducing modulo the P-256 group order N rather than
            // the coordinate-field characteristic p.
            return Os2Ip(uniform) % N;
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

        // ---------------------------------------------------------------------
        // Conversion helpers
        // ---------------------------------------------------------------------

        private static byte[] ScalarToP256FixedWidthBytes(BigInteger scalar)
        {
            // Local fixed-width P-256 scalar encoding: BigInteger is signed
            // little-endian, while OpenSSL BN input expects unsigned big-endian.
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

        // RFC 8017 OS2IP: interpret an octet string as a non-negative integer.
        private static BigInteger Os2Ip(ReadOnlySpan<byte> bytes)
        {
            byte[] padded = new byte[bytes.Length + 1];
            for (int i = 0; i < bytes.Length; i++)
            {
                padded[bytes.Length - 1 - i] = bytes[i];
            }

            // padded[bytes.Length] = 0 by default — explicit positive sign.
            return new BigInteger(padded);
        }

        private static byte[] Sha256(byte[] input)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(input);
        }

        private static SafeEcPoint Sec1ToPoint(SafeEcGroup group, ReadOnlySpan<byte> sec1)
        {
            if (sec1.Length != Sec1UncompressedLength || sec1[0] != Sec1UncompressedTag)
            {
                throw new CryptographicException(
                    "SEC1 point must be a 65-byte uncompressed P-256 point.");
            }

            byte[] x = sec1.Slice(1, P256CoordinateLength).ToArray();
            byte[] y = sec1.Slice(1 + P256CoordinateLength, P256CoordinateLength).ToArray();

            using SafeBigNum xBn = NativeMethods.BnBinaryToBigNum(x);
            using SafeBigNum yBn = NativeMethods.BnBinaryToBigNum(y);
            SafeEcPoint point = NativeMethods.EcPointNew(group);
            int rc = NativeMethods.EcPointSetAffineCoordinates(group, point, xBn, yBn);
            if (rc != 1)
            {
                point.Dispose();
                throw new CryptographicException("SEC1 point is not a valid P-256 point.");
            }

            return point;
        }

        private static byte[] PointToSec1(SafeEcGroup group, SafeEcPoint point)
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
