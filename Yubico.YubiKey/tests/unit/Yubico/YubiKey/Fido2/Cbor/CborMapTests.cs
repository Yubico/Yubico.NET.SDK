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
using Xunit;

namespace Yubico.YubiKey.Fido2.Cbor
{
    public class CborMapTests
    {
        // Read this encoding
        //   A3                           -- map(X) with three items
        //      01                        -- key(X1) = 01, an int
        //         A2                     -- value (X1), another map, map(Y), 2 items
        //            03                  -- key(Y1) = 0x03, an int
        //               44  11 22 33 44  -- value(Y1), a byte array
        //            04                  -- key(Y2) = 0x04, an int
        //               42  11 22        -- value(Y2), a byte array
        //      02                        -- key(X2) = 02, an int
        //         A2                     -- value(X2), another map, map(Z), 2 items
        //            62  5A 31           -- key(Z1) = string, "Z1"
        //               44  11 22 33 44  -- value(Z1), a byte array
        //            62  5A 32           -- key(Z2) = string, "Z2"
        //               42  11 22        -- value, a byte array
        //      03                        -- key(X3) = 03, an int
        //         82                     -- value(X3) = array of length 2, array W
        //            64  59 75 62 69     -- element W1
        //            63  4B 65 79        -- element W2
        [Fact]
        public void ReadMap_IntAndStringKeys_Succeeds()
        {
            byte[] encoding = new byte[] {
                0xA3, 0x01, 0xA2, 0x03, 0x44, 0x11, 0x22, 0x33, 0x44, 0x04, 0x42, 0x11, 0x22, 0x02, 0xA2, 0x62,
                0x5A, 0x31, 0x44, 0x11, 0x22, 0x33, 0x44, 0x62, 0x5A, 0x32, 0x42, 0x11, 0x22, 0x03, 0x82, 0x64,
                0x59, 0x75, 0x62, 0x69, 0x63, 0x4B, 0x65, 0x79
            };

            bool isValid = ReadUsingCborReader(encoding);
            Assert.True(isValid);
            isValid = ReadUsingCborMap(encoding);
            Assert.True(isValid);
        }

        private static bool ReadUsingCborMap(byte[] encoding)
        {
            var cborMap = new CborMap<int>(encoding);

            return cborMap.Contains(3);
        }

        private static bool ReadUsingCborReader(byte[] encoding)
        {
            var cbor = new CborReader(encoding, CborConformanceMode.Ctap2Canonical);

            int? entries = cbor.ReadStartMap();
            int count = entries ?? 0;

            if (count != 3)
            {
                return false;
            }

            while (count > 0)
            {
                int mapKey = (int)cbor.ReadUInt32();

                switch (mapKey)
                {
                    default:
                        return false;

                    case 1:
                        // Read the map with 2 int/byte array key/value pairs.
                        if (!ReadMapX1(cbor))
                        {
                            return false;
                        }
                        break;

                    case 2:
                        // Read the map with 2 string/byte array key/value pairs.
                        if (!ReadMapX2(cbor))
                        {
                            return false;
                        }
                        break;

                    case 3:
                        // Read the array of 2 strings.
                        if (!ReadArrayX3(cbor))
                        {
                            return false;
                        }
                        break;
                }

                count--;
            }

            cbor.ReadEndMap();

            return true;
        }

        private static bool ReadMapX1(CborReader cbor)
        {
            int? entries = cbor.ReadStartMap();
            int count = entries ?? 0;

            if (count != 2)
            {
                return false;
            }

            int mapKeyOne = (int)cbor.ReadUInt32();
            byte[] valueOne = cbor.ReadByteString();

            int mapKeyTwo = (int)cbor.ReadUInt32();
            byte[] valueTwo = cbor.ReadByteString();

            cbor.ReadEndMap();

            return mapKeyOne == 0x03 && mapKeyTwo == 0x04 && valueOne.Length == 4 && valueTwo.Length == 2;
        }

        private static bool ReadMapX2(CborReader cbor)
        {
            int? entries = cbor.ReadStartMap();
            int count = entries ?? 0;

            if (count != 2)
            {
                return false;
            }

            string mapKeyOne = cbor.ReadTextString();
            byte[] valueOne = cbor.ReadByteString();
            bool isValidOne = mapKeyOne.Equals("Z1", StringComparison.Ordinal);

            string mapKeyTwo = cbor.ReadTextString();
            byte[] valueTwo = cbor.ReadByteString();
            bool isValidTwo = mapKeyTwo.Equals("Z2", StringComparison.Ordinal);

            cbor.ReadEndMap();

            return isValidOne && isValidTwo && valueOne.Length == 4 && valueTwo.Length == 2;
        }

