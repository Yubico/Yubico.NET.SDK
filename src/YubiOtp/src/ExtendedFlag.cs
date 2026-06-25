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
/// Extended flags (EXTFLAG) controlling additional OTP slot behavior.
/// </summary>
[Flags]
public enum ExtendedFlag : byte
{
    None = 0x00,
    SerialBtnVisible = 0x01,
    SerialUsbVisible = 0x02,
    SerialApiVisible = 0x04,
    UseNumericKeypad = 0x08,
    FastTrigger = 0x10,
    AllowUpdate = 0x20,
    Dormant = 0x40,
    InvertLed = 0x80
}

/// <summary>
/// Defines which <see cref="ExtendedFlag"/> values are valid for update operations.
/// </summary>
public static class ExtendedFlagMasks
{
    public const ExtendedFlag UpdateMask =
        ExtendedFlag.SerialBtnVisible |
        ExtendedFlag.SerialUsbVisible |
        ExtendedFlag.SerialApiVisible |
        ExtendedFlag.UseNumericKeypad |
        ExtendedFlag.FastTrigger |
        ExtendedFlag.Dormant |
        ExtendedFlag.InvertLed;
}
