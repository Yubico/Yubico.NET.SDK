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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Flags indicating the hardware features available on the device.
/// </summary>
[Flags]
public enum GeneralFeatureManagement : byte
{
    None = 0,
    Touchscreen = 1 << 0,
    Microphone = 1 << 1,
    Loudspeaker = 1 << 2,
    Led = 1 << 3,
    Keypad = 1 << 4,
    Button = 1 << 5,
    Biometric = 1 << 6,
    Display = 1 << 7,
}