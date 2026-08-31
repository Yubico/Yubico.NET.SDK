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
/// Characterization tests for the two independent, hand-rolled HID report-descriptor item
/// walkers in <see cref="LinuxHidDevice"/> and <see cref="LinuxHidIOReportConnection"/>.
///
/// These tests make ZERO claim that the parsers are correct. They pin the CURRENT, observed
/// behaviour of both parsers, byte for byte, so that a future refactor that merges the two
/// walkers into one shared implementation can be verified as behaviour-preserving. Every
/// vector below is run through both <see cref="LinuxHidDevice.ParseHidDescriptorBytes"/> and
/// <see cref="LinuxHidIOReportConnection.ParseReportSizes"/>, even where the result is the
/// uninteresting default, because "this input does not affect me" is itself part of the
/// contract a refactor could silently break.
/// </summary>
public class LinuxHidDescriptorParsingTests
{
    // The real canonical U2F HID descriptor. (0xF1D0, 0x0001) is exactly what
    // HidInterfaceClassifier.Classify requires to return HidInterfaceType.Fido. If this
    // regresses, every YubiKey is dropped from Linux HID discovery. This is the single most
    // important vector in this file.
    public static readonly byte[] FidoU2f =
    [
        0x06, 0xD0, 0xF1, 0x09, 0x01, 0xA1, 0x01, 0x09, 0x20, 0x15, 0x00, 0x26, 0xFF, 0x00, 0x75, 0x08,
        0x95, 0x40, 0x81, 0x02, 0x09, 0x21, 0x15, 0x00, 0x26, 0xFF, 0x00, 0x75, 0x08, 0x95, 0x40, 0x91, 0x02,
        0xC0
    ];

    // (0x0001, 0x0006) is exactly what HidInterfaceClassifier.Classify requires to return
    // HidInterfaceType.Otp.
    public static readonly byte[] KeyboardOtp =
    [
        0x05, 0x01, 0x09, 0x06, 0xA1, 0x01, 0x75, 0x08, 0x95, 0x08, 0x81, 0x02, 0x75, 0x08, 0x95, 0x08,
        0x91, 0x02, 0xC0
    ];

    // Zero bytes: the loop condition `i < descriptor.Length` never enters, so both parsers
    // return their untouched defaults.
    public static readonly byte[] Empty = [];

    // Only a prefix + a truncated value: the `i + size > descriptor.Length` break path fires
    // before any item is decoded.
    public static readonly byte[] TruncatedValue = [0x06, 0xD0];

    // A valid Usage Page item followed by a lone trailing prefix byte with no value bytes
    // behind it: the first item parses successfully (usage page = 0x0001), then the break
    // path fires on the second, incomplete item, leaving usage at its default.
    public static readonly byte[] TruncatedTrailingPrefix = [0x05, 0x01, 0x09];

    // Proves three things at once: size == 3 in the prefix means FOUR value bytes (not
    // three), the value is assembled little-endian, and a 4-byte value is truncated to
    // ushort (0x04030201 -> 0x0201, 0xDDCCBBAA -> 0xBBAA).
    public static readonly byte[] Size3Means4Bytes = [0x07, 0x01, 0x02, 0x03, 0x04, 0x0B, 0xAA, 0xBB, 0xCC, 0xDD];

    // Report Count of 0 leaves the `reportBytes > 0` guard false, so the 64-byte default is
    // never overwritten despite an Input main item being present.
    public static readonly byte[] ReportCountZero = [0x75, 0x08, 0x95, 0x00, 0x81, 0x02];

    // An Input/Output main item appears before any Report Size/Report Count global item, so
    // currentReportSize/currentReportCount are both still 0, `reportBytes > 0` is false, and
    // the 64-byte defaults survive.
    public static readonly byte[] MainBeforeGlobals = [0x81, 0x02, 0x91, 0x02];

    // A non-default report size really is computed and returned (guards against a refactor
    // that always returns the 64-byte fallback regardless of the descriptor).
    public static readonly byte[] Input63Bytes = [0x75, 0x08, 0x95, 0x3F, 0x81, 0x02];

    // (1 * 3 + 7) / 8 == 1: pins the ceiling-division bit-to-byte rounding exactly.
    public static readonly byte[] BitRoundingSize1Count3 = [0x75, 0x01, 0x95, 0x03, 0x81, 0x02];

    // One Report Size/Report Count declaration feeds BOTH the Input and the Output main item
    // that follow it: HID global-item persistence across multiple main items.
    public static readonly byte[] GlobalsPersistAcrossMain = [0x75, 0x08, 0x95, 0x20, 0x81, 0x02, 0x91, 0x02];

    // A second Usage Page item must be ignored: first-value-wins via the `!usagePageFound`
    // guard flag.
    public static readonly byte[] SecondUsagePageIgnored = [0x05, 0x01, 0x05, 0x0C, 0x09, 0x06];

    // Usage item appears before Usage Page in the byte stream: proves the parser is order
    // independent (each field is captured independently, not "first item wins overall").
    public static readonly byte[] UsageBeforeUsagePage = [0x09, 0x06, 0x05, 0x01];

