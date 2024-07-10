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

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    ///     The identifier of the file containing the YubiKey's NDEF data.
    /// </summary>
    public enum NdefFileId
    {
        /// <summary>
        ///     Default value for this enumeration.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Identifier for the NDEF file.
        /// </summary>
        Ndef = unchecked((short)0xE104),

        /// <summary>
        ///     Identifier for the Capability Container file.
        ///     This file is described in the NFC Forum Tag Type 4 specification.
        /// </summary>
        CapabilityContainer = unchecked((short)0xE103)
    }
}
