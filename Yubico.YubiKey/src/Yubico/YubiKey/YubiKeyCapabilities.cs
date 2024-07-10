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
    ///     A set of flags that describe the capabilities that are currently enabled on a YubiKey.
    /// </summary>
    /// <remarks>
    ///     This enumeration can be easily confused with the <see cref="YubiKeyApplication" /> enumeration.
    ///     While these two enumerations share many values, they serve different purposes. For one -
    ///     the YubiKeyApplication enumeration cannot be treated as flags. This enumeration represents
    ///     the applications _and_ interfaces exposed and enabled by a YubiKey. It includes things like
    ///     "CCID" which is an interface, not an application. OTP and FIDO U2F, in the context of older
    ///     generation keys like the YubiKey 4 Series and the YubiKey NEO, are also treated these as
    ///     interfaces. This is in contrast to the YubiKey 5 Series which takes an application-based
    ///     approach to capabilities.
    /// </remarks>
    #pragma warning disable CA2217 // Justification: Enums here are FlagsAttribute
    [Flags]
    public enum YubiKeyCapabilities
        #pragma warning restore CA2217
    {
        /// <summary>
        ///     No enabled.
        /// </summary>
        None = 0b0000_0000,

        /// <summary>
        ///     The OTP application and/or interface is enabled.
        /// </summary>
        Otp = 0b0000_0001,

        /// <summary>
        ///     The FIDO U2F (CTAP1) application and/or interface is enabled.
        /// </summary>
        FidoU2f = 0b0000_0010,

        /// <summary>
        ///     The CCID interface is enabled.
        /// </summary>
        Ccid = 0b0000_0100,

        /// <summary>
        ///     The OpenPGP application is enabled.
        /// </summary>
        OpenPgp = 0b0000_1000,

        /// <summary>
        ///     The PIV application is enabled.
        /// </summary>
        Piv = 0b0001_0000,

        /// <summary>
        ///     The OATH application is enabled.
        /// </summary>
        Oath = 0b0010_0000,

        /// <summary>
        ///     The YubiHSM Auth application is enabled.
        /// </summary>
        YubiHsmAuth = 0b0001_0000_0000,

        /// <summary>
        ///     The FIDO2 (CTAP2) application is enabled.
        /// </summary>
        Fido2 = 0b0010_0000_0000,

        /// <summary>
        ///     A convenience member for representing the state where all applications should be enabled.
        /// </summary>
        All = 0b0011_0011_1111
    }
}
