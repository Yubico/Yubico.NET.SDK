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
/// Parameters for signing with a previewSign generated key.
/// </summary>
/// <remarks>
/// <para>
/// The parameters correspond to the <c>keyHandle</c>, <c>tbs</c>, and optional
/// <c>additionalArgs</c> fields of the
/// <c>AuthenticationExtensionsSignSignInputs</c> dictionary. The <see cref="Tbs"/> and
/// <see cref="AdditionalArgs"/> values are algorithm-specific signing inputs and are passed
/// through unchanged.
/// Pass <c>null</c> to omit <c>additionalArgs</c>. Passing an empty memory value emits key 7 with
/// an empty byte string, but pass-through does not make that value valid under the draft. When
/// present, <c>additionalArgs</c> must encode a CBOR map containing the algorithm's COSE_Sign_Args
/// object. Supply the selected algorithm's required TBS representation, including prehashing when
/// required; generic previewSign does not mandate a 32-byte digest or ARKG.
/// </para>
/// <para>
/// The CTAP2 ARKG typed helpers are experimental conveniences, not the generic previewSign contract
/// or production cryptographic guidance. Their output can be converted to raw bytes with
/// <see cref="Ctap2.Extensions.PreviewSignCbor.EncodeAdditionalArgs"/> before constructing this type.
/// </para>
/// <para>
/// The constructor retains supplied memory without copying or taking ownership. Keep backing buffers
/// unchanged until the ceremony completes, then clear any sensitive buffers you own when no longer
/// needed. The current constructor rejects empty handles even though draft v4 permits them; it also
/// requires nonempty TBS locally. Algorithms can impose additional requirements that this type does not validate.
/// </para>
/// </remarks>
public sealed record class PreviewSignSigningParams
{
    /// <summary>
    /// Gets the key handle used to request signatures from this generated signing key.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; }

    /// <summary>
    /// Gets the to-be-signed value for this signing operation.
    /// </summary>
    public ReadOnlyMemory<byte> Tbs { get; }

    /// <summary>
    /// Gets the optional algorithm-specific <c>additionalArgs</c> value.
    /// </summary>
    public ReadOnlyMemory<byte>? AdditionalArgs { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignSigningParams"/>.
    /// </summary>
    /// <param name="keyHandle">The key handle for the signing key.</param>
    /// <param name="tbs">The nonempty, algorithm-specific to-be-signed representation.</param>
    /// <param name="additionalArgs">Optional CBOR-encoded COSE_Sign_Args map, passed through without semantic validation.</param>
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