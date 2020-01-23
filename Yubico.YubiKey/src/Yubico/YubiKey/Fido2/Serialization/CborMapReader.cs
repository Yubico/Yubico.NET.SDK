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
using System.Formats.Cbor;
using System.Globalization;

namespace Yubico.YubiKey.Fido2.Serialization
{
    /// <summary>
    /// Class to enable reading optional keys from CBOR maps.
    /// </summary>
    /// <remarks>
    /// This is adapted from 'CoseKeyHelpers' at 
    /// https://github.com/dotnet/runtime/blob/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/System.Formats.Cbor/tests/CoseKeyHelpers.cs#L234
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal class CborMapReader<T> where T : IComparable<T>
    {
        private bool _latestIsEmpty;
        private T _latestReadLabel;
        private int _remainingKeys;

        private readonly CborReader _reader;
        private readonly Func<CborReader, T> _deserializeKeyFunc;

        /// <summary>
        /// Initializes a CborMapReader with using the given CborReader and deserialization function.
        /// </summary>
        /// <param name="reader">The CborReader to read from</param>
        /// <param name="deserializeKeyFunc">A function called to deserialize a key for the CBOR map</param>
        // We must suppress this, since we are using a generic across value and reference types
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public CborMapReader(CborReader reader, Func<CborReader, T> deserializeKeyFunc)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
            _reader = reader;
            _deserializeKeyFunc = deserializeKeyFunc;
            _latestIsEmpty = true;
        }

        public void StartReading() =>
            _remainingKeys = _reader.ReadStartMap()
                ?? throw new CborContentException(ExceptionMessages.Ctap2CborIndefiniteLength);

        public void StopReading()
        {
            if (_remainingKeys > 0)
            {
                // eat remaining data
                for (int i = 0; i < _remainingKeys; i++)
                {
                    _ = _reader.ReadInt32();
                    _reader.SkipValue();
                }
            }

            _reader.ReadEndMap();
        }

        // Handles optional labels
        public bool TryReadLabel(T expectedLabel)
        {
            // if we tried to read an optional label last time, but 
            // instead ended up eating the next label
            if (_latestIsEmpty)
            {
                // check that we have not reached the end of the object
                if (_remainingKeys == 0)
                {
                    return false;
                }

                _latestIsEmpty = false;
                _latestReadLabel = _deserializeKeyFunc(_reader);
            }

            if (expectedLabel.CompareTo(_latestReadLabel) != 0)
            {
                return false;
            }

            _latestIsEmpty = true;
            _remainingKeys--;
            return true;
        }

        // Handles required labels
        public void ReadLabel(T expectedLabel, string labelName)
        {
            if (!TryReadLabel(expectedLabel))
            {
                throw new CborContentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2CborUnexpectedKey,
                        expectedLabel,
                        labelName));
            }
        }
    }
}
