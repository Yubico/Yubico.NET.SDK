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
using System.Linq;
using Xunit;

namespace Yubico.YubiKey.Piv.Commands
{
    public class TripleDesTests
    {
        [Theory]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(23)]
        [InlineData(25)]
        public void Constructor_WrongKeyLength_ThrowsException(int keyLength)
        {
#pragma warning disable CS0618 // testing special TripleDES, disable warning about using special TripleDES
            byte[] keyData = new byte[keyLength];
            for (int index = 0; index < keyLength; index++)
            {
                keyData[index] = (byte)index;
            }

            _ = Assert.Throws<ArgumentException>(() => _ = new TripleDesForManagementKey(keyData, true));
#pragma warning restore CS0618
        }

        [Fact]
        public void ThreeBlocks_ProcessesAll()
        {
#pragma warning disable CS0618 // testing special TripleDES, disable warning about using special TripleDES
            byte[] keyData = new byte[] {
                0x5d, 0x71, 0xbe, 0x48, 0x9c, 0x9f, 0xe5, 0x14,
                0xd0, 0x55, 0x68, 0xa2, 0xa1, 0xcb, 0x5e, 0x3b,
                0x2b, 0xc7, 0x53, 0xc6, 0x11, 0xcd, 0x4d, 0xa4
            };

            byte[] dataToEncrypt = new byte[] {
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
            };

            byte[] result = new byte[24];

            using (var tDesObject = new TripleDesForManagementKey(keyData, true))
            {
                int dataLength = tDesObject.TransformBlock(dataToEncrypt, 0, 24, result, 0);

                Assert.Equal(24, dataLength);
            }
#pragma warning restore CS0618
        }

        [Fact]
        public void SetParity_CorrectResult()
        {
#pragma warning disable CS0618 // testing special TripleDES, disable warning about using special TripleDES
            byte[] keyData = new byte[24];
            byte[] allBytes = new byte[256];
            int currentIndex = 0;

            int nextIndex = 0;
            while ((nextIndex = GetNextKeyData(keyData, nextIndex)) > 0)
            {
                byte[] parityKey = TripleDesForManagementKey.SetParity(keyData);
                byte[] dotNetParity = FixupKeyParity(keyData);

                bool compareResult = dotNetParity.SequenceEqual(parityKey);

                Assert.True(compareResult);

                int maxCount = 256;
                if (nextIndex < 256)
                {
                    maxCount = nextIndex;
                }
                int index = 0;
                for (; currentIndex < maxCount; currentIndex++)
                {
                    allBytes[currentIndex] = parityKey[index];
                    index++;
                }
            }
#pragma warning restore CS0618
        }

        [Fact]
        public void RunVector_CorrectResult()
        {
#pragma warning disable CS0618 // testing special TripleDES, disable warning about using special TripleDES
            byte[] keyData = new byte[24];
            byte[] dataToProcess = new byte[8];
            byte[] result = new byte[8];
            byte[] expected = new byte[8];

            int nextIndex = 0;
            while ((nextIndex = GetNextVector(nextIndex, keyData, dataToProcess, expected, out bool isEncrypting)) > 0)
            {
                using (var tDesObject = new TripleDesForManagementKey(keyData, isEncrypting))
                {
                    _ = tDesObject.TransformBlock(dataToProcess, 0, 8, result, 0);

                    bool compareResult = expected.SequenceEqual(result);
                    Assert.True(compareResult);
                }
            }
#pragma warning restore CS0618
        }

