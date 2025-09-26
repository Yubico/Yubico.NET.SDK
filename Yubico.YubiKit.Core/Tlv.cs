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

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core;

/// <summary>
///     Tag, length, Value structure that helps to parse APDU response data.
///     This class handles BER-TLV encoded data with determinate length.
/// </summary>
public sealed class Tlv : IDisposable
{
    private readonly byte[] _bytes;
    private readonly int _offset;
    private bool _disposed;

    /// <summary>
    ///     Creates a new TLV (Tag-Length-Value) object with the specified tag and value.
    /// </summary>
    /// <param name="tag">The tag value, must be between 0x00 and 0xFFFF.</param>
    /// <param name="value">The value data as a read-only span of bytes.</param>
    /// <exception cref="TlvException">Thrown when the tag value is outside the supported range (0x00-0xFFFF).</exception>
    /// <remarks>
    ///     This constructor creates a BER-TLV encoded structure where:
    ///     - The tag is encoded in the minimum number of bytes needed
    ///     - The length is encoded according to BER-TLV rules
    ///     - The value is stored as provided
    /// </remarks>
    public Tlv(int tag, ReadOnlySpan<byte> value)
    {
        if (tag is < 0 or > 0xFFFF)
            throw new ArgumentOutOfRangeException(nameof(tag), "Tag must be between 0 and 0xFFFF.");

        Tag = tag;
        Length = value.Length;
        var tagByteCount = GetMinimalByteCount((uint)Tag);
        int lengthFieldByteCount;
        var bytesForLengthValue = 0;

        var isShortFormLength = Length < 0x80;
        if (isShortFormLength)
        {
            lengthFieldByteCount = 1;
        }
        else
        {
            bytesForLengthValue = GetMinimalByteCount((uint)Length);
            lengthFieldByteCount = 1 + bytesForLengthValue;
        }

        var totalTlvSize = tagByteCount + lengthFieldByteCount + Length;
        _bytes = new byte[totalTlvSize];
        var buffer = _bytes.AsSpan();
        var writePosition = 0;

        // Write the Tag value
        WriteMinimalBigEndianBytes(buffer.Slice(writePosition, tagByteCount), (uint)Tag);
        writePosition += tagByteCount;

        // Write the Length field
        if (isShortFormLength)
        {
            buffer[writePosition] = (byte)Length;
            writePosition++;
        }
        else
        {
            buffer[writePosition] = (byte)(0x80 | bytesForLengthValue);
            writePosition++;
            WriteMinimalBigEndianBytes(buffer.Slice(writePosition, bytesForLengthValue), (uint)Length);
            writePosition += bytesForLengthValue;
        }

        // Write the Value
        _offset = writePosition;
        value.CopyTo(buffer[writePosition..]);
    }

    /// <summary>
    ///     Returns the tag.
    /// </summary>
    public int Tag { get; private set; }

    /// <summary>
    ///     Returns a copy of the value.
    /// </summary>
    public ReadOnlyMemory<byte> Value => _bytes.Skip(_offset).Take(Length).ToArray();

    /// <summary>
    ///     Returns the length of the value.
    /// </summary>
    public int Length { get; private set; }

    #region IDisposable Members

    /// <summary>
    ///     Dispose the object and clears its buffers
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        CryptographicOperations.ZeroMemory(_bytes);
        Length = 0;
        Tag = 0;
        _disposed = true;
    }

    #endregion

    private static int GetMinimalByteCount(uint value)
    {
        if (value == 0) return 1;
        return (32 - BitOperations.LeadingZeroCount(value) + 7) / 8;
    }

    private static void WriteMinimalBigEndianBytes(Span<byte> destination, uint value)
    {
        Span<byte> tempBuffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(tempBuffer, value);

        var leadingZeroBytes = value == 0 ? 3 : BitOperations.LeadingZeroCount(value) / 8;
        var valueBytes = tempBuffer[leadingZeroBytes..];
        valueBytes.CopyTo(destination);
    }


    /// <summary>
    ///     Returns a copy of the Tlv as a BER-TLV encoded byte array.
    /// </summary>
    public Memory<byte> GetBytes() => _bytes.ToArray();

    /// <summary>
    ///     Parse a Tlv from a BER-TLV encoded byte array.
    /// </summary>
    /// <param name="data">A byte array containing the TLV encoded data (and nothing more).</param>
    /// <returns>The parsed Tlv</returns>
    public static Tlv Parse(ReadOnlySpan<byte> data)
    {
        var buffer = data;
        return ParseFrom(ref buffer);
    }

    /// <inheritdoc cref="Tlv.Parse(ReadOnlySpan{byte})" />
    public static bool TryParse(ReadOnlySpan<byte> data, [NotNullWhen(true)] out Tlv? tlvObject)
    {
        // Poor man's TryParse
        tlvObject = null;
        try
        {
            tlvObject = ParseFrom(ref data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ReadOnlySpan<byte> GetValueSpan() => _bytes.AsSpan();

    /// <summary>
    ///     Parses a TLV from a BER-TLV encoded byte array.
    /// </summary>
    /// <param name="buffer">A byte array containing the TLV encoded data.</param>
    /// <returns>The parsed <see cref="Tlv" /></returns>
    /// <remarks>
    ///     This method will parse a TLV from the given buffer and return the parsed Tlv.
    ///     The method will consume the buffer as much as necessary to parse the TLV.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if the buffer does not contain a valid TLV.</exception>
    internal static Tlv ParseFrom(ref ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0) throw new ArgumentException("Insufficient data for tag");

        // The first byte of the TLV is the tag.
        int tag = buffer[0];

        // Determine if the tag is in long form.
        // Long form tags have more than one byte, starting with 0x1F.
        if ((buffer[0] & 0x1F) == 0x1F)
        {
            // Ensure there is enough data for a long form tag.
            if (buffer.Length < 2) throw new ArgumentException("Insufficient data for long form tag");

            // Combine the first two bytes to form the tag.
            tag = (buffer[0] << 8) | buffer[1];
            buffer = buffer[2..]; // Skip the tag bytes
        }
        else
        {
            buffer = buffer[1..]; // Skip the tag byte
        }

        if (buffer.Length < 1) throw new ArgumentException("Insufficient data for length");

        // Read the length of the TLV value.
        int length = buffer[0];
        buffer = buffer[1..];

        // If the length is more than one byte, process remaining bytes.
        if (length > 0x80)
        {
            var lengthLn = length - 0x80;
            length = 0;
            for (var i = 0; i < lengthLn; i++)
            {
                length = (length << 8) | buffer[0];
                buffer = buffer[1..];
            }
        }

        var value = buffer[..length];
        buffer = buffer[length..]; // Advance the buffer to the end of the value

        return new Tlv(tag, value);
    }

    /// <summary>
    ///     Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    /// <remarks>
    ///     The string is of the form <c>Tlv(0xTAG, LENGTH, VALUE)</c>.
    ///     <para>
    ///         The tag is written out in hexadecimal, prefixed by 0x.
    ///         The length is written out in decimal.
    ///         The value is written out in hexadecimal.
    ///     </para>
    /// </remarks>
    public override string ToString() =>
        $"Tlv(0x{Tag:X}, {Length}, {BitConverter.ToString(Value.ToArray()).Replace("-", "", StringComparison.Ordinal)})";
}