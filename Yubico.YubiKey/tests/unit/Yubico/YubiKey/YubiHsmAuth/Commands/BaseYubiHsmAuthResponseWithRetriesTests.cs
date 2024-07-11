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
    public class BaseYubiHsmAuthResponseWithRetriesTests
    {
        public class SampleYubiHsmAuthResponseWithRetries : BaseYubiHsmAuthResponseWithRetries
        {
            public SampleYubiHsmAuthResponseWithRetries(ResponseApdu responseApdu) : base(responseApdu)
            {
            }

            public bool HasRetries => StatusWordContainsRetries;
        }

        private const string AuthenticationRequired0RetriesStatusMessage =
            "Wrong password or authentication key. Retries remaining: 0.";

        private const string AuthenticationRequired15RetriesStatusMessage =
            "Wrong password or authentication key. Retries remaining: 15.";

        [Theory]
        [InlineData(SWConstants.VerifyFail, ResponseStatus.AuthenticationRequired)]
        [InlineData(0x63cf, ResponseStatus.AuthenticationRequired)]
        public void Status_GivenStatusWord_ReturnsCorrectResponseStatus(short responseSw, ResponseStatus expectedStatus)
        {
            SampleYubiHsmAuthResponseWithRetries response = new SampleYubiHsmAuthResponseWithRetries(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.Equal(expectedStatus, response.Status);
        }

        [Theory]
        [InlineData(SWConstants.VerifyFail, AuthenticationRequired0RetriesStatusMessage)]
        [InlineData(0x63cf, AuthenticationRequired15RetriesStatusMessage)]
        public void Status_GivenStatusWord_ReturnsCorrectResponseMessage(short responseSw, string expectedMessage)
        {
            SampleYubiHsmAuthResponseWithRetries response = new SampleYubiHsmAuthResponseWithRetries(
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
            SampleYubiHsmAuthResponseWithRetries response = new SampleYubiHsmAuthResponseWithRetries(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.Equal(expectedResponse, response.HasRetries);
        }

        [Theory]
        [InlineData(SWConstants.VerifyFail, 0)]
        [InlineData(0x63cf, 15)]
        public void RetriesRemaining_GivenSwWithRetryCount_ReturnsCorrectRetryCount(
            short responseSw, int? expectedCount)
        {
            SampleYubiHsmAuthResponseWithRetries response = new SampleYubiHsmAuthResponseWithRetries(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.Equal(expectedCount, response.RetriesRemaining);
        }

        [Theory]
        [InlineData(SWConstants.Success)]
        [InlineData(SWConstants.InvalidParameter)]
        public void RetriesRemaining_GivenSwNoRetryCount_ReturnsNull(short responseSw)
        {
            SampleYubiHsmAuthResponseWithRetries response = new SampleYubiHsmAuthResponseWithRetries(
                new ResponseApdu(new byte[] { }, responseSw));

            Assert.True(!response.RetriesRemaining.HasValue);
        }
    }
}
