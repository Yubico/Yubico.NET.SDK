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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Commands;

public class GenPairCommandTests
{
    [Theory]
    [InlineData(1, 0x9B)]
    [InlineData(2, 0x80)]
    [InlineData(3, 0x81)]
    [InlineData(1, 0x96)]
    public void Constructor_BadSlotNumber_ThrowsException(
        int cStyle,
        byte slotNumber)
    {
        _ = Assert.Throws<ArgumentException>(() => GetCommandObject(
            cStyle, slotNumber, KeyType.RSA1024.GetPivAlgorithm(), PivPinPolicy.Default, PivTouchPolicy.Default));
    }

    [Theory]
    [InlineData(1, PivAlgorithm.None)]
    [InlineData(2, PivAlgorithm.TripleDes)]
    [InlineData(3, PivAlgorithm.Pin)]
    public void Constructor_BadAlgorithm_ThrowsException(
        int cStyle,
        PivAlgorithm keyType)
    {
        _ = Assert.Throws<ArgumentException>(() => GetCommandObject(
            cStyle, 0x90, keyType, PivPinPolicy.Default, PivTouchPolicy.Default));
    }

    [Fact]
    public void NoArgConstructor_NoSlot_ThrowsException()
    {
        var cmd = new GenerateKeyPairCommand
        {
            Algorithm = KeyType.RSA1024.GetPivAlgorithm()
        };

        _ = Assert.Throws<InvalidOperationException>(() => cmd.CreateCommandApdu());
    }

    [Fact]
    public void NoArgConstructor_NoAlg_ThrowsException()
    {
        var cmd = new GenerateKeyPairCommand
        {
            SlotNumber = 0x90
        };

        _ = Assert.Throws<InvalidOperationException>(() => cmd.CreateCommandApdu());
    }

    [Fact]
    public void Constructor_Application_Piv()
    {
        var genPairCommand = new GenerateKeyPairCommand(
            0x9C, KeyType.ECP256, PivPinPolicy.Always, PivTouchPolicy.Cached);

        var application = genPairCommand.Application;

        Assert.Equal(YubiKeyApplication.Piv, application);
    }

    [Fact]
    public void Constructor_Property_SlotNum()
    {
        var slotNumber = PivSlot.Signing;
        var keyType = KeyType.ECP256;
        var pinPolicy = PivPinPolicy.Always;
        var touchPolicy = PivTouchPolicy.Cached;
        var genPairCommand = new GenerateKeyPairCommand(slotNumber, keyType, pinPolicy, touchPolicy);

        var getSlotNum = genPairCommand.SlotNumber;

        Assert.Equal(slotNumber, getSlotNum);
    }

    [Fact]
    public void Constructor_Property_Algorithm()
    {
        var slotNumber = PivSlot.Signing;
        var keyType = KeyType.ECP256;
        var pinPolicy = PivPinPolicy.Always;
        var touchPolicy = PivTouchPolicy.Cached;
        var genPairCommand = new GenerateKeyPairCommand(slotNumber, keyType, pinPolicy, touchPolicy);

        var getAlgorithm = genPairCommand.Algorithm;

        Assert.Equal(keyType.GetPivAlgorithm(), getAlgorithm);
    }

    [Fact]
    public void Constructor_Property_PinPolicy()
    {
        var slotNumber = PivSlot.Signing;
        var keyType = KeyType.ECP256;
        var pinPolicy = PivPinPolicy.Always;
        var touchPolicy = PivTouchPolicy.Cached;
        var genPairCommand = new GenerateKeyPairCommand(slotNumber, keyType, pinPolicy, touchPolicy);

        var getPolicy = genPairCommand.PinPolicy;

        Assert.Equal(pinPolicy, getPolicy);
    }

    [Fact]
    public void Constructor_Property_TouchPolicy()
    {
        var slotNumber = PivSlot.Signing;
        var keyType = KeyType.ECP256;
        var pinPolicy = PivPinPolicy.Always;
        var touchPolicy = PivTouchPolicy.Cached;
        var genPairCommand = new GenerateKeyPairCommand(slotNumber, keyType, pinPolicy, touchPolicy);

        var getPolicy = genPairCommand.TouchPolicy;

        Assert.Equal(touchPolicy, getPolicy);
    }

