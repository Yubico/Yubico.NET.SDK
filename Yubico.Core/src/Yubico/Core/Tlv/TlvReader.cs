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
using System.Buffers.Binary;
using System.Text;

namespace Yubico.Core.Tlv
{
    /// <summary>
    /// Use this class to parse TLV (tag-length-value) constructions.
    /// </summary>
    /// <remarks>
    /// See the User's Manual entry on
    /// <xref href="UsersManualSupportTlv"> TLV</xref> for an in-depth discussion
    /// of what TLV is and a general description of how to use this class.
    /// </remarks>
    public sealed class TlvReader
    {
        private const int MaximumTagLength = 2;
        private const int MaximumLengthCount = 3;
        private const int NoFixedLength = 0;
        private const int FixedLengthByte = 1;
        private const int FixedLengthInt16 = 2;
        private const int FixedLengthInt32 = 4;
        private const int ValidEncoding = 1;
        private const int UnsupportedTag = 2;
        private const int UnsupportedLength = 4;
        private const int UnexpectedEncoding = 8;
        private const int UnexpectedEnd = 256;

        private readonly ReadOnlyMemory<byte> _encoding;
        private int _currentOffset;
        private int _currentTag;
        private int _currentTagLength;
        private int _currentLength;
        private int _currentLengthOfLength;
        private int _currentValueOffset;

        /// <summary>
        /// Indicates whether there is more data to read or not.
        /// </summary>
        public bool HasData => _currentOffset < _encoding.Length;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private TlvReader()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new Reader object based on the given encoding.
        /// </summary>
        /// <remarks>
        /// This sets the position of the reader to the leading byte, the first
        /// tag.
        /// <para>
        /// Note that the object will copy a reference to the encoding. Do not
        /// clear or alter the encoding until after the full encoding has been
        /// read and each value operated on.
        /// </para>
        /// </remarks>
        /// <param name="encoding">
        /// The TLV encoding to read.
        /// </param>
        public TlvReader(ReadOnlyMemory<byte> encoding)
        {
            _encoding = encoding;
        }

        // Reset the internal state of this object to indicate we have not read
        // the current element.
        private void ResetState()
        {
            _currentTagLength = 0;
            _currentLengthOfLength = 0;
        }

        /// <summary>
        /// Read the TLV at the current position as a NestedTlv. Return a new
        /// TlvReader object whose position is the beginning of the NestedTlv's
        /// value, which is the tag of the NestedTlv's first sub-element. Move
        /// the position of the original reader to the byte beyond the current
        /// TLV.
        /// </summary>
        /// <remarks>
        /// The new object returned will contain the encoding of the sub-elements
        /// only.
        /// <para>
        /// If the tag at the current position in the encoding is not what was
        /// given as the expectedTag, or if the tag and/or length make up an
        /// invalid encoding, or if there are not enough bytes left in the
        /// encoding to read the tag, length, and value, this method will throw
        /// an exception.
        /// </para>
        /// <para>
        /// For example, suppose the encoding is the following.
        /// <code>
        ///    7C 0D 01 01 14 02 02 01 80 05 04 00 89 2C 33
        ///    This is a NestedTlv
        ///    7C 0D
        ///       01 01
        ///          14
        ///       02 02
        ///          01 80
        ///       05 04
        ///          00 89 2C 33
        /// </code>
        /// Suppose the internal position is at 0, the beginning of full encoding.
        /// <code>
        ///   TlvReader newReader = reader.ReadNestedTlv(0x7C);
        ///    This will return a new TlvReader object:
        ///     new reader: 01 01 14 02 02 01 80 05 04 00 89 2C 33
        ///                 ^
        ///                 +--current position
        ///    After the call, the internal position of the original reader is at
        ///    the position just after the NestedTlv's value.
        ///     original: 7C 0D 01 01 14 02 02 01 80 05 04 00 89 2C 33
        ///                                                            ^
        ///                                                            +--current position
        /// </code>
        /// Suppose the original reader is parsing something like this:
        /// <code>
        ///    01 01 14 7A 07 31 02 01 00 32 01 20 05 04 00 89 2C 33
        ///  This is a concatenation with a NestedTlv as one of the elements.
        ///       01 01
        ///          14
        ///       7A 07
        ///          31 02
        ///             01 00
        ///          32 01
        ///             20
        ///       05 04
        ///          00 89 2C 33
        /// </code>
        /// Suppose the internal position is at 0, the beginning of full encoding.
        /// <code>
        ///   ReadOnlyMemory&lt;byte&gt; value = reader.ReadValue(0x01);
        ///    This returned a new ReadOnlyMemory object with the contents of the
        ///    first TLV, namely the byte array { 0x14 }. It moved the pointer to
        ///    the next TLV.
        ///    01 01 14 7A 07 31 02 01 00 32 01 20 05 04 00 89 2C 33
        ///             ^
        ///             +--current position
        ///   TlvReader newReader = reader.ReadNestedTlv(0x7A);
        ///    This will return a new TlvReader object:
        ///     new reader: 31 02 01 00 32 01 20
        ///                 ^
        ///                 +--current position
        ///    After the call, the internal position of the original reader is at
        ///    the position just after the NestedTlv's value.
        ///    01 01 14 7A 07 31 02 01 00 32 01 20 05 04 00 89 2C 33
        ///                                        ^
        ///                                        +--current position
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="expectedTag">
        /// The tag that should be at the current position, the NestedTlv's tag.
        /// </param>
        /// <returns>
        /// A new TlvReader object that contains the sub-elements of the
        /// NestedTlv, with the position set at the first sub-element.
        /// </returns>
        /// <exception cref="TlvException">
        /// The tag was not the expected value, the tag or length is unsupported,
        /// or there was not enough data for the lengths given.
        /// </exception>
        public TlvReader ReadNestedTlv(int expectedTag)
        {
            _ = CommonReadValue(out ReadOnlyMemory<byte> value, expectedTag, NoFixedLength, true);

            return new TlvReader(value);
        }

