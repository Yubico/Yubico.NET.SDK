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
using System.Linq;
using System.Text;

namespace Yubico.Core.Buffers
{
    /// <summary>
    /// Utilities for working with multi null-terminated strings.
    /// </summary>
    public static class MultiString
    {
        /// <summary>
        /// Converts the byte array representing a multi-null-terminated string and return them as
        /// .NET strings.
        /// </summary>
        /// <param name="value">Multi-string to convert.</param>
        /// <param name="encoding">Encoding to use for interpreting the strings.</param>
        /// <returns>Array of converted strings.</returns>
        public static string[] GetStrings(byte[] value, Encoding encoding)
        {
            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            return encoding
                .GetString(value)
                .Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Converts an array of strings to a multi-null-terminated string.
        /// </summary>
        /// <param name="value">Array of strings.</param>
        /// <param name="encoding">Encoding to apply to the multi-string.</param>
        /// <returns>Byte array containing multi-string.</returns>
        public static byte[] GetBytes(string[] value, Encoding encoding)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (value.Length == 0)
            {
                return Array.Empty<byte>();
            }

            // Take a reasonable guess at the initial buffer size. Hopefully in the worst case, List
            // would only need to resize once. A multiplier of 2 is a reasonable buffer for unicode
            // length for both UTF-8 and UTF-16.
            int initialLength = value.Sum(str => str.Length) * 2;

            var multiString = new List<byte>(initialLength);
            byte[] nullBytes = encoding.GetBytes("\0");

            foreach (string str in value)
            {
                multiString.AddRange(encoding.GetBytes(str));
                multiString.AddRange(nullBytes);
            }
            multiString.AddRange(nullBytes);

            return multiString.ToArray();
        }
    }
}
