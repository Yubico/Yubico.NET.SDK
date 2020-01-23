// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey
{
    /// <summary>
    /// Represents the form-factor of the YubiKey.
    /// </summary>
    public enum FormFactor
    {
        /// <summary>
        /// The form-factor could not be determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The YubiKey is a USB-A key-chain device.
        /// </summary>
        UsbAKeychain = 1,

        /// <summary>
        /// The YubiKey is a USB-A nano device.
        /// </summary>
        UsbANano = 2,

        /// <summary>
        /// The YubiKey is a USB-C key-chain device.
        /// </summary>
        UsbCKeychain = 3,

        /// <summary>
        /// The YubiKey is a USB-C nano device.
        /// </summary>
        UsbCNano = 4,

        /// <summary>
        /// The YubiKey is a dual-port USB-C and Apple Lightning connector device.
        /// </summary>
        UsbCLightning = 5,
    }
}
