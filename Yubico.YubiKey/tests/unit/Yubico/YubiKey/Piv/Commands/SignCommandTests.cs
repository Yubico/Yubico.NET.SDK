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
using System.Collections.Generic;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Commands;

public class SignCommandTests
{
    [Fact]
    public void ClassType_DerivedFromPivCommand_IsTrue()
    {
        var digest = PivCommandResponseTestData.GetDigestData(KeyType.RSA1024);
        var signCommand = new AuthenticateSignCommand(digest, 0x9A, KeyType.RSA1024.GetPivAlgorithm());

        Assert.True(signCommand is IYubiKeyCommand<AuthenticateSignResponse>);
    }

    [Fact]
    public void FullConstructor_NullDigestData_ThrowsException()
    {
        _ = Assert.Throws<ArgumentException>(() => _ = new AuthenticateSignCommand(null, 0x87));
    }

    [Theory]
    [InlineData(0x9B)]
    [InlineData(0x80)]
    [InlineData(0x81)]
    [InlineData(0x00)]
    [InlineData(0xF9)]
    [InlineData(0x99)]
    public void Constructor_BadSlotNumber_ThrowsException(
        byte slotNumber)
    {
        _ = Assert.Throws<ArgumentException>(() => GetCommandObject(slotNumber, KeyType.ECP256));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Constructor_BadDigestData_ThrowsException(
        int badKeyNum)
    {
        var digest = GetBadDigestData(badKeyNum);
        _ = Assert.Throws<ArgumentException>(() => new AuthenticateSignCommand(digest, 0x9A));
    }

    [Fact]
    public void Constructor_Application_Piv()
    {
        var digest = PivCommandResponseTestData.GetDigestData(KeyType.RSA2048);
        var command = new AuthenticateSignCommand(digest, 0x95);

        var application = command.Application;

        Assert.Equal(YubiKeyApplication.Piv, application);
    }

    [Theory]
    [InlineData(0x9A, KeyType.ECP256)]
    [InlineData(0x9C, KeyType.ECP384)]
    [InlineData(0x82, KeyType.RSA1024)]
    [InlineData(0x83, KeyType.RSA2048)]
    public void Constructor_Property_SlotNum(
        byte slotNumber,
        KeyType keyType)
    {
        var command = GetCommandObject(slotNumber, keyType);

        var getSlotNum = command.SlotNumber;

        Assert.Equal(slotNumber, getSlotNum);
    }

    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    public void CreateCommandApdu_GetClaProperty_ReturnsZero(
        KeyType keyType)
    {
        var cmdApdu = GetSignCommandApdu(0x8F, keyType);

        var Cla = cmdApdu.Cla;

        Assert.Equal(0, Cla);
    }

    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.RSA3072)]
    [InlineData(KeyType.RSA4096)]
    public void CreateCommandApdu_GetInsProperty_ReturnsHex87(
        KeyType keyType)
    {
        var cmdApdu = GetSignCommandApdu(0x90, keyType);

        var Ins = cmdApdu.Ins;

        Assert.Equal(0x87, Ins);
    }

    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.RSA3072)]
    [InlineData(KeyType.RSA4096)]
    public void CreateCommandApdu_GetP1Property_ReturnsAlgorithm(
        KeyType keyType)
    {
        var cmdApdu = GetSignCommandApdu(0x91, keyType);

        var P1 = cmdApdu.P1;

        Assert.Equal((byte)keyType.GetPivAlgorithm(), P1);
    }

    [Theory]
    [InlineData(0x9D, KeyType.ECP256)]
    [InlineData(0x9E, KeyType.ECP384)]
    [InlineData(0x92, KeyType.RSA1024)]
    [InlineData(0x93, KeyType.RSA2048)]
    public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(
        byte slotNumber,
        KeyType keyType)
    {
        var cmdApdu = GetSignCommandApdu(slotNumber, keyType);

        var P2 = cmdApdu.P2;

        Assert.Equal(slotNumber, P2);
    }

    [Theory]
    [InlineData(KeyType.ECP256, 38)]
    [InlineData(KeyType.ECP384, 54)]
    [InlineData(KeyType.RSA1024, 136)]
    [InlineData(KeyType.RSA2048, 266)]
    public void CreateCommandApdu_GetNcProperty_ReturnsCorrect(
        KeyType keyType,
        int expected)
    {
        var cmdApdu = GetSignCommandApdu(0x94, keyType);

        var Nc = cmdApdu.Nc;

        Assert.Equal(expected, Nc);
    }

    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.RSA1024)]
    public void CreateCommandApdu_GetNeProperty_ReturnsZero(
        KeyType keyType)
    {
        var cmdApdu = GetSignCommandApdu(0x95, keyType);

        var Ne = cmdApdu.Ne;

        Assert.Equal(0, Ne);
    }

    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    public void CreateCommandApdu_GetData_ReturnsCorrect(
        KeyType keyType)
    {
        var prefix = GetDigestDataPrefix(keyType);
        var digest = PivCommandResponseTestData.GetDigestData(keyType);
        var expected = new List<byte>(prefix);
        expected.AddRange(digest);

        var cmdApdu = GetSignCommandApdu(0x85, keyType);

        var data = cmdApdu.Data;

        Assert.False(data.IsEmpty);
        if (data.IsEmpty)
        {
            return;
        }

        var compareResult = data.Span.SequenceEqual(expected.ToArray());

        Assert.True(compareResult);
    }

    [Fact]
    public void CreateResponseForApdu_ReturnsCorrectType()
    {
        var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });

        var command = GetCommandObject(0x86, KeyType.ECP256);

        var response = command.CreateResponseForApdu(responseApdu);

        Assert.True(response is AuthenticateSignResponse);
    }

    private static CommandApdu GetSignCommandApdu(
        byte slotNumber,
        KeyType keyType)
    {
        var cmd = GetCommandObject(slotNumber, keyType);

        return cmd.CreateCommandApdu();
    }

    private static AuthenticateSignCommand GetCommandObject(
        byte slotNumber,
        KeyType keyType)
    {
        var digest = PivCommandResponseTestData.GetDigestData(keyType);
        var cmd = new AuthenticateSignCommand(digest, slotNumber);

        return cmd;
    }

    // Get some bad digest data.
    // if badDataNum is
    // 1 RSA-1024 with an extra byte
    // 2 RSA-1024 with a missing byte
    // 3 RSA-2048 with an extra byte
    // 4 RSA-2048 with a missing byte
    // 5 ECC-P256 with an extra byte
    // 6 ECC-P256 with a missing byte
    // 7 ECC-P384 with an extra byte
    // 8 ECC-P384 with a missing byte
    // Get regular data, and then if badDataNum is odd, add a byte, if even,
    // remove a byte.
    private static byte[] GetBadDigestData(
        int badDataNum)
    {
        byte[] digest;
        switch (badDataNum)
        {
            default:
            case 1:
                digest = PivCommandResponseTestData.GetDigestData(KeyType.RSA1024);
                break;

            case 3:
            case 4:
                digest = PivCommandResponseTestData.GetDigestData(KeyType.RSA2048);
                break;

            case 5:
            case 6:
                digest = PivCommandResponseTestData.GetDigestData(KeyType.ECP256);
                break;

            case 7:
            case 8:
                digest = PivCommandResponseTestData.GetDigestData(KeyType.ECP384);
                break;
        }

        if ((badDataNum & 1) != 0)
        {
            Array.Resize(ref digest, digest.Length + 1);
            digest[^1] = 0x44;
        }
        else
        {
            Array.Resize(ref digest, digest.Length - 1);
        }

        return digest;
    }

    // Get the TL TL TL prefix for each keyType.
    private static byte[] GetDigestDataPrefix(
        KeyType keyType)
    {
        return keyType switch
        {
            KeyType.RSA2048 => new byte[] { 0x7C, 0x82, 0x01, 0x06, 0x82, 0x00, 0x81, 0x82, 0x01, 0x00 },
            KeyType.ECP256 => new byte[] { 0x7C, 0x24, 0x82, 0x00, 0x81, 0x20 },
            KeyType.ECP384 => new byte[] { 0x7C, 0x34, 0x82, 0x00, 0x81, 0x30 },
            _ => new byte[] { 0x7C, 0x81, 0x85, 0x82, 0x00, 0x81, 0x81, 0x80 }
        };
    }
}