    [Fact]
    public void ClassType_DerivedFromPivCommand_IsTrue()
    {
        var genPairCommand = new GenerateKeyPairCommand(
            0x9C, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.Never);

        Assert.True(genPairCommand is IYubiKeyCommand<GenerateKeyPairResponse>);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CreateCommandApdu_GetClaProperty_ReturnsZero(
        int cStyle)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x9C, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.Never);

        var Cla = cmdApdu.Cla;

        Assert.Equal(0, Cla);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CreateCommandApdu_GetInsProperty_ReturnsHex47(
        int cStyle)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x91, KeyType.RSA2048, PivPinPolicy.Default, PivTouchPolicy.Never);

        var Ins = cmdApdu.Ins;

        Assert.Equal(0x47, Ins);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CreateCommandApdu_GetP1Property_ReturnsZero(
        int cStyle)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x91, KeyType.RSA2048, PivPinPolicy.Default, PivTouchPolicy.Never);

        var P1 = cmdApdu.P1;

        Assert.Equal(0, P1);
    }

    [Theory]
    [InlineData(1, 0x9A)]
    [InlineData(2, 0x82)]
    [InlineData(3, 0x83)]
    public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(
        int cStyle,
        byte slotNumber)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, slotNumber, KeyType.RSA2048, PivPinPolicy.Default, PivTouchPolicy.Default);

        var P2 = cmdApdu.P2;

        Assert.Equal(slotNumber, P2);
    }

    [Theory]
    [InlineData(1, PivPinPolicy.None, PivTouchPolicy.None, 5)]
    [InlineData(2, PivPinPolicy.None, PivTouchPolicy.Never, 8)]
    [InlineData(3, PivPinPolicy.Once, PivTouchPolicy.None, 8)]
    [InlineData(1, PivPinPolicy.Always, PivTouchPolicy.Cached, 11)]
    [InlineData(2, PivPinPolicy.Default, PivTouchPolicy.Default, 5)]
    [InlineData(3, PivPinPolicy.Default, PivTouchPolicy.Never, 8)]
    [InlineData(1, PivPinPolicy.Once, PivTouchPolicy.Default, 8)]
    [InlineData(2, PivPinPolicy.None, PivTouchPolicy.Default, 5)]
    [InlineData(8, PivPinPolicy.Always, PivTouchPolicy.Always, 5)]
    [InlineData(9, PivPinPolicy.Always, PivTouchPolicy.Always, 5)]
    public void CreateCommandApdu_GetNcProperty_ReturnsCorrect(
        int cStyle,
        PivPinPolicy pinPolicy,
        PivTouchPolicy touchPolicy,
        int expectedLength)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x9c, KeyType.RSA2048, pinPolicy, touchPolicy);

        var Nc = cmdApdu.Nc;

        Assert.Equal(expectedLength, Nc);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CreateCommandApdu_GetNeProperty_ReturnsZero(
        int cStyle)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x9C, KeyType.ECP256, PivPinPolicy.Always, PivTouchPolicy.Never);

        var Ne = cmdApdu.Ne;

        Assert.Equal(0, Ne);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CreateCommandApdu_GetData_ReturnsCorrectPrefix(
        int cStyle)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x9D, KeyType.ECP384, PivPinPolicy.Default, PivTouchPolicy.Default);

        var data = cmdApdu.Data;

        Assert.False(data.IsEmpty);
        if (data.IsEmpty)
        {
            return;
        }

        var compareResult = data.Span[0] == 0xAC && data.Span[1] == 0x03 && data.Span[2] == 0x80 &&
                            data.Span[3] == 0x01;

        Assert.True(compareResult);
    }

    [Theory]
    [InlineData(1, KeyType.ECP256)]
    [InlineData(2, KeyType.ECP384)]
    [InlineData(3, KeyType.RSA1024)]
    [InlineData(4, KeyType.RSA2048)]
    public void CreateCommandApdu_GetData_ReturnsCorrectAlg(
        int cStyle,
        KeyType keyType)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x9C, keyType, PivPinPolicy.Default, PivTouchPolicy.Default);

        var data = cmdApdu.Data;

        Assert.False(data.IsEmpty);
        if (data.IsEmpty)
        {
            return;
        }

        Assert.Equal((byte)keyType.GetPivAlgorithm(), data.Span[4]);
    }

    [Theory]
    [InlineData(1, PivPinPolicy.Never)]
    [InlineData(2, PivPinPolicy.Once)]
    [InlineData(3, PivPinPolicy.Always)]
    public void CreateCommandApdu_GetData_ReturnsCorrectPinPolicy(
        int cStyle,
        PivPinPolicy pinPolicy)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x86, KeyType.RSA1024, pinPolicy, PivTouchPolicy.Always);

        var data = cmdApdu.Data;

        Assert.False(data.IsEmpty);
        if (data.IsEmpty)
        {
            return;
        }

        var compareResult = data.Span[5] == 0xAA && data.Span[6] == 0x01 && data.Span[7] == (byte)pinPolicy;

        Assert.True(compareResult);
    }

    [Theory]
    [InlineData(1, PivPinPolicy.None)]
    [InlineData(2, PivPinPolicy.Default)]
    [InlineData(4, PivPinPolicy.Always)]
    [InlineData(5, PivPinPolicy.Always)]
    public void CreateCommandApdu_DefaultPinGetData_ReturnsCorrect(
        int cStyle,
        PivPinPolicy pinPolicy)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x86, KeyType.RSA1024, pinPolicy, PivTouchPolicy.Always);

        var data = cmdApdu.Data;

        Assert.False(data.IsEmpty);
        if (data.IsEmpty)
        {
            return;
        }

        Assert.NotEqual(0xAA, data.Span[5]);
    }

    [Theory]
    [InlineData(1, PivTouchPolicy.Never)]
    [InlineData(2, PivTouchPolicy.Cached)]
    [InlineData(3, PivTouchPolicy.Always)]
    public void CreateCommandApdu_GetData_ReturnsCorrectTouchPolicy(
        int cStyle,
        PivTouchPolicy touchPolicy)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x87, KeyType.RSA2048, PivPinPolicy.Always, touchPolicy);

        var data = cmdApdu.Data;

        Assert.False(data.IsEmpty);
        if (data.IsEmpty)
        {
            return;
        }

        var compareResult = data.Span[8] == 0xAB && data.Span[9] == 0x01 && data.Span[10] == (byte)touchPolicy;

        Assert.True(compareResult);
    }

    [Theory]
    [InlineData(1, PivTouchPolicy.None)]
    [InlineData(2, PivTouchPolicy.Default)]
    [InlineData(6, PivTouchPolicy.Always)]
    [InlineData(7, PivTouchPolicy.Always)]
    public void CreateCommandApdu_DefaultTouchGetData_ReturnsCorrect(
        int cStyle,
        PivTouchPolicy touchPolicy)
    {
        var cmdApdu = GetGenPairCommandApdu(
            cStyle, 0x87, KeyType.RSA2048, PivPinPolicy.Always, touchPolicy);

        var data = cmdApdu.Data;
        Assert.False(data.IsEmpty);
        if (data.IsEmpty)
        {
            return;
        }

        Assert.Equal(8, data.Length);
    }

    [Fact]
    public void CreateResponseForApdu_ReturnsCorrectType()
    {
        var responseApdu = new ResponseApdu(new byte[]
        {
            0x86, 0x41, 0x04, 0xC4, 0x17, 0x7F, 0x2B, 0x96,
            0x8F, 0x9C, 0x00, 0x0C, 0x4F, 0x3D, 0x2B, 0x88,
            0xB0, 0xAB, 0x5B, 0x0C, 0x3B, 0x19, 0x42, 0x63,
            0x20, 0x8C, 0xA1, 0x2F, 0xEE, 0x1C, 0xB4, 0xD8,
            0x81, 0x96, 0x9F, 0xD8, 0xC8, 0xD0, 0x8D, 0xD1,
            0xBB, 0x66, 0x58, 0x00, 0x26, 0x7D, 0x05, 0x34,
            0xA8, 0xA3, 0x30, 0xD1, 0x59, 0xDE, 0x66, 0x01,
            0x0E, 0x3F, 0x21, 0x13, 0x29, 0xC5, 0x98, 0x56,
            0x07, 0xB5, 0x26,
            0x90, 0x00
        });

        var genPairCommand = new GenerateKeyPairCommand(
            0x9C, KeyType.ECP256, PivPinPolicy.Once, PivTouchPolicy.Default);

        var response = genPairCommand.CreateResponseForApdu(responseApdu);

        Assert.True(response is GenerateKeyPairResponse);
    }

    private static CommandApdu GetGenPairCommandApdu(
        int cStyle,
        byte slotNumber,
        KeyType keyType,
        PivPinPolicy pinPolicy,
        PivTouchPolicy touchPolicy)
    {
        var genPairCommand = GetCommandObject(cStyle, slotNumber, keyType.GetPivAlgorithm(), pinPolicy, touchPolicy);

        return genPairCommand.CreateCommandApdu();
    }

    // Construct a GenerateKeyPairCommand using the style specified.
    // If the style arg is 1, this will build using the full constructor.
    // If it is 2, it will build it using object initializer constructor.
    // If it is 3, create it using the empty constructor and set the
    // properties later.
    // If it is 4, create it using the object initializer constructor but
    // don't set the PinPolicy (it should be default).
    // If it is 5, create it using the empty constructor and set the
    // properties later, except don't set the PinPolicy.
    // If it is 6, create it using the object initializer constructor but
    // don't set the TouchPolicy (it should be default).
    // If it is 7, create it using the empty constructor and set the
    // properties later, except don't set the TouchPolicy.
    // If it is 8, create it using the object initializer constructor but
    // don't set the PinPolicy or the TouchPolicy.
    // If it is 9, create it using the empty constructor and set the
    // properties later, except don't set the PinPolicy or the TouchPolicy.
    private static GenerateKeyPairCommand GetCommandObject(
        int cStyle,
        byte slotNumber,
        PivAlgorithm keyType,
        PivPinPolicy pinPolicy,
        PivTouchPolicy touchPolicy)
    {
        GenerateKeyPairCommand cmd;

        switch (cStyle)
        {
            default:
                cmd = new GenerateKeyPairCommand(slotNumber, keyType, pinPolicy, touchPolicy);
                break;

            case 2:
                cmd = new GenerateKeyPairCommand
                {
                    SlotNumber = slotNumber,
                    Algorithm = keyType,
                    PinPolicy = pinPolicy,
                    TouchPolicy = touchPolicy
                };
                break;


            case 3:
#pragma warning disable IDE0017 // Testing this specific construction
                cmd = new GenerateKeyPairCommand();
                cmd.SlotNumber = slotNumber;
                cmd.Algorithm = keyType;
                cmd.PinPolicy = pinPolicy;
                cmd.TouchPolicy = touchPolicy;
                break;

            case 4:
                cmd = new GenerateKeyPairCommand
                {
                    SlotNumber = slotNumber,
                    Algorithm = keyType,
                    TouchPolicy = touchPolicy
                };
                break;

            case 5:
                cmd = new GenerateKeyPairCommand();
                cmd.SlotNumber = slotNumber;
                cmd.Algorithm = keyType;
                cmd.TouchPolicy = touchPolicy;
                break;

            case 6:
                cmd = new GenerateKeyPairCommand
                {
                    SlotNumber = slotNumber,
                    Algorithm = keyType,
                    PinPolicy = pinPolicy
                };
                break;

            case 7:
                cmd = new GenerateKeyPairCommand();
                cmd.SlotNumber = slotNumber;
                cmd.Algorithm = keyType;
                cmd.PinPolicy = pinPolicy;
                break;

            case 8:
                cmd = new GenerateKeyPairCommand
                {
                    SlotNumber = slotNumber,
                    Algorithm = keyType
                };
                break;

            case 9:
                cmd = new GenerateKeyPairCommand();
                cmd.SlotNumber = slotNumber;
                cmd.Algorithm = keyType;
                break;
#pragma warning restore IDE0017
        }

        return cmd;
    }
}
