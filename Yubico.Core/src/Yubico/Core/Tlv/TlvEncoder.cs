// Copyright 2021 Yubico AB
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
using System.Collections.Generic;

namespace Yubico.Core.Tlv
{
    /// <summary>
    /// An interface for representing a class that can encode itself into a TLV:
    /// tag || length || value
    /// </summary>
    /// <remarks>
    /// Classes that implement this interface can be called upon to build a TLV
    /// based on the tag and value loaded. The length is encoded following the
    /// DER rules. E.g. decimal 32 is encoded as 0x20, 128 is 0x81 80, and so on.
    /// <para>
    /// It is possible a class that implements this will be a single TLV, or a
    /// nested construction, namely TL { TLV, TLV, ..., TLV }.
    /// </para>
    /// </remarks>
    internal abstract class TlvEncoder
    {
        private const int MaximumTag = 0x0000FFFF;
        private const int MaximumLength = 0x00FFFFFF;
        // The longest TL we support would be a two-byte tag with a length that
        // requires 4 bytes (83 plus three bytes). Something like this:
        //   5F 50 83 01 00 01
        // So the maximum length of the length is 4 and the
        // maximum length of the tag + length is 6.
        private const int MaximumLengthByteCount = 4;
        private const int MaximumTagLengthLength = 6;

        /// <summary>
        /// How long will the encoding of this element or NestedTlv be?
        /// </summary>
        public abstract int EncodedLength { get; }

        /// <summary>
        /// Build a buffer that holds the tag and length.
        /// </summary>
        /// <remarks>
        /// The tag might be one or two bytes, the length might be one byte, it
        /// might be 81 xx, 82 xx xx, and so on.
        /// <para>
        /// This method will verify the tag is supported (any two-byte tag is
        /// supported, so any value &gt;=0 and &lt;= 0x0000FFFF). It will also verify
        /// the length is supported (any length &gt;=0 and &lt;= 0x00FFFFFF).
        /// </para>
        /// </remarks>
        /// <param name="tag">
        /// The tag to write out.
        /// </param>
        /// <param name="length">
        /// The length to write out.
        /// </param>
        /// <returns>
        /// A new byte array containing the tag and length.
        /// </returns>
        /// <exception cref="TlvException">
        /// The tag or length is unsupported.
        /// </exception>
        public static byte[] BuildTagAndLength(int tag, int length)
        {
            VerifyTag(tag);
            VerifyLength(length);
            byte[] encoding = new byte[MaximumTagLengthLength];

            int index = 0;
            if (tag > 0xFF)
            {
                encoding[index] = unchecked((byte)(tag >> 8));
                index++;
            }
            encoding[index] = (byte)tag;
            index++;

            byte[] fullLength = new byte[] { 0x83, 0x82, 0x81, (byte)length };
            int count = 1;
            if (length > 0x7F)
            {
                count++;
                if ((length & 0x00FFFF00) != 0)
                {
                    count++;
                    fullLength[2] = (byte)((length & 0x0000FF00) >> 8);

                    if ((length & 0x00FF0000) != 0)
                    {
                        count++;
                        fullLength[1] = (byte)((length & 0x00FF0000) >> 16);
                    }
                }
            }

            Array.Copy(fullLength, MaximumLengthByteCount - count, encoding, index, count);
            Array.Resize(ref encoding, count + index);

            return encoding;
        }

        /// <summary>
        /// Verify that the tag is valid, that it is a tag the TLV code supports.
        /// </summary>
        /// <remarks>
        /// If the tag is valid, the method returns. If not, it throws an
        /// exception.
        /// <para>
        /// Currently supported are one- and two-byte tags, so any input tag that
        /// is &gt;=0 and &lt;= 0x0000FFFF will be valid.
        /// </para>
        /// </remarks>
        /// <param name="tag">
        /// The tag to verify.
        /// </param>
        public static void VerifyTag(int tag)
        {
            if (tag < 0 || tag > MaximumTag)
            {
                throw new TlvException(ExceptionMessages.TlvUnsupportedTag);
            }
        }

        /// <summary>
        /// Verify that the length is valid, that it is a length the TLV code
        /// supports.
        /// </summary>
        /// <remarks>
        /// If the length is valid, the method returns. If not, it throws an
        /// exception.
        /// <para>
        /// Currently supported are lengths from 0 to 0x00FFFFFF.
        /// </para>
        /// </remarks>
        /// <param name="length">
        /// The length to verify.
        /// </param>
        public static void VerifyLength(int length)
        {
            if (length < 0 || length > MaximumLength)
            {
                throw new TlvException(ExceptionMessages.TlvUnsupportedLengthField);
            }
        }

        /// <summary>
        /// Place the TLV of this element into the given <c>Span</c>, beginning
        /// at offset.
        /// </summary>
        /// <remarks>
        /// If the size of the Span is not big enough to hold the TLV, it will
        /// return false and bytesWritten will be set to 0.
        /// </remarks>
        /// <param name="encoding">
        /// The buffer into which the TLV will be placed.
        /// </param>
        /// <param name="offset">
        /// The offset into encoding where the method will begin placing the TLV.
        /// </param>
        /// <param name="bytesWritten">
        /// On success, receives the number of bytes written into the encoding.
        /// </param>
        /// <returns>
        /// A bool, true if the method successfully encoded, false otherwise.
        /// </returns>
        public abstract bool TryEncode(Span<byte> encoding, int offset, out int bytesWritten);

        /// <summary>
        /// Clear any data that had been copied from input.
        /// </summary>
        /// <remarks>
        /// If any of the data to encode had been sensitive (such as private key
        /// material), then call the Clear method after encoding to make sure it
        /// is overwritten.
        /// </remarks>
        public abstract void Clear();
    }
}
