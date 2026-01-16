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

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Represents the type of YubiKey HID interface.
/// This is determined from the HID descriptor UsagePage and Usage combination.
/// </summary>
public enum YubiKeyHidInterfaceType
{
    /// <summary>
    /// Unknown or unsupported HID interface type.
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// FIDO/U2F interface using CTAPHID protocol.
    /// Uses I/O reports (64-byte packets).
    /// Requires UsagePage=0xF1D0 (FIDO Alliance) and Usage=0x01 (U2F Device).
    /// </summary>
    Fido,
    
    /// <summary>
    /// OTP/YubiOTP interface using keyboard emulation.
    /// Uses feature reports (8-byte packets).
    /// Requires UsagePage=0x01 (Generic Desktop) and Usage=0x06 (Keyboard).
    /// </summary>
    Otp
}
