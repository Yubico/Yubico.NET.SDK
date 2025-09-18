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

using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands;

public class BaseYubiHsmAuthResponseTests
{
    private const string SecurityStatusNotSatisfiedStatusMessage = "The device was not touched.";
    private const string AuthenticationMethodBlockedStatusMessage = "The entry is invalid.";
    private const string ReferenceDataUnusableStatusMessage = "Invalid authentication data.";
    private const string SuccessStatusMessage = "The command succeeded.";

    [Fact]
    public void Constructor_GivenSuccessApdu_SetsCorrectStatusWord()
    {
        var expectedSW = SWConstants.Success;

        var response = new SampleYubiHsmAuthResponse(
            new ResponseApdu(new byte[] { }, expectedSW));

        Assert.Equal(expectedSW, response.StatusWord);
    }

    [Theory]
    [InlineData(SWConstants.SecurityStatusNotSatisfied, ResponseStatus.RetryWithTouch)]
    [InlineData(SWConstants.AuthenticationMethodBlocked, ResponseStatus.Failed)]
    [InlineData(SWConstants.ReferenceDataUnusable, ResponseStatus.Failed)]
    [InlineData(SWConstants.Success, ResponseStatus.Success)]
    public void Status_GivenStatusWord_ReturnsCorrectResponseStatus(
        short responseSw,
        ResponseStatus expectedStatus)
    {
        var response = new SampleYubiHsmAuthResponse(
            new ResponseApdu(new byte[] { }, responseSw));

        Assert.Equal(expectedStatus, response.Status);
    }

    [Theory]
    [InlineData(SWConstants.SecurityStatusNotSatisfied, SecurityStatusNotSatisfiedStatusMessage)]
    [InlineData(SWConstants.AuthenticationMethodBlocked, AuthenticationMethodBlockedStatusMessage)]
    [InlineData(SWConstants.ReferenceDataUnusable, ReferenceDataUnusableStatusMessage)]
    [InlineData(SWConstants.Success, SuccessStatusMessage)]
    public void Status_GivenStatusWord_ReturnsCorrectResponseMessage(
        short responseSw,
        string expectedMessage)
    {
        var response = new SampleYubiHsmAuthResponse(
            new ResponseApdu(new byte[] { }, responseSw));

        Assert.Equal(expectedMessage, response.StatusMessage);
    }

    #region Nested type: SampleYubiHsmAuthResponse

    public class SampleYubiHsmAuthResponse : BaseYubiHsmAuthResponse
    {
        public SampleYubiHsmAuthResponse(
            ResponseApdu responseApdu) : base(responseApdu)
        {
        }
    }

    #endregion
}
