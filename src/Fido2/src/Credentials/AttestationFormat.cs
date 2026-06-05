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

namespace Yubico.YubiKit.Fido2.Credentials;

/// <summary>
/// Attestation statement format identifier.
/// </summary>
/// <remarks>
/// See: https://www.w3.org/TR/webauthn-2/#sctn-attstn-fmt-ids
/// </remarks>
public readonly record struct AttestationFormat(string Value)
{
    /// <summary>
    /// Packed attestation format.
    /// </summary>
    public static readonly AttestationFormat Packed = new("packed");

    /// <summary>
    /// FIDO U2F attestation format.
    /// </summary>
    public static readonly AttestationFormat FidoU2F = new("fido-u2f");

    /// <summary>
    /// Apple anonymous attestation format.
    /// </summary>
    public static readonly AttestationFormat Apple = new("apple");

    /// <summary>
    /// No attestation (self-attestation).
    /// </summary>
    public static readonly AttestationFormat None = new("none");

    /// <summary>
    /// Android Key attestation format.
    /// </summary>
    public static readonly AttestationFormat AndroidKey = new("android-key");

    /// <summary>
    /// Android SafetyNet attestation format.
    /// </summary>
    public static readonly AttestationFormat AndroidSafetynet = new("android-safetynet");

    /// <summary>
    /// TPM attestation format.
    /// </summary>
    public static readonly AttestationFormat Tpm = new("tpm");

    /// <summary>
    /// Creates an attestation format with a custom identifier.
    /// </summary>
    public static AttestationFormat Other(string value) => new(value);

    public override string ToString() => Value;
}
