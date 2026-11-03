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

namespace Yubico.YubiKit.WebAuthn.Extensions.Inputs;

/// <summary>
/// Input for the PRF (Pseudo-Random Function) extension.
/// </summary>
/// <remarks>
/// <para>
/// The PRF extension is the WebAuthn-level interface to the CTAP hmac-secret extension.
/// It allows deriving secrets using arbitrary inputs (called "eval" and "evalByCredential").
/// </para>
/// <para>
/// At the CTAP level, this maps to hmac-secret with the salt values derived from
/// the PRF inputs via hashing.
/// </para>
/// <para>
/// See: https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension
/// </para>
/// <para>
/// WebAuthn requires the verified hmac-secret PRF, overriding the user-verification preference when
/// necessary. The current client does not enforce that requirement. Request
/// <c>UserVerification = Required</c> for PRF evaluation; otherwise the same credential and input can
/// produce different results when actual user verification changes. CTAP defines separate verified
/// and unverified PRFs (CTAP 2.1 §12.5; CTAP 2.2–2.3 §12.7). This workaround does not establish general
/// conformance to WebAuthn's PRF requirement.
/// </para>
/// <para>
/// Input memory remains caller-owned. Keep its contents unchanged until the ceremony completes,
/// including output preparation, then clear caller-owned sensitive buffers when no longer needed.
/// The SDK does not guarantee cleanup of every temporary copy of PRF inputs or derived salts.
/// </para>
/// </remarks>
public sealed class PrfInput
{
    /// <summary>
    /// Gets or sets the first PRF input for evaluation.
    /// </summary>
    /// <remarks>
    /// Together with <see cref="Second"/>, supplies the default <c>eval</c> inputs. A matching
    /// <see cref="EvalByCredential"/> entry takes precedence. Registration-time evaluation also needs
    /// advertised <c>hmac-secret-mc</c> support; requesting evaluation does not guarantee results.
    /// Used to derive the first salt for hmac-secret.
    /// Salt is computed as: SHA-256("WebAuthn PRF" || 0x00 || first).
    /// </remarks>
    public ReadOnlyMemory<byte>? First { get; init; }

    /// <summary>
    /// Gets or sets the second PRF input for evaluation (optional).
    /// </summary>
    /// <remarks>
    /// This default second input is evaluated only when <see cref="First"/> is present. A matching
    /// credential-specific entry supplies its own inputs instead. Used to derive the second salt for hmac-secret.
    /// Salt is computed as: SHA-256("WebAuthn PRF" || 0x00 || second).
    /// </remarks>
    public ReadOnlyMemory<byte>? Second { get; init; }

    /// <summary>
    /// Gets or sets per-credential PRF inputs.
    /// </summary>
    /// <remarks>
    /// Maps unpadded base64url credential IDs to PRF inputs. A nonempty map requires a nonempty
    /// <c>AllowCredentials</c> list, and every key must identify an entry in that list. A matching entry
    /// overrides <see cref="First"/> and <see cref="Second"/> for the selected credential.
    /// Registration rejects this property, including an empty map, when PRF processing is active.
    /// </remarks>
    public IReadOnlyDictionary<string, PrfInputValues>? EvalByCredential { get; init; }

    /// <summary>
    /// Computes the salt for hmac-secret from a PRF input.
    /// </summary>
    /// <param name="input">The PRF input value.</param>
    /// <returns>The 32-byte salt for hmac-secret.</returns>
    /// <remarks>
    /// The caller owns the returned array. Clear it when no longer needed if the input is sensitive.
    /// The current implementation does not clear the temporary concatenation containing the input.
    /// </remarks>
    public static byte[] ComputeSalt(ReadOnlySpan<byte> input)
    {
        // "WebAuthn PRF" || 0x00 || input
        var prefix = "WebAuthn PRF"u8;
        var data = new byte[prefix.Length + 1 + input.Length];
        prefix.CopyTo(data);
        data[prefix.Length] = 0x00;
        input.CopyTo(data.AsSpan(prefix.Length + 1));

        var hash = new byte[32];
        System.Security.Cryptography.SHA256.HashData(data, hash);
        return hash;
    }
}