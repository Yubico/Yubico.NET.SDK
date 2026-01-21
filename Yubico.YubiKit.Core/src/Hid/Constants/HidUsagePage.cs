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

namespace Yubico.YubiKit.Core.Hid.Constants;

/// <summary>
/// HID Usage Page enumeration.
/// </summary>
/// <remarks>
/// <strong>OBSOLETE:</strong> This enum is misleading. The value "Keyboard = 1" actually represents
/// the Generic Desktop usage page (0x01), not a keyboard specifically. A keyboard is identified by
/// the combination of UsagePage=0x01 AND Usage=0x06.
/// <para>
/// Use <see cref="HidInterfaceType"/> and <see cref="HidInterfaceClassifier"/> instead for
/// proper YubiKey interface type detection.
/// </para>
/// </remarks>
[Obsolete("This enum is misleading. Use YubiKeyHidInterfaceType and HidInterfaceClassifier instead for proper interface type detection.")]
public enum HidUsagePage 
{
    Unknown = 0,
    Fido = 0xF1D0,  // 61904 - FIDO CTAP HID usage page
    Keyboard = 1    // WARNING: This is actually Generic Desktop (0x01), not specifically a keyboard!
}