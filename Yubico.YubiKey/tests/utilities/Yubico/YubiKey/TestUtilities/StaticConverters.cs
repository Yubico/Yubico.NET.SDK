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
using System.Text.RegularExpressions;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// Static class with converters for TestApp.
    /// </summary>
    /// <remarks>
    /// These conversions can be used anywhere, but they are specifically for
    /// TestApp, so things that might be appropriate there may not be for other
    /// uses. For example, the bool converter accepts "true", "false", "yes",
    /// and "no". If it's an empty string, it return true because it was a
    /// bool-based command-line argument without a parameter. If any other
    /// value, it throws and <c>ArgumentException</c>.
    /// </remarks>
    public static class StaticConverters
    {
        /// <summary>
        /// Parses a command line parameter into a byte array.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method does a best-effort attempt to parse the string into a
        /// byte array.
        /// </para>
        /// <para>
        /// If it's just a string of hex digits (0-9/a-f), then we decode it as
        /// hexadecimal bytes.
        /// </para>
        /// <para>
        /// If it's a string of characters that are legal base64 characters, then
        /// it's decoded as base64.
        /// </para>
        /// <para>
        /// If there are delimiters (comma or period), then we assume that the
        /// parts are bytes. If all of the parts are two digits, we assume that
        /// they're hex. If any are three digits and none of them have invalid
        /// decimal digits, we assume it's decimal.
        /// </para>
        /// <para>
        /// If it doesn't match any of these, we throw an exception.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="s">String representation of byte array</param>
        /// <exception cref="ArgumentException">The string cannot be converted to a byte array.</exception>
        /// <returns>Byte array</returns>
        public static byte[] ParseByteArray(string s)
        {
            const string delimiters = @",.:\- ";

            // Dillema, how to deal with an empty parameter.
            // I could throw an exception, but I think I'll just return an empty
            // array.
            if (string.IsNullOrWhiteSpace(s))
            {
                return Array.Empty<byte>();
            }

            // Is it simply a number?
            if (Regex.IsMatch(s, @"^(?:0(?:x|X))?[\da-fA-F]{1,3}$"))
            {
                return new byte[] { ParseSingleByte(s) };
            }

            // Next, let's see if it's just a string of hex digits.
            // Note: if there are not an even number, then it's not supported.
            //       It would be too uncertain where the missing nibble is from.
            // Note: if we were using .NET 5, we could use Convert.FromHexString().
            if (Regex.IsMatch(s, @"^(?:[\da-fA-F]{2})+$"))
            {
                var result = new List<byte>();
                Match matchResults = Regex.Match(s, @"[\da-fA-F]{2}");
                while (matchResults.Success)
                {
                    result.Add(Convert.ToByte(matchResults.Value, 16));
                    matchResults = matchResults.NextMatch();
                }
                return result.ToArray();
            }

            // Next, let' see if it's delmited.
            if (Regex.IsMatch(s, $"[{ delimiters }]"))
            {
                // Okay, it may be delimited. First, are there any illegal 
                // characters?
                if (!Regex.IsMatch(s, $"^[\\da-fA-FxX{ delimiters }]+$"))
                {
                    throw new ArgumentException($"[{ s }] has invalid characters and can't be parsed as a byte array.");
                }

                // Are they likely decimal or hex? This is kind of risky. It
                // could be a big collection of hex numbers that don't have
                // any digits higher than 9, think BCD. We're just going to
                // call this a limitation of a test tool, though. The correct
                // thing for a caller to do is prefix all hex with '0x'.
                bool isHex = Regex.IsMatch(s, @"[a-fA-F]");

                // Let's split them into proposed bytes and examine them.
                string[] bytes = s.Split(delimiters.ToCharArray());


                var result = new List<byte>();
                foreach (string word in bytes)
                {
                    string parse = isHex && !word.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? "0x" + word
                        : word;
                    result.Add(ParseSingleByte(parse));
                }
                return result.ToArray();
            }

            // Last chance. Is it base64?
            try
            {
                return Convert.FromBase64String(s);
            }
            catch (FormatException)
            {
                // I'm just going to let it throw at the bottom.
            }

            throw new ArgumentException($"[{ s }] cannot be converted to a byte array.");
        }

        /// <summary>
        /// Parses a string into a single byte.
        /// </summary>
        /// <param name="s">String to parse</param>
        /// <exception cref="ArgumentException">The value cannot be converted cleanly to a byte.</exception>
        /// <returns>Byte containing integer value of string</returns>
        public static byte ParseSingleByte(string s)
        {
            const string constHex = @"^(?:0(?:x|X))?([\da-fA-F]{1,2})$";
            Match match = Regex.Match(s, constHex);
            if (match.Success)
            {
                return Convert.ToByte(match.Value, 16);
            }
            match = Regex.Match(s, @"^[\d]{1,3}");
            if (match.Success)
            {
                int value = Convert.ToInt32(match.Value);
                if (value > 0xff)
                {
                    throw new ArgumentException($"[{ s }] cannot be converted to a byte.");
                }
                return Convert.ToByte(value);
            }
            throw new ArgumentException($"[{ s }] cannot be converted to a byte.");
        }

        /// <summary>
        /// Parses a bool from a string.
        /// </summary>
        /// <param name="s">String representation of a bool</param>
        /// <remarks>
        /// We do this instead of using bool.Parse because we want to be able to
        /// be more loose with our bool representation. Also, if a parameter is
        /// specified, but no value is given, we assume true.
        /// </remarks>
        /// <returns>bool</returns>
        public static bool ParseBool(string s)
        {
            return s switch
            {
                "true" => true,
                "false" => false,
                "1" => true,
                "0" => false,
                "yes" => true,
                "no" => false,
                "" => true, // If a bool parameter didn't get a value, we assume true.
                _ => throw new ArgumentException($"[{ s }] can't be parsed as a bool.")
            };
        }

        /// <summary>
        /// Parses an enum from a string.
        /// </summary>
        /// <typeparam name="T">Enum type to parse from string</typeparam>
        /// <param name="s">String representation of an instance of T</param>
        /// <returns>Parsed instance of T</returns>
        /// <remarks>
        /// This method does a non-case-sensitive parse.
        /// </remarks>
        public static T ParseEnum<T>(string s) where T : struct
        {
            if (Enum.TryParse<T>(s, true, out T value))
            {
                if (Enum.IsDefined(typeof(T), value))
                {
                    return value;
                }
            }
            throw new ArgumentException($"Value [{ s }] could not be parsed as type [{ typeof(T).Name }].");
        }
    }
}
