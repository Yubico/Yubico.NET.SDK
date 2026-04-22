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
/// Relying Party entity information for WebAuthn operations.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialrpentity">
/// WebAuthn PublicKeyCredentialRpEntity</see>.
/// Callers must ensure <see cref="Id"/> is non-empty.
/// </remarks>
public sealed record class WebAuthnRelyingParty
{
    /// <summary>
    /// Gets the Relying Party identifier (e.g., "example.com").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-readable relying party name.
    /// </summary>
    public string? Name { get; init; }
}