        /// <summary>
        /// Try to read the TLV at the current position as a NestedTlv. If this
        /// succeeds, return true and set the <c>nestedReader</c> argument to a
        /// new <c>TlvReader</c> object whose position is the beginning of the
        /// NestedTlv's value, which is the tag of the NestedTlv's first
        /// sub-element. Move the position of the original reader to the byte
        /// beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>ReadNestedTlv</c>, except this method will not
        /// throw an exception if there is an error in reading, only return
        /// <c>false</c>. That is, if the expected tag is not found at the
        /// current position, or the length octets are not a valid encoding, or
        /// there is not enough data in the buffer for the length given, this
        /// method will return <c>false</c>.
        /// <para>
        /// See the documentation for <c>ReadNestedTlv</c> for more information
        /// on what this method does.
        /// </para>
        /// </remarks>
        /// <param name="nestedReader">
        /// On success, receives the new <c>TlvReader</c> object.
        /// </param>
        /// <param name="expectedTag">
        /// The tag that should be at the current position, the NestedTlv's tag.
        /// </param>
        public bool TryReadNestedTlv(out TlvReader nestedReader, int expectedTag)
        {
            bool returnValue = CommonReadValue(
                out ReadOnlyMemory<byte> value,
                expectedTag,
                NoFixedLength,
                false);

            nestedReader = new TlvReader(value);

            return returnValue;
        }

        /// <summary>
        /// Read the TLV at the current position, return the value as a byte
        /// array, and move the position to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// The method will verify that the tag is expected. If it is, it will
        /// read the length, verify that the encoding has at least that many
        /// bytes, then create a new ReadOnlyMemory object that points to the
        /// value. Note that the ReadOnlyMemory object will point to the existing
        /// encoding, it will not copy the data into a new buffer.
        /// <para>
        /// Note that this will not treat a NestedTlv any different from a single
        /// element. That is, if the current position points to a NestedTlv, the
        /// method will return a value that is the collection of sub-elements.
        /// </para>
        /// <para>
        /// If the tag at the current position in the encoding is not what was
        /// given as the expectedTag, or if the tag and/or length make up an
        /// invalid encoding, or if there are not enough bytes left in the
        /// encoding to read the tag, length, and value, this method will throw
        /// an exception.
        /// </para>
        /// <para>
        /// For example, suppose the encoding is the following.
        /// <code>
        ///    7C 0D 01 01 14 02 02 01 80 05 04 00 89 2C 33
        ///    This is a NestedTlv
        ///    7C 0D
        ///       01 01
        ///          14
        ///       02 02
        ///          01 80
        ///       05 04
        ///          00 89 2C 33
        /// </code>
        /// Suppose the internal position is at 0, the beginning of full encoding.
        /// <code>
        ///   value = reader.ReadValue(0x7C);
        ///   This will return a new ReadOnlyMemory object that points to
        ///     01 01 14 02 02 01 80 05 04 00 89 2C 33
        ///   Length is 13
        ///   After the call, the internal position of the reader is at the
        ///   position just after the last byte of the value, which is beyond the
        ///   end of the full encoding. There's nothing more to read.
        /// </code>
        /// Suppose the internal position is at 5, the beginning of the second
        /// sub-element.
        /// <code>
        ///   value = reader.ReadValue(0x02);
        ///   This will return a new ReadOnlyMemory object that points to
        ///     01 80
        ///   Length is 2
        ///   After the call, the internal position of the reader is at the
        ///   position just after the last byte of the value, which is the
        ///   next TLV: 05 04 etc.
        /// </code>
        /// </para>
        /// <para>
        /// Note that the value returned is a reference to the input encoding. Do
        /// not clear or alter the encoding until after the full encoding has been
        /// read and each value operated on.
        /// </para>
        /// </remarks>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <returns>
        /// A new ReadOnlyMemory object containing the value, and only the value,
        /// of the current TLV. If there is no value (length is 0), the result
        /// will be an empty object (Length is 0).
        /// </returns>
        /// <exception cref="TlvException">
        /// The tag was not the expected value, the tag or length is unsupported,
        /// or there was not enough data for the lengths given.
        /// </exception>
        public ReadOnlyMemory<byte> ReadValue(int expectedTag)
        {
            _ = CommonReadValue(out ReadOnlyMemory<byte> value, expectedTag, NoFixedLength, true);

            return value;
        }

