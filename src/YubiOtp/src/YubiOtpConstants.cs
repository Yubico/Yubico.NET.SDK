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

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Constants for the YubiOTP application protocol.
/// </summary>
internal static class YubiOtpConstants
{
    public const int FixedSize = 16;
    public const int UidSize = 6;
    public const int KeySize = 16;
    public const int AccessCodeSize = 6;
    public const int ConfigSize = 52;
    public const int NdefDataSize = 54;
    public const int HmacKeySize = 20;
    public const int HmacChallengeSize = 64;
    public const int HmacResponseSize = 20;
    public const int ScanCodesSize = FixedSize + UidSize + KeySize; // 38

    /// <summary>
    /// Status bytes returned by the YubiKey OTP applet are 6 bytes:
    /// [firmware_major, firmware_minor, firmware_patch, prog_seq, touch_level_lo, touch_level_hi]
    /// </summary>
    public const int StatusBytesLength = 6;

    public const byte InsConfig = 0x01;
    public const byte InsYk2Status = 0x03;
}
