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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using static Yubico.YubiKey.Otp.NdefConstants;

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// A static class containing helpers that encode configurations for different types of NDEF data.
    /// </summary>
    public static class NdefConfig
    {
        private const int NdefDataSize = 54;
        private const int NdefConfigSize = 62;

        /// <summary>
        /// Create a configuration buffer for the YubiKey to send a URI when NDEF is triggered.
        /// </summary>
        /// <param name="uri">The URI to send over NDEF.</param>
        /// <returns>
        /// An opaque configuration buffer that can be written to the YubiKey using the
        /// <see cref="Commands.ConfigureNdefCommand"/> command class.
        /// </returns>
        public static byte[] CreateUriConfig(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string uriString = uri.ToString();
            int prefixCode = Array.FindIndex(
                supportedUriPrefixes,
                1, // Skip the first element ("") as it will match with everything!
                prefix => uriString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            // If none of the well-known URI prefixes match, set the prefix code to "N/A" and we'll
            // provide the entire URI in the text section.
            if (prefixCode == -1)
            {
                prefixCode = 0;
            }

            Debug.Assert(prefixCode >= 0 && prefixCode < supportedUriPrefixes.Length);

            uriString = uriString.Remove(0, supportedUriPrefixes[prefixCode].Length);

            int utf8Length = Encoding.UTF8.GetByteCount(uriString);

            if (utf8Length > NdefDataSize - 1)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NdefUriTooLong,
                        NdefDataSize - 1,
                        utf8Length,
                        prefixCode),
                        nameof(uri));
            }

            byte[] buffer = CreateBuffer();
            buffer[0] = (byte)(utf8Length + 1);
            buffer[1] = (byte)'U';
            buffer[2] = (byte)prefixCode;

            int bytesWritten = Encoding.UTF8.GetBytes(uriString, 0, uriString.Length, buffer, 3);
            Debug.Assert(utf8Length == bytesWritten);

            return buffer;
        }

        /// <summary>
        /// Create a configuration buffer for the YubiKey to send text when NDEF is triggered.
        /// </summary>
        /// <param name="value">The text value to send.</param>
        /// <param name="languageCode">The ISO/IANA language code for the language of <paramref name="value"/>.</param>
        /// <param name="encodeAsUtf16">
        /// Indicates whether UTF16 Big Endian encoding is preferred. Default is <see langword="false" />,
        /// denoting a UTF8 encoding.
        /// </param>
        /// <returns>
        /// An opaque configuration buffer that can be written to the YubiKey using the
        /// <see cref="Commands.ConfigureNdefCommand"/> command class.
        /// </returns>
        public static byte[] CreateTextConfig(string value, string languageCode, bool encodeAsUtf16 = false)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (languageCode is null)
            {
                throw new ArgumentNullException(nameof(languageCode));
            }

            var encoding = encodeAsUtf16 ? Encoding.BigEndianUnicode : Encoding.UTF8;

            int languageLength = Encoding.ASCII.GetByteCount(languageCode);
            int valueLength = encoding.GetByteCount(value);
            int totalLength = languageLength + valueLength + 1; // +1 for status byte

            if (languageLength > 0x3F)
            {
                throw new ArgumentException(
                    ExceptionMessages.NdefLanguageCodeTooLong,
                    nameof(languageCode));
            }

            byte status = (byte)((0x3F & languageLength) | (encodeAsUtf16 ? 0x80 : 0x00));

            if (totalLength > NdefDataSize)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NdefTextTooLong,
                        NdefDataSize,
                        totalLength,
                        totalLength - languageLength,
                        languageLength),
                        nameof(value));
            }

            byte[] buffer = CreateBuffer();

            buffer[0] = (byte)totalLength;
            buffer[1] = (byte)'T';
            buffer[2] = status;

            int bytesWritten = Encoding.ASCII.GetBytes(
                languageCode,
                0,
                languageCode.Length,
                buffer,
                3);
            Debug.Assert(languageLength == bytesWritten);

            bytesWritten = encoding.GetBytes(value, 0, value.Length, buffer, 3 + languageLength);
            Debug.Assert(valueLength == bytesWritten);

            return buffer;
        }

        private static byte[] CreateBuffer() => new byte[NdefConfigSize];
    }
}
