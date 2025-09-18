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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands;

public class ReadStatusResponseTests
{
    [Fact]
    public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        static void action()
        {
            _ = new ReadStatusResponse(null);
        }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        _ = Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
    {
        var sw1 = unchecked((byte)(SWConstants.Success >> 8));
        var sw2 = unchecked((byte)SWConstants.Success);
        var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

        var readStatusResponse = new ReadStatusResponse(responseApdu);

        Assert.Equal(SWConstants.Success, readStatusResponse.StatusWord);
    }

    [Fact]
    public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
    {
        var sw1 = unchecked((byte)(SWConstants.Success >> 8));
        var sw2 = unchecked((byte)SWConstants.Success);
        var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

        var readStatusResponse = new ReadStatusResponse(responseApdu);

        Assert.Equal(ResponseStatus.Success, readStatusResponse.Status);
    }

    [Fact]
    public void GetData_ResponseApduFailed_ThrowsInvalidOperationException()
    {
        var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
        var readStatusResponse = new ReadStatusResponse(responseApdu);

        void action()
        {
            _ = readStatusResponse.GetData();
        }

        _ = Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void GetData_ResponseApduWithWrongSizedBuffer_ThrowsMalformedYubiKeyResponseException()
    {
        var responseApdu = new ResponseApdu(new byte[] { 0x00, 0x00, 0x90, 0x00 });
        var readStatusResponse = new ReadStatusResponse(responseApdu);

        void action()
        {
            _ = readStatusResponse.GetData();
        }

        _ = Assert.Throws<MalformedYubiKeyResponseException>(action);
    }

    [Fact]
    public void GetData_ResponseApduWithCorrectData_ReturnsCorrectData()
    {
        var expectedFirmwareVersion = new FirmwareVersion { Major = 5, Minor = 3, Patch = 1 };
        byte expectedSequenceNumber = 0x42;
        var responseApdu = new ResponseApdu(new byte[]
        {
            expectedFirmwareVersion.Major,
            expectedFirmwareVersion.Minor,
            expectedFirmwareVersion.Patch,
            expectedSequenceNumber,
            0b0001_0101,
            0x00,
            0b1001_0000,
            0x00
        });
        var readStatusResponse = new ReadStatusResponse(responseApdu);

        var otpStatus = readStatusResponse.GetData();

        Assert.Equal(expectedFirmwareVersion, otpStatus.FirmwareVersion);
        Assert.Equal(expectedSequenceNumber, otpStatus.SequenceNumber);
        Assert.Equal(0, otpStatus.TouchLevel);
        Assert.True(otpStatus.ShortPressConfigured);
        Assert.False(otpStatus.LongPressConfigured);
        Assert.True(otpStatus.ShortPressRequiresTouch);
        Assert.False(otpStatus.LongPressRequiresTouch);
        Assert.True(otpStatus.LedBehaviorInverted);
    }
}
