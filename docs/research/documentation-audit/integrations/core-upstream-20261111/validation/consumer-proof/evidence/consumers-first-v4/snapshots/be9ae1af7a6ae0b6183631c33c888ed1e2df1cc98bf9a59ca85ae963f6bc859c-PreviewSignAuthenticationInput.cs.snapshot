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

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Input for the previewSign extension authentication.
/// </summary>
/// <remarks>
/// <para>
/// Maps credential IDs to their corresponding signing parameters. Each entry specifies the key
/// handle, to-be-signed value, and optional algorithm-specific arguments for one credential.
/// </para>
/// <para>
/// Supply a nonempty <c>AllowCredentials</c> list with exactly one mapping for every entry, including
/// multi-credential lists. The client selects a credential and uses its matching signing parameters.
/// Unsupported previewSign requests can be omitted; inspect the returned extension result.
/// </para>
/// <para>
/// Credential IDs are compared by byte content. Construction normalizes dictionary comparison but
/// does not copy the backing key buffers or signing-parameter memory. Keep those buffers unchanged
/// until the ceremony completes; ownership remains with the caller.
/// </para>
/// </remarks>
public sealed record class PreviewSignAuthenticationInput
{
    /// <summary>
    /// Gets the dictionary mapping credential IDs to signing parameters.
    /// </summary>
    /// <remarks>
    /// Keys are the raw credential ID bytes.
    /// </remarks>
    public IReadOnlyDictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> SignByCredential { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignAuthenticationInput"/>.
    /// </summary>
    /// <param name="signByCredential">Dictionary mapping credential IDs to signing parameters.</param>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when the dictionary is empty (InvalidRequest).
    /// </exception>
    public PreviewSignAuthenticationInput(
        IReadOnlyDictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> signByCredential)
    {
        if (signByCredential.Count == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign authentication requires at least one credential mapping");
        }

        // Defensively rebuild dictionary with ByteArrayKeyComparer if needed
        if (signByCredential is Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> dict &&
            !ReferenceEquals(dict.Comparer, ByteArrayKeyComparer.Instance))
        {
            var rebuilt = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(
                signByCredential.Count,
                ByteArrayKeyComparer.Instance);
            foreach (var kvp in signByCredential)
            {
                rebuilt[kvp.Key] = kvp.Value;
            }
            SignByCredential = rebuilt;
        }
        else if (signByCredential is not Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>)
        {
            // Not a Dictionary, rebuild to ensure correct comparer
            var rebuilt = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(
                signByCredential.Count,
                ByteArrayKeyComparer.Instance);
            foreach (var kvp in signByCredential)
            {
                rebuilt[kvp.Key] = kvp.Value;
            }
            SignByCredential = rebuilt;
        }
        else
        {
            SignByCredential = signByCredential;
        }
    }

    /// <summary>
    /// Creates an authentication input with a credential-to-params mapping.
    /// </summary>
    /// <param name="signByCredential">Dictionary mapping credential IDs to signing parameters.</param>
    /// <returns>A <see cref="PreviewSignAuthenticationInput"/> instance.</returns>
    /// <remarks>
    /// Pass an ordinary dictionary keyed by raw credential-ID bytes. The input normalizes key
    /// comparison by byte content; no custom comparer is required. Backing buffers remain caller-owned.
    /// </remarks>
    /// <example>
    /// <code>
    /// var input = PreviewSignAuthenticationInput.CreateSignByCredential(
    ///     new Dictionary&lt;ReadOnlyMemory&lt;byte&gt;, PreviewSignSigningParams&gt;
    ///     {
    ///         [credentialId] = signingParams
    ///     });
    /// </code>
    /// </example>
    public static PreviewSignAuthenticationInput CreateSignByCredential(
        IReadOnlyDictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> signByCredential) =>
        new(signByCredential);
}