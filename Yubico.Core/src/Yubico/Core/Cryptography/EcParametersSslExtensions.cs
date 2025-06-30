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
using System.Security.Cryptography;
using Yubico.PlatformInterop;

namespace Yubico.Core.Cryptography
{
    internal static class OpenSslExtensions
    {
        private const int NistP256BitLength = 256;
        private const int NistP384BitLength = 384;
        private const int NistP521BitLength = 521;

        /// <summary>
        /// Converts an ECParameters structure into the OpenSSL data types for the public key: EC_GROUP and EC_POINT
        /// </summary>
        /// <param name="parameters">
        /// The .NET representation of an elliptic curve and point.
        /// </param>
        /// <returns>
        /// A tuple of the OpenSSL group and point. Both are needed to represent the public key.
        /// </returns>
        public static (SafeEcGroup group, SafeEcPoint point) ToSslPublicKey(this ECParameters parameters)
        {
            SafeEcGroup group = NativeMethods.EcGroupNewByCurveName(parameters.Curve.ToSslCurveId());
            SafeEcPoint point = NativeMethods.EcPointNew(group);

            using SafeBigNum bnX = NativeMethods.BnBinaryToBigNum(parameters.Q.X);
            using SafeBigNum bnY = NativeMethods.BnBinaryToBigNum(parameters.Q.Y);
            _ = NativeMethods.EcPointSetAffineCoordinates(group, point, bnX, bnY);

            return (group, point);
        }

        /// <summary>
        /// Converts a .NET named curve structure into its corresponding OpenSSL curve identifier.
        /// </summary>
        /// <param name="curve">
        /// The .NET representation of a named elliptic curve.
        /// </param>
        /// <returns>
        /// The OpenSSL curve ID (sometimes referred to as "NID")
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// This function only supports the NIST P256, P384, and P512 curves as of version 1.5.0.
        /// </exception>
        // Curve IDs from include/openssl/obj_mac.h
        public static int ToSslCurveId(this ECCurve curve) =>
            curve switch
            {
                _ when curve.HasSameOid(ECCurve.NamedCurves.nistP256) => 415, // Exists as X9.64-prime256v1 in OpenSSL
                _ when curve.HasSameOid(ECCurve.NamedCurves.nistP384) => 715,
                _ when curve.HasSameOid(ECCurve.NamedCurves.nistP521) => 716,
                _ => throw new NotSupportedException("Specified elliptic curve is not supported.")
            };

        /// <summary>
        /// Return the bit length of the curve. This will be the bit length of
        /// the private value and each coordinate of a point in the curve.
        /// </summary>
        /// <param name="curve">
        /// The .NET representation of a named elliptic curve.
        /// </param>
        /// <returns>
        /// The curve's bit length.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// This function only supports the NIST P256, P384, and P512 curves as of version 1.5.0.
        /// </exception>
        public static int BitLength(this ECCurve curve) =>
            curve switch
            {
                _ when curve.HasSameOid(ECCurve.NamedCurves.nistP256) => NistP256BitLength,
                _ when curve.HasSameOid(ECCurve.NamedCurves.nistP384) => NistP384BitLength,
                _ when curve.HasSameOid(ECCurve.NamedCurves.nistP521) => NistP521BitLength,
                _ => throw new NotSupportedException("Specified elliptic curve is not supported.")
            };

        private static bool HasSameOid(this ECCurve curve, ECCurve named) => curve.Oid.Value == named.Oid.Value;
    }
}
