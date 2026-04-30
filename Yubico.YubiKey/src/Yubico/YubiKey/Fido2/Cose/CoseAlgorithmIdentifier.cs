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

namespace Yubico.YubiKey.Fido2.Cose
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
        /// ECDSA with SHA-256 using NIST P-256 (ESP256). Identifies the resulting
        /// signature algorithm produced by previewSign — the YubiKey emits a
        /// standard ECDSA-P256 signature.
        /// </summary>
        /// <remarks>
        /// Do NOT pass this value as the <c>alg</c> field of the previewSign
        /// COSE_Sign_Args map; that field requests an ARKG-derived signing
        /// operation and must be <see cref="ArkgP256Esp256"/> (-65539).
        /// </remarks>
        Esp256 = -9,

        /// <summary>
        /// RSASSA-PKCS1-v1_5 with SHA-256
        /// Currently, not supported by any YubiKey
        /// </summary>
        RS256 = -257,

        /// <summary>
        /// ESP256-split with ARKG-P256 (-65539). Used in two places for the
        /// previewSign extension: (1) the algorithms array passed to
        /// <see cref="MakeCredentialParameters.AddPreviewSignGenerateKeyExtension"/>
        /// to request ARKG-P256 key generation, and (2) the <c>alg</c> field of
        /// the COSE_Sign_Args map sent during a sign-by-credential request to
        /// identify the operation as ARKG-derived. The resulting signature
        /// itself is plain ECDSA-P256 (<see cref="Esp256"/>).
        /// </summary>
        ArkgP256Esp256 = -65539,
    }
}