        /// <summary>
        /// Try to read the TLV at the current position. If this succeeds, return
        /// true and set the <c>value</c> argument to a new <c>ReadOnlyMemory</c>
        /// object containing the value as a byte array, and move the position to
        /// the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>ReadValue</c>, except this method will not
        /// throw an exception if there is an error in reading, only return
        /// <c>false</c>. That is, if the expected tag is not found at the
        /// current position, or the length octets are not a valid encoding, or
        /// there is not enough data in the buffer for the length given, this
        /// method will return <c>false</c>.
        /// <para>
        /// See the documentation for <c>ReadValue</c> for more information
        /// on what this method does.
        /// </para>
        /// <para>
        /// Note that the value returned is a reference to the input encoding. Do
        /// not clear or alter the encoding until after the full encoding has been
        /// read and each value operated on.
        /// </para>
        /// </remarks>
        /// <param name="value">
        /// The output parameter where the value will be deposited.
        /// </param>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the read succeeds, <c>false</c> otherwise.
        /// </returns>
        public bool TryReadValue(out ReadOnlyMemory<byte> value, int expectedTag) =>
            CommonReadValue(out value, expectedTag, NoFixedLength, false);

        /// <summary>
        /// Read the TLV at the current position, return the value as a byte,
        /// and move the position to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// The method will verify that the tag is expected. If it is, it will
        /// read the length, verify that it is one, then read that one-byte
        /// value, and return it.
        /// <para>
        /// If the length of the value is not 1 (even if it is 0), this method
        /// will not advance the reader and throw an exception.
        /// </para>
        /// <para>
        /// If the tag at the current position in the encoding is not what was
        /// given as the expectedTag, or if the tag and/or length make up an
        /// invalid encoding, or if there are not enough bytes left in the encoding to
        /// read the tag, length, and value, this method will not advance the
        /// reader and throw an exception.
        /// </para>
        /// </remarks>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <returns>
        /// A byte, the value.
        /// </returns>
        /// <exception cref="TlvException">
        /// The tag was not the expected value, the tag or length is unsupported,
        /// or there was not enough data for the lengths given.
        /// </exception>
        public byte ReadByte(int expectedTag)
        {
            _ = CommonReadValue(out ReadOnlyMemory<byte> value, expectedTag, FixedLengthByte, true);

            return value.Span[0];
        }

        /// <summary>
        /// Try to read the TLV at the current position. If this succeeds, return
        /// true and set the <c>value</c> argument to the byte that is the V part
        /// of the TLV, and move the position to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>ReadByte</c>, except this method will not
        /// throw an exception if there is an error in reading, only return
        /// <c>false</c>. That is, if the expected tag is not found at the
        /// current position, or the length octets are not a valid encoding, or
        /// the value is not a single byte, or there is not enough data in the
        /// buffer for the length given, this method will set <c>value</c> to 0
        /// and will return <c>false</c>.
        /// <para>
        /// See the documentation for <c>ReadByte</c> for more information
        /// on what this method does.
        /// </para>
        /// <para>
        /// Note that if there is a valid TLV with the expected tag, but the
        /// length is not 1, this method will return <c>false</c>.
        /// </para>
        /// </remarks>
        /// <param name="value">
        /// The output parameter where the value will be deposited.
        /// </param>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the read succeeds, <c>false</c> otherwise.
        /// </returns>
        public bool TryReadByte(out byte value, int expectedTag)
        {
            value = 0;
            bool isValid = CommonReadValue(
                out ReadOnlyMemory<byte> fullValue,
                expectedTag,
                FixedLengthByte,
                false);

            if (isValid)
            {
                value = fullValue.Span[0];
            }

            return isValid;
        }

        /// <summary>
        /// Read the TLV at the current position, return the value as a short (a
        /// two-byte integer), and move the position to the byte beyond the
        /// current TLV.
        /// </summary>
        /// <remarks>
        /// The method will verify that the tag is expected. If it is, it will
        /// read the length, verify that it is two, then read that two-byte
        /// value, and return it as a short.
        /// <para>
        /// If the length of the value is not 2 (even if it is 0 or 1), this
        /// method will not advance the reader and throw an exception.
        /// </para>
        /// <para>
        /// The method will return the value in big endian (most significant byte
        /// of the short is taken from value[0]), unless the bigEndian argument
        /// is false. If it is false, the result is returned in little endian
        /// (least significant byte of the short is taken from value[0]). The
        /// bigEndian argument has a default of true. This means you can call
        /// this method and leave out the bigEndian argument, and the method will
        /// return the short in big endian format.
        /// </para>
        /// <para>
        /// If the tag at the current position in the encoding is not what was
        /// given as the expectedTag, or if the tag and/or length make up an
        /// invalid encoding, or if there are not enough bytes left in the encoding to
        /// read the tag, length, and value, this method will not advance the
        /// reader and throw an exception.
        /// </para>
        /// </remarks>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <param name="bigEndian">
        /// Specifies whether the result should be returned as big endian (true
        /// or no argument given) or little endian (false).
        /// </param>
        /// <returns>
        /// A short, the value.
        /// </returns>
        /// <exception cref="TlvException">
        /// The tag was not the expected value, the tag or length is unsupported,
        /// or there was not enough data for the lengths given.
        /// </exception>
        public short ReadInt16(int expectedTag, bool bigEndian = true)
        {
            _ = CommonReadValue(out ReadOnlyMemory<byte> value, expectedTag, FixedLengthInt16, true);

            if (bigEndian)
            {
                return BinaryPrimitives.ReadInt16BigEndian(value.Span);
            }
            else
            {
                return BinaryPrimitives.ReadInt16LittleEndian(value.Span);
            }
        }

