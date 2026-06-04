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
/// The parameters correspond to the <c>keyHandle</c>, <c>tbs</c>, and optional
/// <c>additionalArgs</c> fields of the
/// <c>AuthenticationExtensionsSignSignInputs</c> dictionary. The <see cref="Tbs"/> and
/// <see cref="AdditionalArgs"/> values are algorithm-specific signing inputs and are passed
/// through unchanged.
/// Pass <c>null</c> to omit <c>additionalArgs</c>. Passing an empty memory value emits an empty
/// byte string under key 7. Experimental Fido2 typed helpers can be converted to raw bytes with
/// <see cref="Fido2.Extensions.PreviewSignCbor.EncodeAdditionalArgs"/> before constructing this type.
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
    /// Gets the optional algorithm-specific <c>additionalArgs</c> value.
    /// WebAuthn passes these bytes through unchanged.
    /// </summary>
    public ReadOnlyMemory<byte>? AdditionalArgs { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignSigningParams"/>.
    /// </summary>
    /// <param name="keyHandle">The key handle for the signing key.</param>
    /// <param name="tbs">Data to be signed.</param>
    /// <param name="additionalArgs">Optional algorithm-specific <c>additionalArgs</c> value.</param>
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
