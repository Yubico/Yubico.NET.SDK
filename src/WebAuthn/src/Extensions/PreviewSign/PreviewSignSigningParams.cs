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
/// - CoseSignArgs is the typed, optional <c>COSE_Sign_Args</c> for two-party signing algorithms
///   (e.g. ARKG). The WebAuthn layer re-exports the Fido2 <see cref="Fido2.Extensions.CoseSignArgs"/>
///   type rather than wrapping it: there is exactly one canonical encoder and it lives in Fido2.
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
    /// Gets the optional typed <c>COSE_Sign_Args</c> for algorithms requiring additional
    /// parameters (e.g. ARKG-P256). Construct with
    /// <see cref="Fido2.Extensions.CoseSignArgs.ArkgP256(ReadOnlyMemory{byte}, ReadOnlyMemory{byte})"/>.
    /// The Fido2 layer owns the canonical CBOR encoder; WebAuthn passes this value through
    /// unchanged.
    /// </summary>
    public Fido2.Extensions.CoseSignArgs? CoseSignArgs { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignSigningParams"/>.
    /// </summary>
    /// <param name="keyHandle">The key handle for the signing key.</param>
    /// <param name="tbs">Data to be signed.</param>
    /// <param name="coseSignArgs">Optional typed <c>COSE_Sign_Args</c> (required for ARKG algorithms).</param>
    /// <exception cref="WebAuthnClientError">
    /// Thrown when:
    /// - KeyHandle is empty (InvalidRequest)
    /// - Tbs is empty (InvalidRequest)
    /// </exception>
    public PreviewSignSigningParams(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> tbs,
        Fido2.Extensions.CoseSignArgs? coseSignArgs = null)
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
        CoseSignArgs = coseSignArgs;
    }
}