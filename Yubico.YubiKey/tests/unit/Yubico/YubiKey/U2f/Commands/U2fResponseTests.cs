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
using System.Globalization;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands;

public class U2fResponseTests
{
    [Fact]
    public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        _ = Assert.Throws<ArgumentNullException>(() => new U2fResponse(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public void Constructor_GivenConditionsNotSatisfiedStatusWord_SetsResponseStatus()
    {
        var response = new U2fResponse(new ResponseApdu(Array.Empty<byte>(), SWConstants.ConditionsNotSatisfied));
        Assert.Equal(ResponseStatus.ConditionsNotSatisfied, response.Status);
    }

    [Fact]
    public void Constructor_GivenConditionsNotSatisfiedStatusWord_SetsStatusMessage()
    {
        var response = new U2fResponse(new ResponseApdu(Array.Empty<byte>(), SWConstants.ConditionsNotSatisfied));
        Assert.Equal(ResponseStatusMessages.U2fConditionsNotSatisfied, response.StatusMessage);
    }

    [Fact]
    public void Constructor_GivenInvalidCommandDataParameterStatusWord_SetsResponseStatus()
    {
        var response = new U2fResponse(new ResponseApdu(Array.Empty<byte>(), SWConstants.InvalidCommandDataParameter));
        Assert.Equal(ResponseStatus.Failed, response.Status);
    }

    [Fact]
    public void Constructor_GivenInvalidCommandDataParameterStatusWord_SetsStatusMessage()
    {
        var response = new U2fResponse(new ResponseApdu(Array.Empty<byte>(), SWConstants.InvalidCommandDataParameter));
        Assert.Equal(ResponseStatusMessages.U2fWrongData, response.StatusMessage);
    }

    //
    // U2F HID errors
    //

    [Fact]
    public void Constructor_GivenCommandNotAllowedStatusWord_SetsResponseStatus()
    {
        var responseData = new byte[] { 0x01 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.CommandNotAllowed));
        Assert.Equal(ResponseStatus.Failed, response.Status);
    }

    [Fact]
    public void Constructor_GivenCommandNotAllowedStatusWord_SetsStatusMessage()
    {
        var responseData = new byte[] { 0x01 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.CommandNotAllowed));
        Assert.Equal(ResponseStatusMessages.U2fHidErrorInvalidCommand, response.StatusMessage);
    }

    [Fact]
    public void Constructor_GivenInvalidParameterStatusWord_SetsResponseStatus()
    {
        var responseData = new byte[] { 0x02 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.InvalidParameter));
        Assert.Equal(ResponseStatus.Failed, response.Status);
    }

    [Fact]
    public void Constructor_GivenInvalidParameterStatusWord_SetsStatusMessage()
    {
        var responseData = new byte[] { 0x02 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.InvalidParameter));
        Assert.Equal(ResponseStatusMessages.U2fHidErrorInvalidParameter, response.StatusMessage);
    }

    [Fact]
    public void Constructor_GivenWrongLengthStatusWord_SetsResponseStatus()
    {
        var responseData = new byte[] { 0x03 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.WrongLength));
        Assert.Equal(ResponseStatus.Failed, response.Status);
    }

    [Fact]
    public void Constructor_GivenWrongLengthStatusWord_SetsStatusMessage()
    {
        var responseData = new byte[] { 0x03 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.WrongLength));
        Assert.Equal(ResponseStatusMessages.U2fHidErrorInvalidLength, response.StatusMessage);
    }

    [Fact]
    public void Constructor_GivenU2fHidErrInvalidSequenceResult_SetsResponseStatus()
    {
        var responseData = new byte[] { 0x04 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.NoPreciseDiagnosis));
        Assert.Equal(ResponseStatus.Failed, response.Status);
    }

    [Fact]
    public void Constructor_GivenU2fHidErrInvalidSequenceResult_SetsStatusMessage()
    {
        var responseData = new byte[] { 0x04 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.NoPreciseDiagnosis));
        Assert.Equal(ResponseStatusMessages.U2fHidErrorInvalidSequence, response.StatusMessage);
    }

    [Fact]
    public void Constructor_GivenU2fHidErrTimeoutResult_SetsResponseStatus()
    {
        var responseData = new byte[] { 0x05 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.NoPreciseDiagnosis));
        Assert.Equal(ResponseStatus.Failed, response.Status);
    }

    [Fact]
    public void Constructor_GivenU2fHidErrTimeoutResult_SetsStatusMessage()
    {
        var responseData = new byte[] { 0x05 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.NoPreciseDiagnosis));
        Assert.Equal(ResponseStatusMessages.U2fHidErrorMessageTimeout, response.StatusMessage);
    }

    [Fact]
    public void Constructor_GivenU2fHidErrChannelBusyResult_SetsResponseStatus()
    {
        var responseData = new byte[] { 0x06 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.NoPreciseDiagnosis));
        Assert.Equal(ResponseStatus.Failed, response.Status);
    }

    [Fact]
    public void Constructor_GivenU2fHidErrChannelBusyResult_SetsStatusMessage()
    {
        var responseData = new byte[] { 0x06 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.NoPreciseDiagnosis));
        Assert.Equal(ResponseStatusMessages.U2fHidErrorChannelBusy, response.StatusMessage);
    }

    [Fact]
    public void Constructor_GivenU2fHidErrUnknown0x77Result_SetsResponseStatus()
    {
        var responseData = new byte[] { 0x77 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.NoPreciseDiagnosis));
        Assert.Equal(ResponseStatus.Failed, response.Status);
    }

    [Fact]
    public void Constructor_GivenU2fHidErrUnknown0x77Result_SetsStatusMessage()
    {
        var responseData = new byte[] { 0x77 };
        var response = new U2fResponse(new ResponseApdu(responseData, SWConstants.NoPreciseDiagnosis));
        Assert.Equal(string.Format(
                CultureInfo.CurrentCulture,
                ResponseStatusMessages.U2fHidErrorUnknown,
                responseData[0]),
            response.StatusMessage);
    }
}
