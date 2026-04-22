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
/// Input parameters for previewSign authentication (signing arbitrary data).
/// </summary>
/// <remarks>
/// <para>
/// Maps credential IDs to their corresponding signing parameters. Each entry specifies
/// the key handle, data to sign, and optional algorithm-specific arguments for one credential.
/// </para>
/// <para>
/// Per CTAP v4 draft specification §5.2:
/// - The dictionary must contain at least one entry
/// - Each credential ID in allowCredentials must have a corresponding entry
/// - The size of this dictionary must equal the size of allowCredentials
/// </para>
/// </remarks>
public sealed record class PreviewSignAuthenticationInput
{
    /// <summary>
    /// Gets the dictionary mapping credential IDs to signing parameters.
    /// Keys are the raw credential ID bytes.
    /// </summary>
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
    /// This factory method provides parity with Swift's
    /// <c>PreviewSign.Authentication.Input.signByCredential(_:)</c>.
    /// Use <see cref="ByteArrayKeyComparer.Instance"/> when constructing the dictionary
    /// to ensure correct equality semantics for byte array keys.
    /// </remarks>
    public static PreviewSignAuthenticationInput CreateSignByCredential(
        IReadOnlyDictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> signByCredential) =>
        new(signByCredential);
}
