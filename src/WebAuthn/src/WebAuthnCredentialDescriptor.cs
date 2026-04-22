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

namespace Yubico.YubiKit.WebAuthn;

/// <summary>
/// Public key credential descriptor identifying a specific credential.
/// </summary>
/// <param name="Id">Credential ID (opaque byte sequence).</param>
/// <param name="Transports">Optional transports hint.</param>
/// <remarks>
/// Used in allowList and excludeList parameters to identify credentials for authentication
/// or exclusion during registration. See
/// <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialdescriptor">
/// WebAuthn PublicKeyCredentialDescriptor</see>.
/// </remarks>
public sealed record class WebAuthnCredentialDescriptor(
    ReadOnlyMemory<byte> Id,
    IReadOnlyList<WebAuthnTransport>? Transports = null)
{
    /// <summary>
    /// Credential type (always "public-key" for FIDO2).
    /// </summary>
    public string Type { get; init; } = "public-key";
}
