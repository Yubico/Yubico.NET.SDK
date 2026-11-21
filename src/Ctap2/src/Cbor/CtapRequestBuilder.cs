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
using System.Security.Cryptography;

namespace Yubico.YubiKit.Ctap2.Cbor;

/// <summary>
/// Fluent builder for constructing CTAP2 requests with canonical CBOR encoding.
/// </summary>
/// <remarks>
/// <para>
/// CTAP2 requires deterministic CBOR encoding per specification:
/// https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#ctap2-canonical-cbor-encoding-form
/// </para>
/// <para>
/// A builder is single-use, even if encoding fails. Byte-string and encoded-value methods retain
/// private copies that are cleared on replacement, build, or disposal. Dispose an abandoned builder.
/// Callbacks run during Build and must write one complete value. Keep captured storage valid until
/// Build returns. The builder does not clear borrowed storage or guarantee clearing all writer storage.
/// </para>
/// <para>
/// The Ctap2Canonical writer automatically sorts map key encodings when each map is completed.
/// Parameter insertion here is numerically sorted, which is not generally canonical encoded-key
/// order for mixed positive/negative keys. Writer canonicalization does not validate command schema,
/// authorization, CTAP's nesting limit or the authenticator's maximum message size.
/// </para>
/// </remarks>
public sealed class CtapRequestBuilder : IDisposable
{
    private readonly byte _command;
    private readonly SortedDictionary<int, (Action<CborWriter> Write, byte[]? OwnedBytes)> _parameters = new();
    private bool _consumed;

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
    public CtapRequestBuilder WithInt(int key, int value) => WithValue(key, writer => writer.WriteInt32(value));

    /// <summary>
    /// Adds an unsigned integer parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The unsigned integer value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithUInt(int key, uint value) => WithValue(key, writer => writer.WriteUInt32(value));

    /// <summary>
    /// Immediately copies a byte-string parameter for this request.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The byte array value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithBytes(int key, ReadOnlySpan<byte> value)
        => WithCopiedValue(key, value, encoded: false);

    /// <summary>
    /// Immediately copies a byte-string parameter from memory for this request.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The memory value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithBytes(int key, ReadOnlyMemory<byte> value)
        => WithBytes(key, value.Span);

    /// <summary>
    /// Adds a string parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The string value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithString(int key, string value) => WithValue(key, writer => writer.WriteTextString(value));

    /// <summary>
    /// Adds a boolean parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="value">The boolean value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithBool(int key, bool value) => WithValue(key, writer => writer.WriteBoolean(value));

    /// <summary>
    /// Adds a parameter using a caller-provided CBOR writer callback.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="writeValue">Action invoked during Build to write one complete CBOR value.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithValue(int key, Action<CborWriter> writeValue)
    {
        ThrowIfConsumed();
        SetParameter(key, writeValue, null);
        return this;
    }

    /// <summary>
    /// Adds a CBOR map parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="writeMap">Deferred action that writes the complete map, including start/end markers.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithMap(int key, Action<CborWriter> writeMap)
    {
        return WithValue(key, writeMap);
    }

    /// <summary>
    /// Adds a CBOR array parameter.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="writeArray">Deferred action that writes the complete array, including start/end markers.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithArray(int key, Action<CborWriter> writeArray)
    {
        return WithValue(key, writeArray);
    }

    /// <summary>
    /// Immediately copies a pre-encoded CBOR value for this request.
    /// </summary>
    /// <param name="key">The CBOR map key.</param>
    /// <param name="encodedValue">The pre-encoded CBOR bytes.</param>
    /// <returns>This builder for chaining.</returns>
    public CtapRequestBuilder WithEncodedValue(int key, ReadOnlyMemory<byte> encodedValue)
        => WithCopiedValue(key, encodedValue.Span, encoded: true);

    private CtapRequestBuilder WithCopiedValue(int key, ReadOnlySpan<byte> value, bool encoded)
    {
        ThrowIfConsumed();
        var copy = value.ToArray();
        try
        {
            SetParameter(key, encoded ? writer => writer.WriteEncodedValue(copy) : writer => writer.WriteByteString(copy), copy);
            return this;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(copy);
            throw;
        }
    }

    private void SetParameter(int key, Action<CborWriter> write, byte[]? ownedBytes)
    {
        _parameters.TryGetValue(key, out var previous);
        _parameters[key] = (write, ownedBytes);
        if (previous.OwnedBytes is { } replaced)
        {
            CryptographicOperations.ZeroMemory(replaced);
        }
    }

    /// <summary>
    /// Builds the CTAP request as a byte array.
    /// </summary>
    /// <remarks>Returns a caller-owned command-prefixed array. Clear it after the awaited send if it
    /// contains secrets, including when sending fails. The builder clears its own input copies.</remarks>
    /// <exception cref="InvalidOperationException">The builder was already built or disposed.</exception>
    /// <returns>The serialized CTAP request (command byte + CBOR payload).</returns>
    public byte[] Build()
    {
        ThrowIfConsumed();
        _consumed = true;
        try
        {
            return EncodeRequest();
        }
        finally
        {
            ClearParameters();
        }
    }

    private byte[] EncodeRequest()
    {
        if (_parameters.Count == 0)
        {
            // No parameters - just return the command byte
            return [_command];
        }

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        // Insert in numeric order; Ctap2Canonical sorts encoded keys when the map is completed.
        writer.WriteStartMap(_parameters.Count);

        foreach (var (key, parameter) in _parameters)
        {
            writer.WriteInt32(key);
            parameter.Write(writer);
        }

        writer.WriteEndMap();

        var result = new byte[1 + writer.BytesWritten];
        result[0] = _command;
        writer.Encode(result.AsSpan(1));
        return result;
    }

    /// <summary>
    /// Builds a new CTAP request array and returns its read-only view.
    /// </summary>
    /// <remarks>This is the same allocation/ownership as Build, not a borrowed-input view or a
    /// disposable owner. Read-only access does not automatically clear secret bytes.</remarks>
    /// <returns>The serialized CTAP request.</returns>
    public ReadOnlyMemory<byte> BuildAsMemory() => Build();

    /// <summary>Abandons an unused builder and clears its private input copies.</summary>
    public void Dispose()
    {
        if (!_consumed)
        {
            _consumed = true;
            ClearParameters();
        }
    }

    private void ClearParameters()
    {
        foreach (var parameter in _parameters.Values)
        {
            if (parameter.OwnedBytes is { } bytes)
            {
                CryptographicOperations.ZeroMemory(bytes);
            }
        }

        _parameters.Clear();
    }

    private void ThrowIfConsumed()
    {
        if (_consumed)
        {
            throw new InvalidOperationException("This CTAP request builder has already been consumed.");
        }
    }
}
