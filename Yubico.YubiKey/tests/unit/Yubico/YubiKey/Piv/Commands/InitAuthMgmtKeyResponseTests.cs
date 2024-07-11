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
    public class InitAuthMgmtKeyResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() =>
                new InitializeAuthenticateManagementKeyResponse(responseApdu: null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_InvalidLength_CorrectException()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(
                new byte[] { 0x7C, 0x09, 0x81, 0x07, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() =>
                new InitializeAuthenticateManagementKeyResponse(responseApdu));
        }

        [Fact]
        public void Constructor_InvalidT0_CorrectException()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(
                new byte[] { 0x78, 0x0A, 0x81, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() =>
                new InitializeAuthenticateManagementKeyResponse(responseApdu));
        }

        [Fact]
        public void Constructor_InvalidT2_CorrectException()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(
                new byte[] { 0x7C, 0x0A, 0x82, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() =>
                new InitializeAuthenticateManagementKeyResponse(responseApdu));
        }

        [Fact]
        public void Constructor_InvalidL1_CorrectException()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(
                new byte[] { 0x7C, 0x0A, 0x81, 0x07, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() =>
                new InitializeAuthenticateManagementKeyResponse(responseApdu));
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(
                new byte[] { 0x7C, 0x0A, 0x81, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });

            var response = new InitializeAuthenticateManagementKeyResponse(responseApdu);

            Assert.Equal(SWConstants.Success, response.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(
                new byte[] { 0x7C, 0x0A, 0x81, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });

            var response = new InitializeAuthenticateManagementKeyResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Constructor_SuccessResponseApdu_GetDataCorrectBool(bool isMutual)
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            byte tag2 = 0x81;
            if (isMutual)
            {
                tag2 = 0x80;
            }

            var responseApdu = new ResponseApdu(
                new byte[] { 0x7C, 0x0A, tag2, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });

            var response = new InitializeAuthenticateManagementKeyResponse(responseApdu);

            var (isMutualAuth, clientAuthenticationChallenge) = response.GetData();

            Assert.Equal(expected: 8, clientAuthenticationChallenge.Length);
            Assert.Equal(isMutual, isMutualAuth);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_GetDataCorrectBytes()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var expected = new List<byte>(
                new byte[8] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 });
            var responseApdu = new ResponseApdu(
                new byte[] { 0x7C, 0x0A, 0x81, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });

            var response = new InitializeAuthenticateManagementKeyResponse(responseApdu);

            var (isMutualAuth, clientAuthenticationChallenge) = response.GetData();

            var compareResult = expected.SequenceEqual(clientAuthenticationChallenge.ToArray());

            Assert.False(isMutualAuth);
            Assert.True(compareResult);
        }

        [Fact]
        public void Constructor_FailResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = (byte)(SWConstants.ConditionsNotSatisfied >> 8);
            var sw2 = unchecked((byte)SWConstants.ConditionsNotSatisfied);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new InitializeAuthenticateManagementKeyResponse(responseApdu);

            Assert.Equal(SWConstants.ConditionsNotSatisfied, response.StatusWord);
        }

        [Fact]
        public void Constructor_FailResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = (byte)(SWConstants.ConditionsNotSatisfied >> 8);
            var sw2 = unchecked((byte)SWConstants.ConditionsNotSatisfied);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new InitializeAuthenticateManagementKeyResponse(responseApdu);

            Assert.Equal(ResponseStatus.ConditionsNotSatisfied, response.Status);
        }

        [Fact]
        public void Constructor_FailResponseApdu_ThrowOnGetData()
        {
            var sw1 = (byte)(SWConstants.ConditionsNotSatisfied >> 8);
            var sw2 = unchecked((byte)SWConstants.ConditionsNotSatisfied);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new InitializeAuthenticateManagementKeyResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }
    }
}
