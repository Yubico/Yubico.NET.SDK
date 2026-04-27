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
using System.Security;
using System.Security.Cryptography;
using Yubico.PlatformInterop;

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// OpenSSL-backed implementation of <see cref="IArkgPrimitives"/> for ARKG-P256.
    /// </summary>
    /// <remarks>
    /// Provides the security-critical primitives required by the ARKG-P256
    /// algorithm: on-curve point validation, ECDH shared-secret computation,
    /// and full ARKG public-key derivation. The whole algorithm is orchestrated
    /// by <c>Yubico.YubiKey.Fido2.Arkg.ArkgP256</c> and routed through this
    /// type via <c>Yubico.YubiKey.Cryptography.CryptographyProviders</c>.
    /// </remarks>
    internal sealed class ArkgPrimitivesOpenSsl : IArkgPrimitives
    {
        private const int P256CoordinateLength = 32;
        private const int Sec1UncompressedLength = 1 + (2 * P256CoordinateLength);
        private const byte Sec1UncompressedTag = 0x04;

        /// <summary>
        /// Optional algorithm hook used by <c>ArkgP256</c> after wiring in
        /// Phase 4. Keeping it as a delegate avoids a project-direction violation
        /// (Yubico.Core cannot reference Yubico.YubiKey).
        /// </summary>
        internal static Func<byte[], byte[], byte[], byte[], (byte[] derivedPk, byte[] arkgKeyHandle)>? DeriveImpl { get; set; }

        /// <inheritdoc />
        public bool IsPointOnCurve(byte[] point)
        {
            if (point is null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            if (point.Length != Sec1UncompressedLength || point[0] != Sec1UncompressedTag)
            {
                return false;
            }

            byte[] xBytes = new byte[P256CoordinateLength];
            byte[] yBytes = new byte[P256CoordinateLength];
            Buffer.BlockCopy(point, 1, xBytes, 0, P256CoordinateLength);
            Buffer.BlockCopy(point, 1 + P256CoordinateLength, yBytes, 0, P256CoordinateLength);

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
        public byte[] ComputeEcdhSharedSecret(byte[] privateScalar, byte[] publicPoint)
        {
            if (privateScalar is null)
            {
                throw new ArgumentNullException(nameof(privateScalar));
            }

            if (publicPoint is null)
            {
                throw new ArgumentNullException(nameof(publicPoint));
            }

            if (publicPoint.Length != Sec1UncompressedLength || publicPoint[0] != Sec1UncompressedTag)
            {
                throw new ArgumentException(
                    "Public point must be a 65-byte SEC1 uncompressed P-256 point.",
                    nameof(publicPoint));
            }

            byte[] x = new byte[P256CoordinateLength];
            byte[] y = new byte[P256CoordinateLength];
            Buffer.BlockCopy(publicPoint, 1, x, 0, P256CoordinateLength);
            Buffer.BlockCopy(publicPoint, 1 + P256CoordinateLength, y, 0, P256CoordinateLength);

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

            return EcdhPrimitives.Create().ComputeSharedSecret(publicKey, privateScalar);
        }

        /// <inheritdoc />
        public (byte[] derivedPk, byte[] arkgKeyHandle) Derive(
            byte[] pkBl,
            byte[] pkKem,
            byte[] ikm,
            byte[] ctx)
        {
            if (DeriveImpl is null)
            {
                throw new NotImplementedException(
                    "ARKG derivation requires Yubico.YubiKey.Fido2.Arkg.ArkgP256 to register an implementation.");
            }

            return DeriveImpl(pkBl, pkKem, ikm, ctx);
        }
    }
}
