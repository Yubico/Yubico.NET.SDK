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
using Yubico.YubiKit.Ctap2.Cbor;

namespace Yubico.YubiKit.Ctap2.Extensions;

/// <summary>
/// Output from the previewSign extension authentication.
/// </summary>
/// <remarks>
/// Contains signature bytes without verification. The Yubico Labs previewSign v4 Editor's Draft
/// is experimental; algorithm-specific message hashing and signature format remain caller concerns.
/// The constructor borrows storage, while <see cref="Decode"/> creates an independent byte-string
/// copy. Keep borrowed storage stable during use; this DTO has no disposal/zeroing boundary.
/// </remarks>
[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public sealed class PreviewSignAuthenticationOutput
{
    /// <summary>
    /// Gets the signature bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Signature { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignAuthenticationOutput"/>.
    /// </summary>
    /// <param name="signature">The signature bytes.</param>
    public PreviewSignAuthenticationOutput(ReadOnlyMemory<byte> signature)
    {
        if (signature.Length == 0)
        {
            throw new ArgumentException("Signature must not be empty.", nameof(signature));
        }

        Signature = signature;
    }

    /// <summary>Decodes a previewSign assertion extension value without verifying the signature.</summary>
    /// <param name="cbor">The encoded previewSign map containing the signature at key 6.</param>
    /// <returns>The decoded output with an independent copy of the signature bytes.</returns>
    /// <exception cref="InvalidOperationException">The output is not a map, or the signature is missing, empty, or not a byte string.</exception>
    /// <exception cref="CborContentException">The encoded content is invalid or has trailing bytes.</exception>
    /// <exception cref="OverflowException">A map key is outside the range of a 32-bit signed integer.</exception>
    public static PreviewSignAuthenticationOutput Decode(ReadOnlyMemory<byte> cbor)
    {
        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        var signature = DecodeSignature(reader);
        if (reader.BytesRemaining != 0)
        {
            throw new CborContentException("Trailing data after the previewSign authentication map.");
        }

        if (signature.IsEmpty)
        {
            throw new InvalidOperationException("previewSign authentication output has an empty signature (key 6).");
        }

        return new PreviewSignAuthenticationOutput(signature);
    }

    // WebAuthn preserves the byte string as received, including an empty value.
    internal static ReadOnlyMemory<byte> DecodeSignature(CborReader reader)
    {
        const int signatureKey = 6;
        byte[]? signature = null;

        CtapResponseParser.ReadIntKeyMap(reader, (key, valueReader) =>
        {
            if (key == signatureKey)
            {
                signature = valueReader.ReadByteString();
            }
            else
            {
                valueReader.SkipValue();
            }
        });

        return signature ?? throw new InvalidOperationException("previewSign authentication output missing signature (key 6)");
    }
}