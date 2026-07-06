// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Core.Transports.Hid;

/// <summary>
/// Describes the type of HID topology change reported by a platform listener.
/// </summary>
public enum HidDeviceChangeKind
{
    /// <summary>
    /// The listener could not classify the HID topology change.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A HID interface was added.
    /// </summary>
    Added = 1,

    /// <summary>
    /// A HID interface was removed.
    /// </summary>
    Removed = 2
}