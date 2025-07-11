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
using System.Collections.Generic;
using System.Formats.Cbor;

namespace Yubico.YubiKey.Fido2.Cbor
{
    /// <summary>
    /// Some helpers to make working with CBOR a little easier.
    /// </summary>
    internal static class CborHelpers
    {
        // Use this delegate to pass in an encoding function (rather than the
        // entire object) for WriteEncodedValue.
        // If there is nothing to encode (e.g. localData is null), then return an
        // empty byte array.
        public delegate byte[] CborEncodeDelegate<T>(T? localData) where T : class;

        /// <summary>
        /// Use this method to write CBOR maps (which is mostly what CTAP2 uses). This uses a builder-like pattern
        /// where you can chain calls to add additional entries.
        /// </summary>
        /// <param name="cbor">
        /// An instance of a CborWriter. It must have the `ConvertIndefiniteLengthEncodings` option enabled.
        /// </param>
        /// <returns>
        /// An instance of the `CborMapWriter` builder class. You should not need to store this value anywhere. The intended
        /// use is to chain calls to its method like the following:
        /// <code language="C#">
        /// CborHelper.BeginMap(cborWriter)
        ///     .Entry(123, "abc")
        ///     .Entry(456, "def")
        ///     .OptionalEntry(2, maybeNullVariable)
        ///     .EndMap();
        /// byte[] encoding = cborWriter.Encode();
        /// </code>
        /// </returns>
        public static CborMapWriter<TKey> BeginMap<TKey>(CborWriter cbor) => new CborMapWriter<TKey>(cbor);

        /// <summary>
        /// Read an array of strings, placing them into the given <c>List</c> if
        /// it is not null. Create a new <c>List</c> if it the array is null.
        /// Return the new <c>List</c> if this method creates one, return the
        /// input destination if the method does not.
        /// </summary>
        /// <param name="cbor">
        /// The object that will read the array. This method assumes that this
        /// object is "pointing to" the array.
        /// </param>
        /// <param name="destination">
        /// If the array is not null, the strings will be deposited here. If it
        /// is null, the method will create a new <c>List</c>.
        /// </param>
        /// <returns>
        /// The <c>List</c> containing the strings. If the input
        /// <c>destination</c> argument is null, then the return is a new
        /// <c>List</c>. Otherwise, <c>destination</c> is returned.
        /// </returns>
        public static List<string>? ReadStringArray(CborReader cbor, List<string>? destination)
        {
            int? entries = cbor.ReadStartArray();
            int count = entries ?? 0;

            var allDestinationsList = destination ?? new List<string>(count);

            for (int index = 0; index < count; index++)
            {
                allDestinationsList.Add(cbor.ReadTextString());
            }

            cbor.ReadEndArray();

            return allDestinationsList;
        }

        /// <summary>
        /// Encode an array of strings. This implements <c>CborEncodeDelegate</c>.
        /// </summary>
        /// <remarks>
        /// This method expects the <c>localData</c> to be an instance of
        /// <c>IReadOnlyList&lt;string&gt;</c>. If it is null, this method will
        /// return an empty byte array.
        /// </remarks>
        /// <param name="localData">
        /// The list of strings to encode.
        /// </param>
        /// <returns>
        /// A byte array containing the encoded list. If there is no list
        /// (<c>localData</c> is null or a list with <c>Count</c> zero), the
        /// return will be an empty byte array.
        /// </returns>
        public static byte[] EncodeStringArray(IReadOnlyList<string>? localData)
        {
            if (localData is null || localData.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartArray(localData.Count);
            foreach (string currentString in localData)
            {
                cbor.WriteTextString(currentString);
            }
            cbor.WriteEndArray();

            return cbor.Encode();
        }

        /// <summary>
        /// Encode an array of objects. This implements <c>CborEncodeDelegate</c>.
        /// </summary>
        /// <remarks>
        /// This method expects the <c>localData</c> to be an instance of
        /// <c>IReadOnlyList&lt;ICborEncode&gt;</c>. If it is null, or the list's
        /// count is zero, this method will return an empty byte array. So if you
        /// want "no entries" to mean "don't write anything", don't call this
        /// method.
        /// </remarks>
        /// <param name="localData">
        /// The list of objects to encode.
        /// </param>
        /// <returns>
        /// A byte array containing the encoded list. If there is no list
        /// (<c>localData</c> is null or a list with <c>Count</c> zero), the
        /// return will be an empty byte array.
        /// </returns>
        public static byte[] EncodeArrayOfObjects(IReadOnlyList<ICborEncode>? localData)
        {
            if (localData is null || localData.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartArray(localData.Count);
            foreach (var cborEncode in localData)
            {
                cbor.WriteEncodedValue(cborEncode.CborEncode());
            }
            cbor.WriteEndArray();

            return cbor.Encode();
        }
    }
}
