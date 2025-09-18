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

public class ChangeRefCommandTests
{
    [Fact]
    public void ClassType_DerivedFromPivCommand_IsTrue()
    {
        var currentPin = GetPinArray(6, 0);
        var newPin = GetPinArray(7, 1);
        var changeRefDataCommand = new ChangeReferenceDataCommand(PivSlot.Pin, currentPin, newPin);

        Assert.True(changeRefDataCommand is IYubiKeyCommand<ChangeReferenceDataResponse>);
    }

    [Fact]
    public void Constructor_Application_Piv()
    {
        var currentPin = GetPinArray(6, 0);
        var newPin = GetPinArray(7, 1);
        var command = new ChangeReferenceDataCommand(PivSlot.Pin, currentPin, newPin);

        var application = command.Application;

        Assert.Equal(YubiKeyApplication.Piv, application);
    }

    [Theory]
    [InlineData(PivSlot.Pin)]
    [InlineData(PivSlot.Puk)]
    public void Constructor_Property_SlotNum(
        byte slotNumber)
    {
        var currentPin = GetPinArray(6, 0);
        var newPin = GetPinArray(7, 1);
        var command = new ChangeReferenceDataCommand(slotNumber, currentPin, newPin);

        var getSlotNum = command.SlotNumber;

        Assert.Equal(slotNumber, getSlotNum);
    }

    [Theory]
    [InlineData(PivSlot.Pin)]
    [InlineData(PivSlot.Puk)]
    public void CreateCommandApdu_GetClaProperty_ReturnsZero(
        byte slotNum)
    {
        var cmdApdu = GetChangeRefCommandApdu(slotNum);

        var Cla = cmdApdu.Cla;

        Assert.Equal(0, Cla);
    }

    [Theory]
    [InlineData(PivSlot.Pin)]
    [InlineData(PivSlot.Puk)]
    public void CreateCommandApdu_GetInsProperty_ReturnsHex24(
        byte slotNum)
    {
        var cmdApdu = GetChangeRefCommandApdu(slotNum);

        var Ins = cmdApdu.Ins;

        Assert.Equal(0x24, Ins);
    }

    [Theory]
    [InlineData(PivSlot.Pin)]
    [InlineData(PivSlot.Puk)]
    public void CreateCommandApdu_GetP1Property_ReturnsZero(
        byte slotNum)
    {
        var cmdApdu = GetChangeRefCommandApdu(slotNum);

        var P1 = cmdApdu.P1;

        Assert.Equal(0, P1);
    }

    [Theory]
    [InlineData(PivSlot.Pin)]
    [InlineData(PivSlot.Puk)]
    public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(
        byte slotNum)
    {
        var cmdApdu = GetChangeRefCommandApdu(slotNum);

        var P2 = cmdApdu.P2;

        Assert.Equal(slotNum, P2);
    }

    [Theory]
    [InlineData(PivSlot.Pin)]
    [InlineData(PivSlot.Puk)]
    public void CreateCommandApdu_GetNc_Returns16(
        byte slotNum)
    {
        var cmdApdu = GetChangeRefCommandApdu(slotNum);

        var Nc = cmdApdu.Nc;

        Assert.Equal(16, Nc);
    }

