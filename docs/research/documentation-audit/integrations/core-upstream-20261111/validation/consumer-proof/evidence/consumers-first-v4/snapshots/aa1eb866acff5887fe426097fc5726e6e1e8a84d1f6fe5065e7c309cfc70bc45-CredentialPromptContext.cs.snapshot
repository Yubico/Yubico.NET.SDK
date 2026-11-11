// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// Describes a single credential request made through <see cref="ICredentialPrompt"/>.
/// </summary>
/// <remarks>
/// This is a non-positional record so that optional properties can be added in
/// later releases without breaking implementations or callers. Implementations
/// should ignore properties they do not use.
/// </remarks>
public record CredentialPromptContext
{
    /// <summary>Gets the kind of secret being requested.</summary>
    public required CredentialKind Kind { get; init; }

    /// <summary>
    /// Gets a display-oriented description of what the secret unlocks, such as
    /// a relying-party identifier (<c>"example.com"</c>) for WebAuthn or an
    /// application name (<c>"PIV"</c>) elsewhere. May be <c>null</c> when no
    /// meaningful scope exists.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Gets the number of attempts remaining before the credential is blocked,
    /// when the protocol reports it; otherwise <c>null</c>.
    /// </summary>
    public int? RetriesRemaining { get; init; }

    /// <summary>
    /// Gets a value indicating whether this request follows a rejected attempt.
    /// </summary>
    public bool IsRetry { get; init; }

    /// <summary>
    /// Gets the minimum acceptable secret length, measured in Unicode code points.
    /// </summary>
    /// <remarks>
    /// Protocols that define their minimum over encoded bytes should set this only
    /// when every accepted code point encodes to one byte.
    /// </remarks>
    public int MinLengthCodePoints { get; init; }

    /// <summary>
    /// Gets the maximum acceptable secret length, measured in encoded bytes.
    /// </summary>
    public int MaxLengthBytes { get; init; } = 255;

    /// <summary>
    /// Gets a value indicating whether the implementation should ask for the
    /// secret twice and confirm the entries match, which is conventional when
    /// establishing a new secret.
    /// </summary>
    public bool RequiresConfirmation { get; init; }
}