        // Fill the input buffers with the vector associated with the index.
        // If the index is < 0 or > max, return -1, doing nothing to the buffers.
        // Set the out bool isEncrypting to true if this vector is for
        // encryption, false otherwise.
        private static int GetNextVector(
            int index,
            byte[] keyData,
            byte[] dataToProcess,
            byte[] expectedResult,
            out bool isEncrypting)
        {
            byte[] newKeyData;
            byte[] newData;
            byte[] newExpected;

            isEncrypting = true;
            switch (index)
            {
                default:
                    return -1;

                case 0:
                    newKeyData = new byte[] {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    };
                    newData = new byte[] {
                        0xfa, 0x17, 0xb0, 0xc7, 0x4a, 0x46, 0xb0, 0xa7
                    };
                    newExpected = new byte[] {
                        0x14, 0xcf, 0xc6, 0x5e, 0xea, 0xba, 0xdf, 0x1d
                    };
                    break;

                case 1:
                    newKeyData = new byte[] {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    };
                    newData = new byte[] {
                        0x0a, 0x77, 0x73, 0x0a, 0xaa, 0x1e, 0xc5, 0x66
                    };
                    newExpected = new byte[] {
                        0x34, 0x69, 0x5a, 0xdd, 0x8b, 0x09, 0x49, 0x2c
                    };
                    isEncrypting = false;
                    break;

                case 2:
                    newKeyData = new byte[] {
                        0x31, 0x40, 0x51, 0x61, 0x70, 0x80, 0x91, 0xA1,
                        0x30, 0x41, 0x50, 0x60, 0x70, 0x81, 0x91, 0xA1,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    };
                    newData = new byte[] {
                        0xfa, 0x17, 0xb0, 0xc7, 0x4a, 0x46, 0xb0, 0xa7
                    };
                    newExpected = new byte[] {
                        0x14, 0xcf, 0xc6, 0x5e, 0xea, 0xba, 0xdf, 0x1d
                    };
                    break;

                case 3:
                    newKeyData = new byte[] {
                        0x31, 0x40, 0x51, 0x61, 0x70, 0x80, 0x91, 0xA1,
                        0x30, 0x41, 0x50, 0x60, 0x70, 0x81, 0x91, 0xA1,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    };
                    newData = new byte[] {
                        0x0a, 0x77, 0x73, 0x0a, 0xaa, 0x1e, 0xc5, 0x66
                    };
                    newExpected = new byte[] {
                        0x34, 0x69, 0x5a, 0xdd, 0x8b, 0x09, 0x49, 0x2c
                    };
                    isEncrypting = false;
                    break;

                case 4:
                    newKeyData = new byte[] {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
                    newData = new byte[] {
                        0x14, 0xcf, 0xc6, 0x5e, 0xea, 0xba, 0xdf, 0x1d
                    };
                    newExpected = new byte[] {
                        0x33, 0x7e, 0xd7, 0x9a, 0x5e, 0x20, 0xd6, 0x1e
                    };
                    break;

                case 5:
                    newKeyData = new byte[] {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
                    };
                    newData = new byte[] {
                        0x2b, 0x46, 0x73, 0x0e, 0x85, 0x26, 0x4f, 0xec
                    };
                    newExpected = new byte[] {
                        0xd8, 0xc4, 0xa2, 0x74, 0xec, 0x1c, 0x3e, 0x7b
                    };
                    isEncrypting = false;
                    break;

                case 6:
                    newKeyData = new byte[] {
                        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                        0x36, 0x2C, 0x95, 0xA4, 0x02, 0x58, 0xBD, 0x8E,
                        0x37, 0x2D, 0x94, 0xA5, 0x03, 0x59, 0xBC, 0x8F
                    };
                    newData = new byte[] {
                        0xfa, 0x17, 0xb0, 0xc7, 0x4a, 0x46, 0xb0, 0xa7
                    };
                    newExpected = new byte[] {
                        0x60, 0x46, 0xbf, 0xb8, 0x0e, 0x27, 0x04, 0x16
                    };
                    break;

                case 7:
                    newKeyData = new byte[] {
                        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                        0x36, 0x2C, 0x95, 0xA4, 0x02, 0x58, 0xBD, 0x8E,
                        0x37, 0x2D, 0x94, 0xA5, 0x03, 0x59, 0xBC, 0x8F
                    };
                    newData = new byte[] {
                        0x0a, 0x77, 0x73, 0x0a, 0xaa, 0x1e, 0xc5, 0x66
                    };
                    newExpected = new byte[] {
                        0x2b, 0xc7, 0x53, 0xc6, 0x11, 0xcd, 0x4d, 0xa4
                    };
                    isEncrypting = false;
                    break;

                case 8:
                    newKeyData = new byte[] {
                        0xE0, 0xE0, 0xE0, 0xE0, 0xF0, 0xF0, 0xF0, 0xF1,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
                    newData = new byte[] {
                        0xd0, 0x55, 0x68, 0xa2, 0xa1, 0xcb, 0x5e, 0x3b
                    };
                    newExpected = new byte[] {
                        0xc9, 0xa0, 0x1c, 0x6b, 0xbd, 0xbc, 0x2c, 0x69
                    };
                    break;

                case 9:
                    newKeyData = new byte[] {
                        0xE0, 0xE0, 0xE0, 0xE0, 0xF0, 0xF0, 0xF0, 0xF1,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
                    newData = new byte[] {
                        0xa7, 0x37, 0x0b, 0x6e, 0x08, 0xed, 0xd9, 0x4b
                    };
                    newExpected = new byte[] {
                        0x5d, 0x71, 0xbe, 0x48, 0x9c, 0x9f, 0xe5, 0x14
                    };
                    isEncrypting = false;
                    break;

                case 10:
                    newKeyData = new byte[] {
                        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                        0x1F, 0x1F, 0x1F, 0x1E, 0x0E, 0x0E, 0x0E, 0x0F
                    };
                    newData = new byte[] {
                        0xd0, 0x55, 0x68, 0xa2, 0xa1, 0xcb, 0x5e, 0x3b
                    };
                    newExpected = new byte[] {
                        0x87, 0x1c, 0x67, 0xe8, 0xf2, 0xf4, 0xc2, 0xea
                    };
                    break;

                case 11:
                    newKeyData = new byte[] {
                        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                        0x1F, 0x1F, 0x1F, 0x1E, 0x0E, 0x0E, 0x0F, 0x0E
                    };
                    newData = new byte[] {
                        0xa7, 0x37, 0x0b, 0x6e, 0x08, 0xed, 0xd9, 0x4b
                    };
                    newExpected = new byte[] {
                        0xf1, 0xdd, 0x37, 0xe1, 0x60, 0xfe, 0xed, 0xcb
                    };
                    isEncrypting = false;
                    break;
            }

            Array.Copy(newKeyData, keyData, 24);
            Array.Copy(newData, dataToProcess, 8);
            Array.Copy(newExpected, expectedResult, 8);

            return index + 1;
        }

        // Return the next 24 bytes in the sequence.
        // Look at nextIndex. If it is < 0 or > max, don't place anything into
        // the keyData buffer and return -1.
        // If nextIndex is <= max, get 24 bytes. Return the new nextIndex, which
        // will be the incoming nextIndex + 24. The new nextIndex can be > max.
        // If the incoming nextIndex is good, we want to return bytes in the
        // keyData buffer, so don't return -1. Only return -1 when there will be
        // no bytes placed into the buffer.
        private static int GetNextKeyData(byte[] keyData, int nextIndex)
        {
            if (nextIndex < 0 || nextIndex > 255)
            {
                return -1;
            }

            byte currentValue = (byte)nextIndex;
            for (int index = 0; index < 24; index++)
            {
                keyData[index] = currentValue;
                currentValue++;
            }

            return nextIndex + 24;
        }

        // This is a copy of the source code in .NET that builds a new buffer
        // containing the key data with the parity bits set.
        public static byte[] FixupKeyParity(byte[] key)
        {
            byte[] oddParityKey = new byte[key.Length];
            for (int index = 0; index < key.Length; index++)
            {
                // Get the bits we are interested in
                oddParityKey[index] = (byte)(key[index] & 0xfe);

                // Get the parity of the sum of the previous bits
                byte tmp1 = (byte)((oddParityKey[index] & 0xF) ^ (oddParityKey[index] >> 4));
                byte tmp2 = (byte)((tmp1 & 0x3) ^ (tmp1 >> 2));
                byte sumBitsMod2 = (byte)((tmp2 & 0x1) ^ (tmp2 >> 1));

                // We need to set the last bit in oddParityKey[index] to the negation
                // of the last bit in sumBitsMod2
                if (sumBitsMod2 == 0)
                {
                    oddParityKey[index] |= 1;
                }
            }
            return oddParityKey;
        }
    }
}
