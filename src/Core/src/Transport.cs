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

namespace Yubico.YubiKit.Core;

/// <summary>Physical transport categories, distinct from connection interfaces or application capabilities.</summary>
[Flags]
public enum Transport
{
    /// <summary>No transport selected.</summary>
    None = 0,
    /// <summary>USB attachment, which can expose smart-card and HID interfaces.</summary>
    Usb = 1,
    /// <summary>Contactless access through an NFC reader.</summary>
    Nfc = 2,
    /// <summary>Both modeled transport categories.</summary>
    All = Usb | Nfc
}