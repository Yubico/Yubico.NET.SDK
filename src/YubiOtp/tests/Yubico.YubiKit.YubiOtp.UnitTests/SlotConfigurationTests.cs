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

using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class SlotConfigurationTests
{
    // Offsets in the 52-byte config struct
    private const int FixedOffset = 0;
    private const int UidOffset = 16;
    private const int KeyOffset = 22;
    private const int AccCodeOffset = 38;
    private const int FixedSizeOffset = 44;
    private const int ExtFlagsOffset = 45;
    private const int TktFlagsOffset = 46;
    private const int CfgFlagsOffset = 47;
    private const int RfuOffset = 48;
    private const int CrcOffset = 50;

    private static void AssertValidCrc(byte[] config)
    {
        Assert.Equal(YubiOtpConstants.ConfigSize, config.Length);
        Assert.True(ChecksumUtils.CheckCrc(config, YubiOtpConstants.ConfigSize));
    }


    [Fact]
    public void YubiOtp_GetConfig_ProducesValid52ByteStruct()
    {
        byte[] publicId = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];
        byte[] privateId = [0x10, 0x20, 0x30, 0x40, 0x50, 0x60];
        byte[] aesKey = [0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
                         0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        var result = config.GetConfig();

        AssertValidCrc(result);
    }

    [Fact]
    public void YubiOtp_GetConfig_PlacesFieldsCorrectly()
    {
        byte[] publicId = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];
        byte[] privateId = [0x10, 0x20, 0x30, 0x40, 0x50, 0x60];
        byte[] aesKey = [0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
                         0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        var result = config.GetConfig();

        // fixed field: publicId padded with zeros
        Assert.Equal(publicId, result[FixedOffset..(FixedOffset + publicId.Length)]);
        Assert.All(result[(FixedOffset + publicId.Length)..UidOffset], b => Assert.Equal(0, b));

        // uid field: privateId
        Assert.Equal(privateId, result[UidOffset..(UidOffset + 6)]);

        // key field: aesKey
        Assert.Equal(aesKey, result[KeyOffset..(KeyOffset + 16)]);

        // fixed_size
        Assert.Equal(publicId.Length, result[FixedSizeOffset]);

        // acc_code: zeros
        Assert.All(result[AccCodeOffset..(AccCodeOffset + 6)], b => Assert.Equal(0, b));

        // RFU: zeros
        Assert.Equal(0, result[RfuOffset]);
        Assert.Equal(0, result[RfuOffset + 1]);
    }

    [Fact]
    public void YubiOtp_GetConfig_WithAccessCode_PlacesAccessCode()
    {
        byte[] publicId = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];
        byte[] accCode = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        var result = config.GetConfig(accCode);

        Assert.Equal(accCode, result[AccCodeOffset..(AccCodeOffset + 6)]);
        AssertValidCrc(result);
    }

    [Fact]
    public void YubiOtp_PublicIdTooLong_Throws()
    {
        byte[] publicId = new byte[17];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        Assert.Throws<ArgumentException>(() =>
            new YubiOtpSlotConfiguration(publicId, privateId, aesKey));
    }

    [Fact]
    public void YubiOtp_WrongPrivateIdLength_Throws()
    {
        byte[] publicId = [0x01];
        byte[] privateId = [0x01, 0x02, 0x03]; // Wrong length
        byte[] aesKey = new byte[16];

        Assert.Throws<ArgumentException>(() =>
            new YubiOtpSlotConfiguration(publicId, privateId, aesKey));
    }

    [Fact]
    public void YubiOtp_WrongKeyLength_Throws()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[15]; // Wrong length

        Assert.Throws<ArgumentException>(() =>
            new YubiOtpSlotConfiguration(publicId, privateId, aesKey));
    }

    [Fact]
    public void YubiOtp_WithKeyboardFlags_EncodesCorrectly()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        config.AppendCr().PacingChar10();
        var result = config.GetConfig();

        Assert.Equal((byte)TicketFlag.AppendCr, result[TktFlagsOffset] & (byte)TicketFlag.AppendCr);
        Assert.Equal((byte)ConfigFlag.PacingChar10, result[CfgFlagsOffset] & (byte)ConfigFlag.PacingChar10);
        AssertValidCrc(result);
    }



    [Fact]
    public void HmacSha1_GetConfig_ProducesValid52ByteStruct()
    {
        byte[] hmacKey = new byte[20];
        hmacKey.AsSpan().Fill(0xAB);

        using var config = new HmacSha1SlotConfiguration(hmacKey);
        var result = config.GetConfig();

        AssertValidCrc(result);
    }

    [Fact]
    public void HmacSha1_20ByteKey_SplitsAcrossKeyAndUid()
    {
        byte[] hmacKey =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14
        ];

        using var config = new HmacSha1SlotConfiguration(hmacKey);
        var result = config.GetConfig();

        // First 16 bytes in key field
        Assert.Equal(hmacKey[..16], result[KeyOffset..(KeyOffset + 16)]);

        // Last 4 bytes in uid field
        Assert.Equal(hmacKey[16..20], result[UidOffset..(UidOffset + 4)]);

        // uid[4..6] should be zero
        Assert.Equal(0, result[UidOffset + 4]);
        Assert.Equal(0, result[UidOffset + 5]);
    }

    [Fact]
    public void HmacSha1_ShortKey_ThrowsBeforeAnyProcessing()
    {
        byte[] hmacKey = [0xAA, 0xBB, 0xCC];

        var ex = Assert.Throws<ArgumentException>(() =>
            new HmacSha1SlotConfiguration(hmacKey));
        Assert.Equal("hmacKey", ex.ParamName);
    }

    [Fact]
    public void HmacSha1_LongKey_ThrowsInsteadOfHashing()
    {
        // Keys longer than 20 bytes must be rejected, not silently SHA-1 hashed.
        byte[] longKey = new byte[64];
        longKey.AsSpan().Fill(0xFF);

        var ex = Assert.Throws<ArgumentException>(() =>
            new HmacSha1SlotConfiguration(longKey));
        Assert.Equal("hmacKey", ex.ParamName);
    }

    [Fact]
    public void HmacSha1_EmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new HmacSha1SlotConfiguration(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void HmacSha1_ExactLengthKey_PreservedByteForByte_NotHashedOrPadded()
    {
        // ISC-30: valid-length keys must survive into the wire format unmodified.
        byte[] hmacKey = Enumerable.Range(1, 20).Select(i => (byte)i).ToArray();

        using var config = new HmacSha1SlotConfiguration(hmacKey);
        var result = config.GetConfig();

        Assert.Equal(hmacKey[..16], result[KeyOffset..(KeyOffset + 16)]);
        Assert.Equal(hmacKey[16..20], result[UidOffset..(UidOffset + 4)]);

        // Confirm this is NOT the SHA-1 hash of the key (which would indicate
        // the old silent-hashing behavior leaked back in).
        Span<byte> unexpectedHash = stackalloc byte[20];
        SHA1.HashData(hmacKey, unexpectedHash);
        Assert.NotEqual(unexpectedHash[..16].ToArray(), result[KeyOffset..(KeyOffset + 16)]);
    }

    [Fact]
    public void HmacSha1_SetsChalRespFlag()
    {
        byte[] hmacKey = new byte[20];

        using var config = new HmacSha1SlotConfiguration(hmacKey);
        var result = config.GetConfig();

        Assert.Equal((byte)TicketFlag.ChalResp, result[TktFlagsOffset] & (byte)TicketFlag.ChalResp);
    }

    [Fact]
    public void HmacSha1_RequireTouch_SetsChalBtnTrigFlag()
    {
        byte[] hmacKey = new byte[20];

        using var config = new HmacSha1SlotConfiguration(hmacKey);
        config.RequireTouch();
        var result = config.GetConfig();

        Assert.Equal((byte)ConfigFlag.ChalBtnTrig, result[CfgFlagsOffset] & (byte)ConfigFlag.ChalBtnTrig);
    }

    [Fact]
    public void HmacSha1_UseShortChallenge_SetsHmacLt64Flag()
    {
        byte[] hmacKey = new byte[20];

        using var config = new HmacSha1SlotConfiguration(hmacKey);
        config.UseShortChallenge();
        var result = config.GetConfig();

        Assert.Equal((byte)ConfigFlag.HmacLt64, result[CfgFlagsOffset] & (byte)ConfigFlag.HmacLt64);
    }

    [Fact]
    public void HmacSha1_MinimumFirmware_Is220()
    {
        byte[] hmacKey = new byte[20];
        using var config = new HmacSha1SlotConfiguration(hmacKey);

        Assert.True(config.IsSupportedBy(new FirmwareVersion(2, 2, 0)));
        Assert.False(config.IsSupportedBy(new FirmwareVersion(2, 1, 9)));
    }



    [Fact]
    public void YubicoOtpChallengeResponse_GetConfig_ProducesValid52ByteStruct()
    {
        byte[] aesKey = new byte[16];
        aesKey.AsSpan().Fill(0xAB);

        using var config = new YubicoOtpChallengeResponseSlotConfiguration(aesKey);
        var result = config.GetConfig();

        AssertValidCrc(result);
    }

    [Fact]
    public void YubicoOtpChallengeResponse_ExactLengthKey_PreservedByteForByte()
    {
        // ISC-30: valid-length keys must survive into the wire format unmodified.
        byte[] aesKey = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        using var config = new YubicoOtpChallengeResponseSlotConfiguration(aesKey);
        var result = config.GetConfig();

        Assert.Equal(aesKey, result[KeyOffset..(KeyOffset + 16)]);

        // No uid/private-id material is set for challenge-response-only mode.
        Assert.All(result[UidOffset..(UidOffset + 6)], b => Assert.Equal(0, b));
    }

    [Fact]
    public void YubicoOtpChallengeResponse_SetsChalRespAndChalYubicoFlags()
    {
        byte[] aesKey = new byte[16];

        using var config = new YubicoOtpChallengeResponseSlotConfiguration(aesKey);
        var result = config.GetConfig();

        Assert.Equal((byte)TicketFlag.ChalResp, result[TktFlagsOffset] & (byte)TicketFlag.ChalResp);
        Assert.Equal((byte)ConfigFlag.ChalYubico, result[CfgFlagsOffset] & (byte)ConfigFlag.ChalYubico);

        // Must NOT set the HMAC-only bit that differentiates ChalHmac (0x22) from ChalYubico (0x20).
        Assert.Equal(0, result[CfgFlagsOffset] & 0x02);
    }

    [Fact]
    public void YubicoOtpChallengeResponse_RequireTouch_SetsChalBtnTrigFlag()
    {
        byte[] aesKey = new byte[16];

        using var config = new YubicoOtpChallengeResponseSlotConfiguration(aesKey);
        config.RequireTouch();
        var result = config.GetConfig();

        Assert.Equal((byte)ConfigFlag.ChalBtnTrig, result[CfgFlagsOffset] & (byte)ConfigFlag.ChalBtnTrig);
    }

    [Fact]
    public void YubicoOtpChallengeResponse_EmptyKey_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new YubicoOtpChallengeResponseSlotConfiguration(ReadOnlySpan<byte>.Empty));
        Assert.Equal("aesKey", ex.ParamName);
    }

    [Fact]
    public void YubicoOtpChallengeResponse_ShortKey_ThrowsInsteadOfPadding()
    {
        byte[] shortKey = new byte[15];

        var ex = Assert.Throws<ArgumentException>(() =>
            new YubicoOtpChallengeResponseSlotConfiguration(shortKey));
        Assert.Equal("aesKey", ex.ParamName);
    }

    [Fact]
    public void YubicoOtpChallengeResponse_LongKey_ThrowsInsteadOfHashing()
    {
        byte[] longKey = new byte[17];

        var ex = Assert.Throws<ArgumentException>(() =>
            new YubicoOtpChallengeResponseSlotConfiguration(longKey));
        Assert.Equal("aesKey", ex.ParamName);
    }

    [Fact]
    public void YubicoOtpChallengeResponse_MinimumFirmware_Is220()
    {
        byte[] aesKey = new byte[16];
        using var config = new YubicoOtpChallengeResponseSlotConfiguration(aesKey);

        Assert.True(config.IsSupportedBy(new FirmwareVersion(2, 2, 0)));
        Assert.False(config.IsSupportedBy(new FirmwareVersion(2, 1, 9)));
    }



    [Fact]
    public void Hotp_GetConfig_ProducesValid52ByteStruct()
    {
        byte[] hmacKey = new byte[20];
        hmacKey.AsSpan().Fill(0xCC);

        using var config = new HotpSlotConfiguration(hmacKey);
        var result = config.GetConfig();

        AssertValidCrc(result);
    }

    [Fact]
    public void Hotp_SetsOathHotpFlag()
    {
        byte[] hmacKey = new byte[20];

        using var config = new HotpSlotConfiguration(hmacKey);
        var result = config.GetConfig();

        Assert.Equal((byte)TicketFlag.OathHotp, result[TktFlagsOffset] & (byte)TicketFlag.OathHotp);
    }

    [Fact]
    public void Hotp_KeySplitMatchesHmacSha1()
    {
        byte[] hmacKey =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14
        ];

        using var config = new HotpSlotConfiguration(hmacKey);
        var result = config.GetConfig();

        // First 16 bytes in key field
        Assert.Equal(hmacKey[..16], result[KeyOffset..(KeyOffset + 16)]);

        // Bytes 16-19 in uid[0..4]
        Assert.Equal(hmacKey[16..20], result[UidOffset..(UidOffset + 4)]);
    }

    [Fact]
    public void Hotp_WithImf_StoresInUid()
    {
        byte[] hmacKey = new byte[20];
        int imf = 0x30000; // 3 * 0x10000

        using var config = new HotpSlotConfiguration(hmacKey, imf);
        var result = config.GetConfig();

        // IMF / 0x10000 = 3, stored big-endian in uid[4..6]
        Assert.Equal(0x00, result[UidOffset + 4]);
        Assert.Equal(0x03, result[UidOffset + 5]);
        AssertValidCrc(result);
    }

    [Fact]
    public void Hotp_WithLargeImf_StoresCorrectly()
    {
        byte[] hmacKey = new byte[20];
        int imf = 0xFF0000; // 0xFF * 0x10000

        using var config = new HotpSlotConfiguration(hmacKey, imf);
        var result = config.GetConfig();

        // IMF / 0x10000 = 0x00FF, big-endian
        Assert.Equal(0x00, result[UidOffset + 4]);
        Assert.Equal(0xFF, result[UidOffset + 5]);
    }

    [Fact]
    public void Hotp_ZeroImf_NoDataInUid45()
    {
        byte[] hmacKey = new byte[20];

        using var config = new HotpSlotConfiguration(hmacKey, imf: 0);
        var result = config.GetConfig();

        Assert.Equal(0, result[UidOffset + 4]);
        Assert.Equal(0, result[UidOffset + 5]);
    }

    [Fact]
    public void Hotp_InvalidImf_Throws()
    {
        byte[] hmacKey = new byte[20];

        // Not a multiple of 0x10000
        Assert.Throws<ArgumentException>(() => new HotpSlotConfiguration(hmacKey, imf: 100));
    }

    [Fact]
    public void Hotp_NegativeImf_Throws()
    {
        byte[] hmacKey = new byte[20];

        Assert.Throws<ArgumentException>(() => new HotpSlotConfiguration(hmacKey, imf: -1));
    }

    [Fact]
    public void Hotp_EmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new HotpSlotConfiguration(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Hotp_LongKey_ThrowsInsteadOfHashing()
    {
        byte[] longKey = new byte[32];
        longKey.AsSpan().Fill(0xDD);

        var ex = Assert.Throws<ArgumentException>(() => new HotpSlotConfiguration(longKey));
        Assert.Equal("hmacKey", ex.ParamName);
    }

    [Fact]
    public void Hotp_ShortKey_ThrowsInsteadOfPadding()
    {
        byte[] shortKey = [0x01, 0x02, 0x03];

        var ex = Assert.Throws<ArgumentException>(() => new HotpSlotConfiguration(shortKey));
        Assert.Equal("hmacKey", ex.ParamName);
    }

    [Fact]
    public void Hotp_InvalidImfAndInvalidKeyLength_ThrowsForImfFirst()
    {
        // Locks in validation order: IMF is checked before the key length (via ProcessHmacKey).
        // If this ordering is intentionally reversed in the future, this test must be updated
        // to match, so the change is deliberate rather than an unnoticed side effect.
        byte[] shortKey = [0x01, 0x02, 0x03];

        var ex = Assert.Throws<ArgumentException>(() => new HotpSlotConfiguration(shortKey, imf: 100));

        Assert.Equal("imf", ex.ParamName);
    }

    [Fact]
    public void Hotp_MinimumFirmware_Is210()
    {
        byte[] hmacKey = new byte[20];
        using var config = new HotpSlotConfiguration(hmacKey);

        Assert.True(config.IsSupportedBy(new FirmwareVersion(2, 1, 0)));
        Assert.False(config.IsSupportedBy(new FirmwareVersion(2, 0, 9)));
    }



    [Fact]
    public void StaticPassword_GetConfig_ProducesValid52ByteStruct()
    {
        byte[] scanCodes = [0x04, 0x05, 0x06, 0x07, 0x08];

        using var config = new StaticPasswordSlotConfiguration(scanCodes);
        var result = config.GetConfig();

        AssertValidCrc(result);
    }

    [Fact]
    public void StaticPassword_ShortScanCodes_PlacedInFixed()
    {
        byte[] scanCodes = [0x04, 0x05, 0x06];

        using var config = new StaticPasswordSlotConfiguration(scanCodes);
        var result = config.GetConfig();

        Assert.Equal(scanCodes, result[FixedOffset..(FixedOffset + 3)]);
        Assert.All(result[(FixedOffset + 3)..UidOffset], b => Assert.Equal(0, b));
        Assert.Equal(3, result[FixedSizeOffset]);
    }

    [Fact]
    public void StaticPassword_FullFixed_SpillsIntoUid()
    {
        byte[] scanCodes = new byte[20];
        for (int i = 0; i < 20; i++) scanCodes[i] = (byte)(i + 1);

        using var config = new StaticPasswordSlotConfiguration(scanCodes);
        var result = config.GetConfig();

        // First 16 in fixed
        Assert.Equal(scanCodes[..16], result[FixedOffset..(FixedOffset + 16)]);
        // Next 4 in uid
        Assert.Equal(scanCodes[16..20], result[UidOffset..(UidOffset + 4)]);
        // uid[4..6] should be zero
        Assert.Equal(0, result[UidOffset + 4]);
        Assert.Equal(0, result[UidOffset + 5]);

        Assert.Equal(16, result[FixedSizeOffset]);
        AssertValidCrc(result);
    }

    [Fact]
    public void StaticPassword_Full38Bytes_FillsAllFields()
    {
        byte[] scanCodes = new byte[38];
        for (int i = 0; i < 38; i++) scanCodes[i] = (byte)(i + 1);

        using var config = new StaticPasswordSlotConfiguration(scanCodes);
        var result = config.GetConfig();

        Assert.Equal(scanCodes[..16], result[FixedOffset..(FixedOffset + 16)]);
        Assert.Equal(scanCodes[16..22], result[UidOffset..(UidOffset + 6)]);
        Assert.Equal(scanCodes[22..38], result[KeyOffset..(KeyOffset + 16)]);
        Assert.Equal(16, result[FixedSizeOffset]);
        AssertValidCrc(result);
    }

    [Fact]
    public void StaticPassword_SetsShortTicketFlag()
    {
        byte[] scanCodes = [0x04];

        using var config = new StaticPasswordSlotConfiguration(scanCodes);
        var result = config.GetConfig();

        byte cfgFlags = result[CfgFlagsOffset];
        Assert.Equal((byte)ConfigFlag.ShortTicket, cfgFlags & (byte)ConfigFlag.ShortTicket);
        // ykman canonical: StaticPassword only sets SHORT_TICKET, not STATIC_TICKET
        Assert.Equal(0, cfgFlags & (byte)ConfigFlag.StaticTicket & ~(byte)ConfigFlag.ShortTicket);
    }

    [Fact]
    public void StaticPassword_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new StaticPasswordSlotConfiguration(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void StaticPassword_TooLong_Throws()
    {
        byte[] scanCodes = new byte[39];

        Assert.Throws<ArgumentException>(() =>
            new StaticPasswordSlotConfiguration(scanCodes));
    }

    [Fact]
    public void StaticPassword_FromString_EnUsLayout_TranslatesToExpectedScanCodes()
    {
        // ISC-27: exact HID scan-code vectors matching HidCodeTranslator's en_US mapping.
        using var config = new StaticPasswordSlotConfiguration("abc", KeyboardLayout.en_US);
        var result = config.GetConfig();

        byte[] expectedScanCodes = [0x04, 0x05, 0x06]; // 'a', 'b', 'c'
        Assert.Equal(expectedScanCodes, result[FixedOffset..(FixedOffset + 3)]);
        Assert.Equal(3, result[FixedSizeOffset]);
    }

    [Fact]
    public void StaticPassword_FromString_UppercaseCharacters_SetShiftBit()
    {
        using var config = new StaticPasswordSlotConfiguration("Ab1", KeyboardLayout.en_US);
        var result = config.GetConfig();

        // 'A' = 0x04 with the shift bit (0x80) set, 'b' = 0x05, '1' = 0x1e
        byte[] expectedScanCodes = [0x04 | 0x80, 0x05, 0x1e];
        Assert.Equal(expectedScanCodes, result[FixedOffset..(FixedOffset + 3)]);
    }

    [Fact]
    public void StaticPassword_FromString_ModHexLayout_MatchesHidCodeTranslatorModHex()
    {
        var translator = HidCodeTranslator.GetInstance(KeyboardLayout.ModHex);
        using var config = new StaticPasswordSlotConfiguration("cccb", KeyboardLayout.ModHex);
        var result = config.GetConfig();

        byte[] expectedScanCodes = [translator['c'], translator['c'], translator['c'], translator['b']];
        Assert.Equal(expectedScanCodes, result[FixedOffset..(FixedOffset + 4)]);
    }

    [Fact]
    public void StaticPassword_FromString_MatchesRawScanCodeConstructor()
    {
        // The string overload must produce byte-identical wire output to the
        // equivalent raw scan-code constructor -- it is a convenience translation only.
        var translator = HidCodeTranslator.GetInstance(KeyboardLayout.en_US);
        byte[] rawScanCodes = translator.GetHidCodes("Password1");

        using var fromString = new StaticPasswordSlotConfiguration("Password1", KeyboardLayout.en_US);
        using var fromRaw = new StaticPasswordSlotConfiguration(rawScanCodes);

        Assert.Equal(fromRaw.GetConfig(), fromString.GetConfig());
    }

    [Fact]
    public void StaticPassword_FromString_CopiesOwnedScanCodesBeforeZeroing()
    {
        byte[] ownedScanCodes = [0x04, 0x05, 0x06];
        byte[] expectedScanCodes = [.. ownedScanCodes];

        using var config = new StaticPasswordSlotConfiguration(
            "abc",
            KeyboardLayout.en_US,
            (password, keyboardLayout) =>
            {
                Assert.Equal("abc", password);
                Assert.Equal(KeyboardLayout.en_US, keyboardLayout);
                return ownedScanCodes;
            });
        var result = config.GetConfig();

        Assert.Equal(expectedScanCodes, result[FixedOffset..(FixedOffset + expectedScanCodes.Length)]);
        Assert.All(ownedScanCodes, scanCode => Assert.Equal(0, scanCode));
    }

    [Fact]
    public void StaticPassword_FromString_WhenInitializationFails_ZeroesOwnedScanCodes()
    {
        byte[] ownedScanCodes = Enumerable.Repeat((byte)0x04, 39).ToArray();

        Assert.Throws<ArgumentException>(() =>
            new StaticPasswordSlotConfiguration(
                new string('a', 39),
                KeyboardLayout.en_US,
                (_, _) => ownedScanCodes));

        Assert.All(ownedScanCodes, scanCode => Assert.Equal(0, scanCode));
    }

    [Fact]
    public void StaticPassword_FromRawScanCodes_DoesNotZeroCallerBuffer()
    {
        byte[] callerOwnedScanCodes = [0x04, 0x05, 0x06];
        byte[] expectedScanCodes = [.. callerOwnedScanCodes];

        using var config = new StaticPasswordSlotConfiguration(callerOwnedScanCodes);

        Assert.Equal(expectedScanCodes, callerOwnedScanCodes);
    }

    [Fact]
    public void StaticPassword_FromString_NullPassword_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new StaticPasswordSlotConfiguration(null!, KeyboardLayout.en_US));
    }

    [Fact]
    public void StaticPassword_FromString_EmptyPassword_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new StaticPasswordSlotConfiguration(string.Empty, KeyboardLayout.en_US));
    }

    [Fact]
    public void StaticPassword_FromString_TooLong_Throws()
    {
        string password = new('a', 39);

        Assert.Throws<ArgumentException>(() =>
            new StaticPasswordSlotConfiguration(password, KeyboardLayout.en_US));
    }

    [Fact]
    public void StaticPassword_FromString_UnsupportedCharacterForLayout_Throws()
    {
        // 'é' is not part of the en_US HID scan-code map.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StaticPasswordSlotConfiguration("café", KeyboardLayout.en_US));
    }



    [Fact]
    public void StaticTicket_GetConfig_ProducesValid52ByteStruct()
    {
        byte[] publicId = [0x01, 0x02];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        using var config = new StaticTicketSlotConfiguration(publicId, privateId, aesKey);
        var result = config.GetConfig();

        AssertValidCrc(result);
    }

    [Fact]
    public void StaticTicket_SetsStaticTicketFlag()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        using var config = new StaticTicketSlotConfiguration(publicId, privateId, aesKey);
        var result = config.GetConfig();

        Assert.Equal((byte)ConfigFlag.StaticTicket, result[CfgFlagsOffset] & (byte)ConfigFlag.StaticTicket);
    }

    [Fact]
    public void StaticTicket_PlacesFieldsSameAsYubiOtp()
    {
        byte[] publicId = [0x01, 0x02, 0x03];
        byte[] privateId = [0x10, 0x20, 0x30, 0x40, 0x50, 0x60];
        byte[] aesKey =
        [
            0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
            0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF
        ];

        using var config = new StaticTicketSlotConfiguration(publicId, privateId, aesKey);
        var result = config.GetConfig();

        Assert.Equal(publicId, result[FixedOffset..(FixedOffset + 3)]);
        Assert.Equal(privateId, result[UidOffset..(UidOffset + 6)]);
        Assert.Equal(aesKey, result[KeyOffset..(KeyOffset + 16)]);
        Assert.Equal(3, result[FixedSizeOffset]);
    }



    [Fact]
    public void Update_GetConfig_ProducesValid52ByteStruct()
    {
        using var config = new UpdateConfiguration();
        config.AppendCr().Dormant();
        var result = config.GetConfig();

        AssertValidCrc(result);
    }

    [Fact]
    public void Update_MasksExtendedFlags()
    {
        using var config = new UpdateConfiguration();
        config.Dormant().UseNumericKeypad().FastTrigger();

        var result = config.GetConfig();
        byte extFlags = result[ExtFlagsOffset];

        // AllowUpdate is NOT in the update mask, so shouldn't be writable through the mask
        // But Dormant, UseNumericKeypad, FastTrigger ARE in the mask
        Assert.Equal((byte)ExtendedFlag.Dormant, extFlags & (byte)ExtendedFlag.Dormant);
        Assert.Equal((byte)ExtendedFlag.UseNumericKeypad, extFlags & (byte)ExtendedFlag.UseNumericKeypad);
        Assert.Equal((byte)ExtendedFlag.FastTrigger, extFlags & (byte)ExtendedFlag.FastTrigger);
    }

    [Fact]
    public void Update_MasksTicketFlags()
    {
        using var config = new UpdateConfiguration();
        config.AppendCr().TabFirst().AppendTab1();

        var result = config.GetConfig();
        byte tktFlags = result[TktFlagsOffset];

        Assert.Equal((byte)TicketFlag.AppendCr, tktFlags & (byte)TicketFlag.AppendCr);
        Assert.Equal((byte)TicketFlag.TabFirst, tktFlags & (byte)TicketFlag.TabFirst);
        Assert.Equal((byte)TicketFlag.AppendTab1, tktFlags & (byte)TicketFlag.AppendTab1);
    }

    [Fact]
    public void Update_MasksConfigFlags()
    {
        using var config = new UpdateConfiguration();
        config.PacingChar10().PacingChar20();

        var result = config.GetConfig();
        byte cfgFlags = result[CfgFlagsOffset];

        Assert.Equal((byte)ConfigFlag.PacingChar10, cfgFlags & (byte)ConfigFlag.PacingChar10);
        Assert.Equal((byte)ConfigFlag.PacingChar20, cfgFlags & (byte)ConfigFlag.PacingChar20);
    }

    [Fact]
    public void Update_AllowUpdateFlag_MaskedOut()
    {
        using var config = new UpdateConfiguration();
        // AllowUpdate is set on base class, but UpdateMask excludes it
        config.AllowUpdate();

        var result = config.GetConfig();
        byte extFlags = result[ExtFlagsOffset];

        // AllowUpdate should be masked out
        Assert.Equal(0, extFlags & (byte)ExtendedFlag.AllowUpdate);
    }

    [Fact]
    public void Update_NoKeyMaterial_AllZeros()
    {
        using var config = new UpdateConfiguration();
        config.AppendCr();
        var result = config.GetConfig();

        // fixed, uid, key fields should all be zeros
        Assert.All(result[FixedOffset..UidOffset], b => Assert.Equal(0, b));
        Assert.All(result[UidOffset..KeyOffset], b => Assert.Equal(0, b));
        Assert.All(result[KeyOffset..AccCodeOffset], b => Assert.Equal(0, b));
    }

    [Fact]
    public void Update_MinimumFirmware_Is230()
    {
        using var config = new UpdateConfiguration();

        Assert.True(config.IsSupportedBy(new FirmwareVersion(2, 3, 0)));
        Assert.False(config.IsSupportedBy(new FirmwareVersion(2, 2, 9)));
    }



    [Fact]
    public void Base_AllowUpdate_SetsExtFlag()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        config.AllowUpdate();
        var result = config.GetConfig();

        Assert.Equal((byte)ExtendedFlag.AllowUpdate, result[ExtFlagsOffset] & (byte)ExtendedFlag.AllowUpdate);
    }

    [Fact]
    public void Base_InvertLed_SetsExtFlag()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        config.InvertLed();
        var result = config.GetConfig();

        Assert.Equal((byte)ExtendedFlag.InvertLed, result[ExtFlagsOffset] & (byte)ExtendedFlag.InvertLed);
    }

    [Fact]
    public void Base_ProtectSlot2_SetsTktFlag()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        config.ProtectSlot2();
        var result = config.GetConfig();

        Assert.Equal((byte)TicketFlag.ProtectSlot2, result[TktFlagsOffset] & (byte)TicketFlag.ProtectSlot2);
    }

    [Fact]
    public void Base_MultipleFlags_CombineCorrectly()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        config.AllowUpdate().Dormant().SerialApiVisible().InvertLed();
        var result = config.GetConfig();

        // Includes defaults: SerialApiVisible + AllowUpdate (base) + FastTrigger (keyboard)
        byte expected = (byte)(
            ExtendedFlag.AllowUpdate |
            ExtendedFlag.Dormant |
            ExtendedFlag.SerialApiVisible |
            ExtendedFlag.InvertLed |
            ExtendedFlag.FastTrigger);

        Assert.Equal(expected, result[ExtFlagsOffset]);
        AssertValidCrc(result);
    }

    [Fact]
    public void Base_FlagToggleOff_ClearsFlag()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        config.AllowUpdate(true).AllowUpdate(false);
        var result = config.GetConfig();

        Assert.Equal(0, result[ExtFlagsOffset] & (byte)ExtendedFlag.AllowUpdate);
    }

    [Fact]
    public void Base_Dispose_ZerosKeyMaterial()
    {
        byte[] publicId = [0x01];
        byte[] privateId = [0x10, 0x20, 0x30, 0x40, 0x50, 0x60];
        byte[] aesKey =
        [
            0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
            0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF
        ];

        var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);

        // Verify data is there before dispose
        var priorResult = config.GetConfig();
        Assert.Equal(0xA0, priorResult[KeyOffset]);

        config.Dispose();

        // After dispose, GetConfig should throw
        Assert.Throws<ObjectDisposedException>(() => config.GetConfig());
    }

    [Fact]
    public void Base_DefaultMinimumFirmware_Is200()
    {
        byte[] publicId = [0x01];
        byte[] privateId = new byte[6];
        byte[] aesKey = new byte[16];
        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);

        Assert.True(config.IsSupportedBy(new FirmwareVersion(2, 0, 0)));
    }



    [Fact]
    public void Crc_AllZeroConfig_ValidCrc()
    {
        // UpdateConfiguration with no flags produces a mostly-zero config
        using var config = new UpdateConfiguration();
        var result = config.GetConfig();

        AssertValidCrc(result);
    }

    [Fact]
    public void Crc_DifferentConfigs_ProduceDifferentCrcs()
    {
        byte[] key1 = new byte[16];
        byte[] key2 = new byte[16];
        key2.AsSpan().Fill(0xFF);
        byte[] privateId = new byte[6];
        byte[] publicId = [0x01];

        using var config1 = new YubiOtpSlotConfiguration(publicId, privateId, key1);
        using var config2 = new YubiOtpSlotConfiguration(publicId, privateId, key2);

        var result1 = config1.GetConfig();
        var result2 = config2.GetConfig();

        // CRCs should differ
        ushort crc1 = (ushort)(result1[CrcOffset] | (result1[CrcOffset + 1] << 8));
        ushort crc2 = (ushort)(result2[CrcOffset] | (result2[CrcOffset + 1] << 8));
        Assert.NotEqual(crc1, crc2);
    }

}