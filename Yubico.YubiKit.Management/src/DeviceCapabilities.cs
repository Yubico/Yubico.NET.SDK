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

namespace Yubico.YubiKit.Management;

[Flags]
public enum DeviceCapabilities
{
    None = 0,
    /// <summary>
    ///     Identifies the YubiOTP application.
    /// </summary>
    Otp = 0x01,

    /// <summary>
    ///     Identifies the U2F (CTAP1) portion of the FIDO application.
    /// </summary>
    U2f = 0x02,

    /// <summary>
    ///     Identifies the OpenPGP application, implementing the OpenPGP Card protocol.
    /// </summary>
    OpenPgp = 0x08,

    /// <summary>
    ///     Identifies the PIV application, implementing the PIV protocol.
    /// </summary>
    Piv = 0x10,

    /// <summary>
    ///     Identifies the OATH application, implementing the YKOATH protocol.
    /// </summary>
    Oath = 0x20,

    /// <summary>
    ///     Identifies the HSMAUTH application.
    /// </summary>
    HsmAuth = 0x01_00,

    /// <summary>
    ///     Identifies the FIDO2  = CTAP2 portion of the FIDO application.
    /// </summary>
    Fido2 = 0x02_00,

    All = Otp | U2f | OpenPgp | Piv | Oath | HsmAuth | Fido2
}
