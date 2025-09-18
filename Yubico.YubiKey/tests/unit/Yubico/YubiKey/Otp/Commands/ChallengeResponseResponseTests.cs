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
using System.Linq;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands;

public class ChallengeResponseResponseTests
{
    [Fact]
    public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        static void Action()
        {
            _ = new ChallengeResponseResponse(null, ChallengeResponseAlgorithm.HmacSha1);
        }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        _ = Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
    {
        var sw1 = unchecked((byte)(SWConstants.Success >> 8));
        var sw2 = unchecked((byte)SWConstants.Success);
        var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

        var response = new ChallengeResponseResponse(responseApdu, ChallengeResponseAlgorithm.HmacSha1);

        Assert.Equal(SWConstants.Success, response.StatusWord);
    }

    [Fact]
    public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
    {
        var sw1 = unchecked((byte)(SWConstants.Success >> 8));
        var sw2 = unchecked((byte)SWConstants.Success);
        var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

        var response = new ChallengeResponseResponse(responseApdu, ChallengeResponseAlgorithm.HmacSha1);

        Assert.Equal(ResponseStatus.Success, response.Status);
    }

    [Fact]
    public void GetData_FailedResponseApdu_ThrowsInvalidOperationException()
    {
        var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
        var response = new ChallengeResponseResponse(responseApdu, ChallengeResponseAlgorithm.HmacSha1);

        void Action()
        {
            _ = response.GetData();
        }

        _ = Assert.Throws<InvalidOperationException>(Action);
    }

    [Fact]
    public void GetData_YubicoOtpResponseOfInvalidLength_ThrowsMalformedYubiKeyResponseException()
    {
        var responseApdu = new ResponseApdu(new byte[] { 1, 2, 3, 4, 0x90, 0x00 });
        var response = new ChallengeResponseResponse(responseApdu, ChallengeResponseAlgorithm.YubicoOtp);

        void Action()
        {
            _ = response.GetData();
        }

        _ = Assert.Throws<MalformedYubiKeyResponseException>(Action);
    }

    [Fact]
    public void GetData_ValidYubicoOtpResponse_ReturnedSuccessfully()
    {
        var expectedResponse = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var apduBytes = expectedResponse.Concat(new byte[] { 0x90, 0x00 }).ToArray();
        var responseApdu = new ResponseApdu(apduBytes);
        var response = new ChallengeResponseResponse(responseApdu, ChallengeResponseAlgorithm.YubicoOtp);

        var actualResponse = response.GetData();
        Assert.True(expectedResponse.SequenceEqual(actualResponse.ToArray()));
    }

    [Fact]
    public void GetData_HmacSha1ResponseOfInvalidLength_ThrowsMalformedYubiKeyResponseException()
    {
        var responseApdu = new ResponseApdu(new byte[] { 1, 2, 3, 4, 0x90, 0x00 });
        var response = new ChallengeResponseResponse(responseApdu, ChallengeResponseAlgorithm.HmacSha1);

        void Action()
        {
            _ = response.GetData();
        }

        _ = Assert.Throws<MalformedYubiKeyResponseException>(Action);
    }

    [Fact]
    public void GetData_ValidHmacSha1Response_ReturnsSuccessfully()
    {
        Memory<byte> expectedResponse = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20
        };
        var responseApdu = new ResponseApdu(
            expectedResponse.Span.ToArray().Concat(new byte[] { 0x90, 0x00 }).ToArray());
        var response = new ChallengeResponseResponse(responseApdu, ChallengeResponseAlgorithm.HmacSha1);

        var actualResponse = response.GetData();
        Assert.True(expectedResponse.Span.SequenceEqual(actualResponse.Span));
    }
}
