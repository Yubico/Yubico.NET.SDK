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
/// Fluent builder for constructing CTAP2 requests with canonical CBOR encoding.
/// </summary>
/// <remarks>
/// <para>
/// CTAP2 requires deterministic CBOR encoding per specification:
/// https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#ctap2-canonical-cbor-encoding-form
/// </para>
/// </remarks>
public sealed class CtapRequestBuilder
{
    private readonly byte _command;
    private readonly SortedDictionary<int, Action<CborWriter>> _parameters = new();
    
    private CtapRequestBuilder(byte command)
    {
        _command = command;
    }
    
    /// <summary>
    /// Creates a new builder for the specified CTAP command.
    /// </summary>
    /// <param name="command">The CTAP command byte.</param>
    /// <returns>A new builder instance.</returns>
    public static CtapRequestBuilder Create(byte command) => new(command);
    
    /// <summary>
    /// Adds an integer parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The integer value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithInt(int key, int value)
    {
        _parameters[key] = writer => writer.WriteInt32(value);
        return this;
    }
    
    /// <summary>
    /// Adds an unsigned integer parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The unsigned integer value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithUInt(int key, uint value)
    {
        _parameters[key] = writer => writer.WriteUInt32(value);
        return this;
    }
    
    /// <summary>
    /// Adds a byte array parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The byte array value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithBytes(int key, ReadOnlySpan<byte> value)
    {
        var copy = value.ToArray();
        _parameters[key] = writer => writer.WriteByteString(copy);
        return this;
    }
    
    /// <summary>
    /// Adds a byte array parameter from Memory.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The memory value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithBytes(int key, ReadOnlyMemory<byte> value)
    {
        var copy = value.ToArray();
        _parameters[key] = writer => writer.WriteByteString(copy);
        return this;
    }
    
    /// <summary>
    /// Adds a string parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The string value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithString(int key, string value)
    {
        _parameters[key] = writer => writer.WriteTextString(value);
        return this;
    }
    
    /// <summary>
    /// Adds a boolean parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The boolean value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithBool(int key, bool value)
    {
        _parameters[key] = writer => writer.WriteBoolean(value);
        return this;
    }
    
    /// <summary>
    /// Adds a CBOR map parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="writeMap">Action that writes the map content.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithMap(int key, Action<CborWriter> writeMap)
    {
        _parameters[key] = writeMap;
        return this;
    }
    
    /// <summary>
    /// Adds a CBOR array parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="writeArray">Action that writes the array content.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithArray(int key, Action<CborWriter> writeArray)
    {
        _parameters[key] = writeArray;
        return this;
    }
    
    /// <summary>
    /// Adds a pre-encoded CBOR value parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="encodedValue">The pre-encoded CBOR bytes.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithEncodedValue(int key, ReadOnlyMemory<byte> encodedValue)
    {
        var copy = encodedValue.ToArray();
        _parameters[key] = writer => writer.WriteEncodedValue(copy);
        return this;
    }
    
    /// <summary>
    /// Builds the CTAP request as a byte array.
    /// </summary>
    /// <returns>The serialized CTAP request (command byte + CBOR payload).</returns>
    public byte[] Build()
    {
        if (_parameters.Count == 0)
        {
            // No parameters - just return the command byte
            return [_command];
        }
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        
        // Write CBOR map with parameters sorted by key (canonical encoding)
        writer.WriteStartMap(_parameters.Count);
        
        foreach (var (key, writeValue) in _parameters)
        {
            writer.WriteInt32(key);
            writeValue(writer);
        }
        
        writer.WriteEndMap();
        
        // Prepend command byte
        var cbor = writer.Encode();
        var result = new byte[1 + cbor.Length];
        result[0] = _command;
        cbor.CopyTo(result, 1);
        
        return result;
    }
    
    /// <summary>
    /// Builds the CTAP request as a ReadOnlyMemory.
    /// </summary>
    /// <returns>The serialized CTAP request.</returns>
    public ReadOnlyMemory<byte> BuildAsMemory() => Build();
}
