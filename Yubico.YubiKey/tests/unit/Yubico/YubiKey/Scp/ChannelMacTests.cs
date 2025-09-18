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
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Scp.Helpers;

namespace Yubico.YubiKey.Scp;

public class ChannelMacTests
{
    private static byte[] GetKey()
    {
        return Hex.HexToBytes("404142434445464748494A4B4C4D4E4F");
    }

    private static byte[] GetBadKey()
    {
        return Hex.HexToBytes("40414243");
    }

    private static CommandApdu GetApdu()
    {
        return new CommandApdu { Ins = 0xFD };
    }

    private static byte[] GetCorrectMacdApduBytes()
    {
        return Hex.HexToBytes("00FD00000806e58094d47d8908");
    }

    private static byte[] GetMac()
    {
        return Hex.HexToBytes("CB85E66F9DD7C009");
    }

    private static byte[] GetMcv()
    {
        return Hex.HexToBytes("CB85E66F9DD7C009CB85E66F9DD7C009");
    }

    private static byte[] GetBadMcv()
    {
        return Hex.HexToBytes("B85E66F9DD7C09");
    }

    private static byte[] GetResponseForVerify()
    {
        return Hex.HexToBytes("5F67E9E059DF3C52809DC9F6DDFBEF3E4C45691B2C8CDDD8");
    }

    private static byte[] GetBadResponse()
    {
        return Hex.HexToBytes("5F67E9E059DF3C52809DC9F6DDFBEF3E4C45691B2C8CDDD9");
    }

    private static byte[] GetMcvForVerify()
    {
        return Hex.HexToBytes("53C2C04391250CCEC0213FF68C877EDA");
    }

    private static byte[] GetRmacKey()
    {
        return Hex.HexToBytes("38C0C6E3D0B6AED40FBB420B51399081");
    }

    [Fact]
    public void MacApdu_GivenBadKey_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => ChannelMac.MacApdu(GetApdu(), GetBadKey(), GetMcv()));
    }

    [Fact]
    public void MacApdu_GivenBadMcv_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => ChannelMac.MacApdu(GetApdu(), GetKey(), GetBadMcv()));
    }

    [Fact]
    public void MacApdu_GivenCorrectKeyPayload_ReturnsCorrectMacdApdu()
    {
        var (output, _) = ChannelMac.MacApdu(GetApdu(), GetKey(), GetMcv());
        Assert.Equal(GetCorrectMacdApduBytes(), output.AsByteArray());
    }

    [Fact]
    public void VerifyRmac_GivenBadKey_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() =>
            ChannelMac.VerifyRmac(GetResponseForVerify(), GetBadKey(), GetMcvForVerify()));
    }

    [Fact]
    public void VerifyRmac_GivenBadMcv_ThrowsSecureChannelException()
    {
        _ = Assert.Throws<SecureChannelException>(() =>
            ChannelMac.VerifyRmac(GetResponseForVerify(), GetRmacKey(), GetBadMcv()));
    }

    [Fact]
    public void VerifyRmac_GivenBadRmac_ThrowsSecureChannelException()
    {
        _ = Assert.Throws<SecureChannelException>(() =>
            ChannelMac.VerifyRmac(GetBadResponse(), GetRmacKey(), GetMcvForVerify()));
    }

    [Fact]
    public void VerifyRmac_GivenCorrectKeyPayload_ReturnsCorrectMacdApdu()
    {
        ChannelMac.VerifyRmac(GetResponseForVerify(), GetRmacKey(), GetMcvForVerify());
    }
}
