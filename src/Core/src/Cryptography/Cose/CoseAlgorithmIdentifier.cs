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

namespace Yubico.YubiKit.Core.Cryptography.Cose;

/// <summary>
/// Represents a COSE algorithm identifier.
/// <remarks>
/// This enumeration is based on the IANA COSE Algorithms registry.
/// Registered identity does not establish a valid key/algorithm pairing or provider/applet support.
/// Registry recommendation status is separate from the identifiers required by a protocol profile.
/// <para>
/// https://www.iana.org/assignments/cose/cose.xhtml#algorithms
/// </para>
/// </remarks>
/// </summary>
public enum CoseAlgorithmIdentifier
{
    /// <summary>
    /// Local sentinel for no selected algorithm. Zero is reserved in the IANA registry.
    /// </summary>
    None = 0,

    /// <summary>
    /// ECDSA with SHA-256. Profiles such as WebAuthn constrain the curve to P-256.
    /// </summary>
    Es256 = -7,

    /// <summary>
    /// ECDH ephemeral-static with HKDF using SHA-256 (ECDH-ES + HKDF-256).
    /// </summary>
    EcdhEsHkdf256 = -25,

    /// <summary>
    /// ECDSA with SHA-384. Profiles may constrain the curve to P-384.
    /// </summary>
    Es384 = -35,

    /// <summary>
    /// ECDSA with SHA-512. Profiles may constrain the curve to P-521.
    /// </summary>
    Es512 = -36,

    /// <summary>
    /// Pure EdDSA, not ECDSA. The curve is separate metadata (for example Ed25519 or Ed448).
    /// </summary>
    EdDsa = -8,

    /// <summary>
    /// RSASSA-PKCS1-v1_5 with SHA-256
    /// </summary>
    Rs256 = -257,
}