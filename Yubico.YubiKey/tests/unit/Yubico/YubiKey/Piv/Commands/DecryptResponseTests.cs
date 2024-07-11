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
    public class DecryptResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new AuthenticateDecryptResponse(responseApdu: null));
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

            var response = new AuthenticateDecryptResponse(responseApdu);

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

            var response = new AuthenticateDecryptResponse(responseApdu);

            var StatusWord = response.StatusWord;

            Assert.Equal(statusWord, StatusWord);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void GetData_ReturnsDecrypted(PivAlgorithm algorithm)
        {
            var decryptedData = GetDecryptedData(algorithm);
            var responseApdu = GetResponseApdu(algorithm);

            var response = new AuthenticateDecryptResponse(responseApdu);

            IReadOnlyList<byte> getData = response.GetData();

            var compareResult = decryptedData.SequenceEqual(getData);

            Assert.True(compareResult);
        }

        [Fact]
        public void GetData_NoAuth_Exception()
        {
            var sw1 = (byte)(SWConstants.SecurityStatusNotSatisfied >> 8);
            var sw2 = unchecked((byte)SWConstants.SecurityStatusNotSatisfied);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new AuthenticateDecryptResponse(responseApdu);
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
#pragma warning restore CS8625
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void GetData_Success_NoExceptionThrown(PivAlgorithm algorithm)
        {
            var responseApdu = GetResponseApdu(algorithm);

            var response = new AuthenticateDecryptResponse(responseApdu);

            void action()
            {
                response.GetData();
            }

            var ex = Record.Exception(action);
            Assert.Null(ex);
        }

        [Fact]
        public void GetData_ErrorResponse_Exception()
        {
            var sw1 = (byte)(SWConstants.FunctionNotSupported >> 8);
            var sw2 = unchecked((byte)SWConstants.FunctionNotSupported);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new AuthenticateDecryptResponse(responseApdu);

#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
#pragma warning restore CS8625
        }

        [Fact]
        public void FailResponseApdu_ExceptionOnGetData()
        {
            var sw1 = (byte)(SWConstants.WarningNvmUnchanged >> 8);
            var sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new AuthenticateDecryptResponse(responseApdu);

#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
#pragma warning restore CS8625
        }

        private static ResponseApdu GetResponseApdu(PivAlgorithm algorithm)
        {
            var apduData = GetResponseApduData(algorithm);
            return new ResponseApdu(apduData);
        }

        // Get the data that makes up a response APDU for the given algorithm.
        // This will return the full APDU data:
        // 7C len 82 len decryptedData 90 00
        private static byte[] GetResponseApduData(PivAlgorithm algorithm)
        {
            var decryptedData = GetDecryptedData(algorithm);
            var statusWord = new byte[] { 0x90, 0x00 };

            var prefix = algorithm switch
            {
                PivAlgorithm.Rsa2048 => new byte[] { 0x7C, 0x82, 0x01, 0x04, 0x82, 0x82, 0x01, 0x00 },

                _ => new byte[] { 0x7C, 0x81, 0x83, 0x82, 0x81, 0x80 }
            };

            var returnValue = prefix.Concat(decryptedData);
            returnValue = returnValue.Concat(statusWord);

            return returnValue.ToArray();
        }

        private static byte[] GetDecryptedData(PivAlgorithm algorithm)
        {
            return algorithm switch
            {
                PivAlgorithm.Rsa2048 => new byte[]
                {
                    0x05, 0xA4, 0x60, 0x64, 0x7D, 0x4C, 0x0B, 0x8F, 0x48, 0x2D, 0xC5, 0x50, 0x1D, 0x9D, 0x1F, 0xD2,
                    0xCC, 0x7A, 0x14, 0x74, 0x66, 0x1D, 0xE9, 0x6A, 0x1E, 0x0A, 0xD9, 0x39, 0x5E, 0x1F, 0x0F, 0xFD,
                    0x94, 0xB6, 0xA9, 0x98, 0x84, 0x52, 0xD1, 0xC4, 0xC1, 0x40, 0x1A, 0x5B, 0xCA, 0x32, 0xC0, 0xE9,
                    0x3C, 0xE8, 0xA6, 0xFF, 0xED, 0xE3, 0x11, 0xFF, 0x60, 0x70, 0x76, 0xA6, 0xE8, 0xFA, 0x53, 0xC6,
                    0xEE, 0xEE, 0xA4, 0xB3, 0xF3, 0xD3, 0xD5, 0x47, 0x97, 0x4A, 0x9B, 0x16, 0xB0, 0x50, 0x46, 0xC6,
                    0x24, 0x6E, 0x27, 0x46, 0x21, 0x21, 0x20, 0x02, 0xE6, 0x3F, 0x4C, 0x4A, 0xF0, 0xA0, 0xB6, 0x5B,
                    0x8A, 0x52, 0x51, 0xAD, 0x41, 0x00, 0xC5, 0x7F, 0xA5, 0x1E, 0x11, 0x17, 0x37, 0x4A, 0x71, 0xC7,
                    0x55, 0xF4, 0x90, 0x06, 0xD4, 0x13, 0xC3, 0x91, 0xC9, 0x2F, 0x4B, 0x26, 0xA0, 0xEA, 0x1E, 0x45,
                    0x07, 0xC5, 0x4C, 0x1B, 0x9B, 0xB2, 0xE1, 0x81, 0xF9, 0x00, 0x46, 0xB5, 0x02, 0x6D, 0x08, 0xC5,
                    0x00, 0x59, 0x03, 0x42, 0xCC, 0xAE, 0x6E, 0xF5, 0xA4, 0x31, 0x7E, 0x45, 0x84, 0x4E, 0x3B, 0xE8,
                    0x62, 0x18, 0x87, 0x3E, 0xE7, 0x0F, 0xF2, 0xFC, 0xE9, 0xE2, 0xED, 0x08, 0x91, 0xEA, 0xAF, 0xF1,
                    0x98, 0xE6, 0x8B, 0x98, 0x4D, 0xD3, 0x83, 0xD7, 0xD7, 0xB5, 0x44, 0xFA, 0x09, 0x96, 0xD3, 0xAA,
                    0xF7, 0x72, 0x32, 0xE3, 0xA5, 0x4C, 0xD8, 0x27, 0xD7, 0x4C, 0xB4, 0x94, 0x19, 0x88, 0x52, 0x2F,
                    0x60, 0x8C, 0x01, 0xA6, 0xDA, 0x36, 0xBA, 0xA0, 0xFE, 0xA5, 0x18, 0x16, 0x7E, 0xEA, 0x16, 0x51,
                    0xB1, 0x62, 0x58, 0xC5, 0x1A, 0x84, 0xEB, 0x9C, 0x12, 0x6C, 0x8E, 0x6A, 0x3E, 0x6C, 0x1B, 0x40,
                    0x03, 0x26, 0xA6, 0x79, 0x41, 0x78, 0xDA, 0xEE, 0x08, 0x9A, 0xDA, 0x89, 0xCC, 0xF9, 0x27, 0xF0
                },

                _ => new byte[]
                {
                    0x00, 0x02, 0x70, 0xF1, 0x33, 0x19, 0x74, 0x21, 0x99, 0x36, 0x78, 0x6F, 0x2F, 0x5A, 0x77, 0x67,
                    0x99, 0xD9, 0x0A, 0x37, 0xAA, 0x5E, 0x16, 0xB9, 0x90, 0xA3, 0x1D, 0x6B, 0xD8, 0xF1, 0x31, 0x43,
                    0xA4, 0x8F, 0xD8, 0xDC, 0x33, 0x0D, 0xA1, 0xA6, 0x87, 0x73, 0x07, 0x57, 0x73, 0xAD, 0x90, 0x1B,
                    0xA4, 0x7D, 0x99, 0x5F, 0x91, 0x26, 0xF5, 0x40, 0x15, 0x3B, 0x82, 0xE2, 0xCD, 0x76, 0xC0, 0x32,
                    0x56, 0xE2, 0x46, 0x47, 0x2C, 0xE3, 0x1E, 0x0E, 0xB6, 0xB4, 0xED, 0xD6, 0x28, 0x80, 0xF0, 0x72,
                    0xD1, 0x2C, 0x06, 0x20, 0x0B, 0x8D, 0x33, 0xB2, 0xEF, 0x7E, 0xB7, 0x09, 0x2A, 0xDF, 0x5B, 0x00,
                    0xCF, 0x03, 0x5B, 0xFF, 0x4B, 0x3A, 0x9F, 0xE6, 0x49, 0xFF, 0x51, 0x76, 0x23, 0x62, 0x74, 0xA4,
                    0x2E, 0x83, 0x18, 0x90, 0x5C, 0x92, 0xB8, 0x89, 0x6F, 0xA7, 0x86, 0x0B, 0xB6, 0x1C, 0x6E, 0x60
                }
            };
        }
    }
}
