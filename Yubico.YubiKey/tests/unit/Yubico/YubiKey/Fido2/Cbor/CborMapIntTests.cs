// Copyright 2023 Yubico AB
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
using Xunit;

namespace Yubico.YubiKey.Fido2.Cbor
{
    public class CborMapIntTests
    {
        private readonly long[] _longValues =
        {
            -1, 0x05,
            -7, 0x05,
            -23, 0x05,
            -24, 0x05,
            -25, 0x05,
            -256, 0x05,
            -32767, 0x05,
            -32768, 0x05,
            -16000000, 0x05,
            -8380000, 0x05,
            -2147483648, 0x05,
            -2147483649, 0x04,
            -9223372036854775808, 0x04
        };

        // Each pair is (number to test, flag)
        // where the flag indicates whether it is readable as
        //   int       0x01
        //   uint      0x02
        //   long      0x04
        //   ulong     0x08
        private readonly ulong[] _ulongValues =
        {
            0, 0x0F,
            1, 0x0F,
            2, 0x0F,
            16, 0x0F,
            23, 0x0F,
            24, 0x0F,
            180, 0x0F,
            255, 0x0F,
            0x0100, 0x0F,
            0x8000, 0x0F,
            0xffff, 0x0F,
            0x00010000, 0x0F,
            0x00800000, 0x0F,
            0x00ffffff, 0x0F,
            0x01000000, 0x0F,
            0x7fffffff, 0x0F,
            0x0000000080000000, 0x0E,
            0x00000000ffffffff, 0x0E,
            0x420a57ce911d03d5, 0x0C,
            0xc20a57ce911d03d5, 0x08
        };

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadIntegers_Success(bool isSigned)
        {
            var index = 0;
            bool isValid;

            do
            {
                var encoding = GetNextEncoding(isSigned, ref index, out var flags);
                isValid = TestInt32(encoding, flags);
                Assert.True(isValid);
                isValid = TestUInt32(encoding, flags);
                Assert.True(isValid);
                isValid = TestInt64(encoding, flags);
                Assert.True(isValid);
                isValid = TestUInt64(encoding, flags);
                Assert.True(isValid);
            } while (index > 0);
        }

        private bool TestInt32(byte[] encoding, int flags)
        {
            var cborMap = new CborMap<int>(encoding);
            var isSigned = cborMap.ReadBoolean(key: 1);
            var index = cborMap.ReadInt32(key: 2);

            if ((flags & 0x01) != 0)
            {
                var getValue = cborMap.ReadInt32(key: 3);
                if (isSigned)
                {
                    var expectedValue = _longValues[index];
                    if (getValue == expectedValue)
                    {
                        return true;
                    }
                }
                else
                {
                    var expectedValue = _ulongValues[index];
                    if ((ulong)getValue == expectedValue)
                    {
                        return true;
                    }
                }

                return false;
            }

            try
            {
                _ = cborMap.ReadInt32(key: 3);
            }
            catch (InvalidCastException)
            {
                return true;
            }

            return false;
        }

        private bool TestUInt32(byte[] encoding, int flags)
        {
            var cborMap = new CborMap<int>(encoding);
            var isSigned = cborMap.ReadBoolean(key: 1);
            var index = cborMap.ReadInt32(key: 2);

            if ((flags & 0x02) != 0)
            {
                var getValue = cborMap.ReadUInt32(key: 3);
                if (isSigned)
                {
                    var expectedValue = _longValues[index];
                    if (getValue == expectedValue)
                    {
                        return true;
                    }
                }
                else
                {
                    var expectedValue = _ulongValues[index];
                    if (getValue == expectedValue)
                    {
                        return true;
                    }
                }

                return false;
            }

            try
            {
                _ = cborMap.ReadUInt32(key: 3);
            }
            catch (InvalidCastException)
            {
                return true;
            }

            return false;
        }

        private bool TestInt64(byte[] encoding, int flags)
        {
            var cborMap = new CborMap<int>(encoding);
            var isSigned = cborMap.ReadBoolean(key: 1);
            var index = cborMap.ReadInt32(key: 2);

            if ((flags & 0x04) != 0)
            {
                var getValue = cborMap.ReadInt64(key: 3);
                if (isSigned)
                {
                    var expectedValue = _longValues[index];
                    if (getValue == expectedValue)
                    {
                        return true;
                    }
                }
                else
                {
                    var expectedValue = _ulongValues[index];
                    if ((ulong)getValue == expectedValue)
                    {
                        return true;
                    }
                }

                return false;
            }

            try
            {
                _ = cborMap.ReadInt64(key: 3);
            }
            catch (InvalidCastException)
            {
                return true;
            }

            return false;
        }

        private bool TestUInt64(byte[] encoding, int flags)
        {
            var cborMap = new CborMap<int>(encoding);
            var isSigned = cborMap.ReadBoolean(key: 1);
            var index = cborMap.ReadInt32(key: 2);

            if ((flags & 0x08) != 0)
            {
                var getValue = cborMap.ReadUInt64(key: 3);
                if (isSigned)
                {
                    var expectedValue = _longValues[index];
                    if ((long)getValue == expectedValue)
                    {
                        return true;
                    }
                }
                else
                {
                    var expectedValue = _ulongValues[index];
                    if (getValue == expectedValue)
                    {
                        return true;
                    }
                }

                return false;
            }

            try
            {
                _ = cborMap.ReadUInt64(key: 3);
            }
            catch (InvalidCastException)
            {
                return true;
            }

            return false;
        }

        private byte[] GetNextEncoding(bool isSigned, ref int index, out int flags)
        {
            var cborWriter = new CborWriter();
            cborWriter.WriteStartMap(definiteLength: 3);
            cborWriter.WriteInt32(value: 1);
            cborWriter.WriteBoolean(isSigned);
            cborWriter.WriteInt32(value: 2);
            cborWriter.WriteInt32(index);
            cborWriter.WriteInt32(value: 3);

            if (isSigned)
            {
                var currentValue = _longValues[index];
                flags = (int)_longValues[index + 1];
                index += 2;
                cborWriter.WriteInt64(currentValue);

                if (index >= _longValues.Length)
                {
                    index = -1;
                }
            }
            else
            {
                var currentValue = _ulongValues[index];
                flags = (int)_ulongValues[index + 1];
                index += 2;
                cborWriter.WriteUInt64(currentValue);

                if (index >= _ulongValues.Length)
                {
                    index = -1;
                }
            }

            cborWriter.WriteEndMap();

            return cborWriter.Encode();
        }
    }
}
