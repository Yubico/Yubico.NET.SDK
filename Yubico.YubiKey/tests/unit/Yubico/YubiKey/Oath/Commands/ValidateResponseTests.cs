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
using System.Text;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath.Commands;

public class ValidateResponseTests
{
    private const byte success_sw1 = unchecked((byte)(SWConstants.Success >> 8));
    private const byte success_sw2 = unchecked((byte)SWConstants.Success);

    private const short StatusWordAuthNotEnabled = 0x6984;
    private readonly byte[] _fixedBytes = new byte[8] { 0xF1, 0x03, 0xDA, 0x89, 0x01, 0x02, 0x03, 0x04 };
    private readonly byte[] _password = Encoding.UTF8.GetBytes("test");

    private readonly ResponseApdu selectResponseApdu = new(new byte[]
    {
        0x79, 0x03, 0x05, 0x02, 0x04, 0x71, 0x08, 0xC0, 0xE3, 0xAF,
        0x27, 0xCC, 0x7A, 0x20, 0xEE, 0x74, 0x08, 0xF1, 0x03, 0xDA,
        0x89, 0x58, 0xE4, 0x40, 0x85, 0x7B, 0x01, 0x01, success_sw1, success_sw2
    });

    [Fact]
    public void Status_SuccessResponseApdu_ReturnsSuccess()
    {
        const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
        const byte sw2 = unchecked((byte)SWConstants.Success);

        var selectOathResponse = new SelectOathResponse(selectResponseApdu);
        var oathData = selectOathResponse.GetData();
        var utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

        try
        {
            var command = new ValidateCommand(_password, oathData);
            _ = command.CreateCommandApdu();

            var validateResponseApdu = new ResponseApdu(new byte[]
            {
                0x75, 0x14, 0xDE, 0x73, 0x3D, 0x91, 0xB8, 0x4F, 0x31, 0xF0,
                0x89, 0xEA, 0x93, 0x30, 0x35, 0x53, 0x34, 0x24, 0x3B, 0x18,
                0x09, 0xE0, sw1, sw2
            });

            var validateResponse = new ValidateResponse(validateResponseApdu, command.CalculatedResponse);

            Assert.Equal(ResponseStatus.Success, validateResponse.Status);
        }
        finally
        {
            utility.RestoreRandomProvider();
        }
    }

    [Fact]
    public void Status_AuthNotEnabledResponseApdu_ReturnsFailed()
    {
        const byte sw1 = unchecked(StatusWordAuthNotEnabled >> 8);
        const byte sw2 = unchecked((byte)StatusWordAuthNotEnabled);

        var selectOathResponse = new SelectOathResponse(selectResponseApdu);
        var oathData = selectOathResponse.GetData();
        var utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

        try
        {
            var command = new ValidateCommand(_password, oathData);
            _ = command.CreateCommandApdu();

            var validateResponseApdu = new ResponseApdu(new byte[]
            {
                0x75, 0x14, 0xDE, 0x73, 0x3D, 0x91, 0xB8, 0x4F, 0x31, 0xF0,
                0x89, 0xEA, 0x93, 0x30, 0x35, 0x53, 0x34, 0x24, 0x3B, 0x18,
                0x09, 0xE0, sw1, sw2
            });

            var validateResponse = new ValidateResponse(validateResponseApdu, command.CalculatedResponse);

            Assert.Equal(ResponseStatus.Failed, validateResponse.Status);
        }
        finally
        {
            utility.RestoreRandomProvider();
        }
    }

    [Fact]
    public void SuccessResponseApdu_PasswordIsSet_OathResponseInfoCorrect()
    {
        var selectOathResponse = new SelectOathResponse(selectResponseApdu);
        var oathData = selectOathResponse.GetData();
        var utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

        try
        {
            var command = new ValidateCommand(_password, oathData);
            _ = command.CreateCommandApdu();

            var validateResponseApdu = new ResponseApdu(new byte[]
            {
                0x75, 0x14, 0xDE, 0x73, 0x3D, 0x91, 0xB8, 0x4F, 0x31, 0xF0,
                0x89, 0xEA, 0x93, 0x30, 0x35, 0x53, 0x34, 0x24, 0x3B, 0x18,
                0x09, 0xE0, success_sw1, success_sw2
            });

            var responseCommand = new ValidateResponse(validateResponseApdu, command.CalculatedResponse);

            Assert.Equal(SWConstants.Success, selectOathResponse.StatusWord);
            Assert.True(responseCommand.GetData());
        }
        finally
        {
            utility.RestoreRandomProvider();
        }
    }


    [Fact]
    public void ResponseApduFailed_ThrowsInvalidOperationException()
    {
        var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
        var validateResponse = new ValidateResponse(responseApdu, ReadOnlyMemory<byte>.Empty);

        _ = Assert.Throws<InvalidOperationException>(() => validateResponse.GetData());
    }
}
