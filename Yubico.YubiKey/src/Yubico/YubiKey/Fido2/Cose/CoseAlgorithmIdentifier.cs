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
        /// ECDSA with SHA-256 using NIST P-256, for previewSign extension.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This algorithm identifier (ESP256) is used by the YubiKey hardware when
        /// performing previewSign signing operations. The YubiKey signs with
        /// ESP256, not with ARKG-P256-ESP256.
        /// </para>
        /// <para>
        /// When requesting a previewSign credential via
        /// <see cref="MakeCredentialParameters.AddPreviewSignGenerateKeyExtension"/>,
        /// specify <c>ArkgP256Esp256</c> in the algorithms array. The YubiKey will
        /// generate key material using ARKG-P256, but actual signatures will be
        /// produced using ESP256.
        /// </para>
        /// </remarks>
        Esp256 = -9,

        /// <summary>
        /// RSASSA-PKCS1-v1_5 with SHA-256
        /// Currently, not supported by any YubiKey
        /// </summary>
        RS256 = -257,

        /// <summary>
        /// ARKG-P256 with Esp256 for previewSign extension key generation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This algorithm identifier is used during previewSign credential creation
        /// to request ARKG-P256 key generation. When this algorithm is specified in
        /// <see cref="MakeCredentialParameters.AddPreviewSignGenerateKeyExtension"/>,
        /// the YubiKey generates ARKG-P256 key material.
        /// </para>
        /// <para>
        /// Note: The YubiKey uses <c>Esp256</c> for the actual signing algorithm,
        /// not ARKG-P256-ESP256. This identifier is only for requesting ARKG key
        /// generation during credential creation.
        /// </para>
        /// </remarks>
        ArkgP256Esp256 = -65539,
    }
}
