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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    public class KeyAgreeResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new AuthenticateKeyAgreeResponse(responseApdu: null));
#pragma warning restore CS8625
        }

        [Theory]
        [InlineData(SWConstants.Success, ResponseStatus.Success)]
        [InlineData(SWConstants.SecurityStatusNotSatisfied, ResponseStatus.AuthenticationRequired)]
        [InlineData(SWConstants.FunctionNotSupported, ResponseStatus.Failed)]
        public void Constructor_SetsStatusWordCorrectly(short statusWord, ResponseStatus expectedStatus)
        {
            var sw1 = unchecked((byte)(statusWord >> 8));
            var sw2 = unchecked((byte)statusWord);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new AuthenticateKeyAgreeResponse(responseApdu);

            var Status = response.Status;

            Assert.Equal(expectedStatus, Status);
        }

        [Theory]
        [InlineData(SWConstants.Success)]
        [InlineData(SWConstants.SecurityStatusNotSatisfied)]
        [InlineData(SWConstants.FunctionNotSupported)]
        public void Constructor_SetsStatusCorrectly(short statusWord)
        {
            var sw1 = unchecked((byte)(statusWord >> 8));
            var sw2 = unchecked((byte)statusWord);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new AuthenticateKeyAgreeResponse(responseApdu);

            var StatusWord = response.StatusWord;

            Assert.Equal(statusWord, StatusWord);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void GetData_ReturnsSharedSecret(PivAlgorithm algorithm)
        {
            var sharedSecret = GetSharedSecret(algorithm);
            var responseApdu = GetResponseApdu(algorithm);

            var response = new AuthenticateKeyAgreeResponse(responseApdu);

            IReadOnlyList<byte> getData = response.GetData();

            var compareResult = sharedSecret.SequenceEqual(getData);

            Assert.True(compareResult);
        }

        [Fact]
        public void GetData_NoAuth_Exception()
        {
            byte sw1 = SWConstants.SecurityStatusNotSatisfied >> 8;
            var sw2 = unchecked((byte)SWConstants.SecurityStatusNotSatisfied);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new AuthenticateKeyAgreeResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        [Fact]
        public void FailResponseApdu_FunctionNotSupported_ExceptionOnGetData()
        {
            byte sw1 = SWConstants.FunctionNotSupported >> 8;
            var sw2 = unchecked((byte)SWConstants.FunctionNotSupported);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new AuthenticateKeyAgreeResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        [Fact]
        public void FailResponseApdu_WarningNvmUnchanged_ExceptionOnGetData()
        {
            byte sw1 = SWConstants.WarningNvmUnchanged >> 8;
            var sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new AuthenticateKeyAgreeResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        private static ResponseApdu GetResponseApdu(PivAlgorithm algorithm)
        {
            var apduData = GetResponseApduData(algorithm);
            return new ResponseApdu(apduData);
        }

        // Get the data that makes up a response APDU for the given algorithm.
        // This will return the full APDU data:
        // 7C len 82 len sharedSecret 90 00
        private static byte[] GetResponseApduData(PivAlgorithm algorithm)
        {
            var sharedSecret = GetSharedSecret(algorithm);
            byte[] statusWord = { 0x90, 0x00 };

            var prefix = algorithm switch
            {
                PivAlgorithm.EccP384 => new byte[] { 0x7C, 0x32, 0x82, 0x30 },

                _ => new byte[] { 0x7C, 0x22, 0x82, 0x20 }
            };

            var returnValue = prefix.Concat(sharedSecret);
            returnValue = returnValue.Concat(statusWord);

            return returnValue.ToArray();
        }

        private static byte[] GetSharedSecret(PivAlgorithm algorithm)
        {
            return algorithm switch
            {
                PivAlgorithm.EccP384 => new byte[]
                {
                    0x60, 0x8C, 0x01, 0xA6, 0xDA, 0x36, 0xBA, 0xA0, 0xFE, 0xA5, 0x18, 0x16, 0x7E, 0xEA, 0x16, 0x51,
                    0xB1, 0x62, 0x58, 0xC5, 0x1A, 0x84, 0xEB, 0x9C, 0x12, 0x6C, 0x8E, 0x6A, 0x3E, 0x6C, 0x1B, 0x40,
                    0x03, 0x26, 0xA6, 0x79, 0x41, 0x78, 0xDA, 0xEE, 0x08, 0x9A, 0xDA, 0x89, 0xCC, 0xF9, 0x27, 0xF0
                },

                _ => new byte[]
                {
                    0xCF, 0x03, 0x5B, 0xFF, 0x4B, 0x3A, 0x9F, 0xE6, 0x49, 0xFF, 0x51, 0x76, 0x23, 0x62, 0x74, 0xA4,
                    0x2E, 0x83, 0x18, 0x90, 0x5C, 0x92, 0xB8, 0x89, 0x6F, 0xA7, 0x86, 0x0B, 0xB6, 0x1C, 0x6E, 0x60
                }
            };
        }
    }
}
