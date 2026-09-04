// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Transports.Hid.Linux;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
/// Characterizes Linux HID report-descriptor parsing, including malformed descriptors. Some
/// expectations preserve parser choices rather than define general HID protocol requirements.
/// </summary>
public class LinuxHidDescriptorParsingTests
{
    // Losing the canonical U2F usage pair prevents Linux HID discovery from classifying the device.
    public static readonly byte[] FidoU2f =
    [
        0x06, 0xD0, 0xF1, 0x09, 0x01, 0xA1, 0x01, 0x09, 0x20, 0x15, 0x00, 0x26, 0xFF, 0x00, 0x75, 0x08,
        0x95, 0x40, 0x81, 0x02, 0x09, 0x21, 0x15, 0x00, 0x26, 0xFF, 0x00, 0x75, 0x08, 0x95, 0x40, 0x91, 0x02,
        0xC0
    ];

    // The keyboard usage pair is required for OTP classification.
    public static readonly byte[] KeyboardOtp =
    [
        0x05, 0x01, 0x09, 0x06, 0xA1, 0x01, 0x75, 0x08, 0x95, 0x08, 0x81, 0x02, 0x75, 0x08, 0x95, 0x08,
        0x91, 0x02, 0xC0
    ];

    public static readonly byte[] Empty = [];

    // A truncated short item is not partially decoded.
    public static readonly byte[] TruncatedValue = [0x06, 0xD0];

    // Values decoded before a truncated trailing item are preserved.
    public static readonly byte[] TruncatedTrailingPrefix = [0x05, 0x01, 0x09];

    // HID size code 3 means four little-endian bytes; usage results are narrowed to ushort.
    public static readonly byte[] Size3Means4Bytes = [0x07, 0x01, 0x02, 0x03, 0x04, 0x0B, 0xAA, 0xBB, 0xCC, 0xDD];

    // A zero report count does not replace the default report size.
    public static readonly byte[] ReportCountZero = [0x75, 0x08, 0x95, 0x00, 0x81, 0x02];

    // Main items need prior report-size and report-count globals to determine their byte size.
    public static readonly byte[] MainBeforeGlobals = [0x81, 0x02, 0x91, 0x02];

    // A descriptor can replace the default with a non-default byte-aligned report size.
    public static readonly byte[] Input63Bytes = [0x75, 0x08, 0x95, 0x3F, 0x81, 0x02];

    // Non-byte-aligned reports round up to a whole byte.
    public static readonly byte[] BitRoundingSize1Count3 = [0x75, 0x01, 0x95, 0x03, 0x81, 0x02];

    // HID global items persist across subsequent main items.
    public static readonly byte[] GlobalsPersistAcrossMain = [0x75, 0x08, 0x95, 0x20, 0x81, 0x02, 0x91, 0x02];

    // Classification uses the first Usage Page item.
    public static readonly byte[] SecondUsagePageIgnored = [0x05, 0x01, 0x05, 0x0C, 0x09, 0x06];

    // Usage and Usage Page are captured independently of their order.
    public static readonly byte[] UsageBeforeUsagePage = [0x09, 0x06, 0x05, 0x01];

    public static readonly byte[] LongItem0xFE = [0xFE, 0x02, 0xAA, 0xBB, 0xCC, 0x05, 0x01, 0x09, 0x06];

    public static readonly byte[] LongItemZeroDataSize = [0xFE, 0x00, 0xAA, 0x05, 0x01, 0x09, 0x06];

    public static readonly byte[] LongItemFourByteDataSize =
        [0xFE, 0x04, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0x05, 0x01, 0x09, 0x06];

    // A truncated long-item payload stops parsing after preserving preceding values.
    public static readonly byte[] TruncatedLongItemPayload = [0x05, 0x01, 0xFE, 0x05, 0xAA, 0xBB, 0xCC];

    public static readonly byte[] LongItemPrefixOnly = [0xFE];

    public static readonly byte[] LongItemHeaderTruncated = [0xFE, 0x02];

    // A descriptor containing only complete long items terminates without producing a short item.
    public static readonly byte[] AllLongItems = [0xFE, 0x01, 0xAA, 0xBB, 0xFE, 0x00, 0xCC];

    // Zero-size short items still make progress through the descriptor.
    public static readonly byte[] ZeroSizeItemsOnly = [0xC0, 0xC0, 0xC0];

    // These two globals make multi-byte little-endian assembly observable in report-size parsing.
    public static readonly byte[] TwoByteReportCount = [0x75, 0x08, 0x96, 0x00, 0x01, 0x81, 0x02];

    public static readonly byte[] TwoByteReportSize = [0x76, 0x10, 0x00, 0x95, 0x04, 0x81, 0x02];

    [Theory]
    [MemberData(nameof(UsageVectors))]
    public void ParseHidDescriptorBytes_PinnedVectors_ReturnsExactTuple(
        string vectorName, byte[] descriptor, ushort expectedUsagePage, ushort expectedUsage)
    {
        _ = vectorName;

        (ushort usagePage, ushort usage) = LinuxHidDevice.ParseHidDescriptorBytes(descriptor);

        Assert.Equal(expectedUsagePage, usagePage);
        Assert.Equal(expectedUsage, usage);
    }

