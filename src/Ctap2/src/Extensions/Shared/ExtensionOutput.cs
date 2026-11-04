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
/// Parser for CTAP2 extension output maps.
/// </summary>
/// <remarks>
/// <para>
/// This convenience parser expects a definite-length authData extensions map, not the complete CTAP response or
/// unsigned extension outputs. The memory overload reconstructs supported values into new CBOR arrays;
/// it does not preserve byte-exact original encoding or every valid unknown CBOR value. Supported
/// values are signed-range integers, byte/text strings, booleans, null, arrays and maps of those values.
/// Tags, floats and other simple values can be lost or cause reconstruction failure; large unsigned
/// integers can fail. Duplicate identifiers overwrite earlier values. Retain the source parent raw
/// response/authenticator-data bytes when exact or unknown data is needed; their guarantees are separate.
/// </para>
/// <para>
/// Reconstructed values and byte-string getters allocate independent copies with no disposal/zeroing
/// boundary. Session disposal or clearing the source does not clear those copies. Typed Try methods
/// are shape-specific, may reject present values, and can throw for malformed/wrong-shape input.
/// </para>
/// <para>
/// Composable fragment with existing nonempty ReadOnlyMemory&lt;byte&gt; extensionCbor from authData:
/// <code>
/// var output = ExtensionOutput.Decode(extensionCbor);
/// if (output.TryGetCredProtect(out var policy))
/// {
///     Console.WriteLine($"Credential protection: {policy}");
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class ExtensionOutput
{
    private readonly IReadOnlyDictionary<string, ReadOnlyMemory<byte>> _extensions;

    private ExtensionOutput(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        _extensions = extensions;
    }

    /// <summary>
    /// Gets whether this output contains any extensions.
    /// </summary>
    public bool HasExtensions => _extensions.Count > 0;

    /// <summary>
    /// Gets the extension identifiers present in this output.
    /// </summary>
    public IEnumerable<string> ExtensionIds => _extensions.Keys;

    /// <summary>
    /// Attempts to get the credProtect extension output.
    /// </summary>
    /// <param name="policy">The credential protection policy.</param>
    /// <returns>True for a decoded policy in range 1–3; false for absence or an out-of-range integer.</returns>
    public bool TryGetCredProtect(out CredProtectPolicy policy)
    {
        policy = default;

        if (!_extensions.TryGetValue(ExtensionIdentifiers.CredProtect, out var data))
        {
            return false;
        }

        var reader = new CborReader(data, CborConformanceMode.Lax);
        var value = reader.ReadInt32();

        if (value is >= 1 and <= 3)
        {
            policy = (CredProtectPolicy)value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to get the credBlob extension output (makeCredential response).
    /// </summary>
    /// <param name="stored">Whether the blob was successfully stored.</param>
    /// <returns>True for a boolean storage result, including false; false for absence or another value shape.</returns>
    public bool TryGetCredBlobStored(out bool stored)
    {
        stored = false;

        if (!_extensions.TryGetValue(ExtensionIdentifiers.CredBlob, out var data))
        {
            return false;
        }

        var reader = new CborReader(data, CborConformanceMode.Lax);

        // Check if it's a boolean (makeCredential) or byte string (getAssertion)
        if (reader.PeekState() == CborReaderState.Boolean)
        {
            stored = reader.ReadBoolean();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to get the credBlob extension output (getAssertion response).
    /// </summary>
    /// <param name="blob">An independent byte-array copy, possibly empty; no cleanup owner is provided.</param>
    /// <returns>True for a byte-string result, including an empty stored-blob result; false for absence or another shape.</returns>
    public bool TryGetCredBlob(out ReadOnlyMemory<byte> blob)
    {
        blob = default;

        if (!_extensions.TryGetValue(ExtensionIdentifiers.CredBlob, out var data))
        {
            return false;
        }

        var reader = new CborReader(data, CborConformanceMode.Lax);

        // Check if it's a byte string (getAssertion)
        if (reader.PeekState() == CborReaderState.ByteString)
        {
            blob = reader.ReadByteString();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Decodes encrypted assertion output at the hmac-secret key.
    /// </summary>
    /// <param name="output">The encrypted hmac-secret output.</param>
    /// <remarks>
    /// Not a registration-support accessor: hmac-secret's registration boolean causes a decoding error.
    /// For hmac-secret-mc, use TryGetRawExtension with its identifier and HmacSecretOutput.Decode.
    /// Decode the registration boolean explicitly from its raw value instead.
    /// </remarks>
    /// <returns>True if the extension was present.</returns>
    public bool TryGetHmacSecret(out HmacSecretOutput? output)
    {
        output = null;

        if (!_extensions.TryGetValue(ExtensionIdentifiers.HmacSecret, out var data))
        {
            return false;
        }

        output = HmacSecretOutput.Decode(data);
        return true;
    }

    /// <summary>
    /// Decodes the legacy convenience largeBlob value, not standard direct CTAP output.
    /// </summary>
    /// <param name="output">The largeBlob output.</param>
    /// <remarks>
    /// A boolean is mapped to Written; a map is passed to LargeBlobOutput.Decode. Neither establishes
    /// standard registration support or a completed storage write. CTAP direct largeBlob outputs are
    /// unsigned and require different modeling; the implemented key/storage route uses envelope keys.
    /// </remarks>
    /// <returns>True if the extension was present.</returns>
    public bool TryGetLargeBlob(out LargeBlobOutput? output)
    {
        output = null;

        if (!_extensions.TryGetValue(ExtensionIdentifiers.LargeBlob, out var data))
        {
            return false;
        }

        var reader = new CborReader(data, CborConformanceMode.Lax);

        // Legacy convenience mapping, not the standard unsigned {supported: true} registration output.
        if (reader.PeekState() == CborReaderState.Boolean)
        {
            var supported = reader.ReadBoolean();
            output = new LargeBlobOutput { Written = supported };
            return true;
        }

        // Decode the legacy combined map; this is not a direct CTAP read-output decoder.
        output = LargeBlobOutput.Decode(reader);
        return true;
    }

    /// <summary>
    /// Looks for a largeBlobKey in legacy convenience extension data.
    /// </summary>
    /// <param name="key">The large blob key.</param>
    /// <remarks>
    /// Standard CTAP largeBlobKey has no authData extension output. Use MakeCredentialResponse.LargeBlobKey
    /// or GetAssertionResponse.LargeBlobKey instead. This method's copied secret has no cleanup owner.
    /// </remarks>
    /// <returns>True if the extension was present and contained the key.</returns>
    public bool TryGetLargeBlobKey(out ReadOnlyMemory<byte> key)
    {
        key = default;

        if (!_extensions.TryGetValue(ExtensionIdentifiers.LargeBlobKey, out var data))
        {
            // Also check in largeBlob output
            if (TryGetLargeBlob(out var output) && output?.LargeBlobKey is { } blobKey)
            {
                key = blobKey;
                return true;
            }
            return false;
        }

        var reader = new CborReader(data, CborConformanceMode.Lax);
        key = reader.ReadByteString();
        return true;
    }

    /// <summary>
    /// Attempts to get the minPinLength extension output.
    /// </summary>
    /// <param name="minLength">The reported minimum in Unicode code points; zero on false is only an out-parameter default.</param>
    /// <returns>True if the extension was present.</returns>
    public bool TryGetMinPinLength(out int minLength)
    {
        minLength = 0;

        if (!_extensions.TryGetValue(ExtensionIdentifiers.MinPinLength, out var data))
        {
            return false;
        }

        var reader = new CborReader(data, CborConformanceMode.Lax);
        minLength = reader.ReadInt32();
        return true;
    }

    /// <summary>
    /// Attempts to get raw extension data by identifier.
    /// </summary>
    /// <param name="extensionId">The extension identifier.</param>
    /// <param name="data">Reconstructed CBOR, or empty data from the identifiers-only reader overload; not an original encoded slice.</param>
    /// <returns>True if the extension was present.</returns>
    public bool TryGetRawExtension(string extensionId, out ReadOnlyMemory<byte> data)
    {
        return _extensions.TryGetValue(extensionId, out data);
    }

    /// <summary>
    /// Reconstructs supported extension values from CBOR bytes into independent encoded copies.
    /// </summary>
    /// <param name="data">The CBOR-encoded extensions map.</param>
    /// <returns>The convenience output, subject to the reconstruction and ownership limits in the class remarks.</returns>
    public static ExtensionOutput Decode(ReadOnlyMemory<byte> data) =>
        DecodeWithRawData(data);

    /// <summary>
    /// Decodes extension output from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader positioned at the extensions map.</param>
    /// <returns>The decoded extension output.</returns>
    /// <remarks>
    /// This overload retains identifiers only and stores empty data after skipping each value.
    /// HasExtensions/ExtensionIds remain usable, but typed value accessors are not usable on its output.
    /// Use the memory overload for limited value reconstruction, or retain source raw bytes for exact data.
    /// </remarks>
    public static ExtensionOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var extensions = new Dictionary<string, ReadOnlyMemory<byte>>();

        var mapCount = reader.ReadStartMap() ?? 0;
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadTextString();
            // This overload intentionally retains only the identifier, not a decodable value.
            reader.SkipValue();
            extensions[key] = ReadOnlyMemory<byte>.Empty;
        }
        reader.ReadEndMap();

        return new ExtensionOutput(extensions);
    }

    /// <summary>
    /// Reconstructs supported extension values from CBOR bytes; the name does not imply byte-exact preservation.
    /// </summary>
    /// <param name="data">The CBOR-encoded extensions map.</param>
    /// <returns>Independent reconstructed values with the class-level unknown-data/cleanup limitations.</returns>
    public static ExtensionOutput DecodeWithRawData(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            return new ExtensionOutput(new Dictionary<string, ReadOnlyMemory<byte>>());
        }

        var reader = new CborReader(data, CborConformanceMode.Lax);
        var extensions = new Dictionary<string, ReadOnlyMemory<byte>>();

        var mapCount = reader.ReadStartMap() ?? 0;
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadTextString();

            // Re-encode supported values, rather than retaining a slice of the original encoding.
            var valueWriter = new CborWriter(CborConformanceMode.Lax);
            CopyCborValue(reader, valueWriter);
            extensions[key] = valueWriter.Encode();
        }
        reader.ReadEndMap();

        return new ExtensionOutput(extensions);
    }

    private static void CopyCborValue(CborReader reader, CborWriter writer)
    {
        switch (reader.PeekState())
        {
            case CborReaderState.UnsignedInteger:
            case CborReaderState.NegativeInteger:
                writer.WriteInt64(reader.ReadInt64());
                break;
            case CborReaderState.ByteString:
                writer.WriteByteString(reader.ReadByteString());
                break;
            case CborReaderState.TextString:
                writer.WriteTextString(reader.ReadTextString());
                break;
            case CborReaderState.Boolean:
                writer.WriteBoolean(reader.ReadBoolean());
                break;
            case CborReaderState.Null:
                reader.ReadNull();
                writer.WriteNull();
                break;
            case CborReaderState.StartArray:
                var arrayLength = reader.ReadStartArray();
                writer.WriteStartArray(arrayLength);
                while (reader.PeekState() != CborReaderState.EndArray)
                {
                    CopyCborValue(reader, writer);
                }
                reader.ReadEndArray();
                writer.WriteEndArray();
                break;
            case CborReaderState.StartMap:
                var mapLength = reader.ReadStartMap();
                writer.WriteStartMap(mapLength);
                while (reader.PeekState() != CborReaderState.EndMap)
                {
                    CopyCborValue(reader, writer); // key
                    CopyCborValue(reader, writer); // value
                }
                reader.ReadEndMap();
                writer.WriteEndMap();
                break;
            default:
                reader.SkipValue();
                break;
        }
    }
}