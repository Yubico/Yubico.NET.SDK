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

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Protocol constants for the OATH applet wire format.
/// </summary>
internal static class OathConstants
{
    // TLV tags for credential data
    internal const byte TagName = 0x71;
    internal const byte TagNameList = 0x72;
    internal const byte TagKey = 0x73;
    internal const byte TagChallenge = 0x74;
    internal const byte TagResponse = 0x75;
    internal const byte TagTruncated = 0x76;
    internal const byte TagHotp = 0x77;
    internal const byte TagProperty = 0x78;
    internal const byte TagVersion = 0x79;
    internal const byte TagImf = 0x7A;
    internal const byte TagTouch = 0x7C;

    // Instruction bytes for commands
    internal const byte InsList = 0xA1;
    internal const byte InsPut = 0x01;
    internal const byte InsDelete = 0x02;
    internal const byte InsSetCode = 0x03;
    internal const byte InsReset = 0x04;
    internal const byte InsRename = 0x05;
    internal const byte InsCalculate = 0xA2;
    internal const byte InsValidate = 0xA3;
    internal const byte InsCalculateAll = 0xA4;
    internal const byte InsSendRemaining = 0xA5;

    // Masks for extracting algorithm and type from combined byte
    internal const byte MaskAlgorithm = 0x0F;
    internal const byte MaskType = 0xF0;

    // Property flags
    internal const byte PropRequireTouch = 0x02;

    // Defaults
    internal const int DefaultPeriod = 30;
    internal const int DefaultDigits = 6;
    internal const int DefaultImf = 0;
    internal const int ChallengeLength = 8;
    internal const int HmacMinimumKeySize = 14;
}