// Copyright 2022 Yubico AB
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
using System.Collections.Generic;
using System.Formats.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Some helpers to make working with MakeCredential and GetAssertion
    /// parameters a little easier.
    /// </summary>
    internal static class ParameterHelpers
    {
        public const string TagType = "type";
        public const string TagAlg = "alg";

        public const string DefaultAlgType = "public-key";
        public const CoseAlgorithmIdentifier DefaultAlg = CoseAlgorithmIdentifier.ES256;

        /// <summary>
        /// Add the <c>credentialId</c> to the <c>currentList</c> if it is not
        /// null, and return the <c>currentList</c>. If the <c>currentList</c> is
        /// null, create a new list, add the credentialId and return the new list.
        /// </summary>
        public static List<T> AddToList<T>(T itemToAdd, List<T>? currentList)
        {
            if (itemToAdd is null)
            {
                throw new ArgumentNullException();
            }

            List<T> returnList = (currentList is null) ? new List<T>() : currentList;
            returnList.Add(itemToAdd);

            return returnList;
        }

        /// <summary>
        /// Add the key/value to the <c>currentDictionary</c> if it is not null,
        /// and return the <c>currentDictionary</c>. If the
        /// <c>currentDictionary</c> is  null, create a new Dictionary, add the
        /// key/value and return the new Dictionary.
        /// </summary>
        public static Dictionary<string, TValue> AddKeyValue<TValue>(
            string theKey,
            TValue theValue,
            Dictionary<string, TValue>? currentDictionary)
        {
            if (theKey is null)
            {
                throw new ArgumentNullException(nameof(theKey));
            }
            if (theValue is null)
            {
                throw new ArgumentNullException(nameof(theValue));
            }

            Dictionary<string, TValue> returnDictionary =
                (currentDictionary is null) ? new Dictionary<string, TValue>() : currentDictionary;

            // If the key already exists, relpace the current value in the
            // dictionary with this one.
            // This will add a new entry if there is no entry associated with the
            // given key.
            returnDictionary[theKey] = theValue;

            return returnDictionary;
        }

        /// <summary>
        /// Encode a dictionary as a map. This implements the
        /// <c>CborEncodeDelegate</c>.
        /// </summary>
        /// <remarks>
        /// If the input <c>localData</c> is null, or the its count is zero,
        /// this method will return an empty byte array. So if you want "no
        /// entries" to mean "don't write anything", don't call this method.
        /// <para>
        /// Currently this method supports <c>byte[]</c> and <c>bool</c> as the
        /// <c>TValue</c>.
        /// </para>
        /// </remarks>
        /// <param name="localData">
        /// The set of key/value pairs to encode.
        /// </param>
        /// <returns>
        /// A byte array containing the encoded map. If there is no list
        /// (<c>localData</c> is null or a the <c>Count</c> is zero), the
        /// return will be an empty byte array.
        /// </returns>
        public static byte[] EncodeKeyValues<TValue>(IReadOnlyDictionary<string,TValue>? localData)
        {
            if ((localData is null) || (localData.Count == 0))
            {
                return Array.Empty<byte>();
            }

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartMap(null);
            foreach (KeyValuePair<string, TValue> entry in localData)
            {
                cbor.WriteTextString(entry.Key);
                if (entry.Value is byte[] encodedValue)
                {
                    cbor.WriteEncodedValue(encodedValue);
                }
                else if (entry.Value is bool booleanValue)
                {
                    cbor.WriteBoolean(booleanValue);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            cbor.WriteEndMap();

            return cbor.Encode();
        }
    }
}