    [Theory]
    [InlineData(6, PivSlot.Pin)]
    [InlineData(6, PivSlot.Puk)]
    [InlineData(7, PivSlot.Pin)]
    [InlineData(7, PivSlot.Puk)]
    [InlineData(8, PivSlot.Pin)]
    [InlineData(8, PivSlot.Puk)]
    public void CreateCommandApdu_GetDataProperty_ReturnsPin(
        int pinLength,
        byte slotNum)
    {
        var currentPin = GetPinArray(6, 0);
        var newPin = GetPinArray(pinLength, 1);

        var changeRefDataCommand = new ChangeReferenceDataCommand(slotNum, currentPin, newPin);
        var cmdApdu = changeRefDataCommand.CreateCommandApdu();

        var data = cmdApdu.Data;

        Assert.False(data.IsEmpty);
        if (data.IsEmpty)
        {
            return;
        }

        Assert.Equal(16, data.Length);

        // Verify the first 8 bytes in the Data are the currentPIN + pad.
        var compareResult = true;
        var index = 0;
        for (; index < currentPin.Length; index++)
        {
            if (data.Span[index] != currentPin[index])
            {
                compareResult = false;
            }
        }

        for (; index < 8; index++)
        {
            if (data.Span[index] != 0xFF)
            {
                compareResult = false;
            }
        }

        // Verify the next 8 bytes in the Data are the newPIN + pad.
        for (index = 0; index < newPin.Length; index++)
        {
            if (data.Span[index + 8] != newPin[index])
            {
                compareResult = false;
            }
        }

        for (; index < 8; index++)
        {
            if (data.Span[index + 8] != 0xFF)
            {
                compareResult = false;
            }
        }

        Assert.True(compareResult);
    }

    [Fact]
    public void CreateResponseForApdu_ReturnsCorrectType()
    {
        var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
        var currentPin = GetPinArray(6, 0);
        var newPin = GetPinArray(7, 1);
        var changeRefDataCommand = new ChangeReferenceDataCommand(0x80, currentPin, newPin);

        var changeRefDataResponse = changeRefDataCommand.CreateResponseForApdu(responseApdu);

        Assert.True(changeRefDataResponse is ChangeReferenceDataResponse);
    }

    [Theory]
    [InlineData(1, PivSlot.Pin)]
    [InlineData(1, PivSlot.Puk)]
    [InlineData(2, PivSlot.Pin)]
    [InlineData(2, PivSlot.Puk)]
    [InlineData(3, PivSlot.Pin)]
    [InlineData(3, PivSlot.Puk)]
    [InlineData(4, PivSlot.Pin)]
    [InlineData(4, PivSlot.Puk)]
    [InlineData(5, PivSlot.Pin)]
    [InlineData(5, PivSlot.Puk)]
    [InlineData(9, PivSlot.Pin)]
    [InlineData(9, PivSlot.Puk)]
    public void Constructor_BadPin_CorrectException(
        int pinLength,
        byte slotNum)
    {
        var currentPin = GetPinArray(6, 0);
        var newPin = GetPinArray(pinLength, 1);
        _ = Assert.Throws<ArgumentException>(() => new ChangeReferenceDataCommand(slotNum, currentPin, newPin));
    }

    [Fact]
    public void Constructor_NullCurrentPin_CorrectException()
    {
        var pin = GetPinArray(6, 0);
        _ = Assert.Throws<ArgumentException>(() => new ChangeReferenceDataCommand(0x80, null, pin));
    }

    [Fact]
    public void Constructor_NullNewPin_CorrectException()
    {
        var pin = GetPinArray(6, 0);
        _ = Assert.Throws<ArgumentException>(() => new ChangeReferenceDataCommand(0x81, pin, null));
    }

    [Fact]
    public void Constructor_BadSlotNum_CorrectException()
    {
        var pin = GetPinArray(6, 0);
        var newPin = GetPinArray(8, 1);
        _ = Assert.Throws<ArgumentException>(() => new ChangeReferenceDataCommand(0x82, pin, newPin));
    }

    private static CommandApdu GetChangeRefCommandApdu(
        byte slotNum)
    {
        var currentPin = GetPinArray(6, 0);
        var newPin = GetPinArray(7, 1);
        var changeRefDataCommand = new ChangeReferenceDataCommand(slotNum, currentPin, newPin);
        var returnValue = changeRefDataCommand.CreateCommandApdu();

        return returnValue;
    }

    // If startingPoint is 0, start with 30
    // Otherwise, start with 32
    private static byte[] GetPinArray(
        int pinLength,
        int startingPoint)
    {
        var returnValue = new byte[pinLength];
        byte increment = 0x32;
        if (startingPoint != 0)
        {
            increment = 0x30;
        }

        for (var index = 0; index < pinLength; index++)
        {
            var value = (byte)(index & 15);
            value += increment;
            returnValue[index] = value;
        }

        return returnValue;
    }
}
