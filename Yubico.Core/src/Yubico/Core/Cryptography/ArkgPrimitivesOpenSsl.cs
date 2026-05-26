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
using System.Numerics;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Diagnostics;
using Yubico.PlatformInterop;
using static Yubico.Core.Cryptography.ArkgByteUtilities;

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
                Encoding.ASCII.GetBytes(ArkgP256Scalar.DstExt),
                context.ToArray());
            return ArkgP256Scalar.HashToScalar(inputKeyingMaterialTau, dst);
        }

        private static byte[] BlBlindPublicKey(ReadOnlySpan<byte> blindingPublicKey, BigInteger tau)
        {
            byte[] tauBytes = ArkgP256Scalar.ToFixedWidthBytes(tau);
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

            byte[] ephemeralSecretScalarBytes = ArkgP256Scalar.ToFixedWidthBytes(ephemeralSecretScalar);
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
                Encoding.ASCII.GetBytes(ArkgP256Scalar.DstExt));
            BigInteger secretScalar = ArkgP256Scalar.HashToScalar(inputKeyingMaterial, dst);
            byte[] publicKey = ScalarMulGenerator(secretScalar);
            return (publicKey, secretScalar);
        }

        private static byte[] ScalarMulGenerator(BigInteger scalar)
        {
            byte[] scalarBytes = ArkgP256Scalar.ToFixedWidthBytes(scalar);
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
    }
}