    // HID long item (prefix 0xFE): bDataSize = 0x02, bLongItemTag = 0xAA, 2 data bytes
    // (0xBB, 0xCC), for a total of 3 + 2 = 5 bytes (position 0 -> 5). TryReadItem skips the
    // whole long item and resumes at position 5, where the perfectly valid `05 01 09 06`
    // keyboard Usage Page + Usage pair is decoded normally, giving (0x0001, 0x0006). Neither
    // Report Size, Report Count, Input, nor Output items are present, so the report-size
    // parser's (64, 64) defaults are unaffected.
    public static readonly byte[] LongItem0xFE = [0xFE, 0x02, 0xAA, 0xBB, 0xCC, 0x05, 0x01, 0x09, 0x06];

    // Long item with bDataSize == 0: prefix 0xFE, dataSize 0, tag 0xAA — exactly 3 bytes total
    // (position 0 -> 3), the minimum legal long item. Followed by the same valid Usage Page +
    // Usage pair, decoded normally: usage page = 0x0001, usage = 0x0006. No Report Size/Report
    // Count/Input/Output items are present, so report sizes stay at the (64, 64) defaults.
    public static readonly byte[] LongItemZeroDataSize = [0xFE, 0x00, 0xAA, 0x05, 0x01, 0x09, 0x06];

    // Long item with a 4-byte payload: prefix 0xFE, dataSize 4, tag 0xAA, four data bytes
    // (0xBB..0xEE), total 3 + 4 = 7 bytes (position 0 -> 7). Confirms the skip length is
    // 3 + bDataSize, not a fixed size, before the same valid Usage Page + Usage pair is
    // decoded: usage page = 0x0001, usage = 0x0006. Report sizes stay at (64, 64).
    public static readonly byte[] LongItemFourByteDataSize =
        [0xFE, 0x04, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0x05, 0x01, 0x09, 0x06];

    // A valid Usage Page item (usage page = 0x0001) is read first (position 0 -> 2). Then a
    // long-item header declares bDataSize = 5 (position 2 -> 5 reads FE 05 AA), but only 2
    // data bytes remain (BB CC) instead of 5, so the bounds check at position 5
    // (5 + 5 = 10 > length 7) fails and TryReadItem returns false. The walk stops there: the
    // Usage Page already read survives (usage page = 0x0001), but Usage is never reached, so
    // usage stays at its default 0x0000. No Report Size/Report Count/Input/Output items are
    // present, so report sizes stay at (64, 64).
    public static readonly byte[] TruncatedLongItemPayload = [0x05, 0x01, 0xFE, 0x05, 0xAA, 0xBB, 0xCC];

    // Only the long-item prefix byte itself, nothing else: the header bounds check
    // (0 + 3 > length 1) fails immediately, so TryReadItem returns false on the very first
    // call. No items are ever produced; both parsers return their untouched defaults.
    public static readonly byte[] LongItemPrefixOnly = [0xFE];

    // The long-item prefix plus its bDataSize byte, but the bLongItemTag byte is missing: the
    // header bounds check (0 + 3 > length 2) fails for the same reason. No items are ever
    // produced; both parsers return their untouched defaults.
    public static readonly byte[] LongItemHeaderTruncated = [0xFE, 0x02];

    // A descriptor consisting solely of two back-to-back long items (bDataSize 1, then
    // bDataSize 0), with no short items anywhere. Both are skipped in full
    // (position 0 -> 4, then 4 -> 7); position then equals the descriptor length (7), so
    // TryReadItem returns false and the walk terminates cleanly. Both parsers return their
    // untouched defaults.
    public static readonly byte[] AllLongItems = [0xFE, 0x01, 0xAA, 0xBB, 0xFE, 0x00, 0xCC];

    // Items with size 0 (bits 0-1 of prefix are both 0) still advance the cursor by exactly
    // one byte (the prefix itself) and the loop terminates cleanly once the bytes run out.
    public static readonly byte[] ZeroSizeItemsOnly = [0xC0, 0xC0, 0xC0];

    // A two-byte Report Count (prefix 0x96 = size 2, global, tag 9) holding 0x0100 = 256.
    // (8 * 256 + 7) / 8 == 256. Without this vector the little-endian value assembly inside
    // ParseReportSizes is completely unobservable, because every other vector feeds it only
    // single-byte Report Size/Report Count values, and the one multi-byte vector
    // (Size3Means4Bytes) uses tags that ParseReportSizes ignores. Verified: a big-endian
    // assembly would read this count as 1 and return an input size of 1 instead of 256.
    public static readonly byte[] TwoByteReportCount = [0x75, 0x08, 0x96, 0x00, 0x01, 0x81, 0x02];

    // The same guard for the other multi-byte global: a two-byte Report Size (prefix 0x76 =
    // size 2, global, tag 7) holding 0x0010 = 16 bits, with a Report Count of 4.
    // (16 * 4 + 7) / 8 == 8. A big-endian assembly would read the size as 0x1000 and return
    // 2048 instead of 8.
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