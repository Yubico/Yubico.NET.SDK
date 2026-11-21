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

using System.Formats.Cbor;

namespace Yubico.YubiKit.Ctap2.Extensions;

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
/// Pass <c>null</c> to omit key 7; a present value must encode one complete CBOR map.
/// Experimental typed helpers such as <see cref="CoseSignArgs"/> can be converted to raw
/// <c>additionalArgs</c> bytes with <see cref="PreviewSignCbor.EncodeAdditionalArgs"/>.
/// The map's algorithm-specific semantics are the caller's responsibility. Tbs may be raw data
/// or a digest depending on the selected algorithm; the encoder does not hash it.
/// </para>
/// <para>
/// Inputs are borrowed until encoding and awaited transmission complete; keep storage unchanged.
/// Encoders make independent copies without a disposable cleanup owner. Clear caller-owned
/// application-sensitive data after use. This preview is experimental and not production guidance.
/// A generated handle may be empty when the authenticator stores equivalent information internally.
/// Key 2 is still encoded as a byte string. The nonempty TBS requirement is an SDK restriction.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public sealed class PreviewSignSigningParams
{
    /// <summary>
    /// Gets the key handle from registration output.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; init; }

    /// <summary>
    /// Gets the algorithm-specific to-be-signed bytes, which may already be a digest.
    /// </summary>
    public ReadOnlyMemory<byte> Tbs
    {
        get;
        init => field = RequireNonEmpty(value, "tbs");
    }

    /// <summary>
    /// Gets the optional algorithm-specific <c>additionalArgs</c> value.
    /// When present, the encoder emits this validated map unchanged under
    /// authentication input key 7. A <c>null</c> value omits key 7.
    /// </summary>
    public ReadOnlyMemory<byte>? AdditionalArgs
    {
        get;
        init
        {
            if (value is { } args)
                ValidateAdditionalArgs(args);
            field = value;
        }
    }

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
        KeyHandle = keyHandle;
        Tbs = tbs;
        AdditionalArgs = additionalArgs;
    }

    private static ReadOnlyMemory<byte> RequireNonEmpty(ReadOnlyMemory<byte> input, string parameter) =>
        input.IsEmpty ? throw new ArgumentException("Signing input must not be empty.", parameter) : input;

    private static void ValidateAdditionalArgs(ReadOnlyMemory<byte> additionalArgs)
    {
        var reader = new CborReader(additionalArgs, CborConformanceMode.Strict);
        try
        {
            if (reader.PeekState() == CborReaderState.StartMap)
            {
                reader.SkipValue();
                if (reader.BytesRemaining == 0)
                    return;
            }
        }
        catch (CborContentException ex)
        {
            throw new ArgumentException("Additional arguments must encode one complete CBOR map.", nameof(additionalArgs), ex);
        }

        throw new ArgumentException("Additional arguments must encode one complete CBOR map.", nameof(additionalArgs));
    }
}