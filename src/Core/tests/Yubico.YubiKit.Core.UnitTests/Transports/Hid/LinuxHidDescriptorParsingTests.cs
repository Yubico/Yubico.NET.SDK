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

    // KNOWN DEFECT, PINNED DELIBERATELY. HID long items (prefix 0xFE) are not handled by
    // either parser. 0xFE decodes via the short-item bit layout as size=2/type=3/tag=15, so
    // its two payload bytes (0xAA, 0xBB) are consumed as an ordinary item value, the cursor
    // then lands mid-stream at the long item's data-length/tag bytes, and the perfectly
    // valid `05 01 09 06` keyboard descriptor that follows it is misread and lost, producing
    // (0, 0) instead of (0x0001, 0x0006). YubiKeys do not emit long items in practice, so
    // this is latent, not live. This test pins CURRENT behaviour for refactor safety only —
    // it is NOT an endorsement of the defect and must not be "fixed" as part of any
    // characterization or refactor work.
    public static readonly byte[] LongItem0xFE = [0xFE, 0x02, 0xAA, 0xBB, 0xCC, 0x05, 0x01, 0x09, 0x06];

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
        { nameof(LongItem0xFE), LongItem0xFE, 0x0000, 0x0000 },
        { nameof(ZeroSizeItemsOnly), ZeroSizeItemsOnly, 0x0000, 0x0000 },
        { nameof(TwoByteReportCount), TwoByteReportCount, 0x0000, 0x0000 },
        { nameof(TwoByteReportSize), TwoByteReportSize, 0x0000, 0x0000 }
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
        { nameof(TwoByteReportSize), TwoByteReportSize, 8, 64 }
    };
}