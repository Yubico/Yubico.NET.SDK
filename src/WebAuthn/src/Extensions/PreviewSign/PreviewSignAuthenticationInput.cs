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
/// Construction copies the mapping and credential ID bytes. IDs are compared by byte content.
/// Signing-parameter buffers are still borrowed; keep them unchanged until the ceremony completes.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
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
    /// Thrown when the dictionary is empty, contains a null value, or maps the same credential ID
    /// more than once by byte content (InvalidRequest).
    /// </exception>
    /// <exception cref="ArgumentNullException">The dictionary is null.</exception>
    public PreviewSignAuthenticationInput(
        IReadOnlyDictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> signByCredential)
    {
        ArgumentNullException.ThrowIfNull(signByCredential);
        if (signByCredential.Count == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign authentication requires at least one credential mapping");
        }

        var copy = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(
            signByCredential.Count, ByteArrayKeyComparer.Instance);
        foreach (var (id, signingParams) in signByCredential)
        {
            if (signingParams is null || !copy.TryAdd(id.ToArray(), signingParams))
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidRequest,
                    "previewSign authentication requires one non-null mapping per credential ID");
            }
        }

        SignByCredential = copy.AsReadOnly();
    }
}