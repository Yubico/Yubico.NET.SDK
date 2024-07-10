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

using System;

namespace Yubico.YubiKey
{
    /// <summary>
    ///     Miscellaneous flags representing various settings available on the YubiKey.
    /// </summary>
    /// <seealso cref="Yubico.YubiKey.YubiKeyDeviceInfo" />
    [Flags]
    #pragma warning disable CA1711 // Justification: Keep using the variable name "DeviceFlags"
    public enum DeviceFlags
        #pragma warning restore CA1711
    {
        /// <summary>
        ///     No device flags are set.
        /// </summary>
        None = 0x00,

        /// <summary>
        ///     USB remote wakeup is enabled.
        /// </summary>
        RemoteWakeup = 0x40,

        /// <summary>
        ///     The CCID touch-eject feature is enabled.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For the CCID connection, the YubiKey behaves as a smart card reader and smart
        ///         card. When this flag is disabled, the smart card is always present in the smart
        ///         card reader. When enabled, the smart card will be ejected by default,
        ///         and the user is required to touch the YubiKey to insert the smart card. For
        ///         this to take effect, all <see cref="YubiKeyCapabilities" /> which do not depend
        ///         on the CCID connection (such as <c>Fido2</c>, <c>FidoU2f</c>, and <c>Otp</c>)
        ///         must be disabled.
        ///     </para>
        ///     <para>
        ///         To automatically eject the smart card following a touch, see
        ///         <see cref="IYubiKeyDeviceInfo.AutoEjectTimeout" />.
        ///     </para>
        /// </remarks>
        TouchEject = 0x80
    }
}
