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
/// unsigned extension outputs. Values are retained as exact encoded slices, including unknown values.
/// Duplicate identifiers are rejected. Keep the borrowed source bytes unchanged while using the result.
/// </para>
/// <para>
/// Some typed getters allocate independent copies; clearing the source does not clear those copies. Typed Try methods
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
    /// <param name="data">A borrowed slice containing the original encoded CBOR value.</param>
    /// <returns>True if the extension was present.</returns>
    public bool TryGetRawExtension(string extensionId, out ReadOnlyMemory<byte> data)
    {
        return _extensions.TryGetValue(extensionId, out data);
    }

    /// <summary>
    /// Decodes one extensions map, borrowing its encoded values.
    /// </summary>
    /// <param name="data">The CBOR-encoded extensions map.</param>
    /// <returns>The extension output with borrowed encoded values.</returns>
    public static ExtensionOutput Decode(ReadOnlyMemory<byte> data) =>
        DecodeWithRawData(data);

    /// <summary>
    /// Decodes extension output from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader positioned at the extensions map.</param>
    /// <returns>The decoded extension output.</returns>
    /// <remarks>
    /// Borrows encoded values from the reader's input and respects its conformance mode.
    /// Only the current map is consumed; the enclosing reader remains positioned after that map.
    /// </remarks>
    public static ExtensionOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var extensions = new Dictionary<string, ReadOnlyMemory<byte>>();

        var mapCount = reader.ReadStartMap()
            ?? throw new CborContentException("The extensions map must have a definite length.");
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadTextString();
            if (!extensions.TryAdd(key, reader.ReadEncodedValue()))
                throw new CborContentException("Duplicate extension identifier.");
        }
        reader.ReadEndMap();

        return new ExtensionOutput(extensions);
    }

    /// <summary>
    /// Decodes one extensions map, borrowing its exact encoded values.
    /// </summary>
    /// <param name="data">The CBOR-encoded extensions map.</param>
    /// <returns>The extension output with borrowed encoded values.</returns>
    public static ExtensionOutput DecodeWithRawData(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            return new ExtensionOutput(new Dictionary<string, ReadOnlyMemory<byte>>());
        }

        var reader = new CborReader(data, CborConformanceMode.Strict);
        var result = Decode(reader);
        if (reader.BytesRemaining != 0)
            throw new CborContentException("Unexpected bytes after the extensions map.");

        return result;
    }
}