    [Theory]
    [MemberData(nameof(ReportSizeVectors))]
    public void ParseReportSizes_PinnedVectors_ReturnsExactTuple(
        string vectorName, byte[] descriptor, int expectedInputSize, int expectedOutputSize)
    {
        _ = vectorName;

        (int inputSize, int outputSize) = LinuxHidIOReportConnection.ParseReportSizes(descriptor);

        Assert.Equal(expectedInputSize, inputSize);
        Assert.Equal(expectedOutputSize, outputSize);
    }

    public static TheoryData<string, byte[], ushort, ushort> UsageVectors() => new()
    {
        { nameof(FidoU2f), FidoU2f, 0xF1D0, 0x0001 },
        { nameof(KeyboardOtp), KeyboardOtp, 0x0001, 0x0006 },
        { nameof(Empty), Empty, 0x0000, 0x0000 },
        { nameof(TruncatedValue), TruncatedValue, 0x0000, 0x0000 },
        { nameof(TruncatedTrailingPrefix), TruncatedTrailingPrefix, 0x0001, 0x0000 },
        { nameof(Size3Means4Bytes), Size3Means4Bytes, 0x0201, 0xBBAA },
        { nameof(ReportCountZero), ReportCountZero, 0x0000, 0x0000 },
        { nameof(Input63Bytes), Input63Bytes, 0x0000, 0x0000 },
        { nameof(BitRoundingSize1Count3), BitRoundingSize1Count3, 0x0000, 0x0000 },
        { nameof(MainBeforeGlobals), MainBeforeGlobals, 0x0000, 0x0000 },
        { nameof(GlobalsPersistAcrossMain), GlobalsPersistAcrossMain, 0x0000, 0x0000 },
        { nameof(SecondUsagePageIgnored), SecondUsagePageIgnored, 0x0001, 0x0006 },
        { nameof(UsageBeforeUsagePage), UsageBeforeUsagePage, 0x0001, 0x0006 },
        { nameof(LongItem0xFE), LongItem0xFE, 0x0001, 0x0006 },
        { nameof(ZeroSizeItemsOnly), ZeroSizeItemsOnly, 0x0000, 0x0000 },
        { nameof(TwoByteReportCount), TwoByteReportCount, 0x0000, 0x0000 },
        { nameof(TwoByteReportSize), TwoByteReportSize, 0x0000, 0x0000 },
        { nameof(LongItemZeroDataSize), LongItemZeroDataSize, 0x0001, 0x0006 },
        { nameof(LongItemFourByteDataSize), LongItemFourByteDataSize, 0x0001, 0x0006 },
        { nameof(TruncatedLongItemPayload), TruncatedLongItemPayload, 0x0001, 0x0000 },
        { nameof(LongItemPrefixOnly), LongItemPrefixOnly, 0x0000, 0x0000 },
        { nameof(LongItemHeaderTruncated), LongItemHeaderTruncated, 0x0000, 0x0000 },
        { nameof(AllLongItems), AllLongItems, 0x0000, 0x0000 }
    };

    public static TheoryData<string, byte[], int, int> ReportSizeVectors() => new()
    {
        { nameof(FidoU2f), FidoU2f, 64, 64 },
        { nameof(KeyboardOtp), KeyboardOtp, 8, 8 },
        { nameof(Empty), Empty, 64, 64 },
        { nameof(TruncatedValue), TruncatedValue, 64, 64 },
        { nameof(TruncatedTrailingPrefix), TruncatedTrailingPrefix, 64, 64 },
        { nameof(Size3Means4Bytes), Size3Means4Bytes, 64, 64 },
        { nameof(ReportCountZero), ReportCountZero, 64, 64 },
        { nameof(Input63Bytes), Input63Bytes, 63, 64 },
        { nameof(BitRoundingSize1Count3), BitRoundingSize1Count3, 1, 64 },
        { nameof(MainBeforeGlobals), MainBeforeGlobals, 64, 64 },
        { nameof(GlobalsPersistAcrossMain), GlobalsPersistAcrossMain, 32, 32 },
        { nameof(SecondUsagePageIgnored), SecondUsagePageIgnored, 64, 64 },
        { nameof(UsageBeforeUsagePage), UsageBeforeUsagePage, 64, 64 },
        { nameof(LongItem0xFE), LongItem0xFE, 64, 64 },
        { nameof(ZeroSizeItemsOnly), ZeroSizeItemsOnly, 64, 64 },
        { nameof(TwoByteReportCount), TwoByteReportCount, 256, 64 },
        { nameof(TwoByteReportSize), TwoByteReportSize, 8, 64 },
        { nameof(LongItemZeroDataSize), LongItemZeroDataSize, 64, 64 },
        { nameof(LongItemFourByteDataSize), LongItemFourByteDataSize, 64, 64 },
        { nameof(TruncatedLongItemPayload), TruncatedLongItemPayload, 64, 64 },
        { nameof(LongItemPrefixOnly), LongItemPrefixOnly, 64, 64 },
        { nameof(LongItemHeaderTruncated), LongItemHeaderTruncated, 64, 64 },
        { nameof(AllLongItems), AllLongItems, 64, 64 }
    };
}