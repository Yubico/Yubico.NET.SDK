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

using System;

namespace Yubico.YubiKey
{
    /// <summary>
    /// The communication methods a YubiKey may use to connect with a host device.
    /// </summary>
    [Flags]
    public enum Transport
    {
        /// <summary>
        /// This flag is equivalent to all flags being disabled.
        /// </summary>
        None = 0,

        /// <summary>
        /// USB HID class keyboard.
        /// </summary>
        HidKeyboard = 0x01,

        /// <summary>
        /// FIDO U2F authenticator device.
        /// </summary>
        HidFido = 0x02,

        /// <summary>
        /// Smart card connection over USB.
        /// </summary>
        UsbSmartCard = 0x04,

        /// <summary>
        /// Smart card connection over NFC.
        /// </summary>
        NfcSmartCard = 0x08,

        /// <summary>
        /// A convenience member that combines <see cref="UsbSmartCard"/> and
        /// <see cref="NfcSmartCard"/>.
        /// </summary>
        SmartCard = UsbSmartCard | NfcSmartCard,

        /// <summary>
        /// A convenience member that combines all flags.
        /// </summary>
        All = SmartCard | HidKeyboard | HidFido,
    }
}
