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
/// Parameters for signing arbitrary data with a previewSign credential.
/// </summary>
/// <remarks>
/// <para>
/// Specifies the key handle, data to be signed, and optional algorithm-specific arguments
/// for a single signing operation.
/// </para>
/// <para>
/// Per CTAP v4 draft specification §3.2:
/// - KeyHandle identifies which signing key to use (from prior registration)
/// - Tbs (to-be-signed) is the raw data to sign, unaltered by the authenticator
/// - AdditionalArgs is optional CBOR-encoded COSE_Sign_Args for two-party signing algorithms
/// </para>
/// </remarks>
public sealed record class PreviewSignSigningParams
{
    /// <summary>
    /// Gets the key handle from registration output. Used by the authenticator to re-derive
    /// the signing private key.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; }

    /// <summary>
    /// Gets the raw data to be signed. The authenticator signs this directly without wrapping
    /// in clientDataJSON or authenticator data. Depending on the algorithm, the relying
    /// party may need to pre-hash this data.
    /// </summary>
    public ReadOnlyMemory<byte> Tbs { get; }

    /// <summary>
    /// Gets the optional CBOR-encoded COSE_Sign_Args for algorithms requiring additional parameters
    /// (e.g., split-signing algorithms). Must be valid CBOR when present.
    /// </summary>
    public ReadOnlyMemory<byte>? AdditionalArgs { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignSigningParams"/>.
    /// </summary>
    /// <param name="keyHandle">The key handle for the signing key.</param>
    /// <param name="tbs">Data to be signed.</param>
    /// <param name="additionalArgs">Optional additional signing arguments.</param>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when:
    /// - KeyHandle is empty (InvalidRequest)
    /// - Tbs is empty (InvalidRequest)
    /// </exception>
    public PreviewSignSigningParams(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> tbs,
        ReadOnlyMemory<byte>? additionalArgs = null)
    {
        if (keyHandle.Length == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign KeyHandle must not be empty");
        }

        if (tbs.Length == 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.InvalidRequest,
                "previewSign Tbs (to-be-signed data) must not be empty");
        }

        KeyHandle = keyHandle;
        Tbs = tbs;
        AdditionalArgs = additionalArgs;
    }
}
