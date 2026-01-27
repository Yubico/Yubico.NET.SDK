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

namespace Yubico.YubiKit.Fido2.Cbor;

/// <summary>
/// Utility class for parsing CTAP2 CBOR responses.
/// Provides common deserialization patterns to reduce duplication across response models.
/// </summary>
internal static class CtapResponseParser
{
    /// <summary>
    /// Reads a CBOR map with integer keys and invokes the handler for each key-value pair.
    /// </summary>
    /// <remarks>
    /// This is the most common pattern in CTAP2 responses where maps use integer keys
    /// (e.g., 0x01, 0x02, etc.) to identify fields.
    /// </remarks>
    /// <param name="reader">The CBOR reader positioned at the start of a map.</param>
    /// <param name="fieldHandler">
    /// Action invoked for each field. Parameters are (key, reader).
    /// The handler should read exactly one value from the reader for the given key.
    /// </param>
    public static void ReadIntKeyMap(CborReader reader, Action<int, CborReader> fieldHandler)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(fieldHandler);
        
        var mapLength = reader.ReadStartMap();
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            fieldHandler(key, reader);
        }
        reader.ReadEndMap();
    }
    
    /// <summary>
    /// Reads a CBOR map with text (string) keys and invokes the handler for each key-value pair.
    /// </summary>
    /// <remarks>
    /// Used for extension outputs and other maps that use string keys (e.g., "hmac-secret").
    /// </remarks>
    /// <param name="reader">The CBOR reader positioned at the start of a map.</param>
    /// <param name="fieldHandler">
    /// Action invoked for each field. Parameters are (key, reader).
    /// The handler should read exactly one value from the reader for the given key.
    /// </param>
    public static void ReadTextKeyMap(CborReader reader, Action<string, CborReader> fieldHandler)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(fieldHandler);
        
        var mapLength = reader.ReadStartMap();
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            fieldHandler(key, reader);
        }
        reader.ReadEndMap();
    }
    
    /// <summary>
    /// Converts a nullable byte array to nullable ReadOnlyMemory.
    /// </summary>
    /// <param name="data">The byte array to convert, or null.</param>
    /// <returns>ReadOnlyMemory wrapping the data, or null if data is null.</returns>
    public static ReadOnlyMemory<byte>? ToNullableMemory(byte[]? data) =>
        data is not null ? new ReadOnlyMemory<byte>(data) : null;
    
    /// <summary>
    /// Reads a CBOR array and invokes the handler for each element.
    /// </summary>
    /// <param name="reader">The CBOR reader positioned at the start of an array.</param>
    /// <param name="elementHandler">
    /// Action invoked for each element. The handler should read exactly one value from the reader.
    /// </param>
    public static void ReadArray(CborReader reader, Action<CborReader> elementHandler)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(elementHandler);
        
        var arrayLength = reader.ReadStartArray();
        for (var i = 0; i < arrayLength; i++)
        {
            elementHandler(reader);
        }
        reader.ReadEndArray();
    }
    
    /// <summary>
    /// Reads a CBOR array and returns a list of parsed elements.
    /// </summary>
    /// <typeparam name="T">The type of elements to parse.</typeparam>
    /// <param name="reader">The CBOR reader positioned at the start of an array.</param>
    /// <param name="elementParser">
    /// Function that parses a single element from the reader and returns it.
    /// </param>
    /// <returns>A list of parsed elements.</returns>
    public static List<T> ReadArrayAsList<T>(CborReader reader, Func<CborReader, T> elementParser)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(elementParser);
        
        var result = new List<T>();
        var arrayLength = reader.ReadStartArray();
        for (var i = 0; i < arrayLength; i++)
        {
            result.Add(elementParser(reader));
        }
        reader.ReadEndArray();
        return result;
    }
    
    /// <summary>
    /// Safely reads an optional integer value from the reader, returning null if not present.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The integer value, or null if the value is null/undefined.</returns>
    public static int? ReadOptionalInt32(CborReader reader)
    {
        if (reader.PeekState() == CborReaderState.Null || reader.PeekState() == CborReaderState.Undefined)
        {
            reader.SkipValue();
            return null;
        }
        return reader.ReadInt32();
    }
    
    /// <summary>
    /// Safely reads an optional boolean value from the reader, returning null if not present.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The boolean value, or null if the value is null/undefined.</returns>
    public static bool? ReadOptionalBoolean(CborReader reader)
    {
        if (reader.PeekState() == CborReaderState.Null || reader.PeekState() == CborReaderState.Undefined)
        {
            reader.SkipValue();
            return null;
        }
        return reader.ReadBoolean();
    }
    
    /// <summary>
    /// Safely reads an optional byte array from the reader, returning null if not present.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The byte array, or null if the value is null/undefined.</returns>
    public static byte[]? ReadOptionalByteString(CborReader reader)
    {
        if (reader.PeekState() == CborReaderState.Null || reader.PeekState() == CborReaderState.Undefined)
        {
            reader.SkipValue();
            return null;
        }
        return reader.ReadByteString();
    }
    
    /// <summary>
    /// Safely reads an optional text string from the reader, returning null if not present.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The text string, or null if the value is null/undefined.</returns>
    public static string? ReadOptionalTextString(CborReader reader)
    {
        if (reader.PeekState() == CborReaderState.Null || reader.PeekState() == CborReaderState.Undefined)
        {
            reader.SkipValue();
            return null;
        }
        return reader.ReadTextString();
    }
}
