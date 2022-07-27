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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class BaseYubiHsmAuthResponseTests
    {
        public class SampleYubiHsmAuthResponse : BaseYubiHsmAuthResponse
        {
            public SampleYubiHsmAuthResponse(ResponseApdu responseApdu) : base(responseApdu)
            {
            }

            public bool HasRetries => StatusWordContainsRetries;
            public int? RetryCount => RetriesRemaining;
        }

        private const string AuthenticationRequired0RetriesStatusMessage = "Wrong password or authentication key. Retries remaining: 0.";
        private const string AuthenticationRequired15RetriesStatusMessage = "Wrong password or authentication key. Retries remaining: 15.";
        private const string SecurityStatusNotSatisfiedStatusMessage = "The device was not touched.";
        private const string AuthenticationMethodBlockedStatusMessage = "The entry is invalid.";
        private const string ReferenceDataUnusableStatusMessage = "Invalid authentication data.";
        private const string SuccessStatusMessage = "The command succeeded.";

        [Fact]
        public void Constructor_GivenSuccessApdu_SetsCorrectStatusWord()
        {
            short expectedSW = SWConstants.Success;

            SampleYubiHsmAuthResponse response = new SampleYubiHsmAuthResponse(
                new ResponseApdu(new byte[] { }, expectedSW));

            Assert.Equal(expectedSW, response.StatusWord);
        }

        [Theory]
        [InlineData(SWConstants.VerifyFail, ResponseStatus.AuthenticationRequired)]
        [InlineData(0x63cf, ResponseStatus.AuthenticationRequired)]
        [InlineData(SWConstants.SecurityStatusNotSatisfied, ResponseStatus.RetryWithTouch)]
        [InlineData(SWConstants.AuthenticationMethodBlocked, ResponseStatus.Failed)]
        [InlineData(SWConstants.ReferenceDataUnusable, ResponseStatus.Failed)]
        [InlineData(SWConstants.Success, ResponseStatus.Success)]
        public void Status_GivenStatusWord_ReturnsCorrectResponseStatus(short responseSw, ResponseStatus expectedStatus)
        {
            SampleYubiHsmAuthResponse response = new SampleYubiHsmAuthResponse(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.Equal(expectedStatus, response.Status);
        }

        [Theory]
        [InlineData(SWConstants.VerifyFail, AuthenticationRequired0RetriesStatusMessage)]
        [InlineData(0x63cf, AuthenticationRequired15RetriesStatusMessage)]
        [InlineData(SWConstants.SecurityStatusNotSatisfied, SecurityStatusNotSatisfiedStatusMessage)]
        [InlineData(SWConstants.AuthenticationMethodBlocked, AuthenticationMethodBlockedStatusMessage)]
        [InlineData(SWConstants.ReferenceDataUnusable, ReferenceDataUnusableStatusMessage)]
        [InlineData(SWConstants.Success, SuccessStatusMessage)]
        public void Status_GivenStatusWord_ReturnsCorrectResponseMessage(short responseSw, string expectedMessage)
        {
            SampleYubiHsmAuthResponse response = new SampleYubiHsmAuthResponse(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.Equal(expectedMessage, response.StatusMessage);
        }

        [Theory]
        [InlineData(SWConstants.VerifyFail, true)]
        [InlineData(0x63cf, true)]
        [InlineData(SWConstants.Success, false)]
        [InlineData(SWConstants.InvalidParameter, false)]
        public void SwContainsRetries_GivenSw_ReturnsTrueWhenRetriesPresent(short responseSw, bool expectedResponse)
        {
            SampleYubiHsmAuthResponse response = new SampleYubiHsmAuthResponse(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.Equal(expectedResponse, response.HasRetries);
        }

        [Theory]
        [InlineData(SWConstants.VerifyFail, 0)]
        [InlineData(0x63cf, 15)]
        public void RetriesRemaining_GivenSwWithRetryCount_ReturnsCorrectRetryCount(short responseSw, int? expectedCount)
        {
            SampleYubiHsmAuthResponse response = new SampleYubiHsmAuthResponse(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.Equal(expectedCount, response.RetryCount);
        }

        [Theory]
        [InlineData(SWConstants.Success)]
        [InlineData(SWConstants.InvalidParameter)]
        public void RetriesRemaining_GivenSwNoRetryCount_ReturnsNull(short responseSw)
        {
            SampleYubiHsmAuthResponse response = new SampleYubiHsmAuthResponse(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.True(!response.RetryCount.HasValue);
        }
    }
}
