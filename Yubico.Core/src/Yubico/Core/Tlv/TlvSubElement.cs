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
using CryptographicOperations = Yubico.Core.Cryptography.CryptographicOperations;

namespace Yubico.Core.Tlv
{
    /// <summary>
    /// A single Tag-Length-Value entry.
    /// </summary>
    internal class TlvSubElement : TlvEncoder
    {
        private readonly int _encodedLength;

        /// <inheritdoc/>
        override public int EncodedLength => _encodedLength;

        private readonly byte[] _tagAndLength;
        private readonly byte[] _value;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private TlvSubElement()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new TLV object that will be able to encode itself with the
        /// given tag and value.
        /// </summary>
        /// <remarks>
        /// This class supports one- or two-byte tags, an input tag of a negative
        /// number or a number greater than 0x0000FFFF will result in an
        /// exception.
        /// <para>
        /// The value that will be written out is the byte array provided
        /// (essentially an Array.Copy). It is the responsibility of the caller
        /// to format the value into an appropriate byte array if necessary.
        /// </para>
        /// <para>
        /// If there is no data, pass an empty <c>Span</c>:
        /// <c>ReadOnlySpan&lt;byte&gt;.Empty</c>. In that case, what is written
        /// out is simply tag 00.
        /// </para>
        /// </remarks>
        /// <param name="tag">
        /// The tag that will be written out.
        /// </param>
        /// <param name="value">
        /// The value to write out.
        /// </param>
        /// <exception cref="TlvException">
        /// The tag is invalid, or the length is unsupported.
        /// </exception>
        public TlvSubElement(int tag, ReadOnlySpan<byte> value)
        {
            _tagAndLength = BuildTagAndLength(tag, value.Length);
            _value = value.ToArray();

            _encodedLength = _tagAndLength.Length + _value.Length;
        }

        /// <summary>
        /// Create a new TLV object with the given encoded byte array.
        /// </summary>
        /// <remarks>
        /// If an object is created using this constructor,
        /// the data given is assumed to be a full TLV encoded element.
        /// Whatever is passed in will be written out as the encoding.
        /// The class will not verify the tag or length.
        /// </remarks>
        /// <param name="encodedTlv">
        /// The encoded byte array that will be written out.
        /// </param>
        public TlvSubElement(ReadOnlySpan<byte> encodedTlv)
        {
            _tagAndLength = Array.Empty<byte>();
            _value = encodedTlv.ToArray();

            _encodedLength = _value.Length;
        }

        /// <inheritdoc/>
        override public bool TryEncode(Span<byte> encoding, int offset, out int bytesWritten)
        {
            bytesWritten = 0;
            if (encoding.Length < offset + _encodedLength)
            {
                return false;
            }

            Span<byte> destination = encoding[offset..];
            _tagAndLength.AsSpan<byte>().CopyTo(destination);

            destination = encoding[(offset + _tagAndLength.Length)..];
            _value.AsSpan<byte>().CopyTo(destination);

            bytesWritten = _encodedLength;
            return true;
        }

        /// <inheritdoc />
        override public void Clear() => CryptographicOperations.ZeroMemory(_value);
    }
}
