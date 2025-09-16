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

namespace Yubico.YubiKey.Fido2;

/// <summary>
/// WebAuthn Attestation Statement Format Identifiers.
/// <br/>
/// See the FIDO2 standard for more information on these formats.
/// <br/>
/// https://www.iana.org/assignments/webauthn/webauthn.xhtml
/// </summary>
public static class AttestationFormats
{
    /// <summary>
    /// The "packed" attestation statement format is a WebAuthn-optimized format for attestation. It uses a very compact but still extensible encoding method. This format is implementable by authenticators with limited resources (e.g., secure elements).
    /// </summary>
    public const string Packed = "packed";
    /// <summary>
    /// The TPM attestation statement format returns an attestation statement in the same format as the packed attestation statement format, although the rawData and signature fields are computed differently.	
    /// </summary>
    public const string Tpm = "tpm";
    /// <summary>
    /// Platform authenticators on versions "N", and later, may provide this proprietary "hardware attestation" statement.	
    /// </summary>
    public const string AndroidKey = "android";
    /// <summary>
    /// Android-based platform authenticators MAY produce an attestation statement based on the Android SafetyNet API.	
    /// </summary>
    public const string AndroidSafetyNet = "android-safetynet";
    /// <summary>
    /// Used with FIDO U2F authenticators	
    /// </summary>
    public const string FidoU2f = "fido-u2f";
    /// <summary>
    /// Used with Apple devices' platform authenticators	
    /// </summary>
    public const string Apple = "apple";
    /// <summary>
    /// Used to replace any authenticator-provided attestation statement when a WebAuthn Relying Party indicates it does not wish to receive attestation information.	
    /// </summary>
    public const string None = "none";
}
