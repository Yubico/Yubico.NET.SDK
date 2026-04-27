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

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// Defines cryptographic primitives required for ARKG-P256 operations.
    /// </summary>
    /// <remarks>
    /// This interface abstracts platform-specific implementations of
    /// elliptic curve operations needed for Asynchronous Remote Key Generation.
    /// </remarks>
    public interface IArkgPrimitives
    {
        /// <summary>
        /// Verifies that an elliptic curve point lies on the P-256 curve.
        /// </summary>
        /// <param name="point">The point to verify, in uncompressed SEC1 format.</param>
        /// <returns><c>true</c> if the point is on the curve; otherwise, <c>false</c>.</returns>
        bool IsPointOnCurve(byte[] point);

        /// <summary>
        /// Computes an ECDH shared secret using a private scalar and public point.
        /// </summary>
        /// <param name="privateScalar">The private scalar value.</param>
        /// <param name="publicPoint">The public point in uncompressed SEC1 format.</param>
        /// <returns>The computed shared secret.</returns>
        byte[] ComputeEcdhSharedSecret(byte[] privateScalar, byte[] publicPoint);

        /// <summary>
        /// Derives a public key and ARKG key handle using ARKG-P256.
        /// </summary>
        /// <param name="pkBl">The blinding public key.</param>
        /// <param name="pkKem">The KEM public key.</param>
        /// <param name="ikm">Input keying material.</param>
        /// <param name="ctx">Context string.</param>
        /// <returns>A tuple containing the derived public key and ARKG key handle.</returns>
        (byte[] derivedPk, byte[] arkgKeyHandle) Derive(
            byte[] pkBl,
            byte[] pkKem,
            byte[] ikm,
            byte[] ctx);
    }
}
