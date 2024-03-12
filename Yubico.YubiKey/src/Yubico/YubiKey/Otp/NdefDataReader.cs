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
using System.Diagnostics;
using System.Globalization;
using System.Text;

using static Yubico.YubiKey.Otp.NdefConstants;

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// Reads NDEF Record data types supported by the YubiKey.
    /// </summary>
    /// <remarks>
    /// This class is used to interpret the byte array returned by the <see cref="Commands.ReadNdefDataResponse"/>
    /// class. Note that this class does not interpret the Configuration Container data that can also
    /// be returned through the same API. That data is not technically an NDEF message or record.
    /// </remarks>
    public class NdefDataReader
    {
        private const int MessageLengthOffset = 1;
        private const int MessageRecordOffset = 2;
        private const int TypeLengthOffset = 3;
        private const int DataLengthOffset = 4;
        private const int TypeOffset = 5;
        private const int DataOffset = 6;
        private const char NdefUriRecordType = 'U';
        private const char NdefTextRecordType = 'T';

        private readonly byte[] _data;

        /// <summary>
        /// Indicates the type of NDEF record that was read.
        /// </summary>
        /// <remarks>
        /// The YubiKey supports the following NDEF record types:
        /// - 'T' Text
        /// - 'U' URI
        /// </remarks>
        public NdefDataType Type { get; private set; }

        /// <summary>
        /// Returns the uninterpreted data inside of the NDEF record.
        /// </summary>
        public IReadOnlyList<byte> Data => _data;

        /// <summary>
        /// Constructs a new instance of the NdefDataReader class.
        /// </summary>
        /// <param name="responseData">The NDEF message data returned by the YubiKey.</param>
        public NdefDataReader(ReadOnlySpan<byte> responseData)
        {
            if (responseData[0] != 0)
            {
                throw new ArgumentException(ExceptionMessages.MalformedNdefRecord);
            }

            byte messageLength = responseData[MessageLengthOffset];
            byte typeLength = responseData[TypeLengthOffset];
            byte dataLength = responseData[DataLengthOffset];
            const int validTypeLength = 1;

            if (typeLength != validTypeLength)
            {
                throw new NotSupportedException(ExceptionMessages.BadNdefRecordType);
            }

            if (messageLength != responseData.Length - MessageRecordOffset)
            {
                throw new ArgumentException(ExceptionMessages.MalformedNdefRecord);
            }

            if (dataLength != responseData.Length - DataOffset)
            {
                throw new ArgumentException(ExceptionMessages.MalformedNdefRecord);
            }

            Debug.Assert(responseData[2] == 0xD1); // Short Record, Well-known TypeName, Message Begin, Message End

            ReadOnlySpan<byte> recordType = responseData.Slice(TypeOffset, typeLength);

            Type = recordType[0] switch
            {
                (byte)NdefTextRecordType => NdefDataType.Text,
                (byte)NdefUriRecordType => NdefDataType.Uri,
                _ => throw new NotSupportedException(ExceptionMessages.BadNdefRecordType),
            };

            _data = responseData.Slice(TypeOffset + typeLength, dataLength).ToArray();
        }

        /// <summary>
        /// Interprets the NDEF data as a text record.
        /// </summary>
        /// <returns>The text of the message, and the language information specified in the record.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Type"/> is not <see cref="NdefDataType.Text"/>.
        /// </exception>
        public NdefText ToText()
        {
            const byte utf16mask = 0x80;
            const byte languageCodeLengthMask = 0x3F;

            if (Type != NdefDataType.Text)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.WrongNdefType,
                        Type,
                        NdefDataType.Text));
            }

            byte header = _data[0];

            bool isUtf16 = (header & utf16mask) != 0;
            int languageCodeLength = header & languageCodeLengthMask;
            int textOffset = languageCodeLength + 1;

            Encoding encoding = Encoding.UTF8;
            if (isUtf16)
            {
                bool bomPresent;
                (encoding, bomPresent) = DetectCorrectUtf16Encoding(_data.AsSpan(textOffset));
                textOffset += bomPresent ? 2 : 0;
            }

            // Empty string specifies the invariant culture, which seems like the correct thing to do anyways.
            string languageCode = encoding.GetString(_data, 1, languageCodeLength);
            string text = encoding.GetString(_data, textOffset, _data.Length - textOffset);

            return new NdefText()
            {
                Encoding = isUtf16 ? NdefTextEncoding.Utf16 : NdefTextEncoding.Utf8,
                Language = new CultureInfo(languageCode),
                Text = text
            };
        }

        /// <summary>
        /// Interprets the NDEF data as a URI record.
        /// </summary>
        /// <returns>The message as a Uniform Resource Identifier (URI).</returns>
        /// <exception cref="InvalidOperationException">
        /// Throw if <see cref="Type"/> is not <see cref="NdefDataType.Uri"/>.
        /// </exception>
        public Uri ToUri()
        {
            if (Type != NdefDataType.Uri)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.WrongNdefType,
                        Type,
                        NdefDataType.Uri));
            }

            byte prefixCode = _data[0];

            if (prefixCode >= supportedUriPrefixes.Length)
            {
                throw new InvalidOperationException(ExceptionMessages.OutOfRangeUriPrefixCode);
            }

            string uriString = supportedUriPrefixes[prefixCode] + Encoding.ASCII.GetString(_data, 1, _data.Length - 1);
            bool isAbsolute = Uri.IsWellFormedUriString(uriString, UriKind.Absolute);

            return new Uri(uriString, isAbsolute ? UriKind.Absolute : UriKind.Relative);
        }

        private static (Encoding encoding, bool bomPresent) DetectCorrectUtf16Encoding(ReadOnlySpan<byte> stringData)
        {
            ReadOnlySpan<byte> lePreamble = Encoding.Unicode.GetPreamble();
            ReadOnlySpan<byte> bePreamble = Encoding.BigEndianUnicode.GetPreamble();

            if (stringData.StartsWith(lePreamble))
            {
                return (Encoding.Unicode, true);
            }
            else if (stringData.StartsWith(bePreamble))
            {
                return (Encoding.BigEndianUnicode, true);
            }
            else
            {
                // No byte-order-mark present. NDEF spec says to assume big-endian... but since so many
                // UTF-16 clients (i.e. Windows) use little-endian, I don't think it's good to assume
                // one way or the other. If we assume the most commonly used characters will fall on a
                // single byte boundary, then we should be able to reliably determine which encoding it
                // is by detecting which side we find the 0-byte on. Even == Big-Endian, Odd = Little-Endian.
                int[] score = new int[2] { 0, 0 }; // BigEndian, LittleEndian
                int index = 0;
                foreach (byte b in stringData)
                {
                    if (b == 0)
                    {
                        score[index % 2]++;
                    }
                    index++;
                }

                // RFC 2781 does say to give preference to big endian, so I guess that'll be the tie-breaker.
                return score[0] >= score[1] ? (Encoding.BigEndianUnicode, false) : (Encoding.Unicode, false);
            }
        }
    }
}
