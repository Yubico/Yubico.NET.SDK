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
/// The parameters correspond to the <c>keyHandle</c>, <c>tbs</c>, and optional
/// <c>additionalArgs</c> fields of the previewSign signing input dictionary.
/// The <see cref="Tbs"/> and <see cref="AdditionalArgs"/> values are algorithm-specific
/// signing inputs and are encoded unchanged.
/// Pass <c>null</c> to omit key 7. Passing an empty memory value emits key 7 with an empty
/// byte string, matching the caller-supplied algorithm-specific value.
/// Experimental typed helpers such as <see cref="CoseSignArgs"/> can be converted to raw
/// <c>additionalArgs</c> bytes with <see cref="PreviewSignCbor.EncodeAdditionalArgs"/>.
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
    /// Gets the optional algorithm-specific <c>additionalArgs</c> value.
    /// When present, including when empty, the encoder emits these bytes unchanged under
    /// authentication input key 7. A <c>null</c> value omits key 7.
    /// </summary>
    public ReadOnlyMemory<byte>? AdditionalArgs { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignSigningParams"/>.
    /// </summary>
    /// <param name="keyHandle">The key handle for the signing key.</param>
    /// <param name="tbs">Data to be signed.</param>
    /// <param name="additionalArgs">Optional algorithm-specific <c>additionalArgs</c> value.</param>
    public PreviewSignSigningParams(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> tbs,
        ReadOnlyMemory<byte>? additionalArgs = null)
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
        AdditionalArgs = additionalArgs;
    }
}
