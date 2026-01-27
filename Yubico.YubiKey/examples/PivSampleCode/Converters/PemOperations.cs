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
using System.Globalization;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class contains methods that build and parse PEM constructions.
    public static class PemOperations
    {
        private const string InvalidPemDataMessage = "The input PEM data was not recognized.";

        private const string Part1 = "-----BEGIN ";
        private const string Part2And4 = "-----";
        private const string Part3 = "-----END ";
        private const string NewLine = "\r\n";
        private const int Part1Length = 11;
        private const int Part2And4Length = 5;
        private const int Part3Length = 9;

        // Build a PEM construction using the given title and encoding.
        // This method will build
        //
        //    -----BEGIN title-----
        //    base64 of encoding
        //    -----END title-----
        //
        // For example, provide the encoding of a SubjectPublicKeyInfo and the
        // tile of "PUBLIC KEY", and this method will build
        //
        //    -----BEGIN PUBLIC KEY-----
        //    base64 of SubjectPublicKeyInfo
        //    -----END PUBLIC KEY-----
        //
        // That is a PEM public key.
        // Note that this method will place a space between the "BEGIN" and the
        // title provided. Hence, the title should be
        //       "PUBLIC KEY"
        //   not " PUBLIC KEY"
        //   not "PUBLIC KEY "
        // Note that this method will "blindly" put the title into the result and
        // will "blindly" base64 encode the input encoding. It does not check to
        // see if the title is a valid PEM title (it does not even check if all
        // the letters are upper case), nor does it check the encoding.
        public static char[] BuildPem(string title, byte[] encoding)
        {
            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            char[] temp = Array.Empty<char>();

            string header = Part1 + title + Part2And4 + NewLine;
            string footer = NewLine + Part3 + title + Part2And4;
            char[] prefix = header.ToCharArray();
            char[] suffix = footer.ToCharArray();

            try
            {
                // The length of the char array will be the lengths of the prefix and
                // suffix, along with the length of the Base64 data, and new line
                // characters. Create an upper bound.
                int blockCount = (encoding.Length + 2) / 3;
                int totalLength = blockCount * 4;
                int lineCount = (totalLength + 75) / 76;
                totalLength += lineCount * 4;
                totalLength += prefix.Length;
                totalLength += suffix.Length;

                temp = new char[totalLength];
                Array.Copy(prefix, 0, temp, 0, prefix.Length);
                int count = Convert.ToBase64CharArray(
                    encoding, 0, encoding.Length,
                    temp, prefix.Length,
                    Base64FormattingOptions.InsertLineBreaks);
                Array.Copy(suffix, 0, temp, prefix.Length + count, suffix.Length);
                totalLength = prefix.Length + suffix.Length + count;
                char[] returnValue = new char[totalLength];
                Array.Copy(temp, 0, returnValue, 0, totalLength);

                return returnValue;
            }
            finally
            {
                KeyConverter.OverwriteChars(temp);
            }
        }

        // Base64 decode the PEM contents and return a new byte array containing
        // the result. Set the output argument title to the PEM header title.
        // The title will be something like "PRIVATE KEY" or
        // "CERTIFICATE REQUEST".
        // A PEM structure will be
        //   -----BEGIN something-----
        //   Base64 encoded data
        //   -----END something-----
        // This method will find the base64 data and decode it. The result will
        // be the encoded data. For example, if the header is
        //   -----BEGIN PUBLIC KEY-----
        // then the data will be the DER encoding of SubjectPublicKeyInfo.
        // The method will isolate the title, it is the "something" after the
        // BEGIN and END in the header and footer.
        // The method will also check to verify the header and footer match.
        public static byte[] GetEncodingFromPem(char[] pemString, out string title)
        {
            if (pemString is null)
            {
                throw new ArgumentNullException(nameof(pemString));
            }

            bool isValid = false;
            title = "";
            int titleLength = 0;

            // Find the title and verify the header and footer match.
            if (CompareToTarget(pemString, 0, Part1.ToCharArray()))
            {
                int indexStart = Part1Length;
                int indexEnd = Array.FindIndex<char>(pemString, indexStart, x => x == '-');
                if (indexEnd > 0)
                {
                    titleLength = indexEnd - indexStart;
                    char[] titleChars = new char[titleLength];
                    Array.Copy(pemString, indexStart, titleChars, 0, titleLength);
                    title = new string(titleChars);
                    isValid = VerifyPemHeaderAndFooter(pemString, title);
                }
            }

            if (isValid)
            {
                int prefixLength = Part1Length + titleLength + Part2And4Length;
                int suffixLength = Part3Length + titleLength + Part2And4Length;

                // Base64 decode everything between the labels.
                return Convert.FromBase64CharArray(
                    pemString,
                    prefixLength,
                    pemString.Length - (prefixLength + suffixLength));
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    InvalidPemDataMessage));
        }

        // This is the same as the other GetEncodingFromPem, except this takes in
        // an expected title rather than returning the one found.
        // Call this with the expected title. If it is the one in the PEM
        // construction, the method will return the encoding. If it is not, the
        // method will throw an exception.
        public static byte[] GetEncodingFromPem(char[] pemString, string expectedTitle)
        {
            byte[] encoding = GetEncodingFromPem(pemString, out string title);
            if (string.Equals(title, expectedTitle, StringComparison.Ordinal))
            {
                return encoding;
            }

            KeyConverter.OverwriteBytes(encoding);
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    InvalidPemDataMessage));
        }

        // Verify that the given string begins with the targetStart and ends with
        // the targetEnd.
        private static bool VerifyPemHeaderAndFooter(char[] pemKeyString, string title)
        {
            char[] targetStart = (Part1 + title + Part2And4).ToCharArray();
            char[] targetEnd = (Part3 + title + Part2And4).ToCharArray();
            bool returnValue = false;
            if (pemKeyString.Length > targetStart.Length + targetEnd.Length &&
                CompareToTarget(pemKeyString, 0, targetStart))
            {
                returnValue = CompareToTarget(pemKeyString, pemKeyString.Length - targetEnd.Length, targetEnd);
            }

            return returnValue;
        }

        // Compare the chars in buffer beginning at offset with the chars in
        // target.
        private static bool CompareToTarget(char[] buffer, int offset, char[] target)
        {
            int index = 0;
            for (; index < target.Length; index++)
            {
                if (buffer[index + offset] != target[index])
                {
                    break;
                }
            }

            return index >= target.Length;
        }
    }
}
