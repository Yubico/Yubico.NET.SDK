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

namespace Yubico.YubiKey.Piv.Commands;

public class VerifyPinCommandTests
{
    [Fact]
    public void ClassType_DerivedFromPivCommand_IsTrue()
    {
        var pin = GetPinArray(8);
        var verifyPinCommand = new VerifyPinCommand(pin);

        Assert.True(verifyPinCommand is IYubiKeyCommand<VerifyPinResponse>);
    }

    [Fact]
    public void Constructor_Application_Piv()
    {
        var pin = GetPinArray(8);
        var command = new VerifyPinCommand(pin);

        var application = command.Application;

        Assert.Equal(YubiKeyApplication.Piv, application);
    }

    [Fact]
    public void CreateCommandApdu_GetClaProperty_ReturnsZero()
    {
        var cmdApdu = GetVerifyCommandApdu();

        var Cla = cmdApdu.Cla;

        Assert.Equal(0, Cla);
    }

    [Fact]
    public void CreateCommandApdu_GetInsProperty_ReturnsHex20()
    {
        var cmdApdu = GetVerifyCommandApdu();

        var Ins = cmdApdu.Ins;

        Assert.Equal(0x20, Ins);
    }

    [Fact]
    public void CreateCommandApdu_GetP1Property_ReturnsZero()
    {
        var cmdApdu = GetVerifyCommandApdu();

        var P1 = cmdApdu.P1;

        Assert.Equal(0, P1);
    }

    [Fact]
    public void CreateCommandApdu_GetP2Property_ReturnsHex80()
    {
        var cmdApdu = GetVerifyCommandApdu();

        var P2 = cmdApdu.P2;

        Assert.Equal(0x80, P2);
    }

    [Fact]
    public void CreateCommandApdu_GetNc_Returns8()
    {
        var cmdApdu = GetVerifyCommandApdu();

        var Nc = cmdApdu.Nc;

        Assert.Equal(8, Nc);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void CreateCommandApdu_GetDataProperty_ReturnsCorrectLength(
        int pinLength)
    {
        var pin = GetPinArray(pinLength);

        var verifyPinCommand = new VerifyPinCommand(pin);
        var cmdApdu = verifyPinCommand.CreateCommandApdu();

        Assert.False(cmdApdu.Data.IsEmpty);
        if (cmdApdu.Data.IsEmpty)
        {
            return;
        }

        Assert.True(cmdApdu.Data.Length == 8);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void CreateCommandApdu_GetDataProperty_ReturnsPin(
        int pinLength)
    {
        var pin = GetPinArray(pinLength);

        var verifyPinCommand = new VerifyPinCommand(pin);
        var cmdApdu = verifyPinCommand.CreateCommandApdu();

        Assert.False(cmdApdu.Data.IsEmpty);
        if (cmdApdu.Data.IsEmpty)
        {
            return;
        }

        Assert.Equal(8, cmdApdu.Data.Length);

        var compareResult = true;
        var index = 0;
        for (; index < pin.Length; index++)
        {
            if (cmdApdu.Data.Span[index] != pin[index])
            {
                compareResult = false;
            }
        }

        for (; index < 8; index++)
        {
            if (cmdApdu.Data.Span[index] != 0xFF)
            {
                compareResult = false;
            }
        }

        Assert.True(compareResult);
    }

    [Fact]
    public void CreateCommandApdu_GetNe_ReturnsZero()
    {
        var cmdApdu = GetVerifyCommandApdu();

        var Ne = cmdApdu.Ne;

        Assert.Equal(0, Ne);
    }

    [Fact]
    public void CreateResponseForApdu_ReturnsCorrectType()
    {
        var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
        var pin = GetPinArray(8);
        var verifyPinCommand = new VerifyPinCommand(pin);

        var verifyPinResponse = verifyPinCommand.CreateResponseForApdu(responseApdu);

        Assert.True(verifyPinResponse is VerifyPinResponse);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(9)]
    public void Constructor_BadPin_CorrectException(
        int pinLength)
    {
        var pin = GetPinArray(pinLength);
        _ = Assert.Throws<ArgumentException>(() => new VerifyPinCommand(pin));
    }

    [Fact]
    public void Constructor_NullPin_CorrectException()
    {
        _ = Assert.Throws<ArgumentException>(() => new VerifyPinCommand(null));
    }

    private static CommandApdu GetVerifyCommandApdu()
    {
        var pin = GetPinArray(8);
        var verifyPinCommand = new VerifyPinCommand(pin);
        var returnValue = verifyPinCommand.CreateCommandApdu();

        return returnValue;
    }

    private static byte[] GetPinArray(
        int pinLength)
    {
        var returnValue = new byte[pinLength];
        for (var index = 0; index < pinLength; index++)
        {
            var value = (byte)(index & 15);
            value += 0x31;
            returnValue[index] = value;
        }

        return returnValue;
    }
}
