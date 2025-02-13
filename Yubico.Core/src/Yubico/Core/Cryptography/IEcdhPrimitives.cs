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

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// An interface exposing ECDH primitive operations.
    /// </summary>
    public interface IEcdhPrimitives
    {
        /// <summary>
        /// Generates a elliptic curve public/private keypair that can be used for ECDH operations.
        /// </summary>
        /// <param name="curve">
        /// The elliptic curve that the keypair should be generated on.
        /// </param>
        /// <returns>
        /// An `ECParameters` structure representing the `Curve`, the public point `Q`, and the private key `D`.
        /// </returns>
        /// <remarks>
        /// <para>
        /// As of SDK 1.5.0, only the named curves `ECCurve.NamedCurves.nistP256`, `ECCurve.NamedCurves.nistP384`,
        /// and `ECCurve.NamedCurves.nistP521` are required to be supported.
        /// </para>
        /// <para>
        /// Callers of this function should take care when handling this structure. Since it will contain the private
        /// key value in `D`, it is recommended that `CryptographicOperations.ZeroMemory` be called as soon as the
        /// key is no longer needed.
        /// </para>
        /// </remarks>
        public ECParameters GenerateKeyPair(ECCurve curve);

        /// <summary>
        /// Computes a shared secret by producing the ECDH shared point without running it through a KDF. Only the
        /// X-coordinate is returned.
        /// </summary>
        /// <param name="publicKey">
        /// The other party's public key.
        /// </param>
        /// <param name="privateValue">
        /// Your private key value that was generated based on the same curve as the other party's public key.
        /// </param>
        /// <returns>
        /// The X-coordinate of the computed shared point.
        /// </returns>
        /// <remarks>
        /// This function calculates the shared point - the result of the scalar-multiplication of the peer's
        /// <paramref name="publicKey"/> and the local <paramref name="privateValue"/>. Only the X coordinate
        /// of the shared point is returned.
        /// </remarks>
        public byte[] ComputeSharedSecret(ECParameters publicKey, ReadOnlySpan<byte> privateValue);
    }
}
