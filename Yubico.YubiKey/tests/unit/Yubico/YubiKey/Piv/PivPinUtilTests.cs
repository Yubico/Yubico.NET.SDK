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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv
{
    public class PivPinUtilTests
    {
        [Theory]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void IsValidPinLength_CorrectPinLength_ReturnsTrue(int pinLength)
        {
            bool isValidLength = PivPinUtilities.IsValidPinLength(pinLength);
            Assert.True(isValidLength);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(17)]
        public void IsValidPinLength_BadPinLength_ReturnsFalse(int pinLength)
        {
            bool isValidLength = PivPinUtilities.IsValidPinLength(pinLength);
            Assert.False(isValidLength);
        }

        [Theory]
        [InlineData(0x63C4)]
        [InlineData(0x63CF)]
        [InlineData(0x63C0)]
        public void HasRetryCountSW_WrongPinWithRetriesStatusWord_ReturnsTrue(short statusWord)
        {
            bool result = PivPinUtilities.HasRetryCount(statusWord);

            Assert.True(result);
        }

        [Theory]
        [InlineData(SWConstants.Success)]
        [InlineData(SWConstants.AuthenticationMethodBlocked)]
        [InlineData(SWConstants.SecurityStatusNotSatisfied)]
        [InlineData(SWConstants.ConditionsNotSatisfied)]
        [InlineData(SWConstants.FunctionError)]
        public void HasRetryCountSW_NotWrongPinStatusWord_ReturnsFalse(short statusWord)
        {
            bool result = PivPinUtilities.HasRetryCount(statusWord);

            Assert.False(result);
        }

        [Theory]
        [InlineData(0x63C4)]
        [InlineData(0x63CF)]
        [InlineData(0x63C0)]
        public void ParseSW_WrongPin_ReturnsAuthRequired(short statusWord)
        {
            int count = (int)(statusWord & 15);
            int parseResponse = PivPinUtilities.GetRetriesRemaining(statusWord);

            Assert.Equal(count, parseResponse);
        }

        [Theory]
        [InlineData(SWConstants.Success)]
        [InlineData(SWConstants.AuthenticationMethodBlocked)]
        [InlineData(SWConstants.SecurityStatusNotSatisfied)]
        [InlineData(SWConstants.ConditionsNotSatisfied)]
        [InlineData(SWConstants.ExecutionError)]
        public void ParseSW_InvalidStatusWord_ThrowsInvalidOperationException(short statusWord)
        {
            _ = Assert.Throws<InvalidOperationException>(() => PivPinUtilities.GetRetriesRemaining(statusWord));
        }

        [Fact]
        public void CopyPin_NullPin_ThrowsException()
        {
            _ = Assert.Throws<ArgumentException>(() => PivPinUtilities.CopySinglePinWithPadding(null));
        }

        [Fact]
        public void CopyPin_BadPin_ThrowsException()
        {
            byte[] badPin = new byte[] { 0x31, 0x32, 0x33 };
            _ = Assert.Throws<ArgumentException>(() => PivPinUtilities.CopySinglePinWithPadding(badPin));
        }

        [Fact]
        public void CopyPin_ReturnsPaddedPin()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            byte[] paddedPin = PivPinUtilities.CopySinglePinWithPadding(pin);

            Assert.Equal(8, paddedPin.Length);

            bool compareResult = true;
            int index = 0;
            for (; index < pin.Length; index++)
            {
                if (paddedPin[index] != pin[index])
                {
                    compareResult = false;
                }
            }

            for (; index < 8; index++)
            {
                if (paddedPin[index] != 0xFF)
                {
                    compareResult = false;
                }
            }

            Assert.True(compareResult);
        }

        [Fact]
        public void CopyTwoPins_NullPin1_ThrowsException()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            _ = Assert.Throws<ArgumentException>(() => PivPinUtilities.CopyTwoPinsWithPadding(null, pin));
        }

        [Fact]
        public void CopyTwoPins_NullPin2_ThrowsException()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            _ = Assert.Throws<ArgumentException>(() => PivPinUtilities.CopyTwoPinsWithPadding(pin, null));
        }

        [Fact]
        public void CopyTwoPins_BadPin1_ThrowsException()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35 };
            byte[] puk = new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46 };
            _ = Assert.Throws<ArgumentException>(() => PivPinUtilities.CopyTwoPinsWithPadding(pin, puk));
        }

        [Fact]
        public void CopyTwoPins_BadPin2_ThrowsException()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            byte[] puk = new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45 };
            _ = Assert.Throws<ArgumentException>(() => PivPinUtilities.CopyTwoPinsWithPadding(pin, puk));
        }

        [Fact]
        public void Copy2Pins_ReturnsPaddedPin()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            byte[] puk = new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46 };
            byte[] paddedPin = PivPinUtilities.CopyTwoPinsWithPadding(pin, puk);

            Assert.Equal(16, paddedPin.Length);

            bool compareResult = true;
            int index = 0;
            for (; index < pin.Length; index++)
            {
                if (paddedPin[index] != pin[index])
                {
                    compareResult = false;
                }
            }

            for (; index < 8; index++)
            {
                if (paddedPin[index] != 0xFF)
                {
                    compareResult = false;
                }
            }

            for (index = 0; index < puk.Length; index++)
            {
                if (paddedPin[index + 8] != puk[index])
                {
                    compareResult = false;
                }
            }

            for (; index < 8; index++)
            {
                if (paddedPin[index + 8] != 0xFF)
                {
                    compareResult = false;
                }
            }

            Assert.True(compareResult);
        }
    }
}