        /// <summary>
        /// Try to read the TLV at the current position. If this succeeds, return
        /// true and set the <c>value</c> argument to the short that is the V part
        /// of the TLV, and move the position to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>ReadInt16</c>, except this method will not
        /// throw an exception if there is an error in reading, only return
        /// <c>false</c>. That is, if the expected tag is not found at the
        /// current position, or the length octets are not a valid encoding, or
        /// the value is not exactly two bytes, or there is not enough data in
        /// the buffer for the length given, this method will set <c>value</c> to
        /// 0 and will return <c>false</c>.
        /// <para>
        /// See the documentation for <c>ReadInt16</c> for more information
        /// on what this method does.
        /// </para>
        /// <para>
        /// Note that if there is a valid TLV with the expected tag, but the
        /// length is not 2, this method will return <c>false</c>.
        /// </para>
        /// </remarks>
        /// <param name="value">
        /// The output parameter where the value will be deposited.
        /// </param>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <param name="bigEndian">
        /// Specifies whether the result should be returned as big endian (true
        /// or no argument given) or little endian (false).
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the read succeeds, <c>false</c> otherwise.
        /// </returns>
        public bool TryReadInt16(out short value, int expectedTag, bool bigEndian = true)
        {
            value = 0;
            bool isValid = CommonReadValue(
                out ReadOnlyMemory<byte> fullValue,
                expectedTag,
                FixedLengthInt16,
                false);

            if (isValid)
            {
                if (bigEndian)
                {
                    value = BinaryPrimitives.ReadInt16BigEndian(fullValue.Span);
                }
                else
                {
                    value = BinaryPrimitives.ReadInt16LittleEndian(fullValue.Span);
                }
            }

            return isValid;
        }