        private static bool ReadArrayX3(CborReader cbor)
        {
            int? entries = cbor.ReadStartArray();
            int count = entries ?? 0;

            if (count != 2)
            {
                return false;
            }

            string entryOne = cbor.ReadTextString();
            bool isValidOne = entryOne.Equals("Yubi", StringComparison.Ordinal);

            string entryTwo = cbor.ReadTextString();
            bool isValidTwo = entryTwo.Equals("Key", StringComparison.Ordinal);

            cbor.ReadEndArray();

            return isValidOne && isValidTwo;
        }

        [Fact]
        public void ReadEncodedValue_ReturnsByteIdenticalValue()
        {
            byte[] encoded = new byte[]
            {
                0xA3,
                0x01, 0x64, 0x74, 0x65, 0x73, 0x74,
                0x02, 0x42, 0x11, 0x22,
                0x03, 0xA2, 0x63, 0x61, 0x6C, 0x67, 0x26, 0x63, 0x73, 0x69, 0x67, 0x43, 0xAA, 0xBB, 0xCC
            };
            byte[] expectedKey3Value = new byte[]
            {
                0xA2, 0x63, 0x61, 0x6C, 0x67, 0x26, 0x63, 0x73, 0x69, 0x67, 0x43, 0xAA, 0xBB, 0xCC
            };

            var map = new CborMap<int>(encoded);
            ReadOnlyMemory<byte> retrievedSlice = map.ReadEncodedValue(3);

            Assert.True(retrievedSlice.Span.SequenceEqual(expectedKey3Value),
                "ReadEncodedValue must return byte-identical bytes of the original value");
        }

        [Fact]
        public void ReadEncodedValue_MissingKey_ThrowsKeyNotFound()
        {
            // Build a simple CBOR map with only key 1
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            writer.WriteStartMap(1);
            writer.WriteInt32(1);
            writer.WriteTextString("test");
            writer.WriteEndMap();

            byte[] encoded = writer.Encode();
            var map = new CborMap<int>(encoded);

            Assert.Throws<KeyNotFoundException>(() => map.ReadEncodedValue(999));
        }

        [Fact]
        public void Keys_ReturnsMapKeys()
        {
            byte[] encoded = new byte[]
            {
                0xA2,
                0x01, 0x01,
                0x02, 0x02,
            };

            var map = new CborMap<int>(encoded);

            Assert.Contains(1, map.Keys);
            Assert.Contains(2, map.Keys);
            Assert.Equal(2, map.Count);
        }

        [Fact]
        public void Constructor_AllowsEmptyNestedMap_ForRawValueRead()
        {
            byte[] encoded = new byte[]
            {
                0xA1,
                0x01, 0xA0,
            };

            var map = new CborMap<int>(encoded);

            Assert.Equal(new byte[] { 0xA0 }, map.ReadEncodedValue(1).ToArray());
            Assert.Equal(0, map.ReadMap<int>(1).Count);
        }

        [Fact]
        public void Constructor_AllowsOpaqueNestedMap_ForRawValueRead()
        {
            byte[] encoded = new byte[]
            {
                0xA2,
                0x01, 0xA1, 0x41, 0x01, 0x41, 0x02,
                0x02, 0x64, 0x64, 0x6F, 0x6E, 0x65,
            };
            byte[] expectedOpaqueMap = new byte[]
            {
                0xA1, 0x41, 0x01, 0x41, 0x02,
            };

            var map = new CborMap<int>(encoded);

            Assert.Equal(expectedOpaqueMap, map.ReadEncodedValue(1).ToArray());
            Assert.Equal("done", map.ReadTextString(2));
            Assert.Throws<InvalidCastException>(() => map.ReadMap<int>(1));
            Assert.Throws<InvalidCastException>(() => map.ReadMap<byte[]>(1));
        }

        [Fact]
        public void Constructor_AllowsArraysWithOpaqueValues_ForRawValueRead()
        {
            byte[] encoded = new byte[]
            {
                0xA1,
                0x01, 0x83,
                0xA0,
                0xA1, 0x41, 0x01, 0x41, 0x02,
                0x01,
            };
            byte[] expectedArray = new byte[]
            {
                0x83,
                0xA0,
                0xA1, 0x41, 0x01, 0x41, 0x02,
                0x01,
            };

            var map = new CborMap<int>(encoded);

            Assert.Equal(expectedArray, map.ReadEncodedValue(1).ToArray());
            Assert.Throws<InvalidCastException>(() => map.ReadArray<int>(1));
        }

        [Fact]
        public void Constructor_AllowsFutureCborValue_ForRawValueRead()
        {
            byte[] encoded = new byte[]
            {
                0xA1,
                0x01, 0xF9, 0x3C, 0x00,
            };

            var map = new CborMap<int>(encoded);

            Assert.Equal(new byte[] { 0xF9, 0x3C, 0x00 }, map.ReadEncodedValue(1).ToArray());
            Assert.Throws<InvalidCastException>(() => map.ReadInt32(1));
        }
    }
}
