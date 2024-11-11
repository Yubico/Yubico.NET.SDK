// Copyright 2024 Yubico AB
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

using System;
using System.IO;
using System.Linq;

namespace Yubico.Core.Tlv
{
    /// <summary>
    /// Tag, length, Value structure that helps to parse APDU response data.
    /// This class handles BER-TLV encoded data with determinate length.
    /// </summary>
    public class TlvObject
    {
        /// <summary>
        /// Returns the tag.
        /// </summary>
        public int Tag { get; }

        /// <summary>
        /// Returns the value.
        /// </summary>
        public Memory<byte> Value => _bytes.Skip(_offset).Take(Length).ToArray();

        /// <summary>
        /// Returns the length of the value.
        /// </summary>
        public int Length { get; }

        private readonly byte[] _bytes;
        private readonly int _offset;

        /// <summary>
        /// Creates a new Tlv given a tag and a value.
        /// </summary>
        public TlvObject(int tag, ReadOnlySpan<byte> value)
        {
            if (tag < 0 || tag > 0xFFFF)
            {
                throw new TlvException(ExceptionMessages.TlvUnsupportedTag);
            }

            Tag = tag;
            byte[] valueBuffer = value.ToArray();
            using var ms = new MemoryStream();

            byte[] tagBytes = BitConverter.GetBytes(tag).Reverse().SkipWhile(b => b == 0).ToArray();
            ms.Write(tagBytes, 0, tagBytes.Length);

            Length = valueBuffer.Length;
            if (Length < 0x80)
            {
                ms.WriteByte((byte)Length);
            }
            else
            {
                byte[] lnBytes = BitConverter.GetBytes(Length).Reverse().SkipWhile(b => b == 0).ToArray();
                ms.WriteByte((byte)(0x80 | lnBytes.Length));
                ms.Write(lnBytes, 0, lnBytes.Length);
            }

            _offset = (int)ms.Position;

            ms.Write(valueBuffer, 0, Length);

            _bytes = ms.ToArray();
        }

        /// <summary>
        /// Returns a copy ofthe Tlv as a BER-TLV encoded byte array.
        /// </summary>
        public Memory<byte> GetBytes() => _bytes.ToArray();

        /// <summary>
        /// Parse a Tlv from a BER-TLV encoded byte array.
        /// </summary>
        /// <param name="data">A byte array containing the TLV encoded data (and nothing more).</param>
        /// <returns>The parsed Tlv</returns>
        public static TlvObject Parse(ReadOnlySpan<byte> data)
        {
            ReadOnlySpan<byte> buffer = data;
            return ParseFrom(ref buffer);
        }

        /// <summary>
        /// Parses a TLV from a BER-TLV encoded byte array.
        /// </summary>
        /// <param name="buffer">A byte array containing the TLV encoded data.</param>
        /// <returns>The parsed <see cref="TlvObject"/></returns>
        /// <remarks>
        /// This method will parse a TLV from the given buffer and return the parsed Tlv.
        /// The method will consume the buffer as much as necessary to parse the TLV.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the buffer does not contain a valid TLV.</exception>
        internal static TlvObject ParseFrom(ref ReadOnlySpan<byte> buffer)
        {
            // The first byte of the TLV is the tag.
            int tag = buffer[0];

            // Determine if the tag is in long form.
            // Long form tags have more than one byte, starting with 0x1F.
            if ((buffer[0] & 0x1F) == 0x1F)
            {
                // Ensure there is enough data for a long form tag.
                if (buffer.Length < 2)
                {
                    throw new ArgumentException("Insufficient data for long form tag");
                }
                // Combine the first two bytes to form the tag.
                tag = (buffer[0] << 8) | buffer[1];
                buffer = buffer[2..]; // Skip the tag bytes
            }
            else
            {
                buffer = buffer[1..]; // Skip the tag byte
            }

            if (buffer.Length < 1)
            {
                throw new ArgumentException("Insufficient data for length");
            }

            // Read the length of the TLV value.
            int length = buffer[0];
            buffer = buffer[1..];

            // If the length is more than one byte, process remaining bytes.
            if (length > 0x80)
            {
                int lengthLn = length - 0x80;
                length = 0;
                for (int i = 0; i < lengthLn; i++)
                {
                    length = (length << 8) | buffer[0];
                    buffer = buffer[1..];
                }
            }

            ReadOnlySpan<byte> value = buffer[..length];
            buffer = buffer[length..]; // Advance the buffer to the end of the value

            return new TlvObject(tag, value);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>
        /// The string is of the form <c>Tlv(0xTAG, LENGTH, VALUE)</c>.
        /// <para>
        /// The tag is written out in hexadecimal, prefixed by 0x.
        /// The length is written out in decimal.
        /// The value is written out in hexadecimal.
        /// </para>
        /// </remarks>
        public override string ToString()
        {
#if NETSTANDARD2_1_OR_GREATER
            return $"Tlv(0x{Tag:X}, {Length}, {BitConverter.ToString(Value.ToArray()).Replace("-", "", StringComparison.Ordinal)})";
#else
            return $"Tlv(0x{Tag:X}, {Length}, {BitConverter.ToString(Value.ToArray()).Replace("-", "")})";
#endif
        }
    }
}
