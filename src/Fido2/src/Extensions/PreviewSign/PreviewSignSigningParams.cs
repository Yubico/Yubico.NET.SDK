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

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Parameters for signing arbitrary data with a previewSign credential.
/// </summary>
/// <remarks>
/// <para>
/// Specifies the key handle, data to be signed, and optional algorithm-specific arguments
/// for a single signing operation.
/// </para>
/// <para>
/// Per CTAP v4 draft specification:
/// - KeyHandle identifies which signing key to use (from prior registration)
/// - Tbs (to-be-signed) is the raw data to sign
/// - CoseSignArgs is the typed, optional COSE_Sign_Args for two-party signing algorithms (e.g. ARKG)
/// </para>
/// </remarks>
public sealed class PreviewSignSigningParams
{
    /// <summary>
    /// Gets the key handle from registration output.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; init; }

    /// <summary>
    /// Gets the raw data to be signed.
    /// </summary>
    public ReadOnlyMemory<byte> Tbs { get; init; }

    /// <summary>
    /// Gets the optional typed <c>COSE_Sign_Args</c> for algorithms requiring additional parameters
    /// (e.g. ARKG). When present, the encoder emits canonical CBOR under authentication input
    /// key 7 (wrapped as bstr).
    /// </summary>
    public CoseSignArgs? CoseSignArgs { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignSigningParams"/>.
    /// </summary>
    /// <param name="keyHandle">The key handle for the signing key.</param>
    /// <param name="tbs">Data to be signed.</param>
    /// <param name="coseSignArgs">Optional typed <c>COSE_Sign_Args</c> (required for ARKG algorithms).</param>
    public PreviewSignSigningParams(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> tbs,
        CoseSignArgs? coseSignArgs = null)
    {
        if (keyHandle.Length == 0)
        {
            throw new ArgumentException("KeyHandle must not be empty.", nameof(keyHandle));
        }

        if (tbs.Length == 0)
        {
            throw new ArgumentException("Tbs must not be empty.", nameof(tbs));
        }

        KeyHandle = keyHandle;
        Tbs = tbs;
        CoseSignArgs = coseSignArgs;
    }
}
