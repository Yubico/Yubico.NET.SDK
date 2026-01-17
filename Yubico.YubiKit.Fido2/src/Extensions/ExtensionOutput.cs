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

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Parser for CTAP2 extension output maps.
/// </summary>
/// <remarks>
/// <para>
/// This parser decodes the CBOR-encoded extensions map returned in makeCredential
/// and getAssertion responses. Extension outputs are accessed via property methods.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var output = ExtensionOutput.Decode(response.Extensions);
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
    /// <returns>True if the extension was present.</returns>
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
    /// <returns>True if the extension was present.</returns>
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
    /// <param name="blob">The stored blob data.</param>
    /// <returns>True if the extension was present and contained blob data.</returns>
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
    /// Attempts to get the hmac-secret extension output.
    /// </summary>
    /// <param name="output">The encrypted hmac-secret output.</param>
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
    /// Attempts to get the largeBlob extension output.
    /// </summary>
    /// <param name="output">The largeBlob output.</param>
    /// <returns>True if the extension was present.</returns>
    public bool TryGetLargeBlob(out LargeBlobOutput? output)
    {
        output = null;
        
        if (!_extensions.TryGetValue(ExtensionIdentifiers.LargeBlob, out var data))
        {
            return false;
        }
        
        var reader = new CborReader(data, CborConformanceMode.Lax);
        
        // During makeCredential, output is just a boolean
        if (reader.PeekState() == CborReaderState.Boolean)
        {
            var supported = reader.ReadBoolean();
            output = new LargeBlobOutput { Written = supported };
            return true;
        }
        
        // During getAssertion, output is a map
        output = LargeBlobOutput.Decode(reader);
        return true;
    }
    
    /// <summary>
    /// Attempts to get the largeBlobKey from extension output.
    /// </summary>
    /// <param name="key">The large blob key.</param>
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
    /// <param name="minLength">The minimum PIN length.</param>
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
    /// <param name="data">The raw CBOR-encoded extension data.</param>
    /// <returns>True if the extension was present.</returns>
    public bool TryGetRawExtension(string extensionId, out ReadOnlyMemory<byte> data)
    {
        return _extensions.TryGetValue(extensionId, out data);
    }
    
    /// <summary>
    /// Decodes extension output from CBOR bytes.
    /// </summary>
    /// <param name="data">The CBOR-encoded extensions map.</param>
    /// <returns>The decoded extension output.</returns>
    public static ExtensionOutput Decode(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            return new ExtensionOutput(new Dictionary<string, ReadOnlyMemory<byte>>());
        }
        
        var reader = new CborReader(data, CborConformanceMode.Lax);
        return Decode(reader);
    }
    
    /// <summary>
    /// Decodes extension output from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader positioned at the extensions map.</param>
    /// <returns>The decoded extension output.</returns>
    /// <remarks>
    /// Note: This method skips values and stores empty data. Use DecodeWithRawData for full data preservation.
    /// </remarks>
    public static ExtensionOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        
        var extensions = new Dictionary<string, ReadOnlyMemory<byte>>();
        
        var mapCount = reader.ReadStartMap() ?? 0;
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadTextString();
            // Skip the value - use DecodeWithRawData if you need the raw data
            reader.SkipValue();
            extensions[key] = ReadOnlyMemory<byte>.Empty;
        }
        reader.ReadEndMap();
        
        return new ExtensionOutput(extensions);
    }
    
    /// <summary>
    /// Decodes extension output from CBOR bytes, preserving raw data.
    /// </summary>
    /// <param name="data">The CBOR-encoded extensions map.</param>
    /// <returns>The decoded extension output with raw data preserved.</returns>
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
            
            // Get the start position and read/encode the value to get its bytes
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
