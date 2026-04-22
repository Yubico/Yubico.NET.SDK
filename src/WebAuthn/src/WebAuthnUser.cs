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
/// User account entity information for WebAuthn operations.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialuserentity">
/// WebAuthn PublicKeyCredentialUserEntity</see>.
/// Callers must ensure <see cref="Id"/> is between 1 and 64 bytes per the WebAuthn specification.
/// </remarks>
public sealed record class WebAuthnUser
{
    /// <summary>
    /// Gets the user handle (opaque byte sequence, 1-64 bytes).
    /// </summary>
    public required ReadOnlyMemory<byte> Id { get; init; }

    /// <summary>
    /// Gets the user identifier (e.g., "alice@example.com").
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the display name (e.g., "Alice Smith").
    /// </summary>
    public string? DisplayName { get; init; }
}
