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

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// Defines cryptographic primitives required for ARKG-P256 operations.
    /// </summary>
    /// <remarks>
    /// WARNING: This code is for testing purposes only and is not intended to be a
    /// secure or complete implementation of ARKG.
    /// </remarks>
    internal interface IArkgPrimitives
    {
        /// <summary>
        /// Verifies that an elliptic curve point lies on the P-256 curve.
        /// </summary>
        /// <param name="point">The point to verify, in uncompressed SEC1 format.</param>
        /// <returns><c>true</c> if the point is on the curve; otherwise, <c>false</c>.</returns>
        bool IsPointOnCurve(ReadOnlySpan<byte> point);

        /// <summary>
        /// Computes an ECDH shared secret using a private scalar and public point.
        /// </summary>
        /// <param name="privateScalar">The private scalar value, encoded as unsigned big-endian bytes.</param>
        /// <param name="publicPoint">The public point in uncompressed SEC1 format.</param>
        /// <returns>The computed shared secret.</returns>
        byte[] ComputeEcdhSharedSecret(ReadOnlySpan<byte> privateScalar, ReadOnlySpan<byte> publicPoint);

        /// <summary>
        /// Derives a public key and ARKG key handle from ARKG-P256 seed public keys.
        /// </summary>
        /// <remarks>
        /// WARNING: This code is for testing purposes only and is not intended to be a
        /// secure or complete implementation of ARKG.
        /// </remarks>
        /// <param name="blindingPublicKey">The blinding public key in uncompressed SEC1 format.</param>
        /// <param name="kemPublicKey">The KEM public key in uncompressed SEC1 format.</param>
        /// <param name="inputKeyingMaterial">Input keying material for key derivation.</param>
        /// <param name="context">Context bytes for key derivation.</param>
        /// <returns>
        /// A tuple containing the derived public key in uncompressed SEC1 format
        /// and the ARKG key handle.
        /// </returns>
        (byte[] derivedPublicKey, byte[] arkgKeyHandle) DerivePublicKey(
            ReadOnlySpan<byte> blindingPublicKey,
            ReadOnlySpan<byte> kemPublicKey,
            ReadOnlySpan<byte> inputKeyingMaterial,
            ReadOnlySpan<byte> context);
    }
}
