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

namespace Yubico.YubiKit.Core.Cryptography.Cose
{
    /// <summary>
    /// Represents a COSE algorithm identifier.
    /// <remarks>
    /// This enumeration is based on the IANA COSE Algorithms registry.
    /// <para>
    /// https://www.iana.org/assignments/cose/cose.xhtml#algorithms
    /// </para>
    /// </remarks>
    /// </summary>
    public enum CoseAlgorithmIdentifier
    {
        /// <summary>
        /// No algorithm specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// ECDSA with SHA-256 using the NIST P-256 curve.
        /// </summary>
        ES256 = -7,

        /// <summary>
        /// ECDH with key derivation function HKDF using SHA-256.
        /// </summary>
        ECDHwHKDF256 = -25,

        /// <summary>
        /// ECDSA with SHA-384 using the NIST P-384 curve.
        /// </summary>
        ES384 = -35,

        /// <summary>
        /// ECDSA with SHA-512 using the NIST P-521 curve.
        /// </summary>
        ES512 = -36,

        /// <summary>
        /// ECDSA using the Ed25519 curve and "Pure" EdDSA which uses no digest
        /// algorithm.
        /// </summary>
        EdDSA = -8,

        /// <summary>
        /// RSASSA-PKCS1-v1_5 with SHA-256
        /// Currently, not supported by any YubiKey
        /// </summary>
        RS256 = -257,
    }
}