        /// <summary>
        /// Read the TLV at the current position, return the value as an
        /// unsigned short (a two-byte unsigned integer), and move the position
        /// to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// The method will verify that the tag is expected. If it is, it will
        /// read the length, verify that it is two, then read and return that two-byte
        /// unsigned value.
        /// <para>
        /// If the length of the value is not 2 (even if it is 0 or 1), this
        /// method will not advance the reader and throw an exception.
        /// </para>
        /// <para>
        /// The method will return the value in big endian (most significant byte
        /// of the short is taken from value[0]), unless the bigEndian argument
        /// is false. If it is false, the result is returned in little endian
        /// (least significant byte of the short is taken from value[0]). The
        /// bigEndian argument has a default of true. This means you can call
        /// this method and leave out the bigEndian argument, and the method will
        /// return the unsigned short in big endian format.
        /// </para>
        /// <para>
        /// If the tag at the current position in the encoding is not what was
        /// given as the expectedTag, or if the tag and/or length make up an
        /// invalid encoding, or if there are not enough bytes left in the encoding to
        /// read the tag, length, and value, this method will not advance the
        /// reader and throw an exception.
        /// </para>
        /// </remarks>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <param name="bigEndian">
        /// Specifies whether the result should be returned as big endian (true
        /// or no argument given) or little endian (false).
        /// </param>
        /// <returns>
        /// The unsigned short value.
        /// </returns>
        /// <exception cref="TlvException">
        /// An unexpected error occurred while reading from the TLV.
        /// </exception>
        [CLSCompliant(false)]
        public ushort ReadUInt16(int expectedTag, bool bigEndian = true)
        {
            _ = CommonReadValue(out ReadOnlyMemory<byte> value, expectedTag, FixedLengthInt16, true);

            if (bigEndian)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(value.Span);
            }
            else
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(value.Span);
            }
        }

        /// <summary>
        /// Try to read the TLV at the current position. If this succeeds, return
        /// true and set the <c>value</c> argument to the unsigned short that is the
        /// V part of the TLV, and move the position to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// This is the same as <see cref="ReadUInt16(int, bool)"/>, except this method
        /// will not throw an exception if there is an error in reading, only return
        /// <c>false</c>. That is, if the expected tag is not found at the
        /// current position, or the length octets are not a valid encoding, or
        /// the value is not exactly two bytes, or there is not enough data in
        /// the buffer for the length given, this method will set <c>value</c> to
        /// 0 and will return <c>false</c>.
        /// <para>
        /// See <see cref="ReadUInt16(int, bool)"/> for more information
        /// on what this method does.
        /// </para>
        /// <para>
        /// Note that if there is a valid TLV with the expected tag, but the
        /// length is not 2, this method will return <c>false</c>.
        /// </para>
        /// </remarks>
        /// <param name="value">
        /// The output parameter where the value will be deposited.
        /// </param>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <param name="bigEndian">
        /// Specifies whether the result should be returned as big endian (true
        /// or no argument given) or little endian (false).
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the read succeeds, <c>false</c> otherwise.
        /// </returns>
        [CLSCompliant(false)]
        public bool TryReadUInt16(out ushort value, int expectedTag, bool bigEndian = true)
        {
            value = 0;
            bool isValid = CommonReadValue(
                out ReadOnlyMemory<byte> fullValue,
                expectedTag,
                FixedLengthInt16,
                false);

            if (isValid)
            {
                if (bigEndian)
                {
                    value = BinaryPrimitives.ReadUInt16BigEndian(fullValue.Span);
                }
                else
                {
                    value = BinaryPrimitives.ReadUInt16LittleEndian(fullValue.Span);
                }
            }

            return isValid;
        }

        /// <summary>
        /// Read the TLV at the current position, return the value as an int (a
        /// four-byte integer), and move the position to the byte beyond the
        /// current TLV.
        /// </summary>
        /// <remarks>
        /// The method will verify that the tag is expected. If it is, it will
        /// read the length, verify that it is four, then read that four-byte
        /// value, and return it as an int.
        /// <para>
        /// If the length of the value is not 4 (even if it is 0, 1, 2, or 3),
        /// this method will not advance the reader and throw an exception.
        /// </para>
        /// <para>
        /// The method will return the value in big endian (most significant byte
        /// of the int is taken from value[0]), unless the bigEndian argument
        /// is false. If it is false, the result is returned in little endian
        /// (least significant byte of the int is taken from value[0]). The
        /// bigEndian argument has a default of true. This means you can call
        /// this method and leave out the bigEndian argument, and the method will
        /// return the int in big endian format.
        /// </para>
        /// <para>
        /// If the tag at the current position in the encoding is not what was
        /// given as the expectedTag, or if the tag and/or length make up an
        /// invalid encoding, or if there are not enough bytes left in the encoding to
        /// read the tag, length, and value, this method will not advance the
        /// reader and throw an exception.
        /// </para>
        /// </remarks>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <param name="bigEndian">
        /// Specifies whether the result should be returned as big endian (true
        /// or no argument given) or little endian (false).
        /// </param>
        /// <returns>
        /// An int, the value.
        /// </returns>
        /// <exception cref="TlvException">
        /// The tag was not the expected value, the tag or length is unsupported,
        /// or there was not enough data for the lengths given.
        /// </exception>
        public int ReadInt32(int expectedTag, bool bigEndian = true)
        {
            _ = CommonReadValue(out ReadOnlyMemory<byte> value, expectedTag, FixedLengthInt32, true);

            if (bigEndian)
            {
                return BinaryPrimitives.ReadInt32BigEndian(value.Span);
            }
            else
            {
                return BinaryPrimitives.ReadInt32LittleEndian(value.Span);
            }
        }

        /// <summary>
        /// Try to read the TLV at the current position. If this succeeds, return
        /// true and set the <c>value</c> argument to the int that is the V part
        /// of the TLV, and move the position to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>ReadInt32</c>, except this method will not
        /// throw an exception if there is an error in reading, only return
        /// <c>false</c>. That is, if the expected tag is not found at the
        /// current position, or the length octets are not a valid encoding, or
        /// the value is not exactly four bytes, or there is not enough data in
        /// the buffer for the length given, this method will set <c>value</c> to
        /// 0 and will return <c>false</c>.
        /// <para>
        /// See the documentation for <c>ReadInt32</c> for more information
        /// on what this method does.
        /// </para>
        /// <para>
        /// Note that if there is a valid TLV with the expected tag, but the
        /// length is not 4, this method will return <c>false</c>.
        /// </para>
        /// </remarks>
        /// <param name="value">
        /// The output parameter where the value will be deposited.
        /// </param>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <param name="bigEndian">
        /// Specifies whether the result should be returned as big endian (true
        /// or no argument given) or little endian (false).
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the read succeeds, <c>false</c> otherwise.
        /// </returns>
        public bool TryReadInt32(out int value, int expectedTag, bool bigEndian = true)
        {
            value = 0;
            bool isValid = CommonReadValue(
                out ReadOnlyMemory<byte> fullValue,
                expectedTag,
                FixedLengthInt32,
                false);

            if (isValid)
            {
                if (bigEndian)
                {
                    value = BinaryPrimitives.ReadInt32BigEndian(fullValue.Span);
                }
                else
                {
                    value = BinaryPrimitives.ReadInt32LittleEndian(fullValue.Span);
                }
            }

            return isValid;
        }

        /// <summary>
        /// Read the TLV at the current position, return the value as a string,
        /// and move the position to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// See the documentation for the method TlvWriter.WriteString for a
        /// discussion of strings and encodings.
        /// <para>
        /// The method will verify that the tag is expected. If it is, it will
        /// read the length, verify that there are enough bytes in the buffer to
        /// read, then read the value (a byte array) and returning it as a string,
        /// converting the bytes array following the scheme specified by the
        /// encoding argument.
        /// </para>
        /// </remarks>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <param name="encoding">
        /// The scheme the method will use to convert the byte array into a
        /// string, such as System.Text.Encoding.ASCII or UTF8.
        /// </param>
        /// <returns>
        /// A string, the value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The encoding argument is null.
        /// </exception>
        /// <exception cref="TlvException">
        /// The tag was not the expected value, the tag or length is unsupported,
        /// or there was not enough data for the lengths given.
        /// </exception>
        public string ReadString(int expectedTag, Encoding encoding)
        {
            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            _ = CommonReadValue(out ReadOnlyMemory<byte> value, expectedTag, NoFixedLength, true);

            return encoding.GetString(value.ToArray());
        }

        /// <summary>
        /// Try to read the TLV at the current position. If this succeeds, return
        /// true and set the <c>value</c> argument to a <c>string</c>, and move
        /// the position to the byte beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>ReadString</c>, except this method will not
        /// throw an exception if there is an error in reading, only return
        /// <c>false</c>. That is, if the expected tag is not found at the
        /// current position, or the length octets are not a valid encoding, or
        /// there is not enough data in the buffer for the length given, this
        /// method will return <c>false</c>.
        /// <para>
        /// See the documentation for <c>ReadString</c> for more information
        /// on what this method does.
        /// </para>
        /// </remarks>
        /// <param name="value">
        /// The output parameter where the value will be deposited.
        /// </param>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <param name="encoding">
        /// The scheme the method will use to convert the byte array into a
        /// string, such as System.Text.Encoding.ASCII or UTF8.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the read succeeds, <c>false</c> otherwise.
        /// </returns>
        public bool TryReadString(out string value, int expectedTag, Encoding encoding)
        {
            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            bool isValid = CommonReadValue(
                out ReadOnlyMemory<byte> fullValue,
                expectedTag,
                NoFixedLength,
                false);

            value = encoding.GetString(fullValue.ToArray());

            return isValid;
        }

        /// <summary>
        /// Return the entire encoding of the next element.
        /// </summary>
        /// <remarks>
        /// The method will verify that the tag is expected. If it is, it will
        /// read the length, verify that the encoding has at least that many
        /// bytes, then create a new ReadOnlyMemory object that points to the
        /// entire TLV (not just the value). Note that the ReadOnlyMemory object
        /// will point to the existing encoding, it will not copy the data into a
        /// new buffer.
        /// <para>
        /// Note that this will not treat a NestedTlv any different from a single
        /// element. That is, if the current position points to a NestedTlv, the
        /// method will return the entire encoding, including the sub-elements.
        /// </para>
        /// <para>
        /// If the tag at the current position in the encoding is not what was
        /// given as the expectedTag, or if the tag and/or length make up an
        /// invalid encoding, or if there are not enough bytes left in the
        /// encoding to read the tag, length, and value, this method will throw
        /// an exception.
        /// </para>
        /// <para>
        /// For example, suppose the encoding is the following.
        /// <code>
        ///    7C 0D 01 01 14 02 02 01 80 05 04 00 89 2C 33
        ///    This is a NestedTlv
        ///    7C 0D
        ///       01 01
        ///          14
        ///       02 02
        ///          01 80
        ///       05 04
        ///          00 89 2C 33
        /// </code>
        /// Suppose the internal position is at 0, the beginning of full encoding.
        /// <code>
        ///   encoded = reader.ReadEncoded(0x7C);
        ///   This will return a new ReadOnlyMemory object that points to
        ///     7C 0D 01 01 14 02 02 01 80 05 04 00 89 2C 33
        ///   Length is 15
        ///   After the call, the internal position of the reader is at the
        ///   position just after the last byte of the value, which is beyond the
        ///   end of the full encoding.
        /// </code>
        /// Suppose the internal position is at 5, the beginning of the second
        /// sub-element.
        /// <code>
        ///   value = reader.ReadEncoded(0x02);
        ///   This will return a new ReadOnlyMemory object that points to
        ///     02 02 01 80
        ///   Length is 4
        ///   After the call, the internal position of the reader is at the
        ///   position just after the last byte of the value, which is the
        ///   next TLV: 05 04 etc.
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <returns>
        /// A new ReadOnlyMemory object containing the tag, length and value of
        /// the current TLV.
        /// </returns>
        /// <exception cref="TlvException">
        /// The tag was not the expected value, the tag or length is unsupported,
        /// or there was not enough data for the lengths given.
        /// </exception>
        public ReadOnlyMemory<byte> ReadEncoded(int expectedTag)
        {
            int startOffset = _currentOffset;
            _ = CommonReadValue(out _, expectedTag, NoFixedLength, true);

            return _encoding[startOffset.._currentOffset];
        }

        /// <summary>
        /// Try to read the TLV at the current position. If this succeeds, return
        /// true and set the <c>encoded</c> argument to a new <c>ReadOnlyMemory</c>
        /// object containing the full TLV, and move the position to the byte
        /// beyond the current TLV.
        /// </summary>
        /// <remarks>
        /// This is the same as <c>ReadEncoded</c>, except this method will not
        /// throw an exception if there is an error in reading, only return
        /// <c>false</c>. That is, if the expected tag is not found at the
        /// current position, or the length octets are not a valid encoding, or
        /// there is not enough data in the buffer for the length given, this
        /// method will return <c>false</c>.
        /// <para>
        /// See the documentation for <c>ReadEncoded</c> for more information
        /// on what this method does.
        /// </para>
        /// </remarks>
        /// <param name="encoded">
        /// The output parameter where the encoded TLV will be deposited.
        /// </param>
        /// <param name="expectedTag">
        /// The tag that should be at the current position.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the read succeeds, <c>false</c> otherwise.
        /// </returns>
        public bool TryReadEncoded(out ReadOnlyMemory<byte> encoded, int expectedTag)
        {
            encoded = ReadOnlyMemory<byte>.Empty;
            int startOffset = _currentOffset;
            if (CommonReadValue(out _, expectedTag, NoFixedLength, false))
            {
                encoded = _encoding[startOffset.._currentOffset];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Read the tag at the current data position, without advancing the
        /// reader.
        /// </summary>
        /// <remarks>
        /// The caller passes in either no argument, or 1 or 2 as the tag length.
        /// No argument passed in means the default tag length of 1 will be used.
        /// If 1, the method returns the next byte as an int. If 2, the method
        /// returns the next two bytes as an int (e.g. 71 01 is returned as
        /// 0x00007101). If you pass in any other length other than 1 or 2, this
        /// method will throw an exception.
        /// <para>
        /// If there is no data at the current position (the entire encoding has
        /// been read), this method will throw an exception (check the HasData
        /// property to verify there is a tag available). If the <c>tagLength</c>
        /// is 2 and there is only one byte left in the encoding, this method throws
        /// an exception.
        /// </para>
        /// </remarks>
        /// <param name="tagLength">
        /// The length of the tag to read. If this argument is not given the
        /// default length of 1 will be used.
        /// </param>
        /// <returns>
        /// The tag as an int.
        /// </returns>
        /// <exception cref="TlvException">
        /// The <c>tagLength</c> is unsupported, or there was not enough data to read.
        /// </exception>
        public int PeekTag(int tagLength = 1)
        {
            int result = ReadTag(tagLength);
            if (result != ValidEncoding)
            {
                ThrowOnFailedRead(result);
            }

            return _currentTag;
        }

        /// <summary>
        /// Skip the <c>tagLength</c> bytes and read the length octets, decode and return
        /// the length without advancing the reader.
        /// </summary>
        /// <remarks>
        /// For example, if the next byte after the tag is 0x20, this method will
        /// return 0x00000020. If the next bytes are 0x81 80, this method will
        /// return 0x00000080. For 0x82 01 00, the return is 0x0000000100.
        /// <para>
        /// If the next byte after the tag is an invalid or unsupported initial
        /// length octet, the method will throw an exception. Invalid initial
        /// length octets are those where the most significant bit is set but
        /// the value is not 0x81, 0x82, or 0x83. Although 0x84, and 0x85 (and
        /// higher) are valid initial length octets, they are not supported by
        /// this class. Note that 0x80 is an unsupported initial length octet (it
        /// is BER for indefinite length, and this class supports only DER length
        /// rules).
        /// </para>
        /// <para>
        /// If there are not enough bytes left in the encoding to build the
        /// length, this method will throw an exception. For example, if there
        /// are only two bytes left in encoding, and you pass in 2 for the tag
        /// length, this method will throw an exception. If there are two bytes
        /// left in encoding, you pass in 1 as the <c>tagLength</c>, and the initial
        /// length octet is0x81 (which means there should be one length octet
        /// following the 0x81), the method will throw an exception.
        /// </para>
        /// <para>
        /// Note that this method does not verify if there are at least length
        /// octets available in the encoding after the tag and length, it only
        /// returns the length and the number of octets that make up the length.
        /// For example, if at the current pointer in the encoding are the octets
        /// 01 81 80, and nothing more, the method will return 0x00000080. If you
        /// try to decode (i.e. call ReadValue), that will throw an
        /// exception. But this method will return the length, even though the
        /// encoding is not valid.
        /// </para>
        /// </remarks>
        /// <param name="tagLength">
        /// The length of the tag to read. If this argument is not given the
        /// default length of 1 will be used.
        /// </param>
        /// <returns>
        /// The length.
        /// </returns>
        /// <exception cref="TlvException">
        /// The <c>tagLength</c> is unsupported, the length read is unsupported, or
        /// there was not enough data to read.
        /// </exception>
        public int PeekLength(int tagLength = 1)
        {
            _ = PeekTag(tagLength);

            int result = ReadLength();

            if (result != ValidEncoding)
            {
                ThrowOnFailedRead(result);
            }

            return _currentLength;
        }

        // Read the value. Set the value input/output arg to point to the decoded
        // V of TLV. If the read is successful, return true, otherwise, return
        // false.
        // If the read is not successful and throwIfFailed is true, throw an
        // exception with a message corresponding to what the failure was.
        // If the fixedLength is either FixedLengthByte, FixedLengthInt16, or
        // FixedLengthInt32, verify that the value length is exactly what is
        // expected.
        // If the fixedLength arg is anything else, ignore it.
        // If the current tag is not expectedTag, or the length is invalid, or
        // the fixedLength is incorrect, this is an invalid read. If it is then
        // the value arg will be set to an Empty buffer.
        private bool CommonReadValue(
            out ReadOnlyMemory<byte> value,
            int expectedTag,
            int fixedLength,
            bool throwIfFailed)
        {
            bool isValidTlv = IsValidTlv(expectedTag, fixedLength, out int resultCode);
            if (isValidTlv)
            {
                value = _encoding.Slice(_currentValueOffset, _currentLength);
                _currentOffset = _currentValueOffset + _currentLength;

                ResetState();
                return true;
            }

            // We must reset the state before we throw or return as well, so ResetState() is duplicated but the
            // intent is more clear that we need to reset the state in all (3) cases. 
            ResetState();
            if (throwIfFailed)
            {
                ThrowOnFailedRead(resultCode);
            }

            value = Memory<byte>.Empty;
            return false;
        }

        /// <summary>
        /// Attempts to read and validate the expected tag, its length, and its value, indicating success through the return value and providing a result code.
        /// </summary>
        /// <param name="expectedTag">The tag expected to be read.</param>
        /// <param name="fixedLength">The fixed length that the value is expected to adhere to.</param>
        /// <param name="resultCode">Out parameter that returns the result code of the operation, indicating the specific outcome or error.</param>
        /// <returns>Returns true if the tag, length, and value are successfully read and validated. Returns false if any validation fails, along with the appropriate result code.</returns>
        private bool IsValidTlv(int expectedTag, int fixedLength, out int resultCode)
        {
            resultCode = ReadTagExpected(expectedTag);
            if (resultCode != ValidEncoding)
            {
                return false;
            }

            resultCode = ReadLength();
            if (resultCode != ValidEncoding)
            {
                return false;
            }

            resultCode = VerifyValue(fixedLength);
            if (resultCode != ValidEncoding)
            {
                return false;
            }

            // Valid Tlv, we were able to read the tag, length and verify the value
            return true;
        }

        // Read the tag, verifying the tag in the encoding is the same as the
        // expectedTag.
        // Return either
        //   ValidEncoding (the tag was read and it is the same as expectedTag)
        //   UnexpectedEncoding (the tag was read but it is not the expected)
        //   UnsupportedTag (the expectedTag was > 0xFFFF).
        //   UnexpectedEnd (not enough bytes in the buffer to read a
        //     tagLength tag)
        // If the tag is successfully read, this will set _currentTag and
        // _currentTagLength.
        // Note that if if the return is ValidEncoding or UnexpectedEncoding, the
        // tag was successfully read. In the case of UnexpectedEncoding, the tag
        // was read, but it just didn't match the expectedTag.
        private int ReadTagExpected(int expectedTag)
        {
            int tagLength = 3;
            if (expectedTag <= 0xFFFF)
            {
                tagLength = 2;
                if (expectedTag <= 0xFF)
                {
                    tagLength = 1;
                }
            }

            int result = ReadTag(tagLength);
            if (result == ValidEncoding && _currentTag != expectedTag)
            {
                return UnexpectedEncoding;
            }

            return result;
        }

        // Read the tag.
        // Set _currentTag and _currentTagLength.
        // Return either
        //   ValidEncoding (successful read)
        //   UnsupportedTag (unsupported tag, e.g. tagLength = -1 or 5)
        //   UnexpectedEnd (not enough bytes to read)
        private int ReadTag(int tagLength)
        {
            if (tagLength <= 0 || tagLength > MaximumTagLength)
            {
                return UnsupportedTag;
            }

            if (tagLength == _currentTagLength)
            {
                return ValidEncoding;
            }

            ResetState();

            if (_currentOffset + tagLength > _encoding.Length)
            {
                return UnexpectedEnd;
            }

            _currentTag = (int)_encoding.Span[_currentOffset];
            for (int index = 1; index < tagLength; index++)
            {
                _currentTag <<= 8;
                _currentTag += (int)_encoding.Span[_currentOffset + index];
            }
            _currentTagLength = tagLength;

            return ValidEncoding;
        }

        // This call assumes the tag has been read and that the _currentTag and
        // _currentTagLength fields have been set.
        // Skip the _currentTagLength bytes and read the length octets.
        // Set _currentLength and _currentLengthOfLength.
        // Return either
        //   ValidEncoding (successful read)
        //   UnsupportedLength (unsupported length encoding, e.g. 0x80 or 0x88)
        //   UnexpectedEnd (not enough bytes to read)
        private int ReadLength()
        {
            if (_currentLengthOfLength != 0)
            {
                return ValidEncoding;
            }

            int count = 1;
            if (_currentOffset + _currentTagLength < _encoding.Length)
            {
                _currentLength = (int)_encoding.Span[_currentOffset + _currentTagLength];
                if (_currentLength <= 0x7F)
                {
                    _currentLengthOfLength = 1;
                    return ValidEncoding;
                }

                count = _currentLength & 0x7F;
                _currentLength = 0;
            }

            // If the initial length byte is 0x80, that is an unsupported value
            // (it's BER for indefinite length and we support DER only). In that
            // case, we would have set count to 0 (0x80 & 0x7F yields 0).
            if (count == 0 || count > MaximumLengthCount)
            {
                return UnsupportedLength;
            }
            if (_currentOffset + _currentTagLength + count + 1 > _encoding.Length)
            {
                return UnexpectedEnd;
            }

            for (int index = 1; index <= count; index++)
            {
                _currentLength <<= 8;
                _currentLength += (int)_encoding.Span[_currentOffset + _currentTagLength + index];
            }

            _currentLengthOfLength = count + 1;
            return ValidEncoding;
        }

        // This call assumes the tag and length have been read and that the
        // _currentTag, _currenTagLength, _currentLength, and
        // _currentLengthLength fields have been set.
        // Verify that the encoding has enough space for the computed length, and
        // that if fixedLength is 1, 2, or 4, the length is exactly that.
        // Set _currentValueOffset.
        // Return either
        //   ValidEncoding (successful read)
        //   UnexpectedEncoding (fixedLength not equal to computed value length)
        //   UnexpectedEnd (not enough bytes to read)
        private int VerifyValue(int fixedLength)
        {
            _currentValueOffset = _currentOffset + _currentTagLength + _currentLengthOfLength;

            switch (fixedLength)
            {
                case FixedLengthByte:
                case FixedLengthInt16:
                case FixedLengthInt32:
                    if (_currentLength != fixedLength)
                    {
                        return UnexpectedEncoding;
                    }
                    break;
            }

            return _currentValueOffset + _currentLength <= _encoding.Length
                ? ValidEncoding
                : UnexpectedEnd;
        }

        // Throw the TlvException, choose the message to use based on the
        // errorCode.
        // Note that this will always throw. That is, this method does NOT
        // determine if there is a throw-worthy error.
        private static void ThrowOnFailedRead(int errorCode)
        {
            string message = errorCode switch
            {
                UnsupportedTag => ExceptionMessages.TlvUnsupportedTag,
                UnsupportedLength => ExceptionMessages.TlvUnsupportedLengthField,
                UnexpectedEnd => ExceptionMessages.TlvUnexpectedEndOfBuffer,
                _ => ExceptionMessages.TlvUnexpectedEncoding,
            };

            throw new TlvException(message);
        }
    }
}
