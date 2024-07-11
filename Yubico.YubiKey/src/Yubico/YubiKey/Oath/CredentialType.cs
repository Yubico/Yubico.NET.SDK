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

namespace Yubico.YubiKey.Oath
{
    /// <summary>
    /// Describes the class of one-time password algorithm to be used.
    /// </summary>
    public enum CredentialType
    {
        /// <summary>
        /// No type is specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// HMAC-based One-time Password algorithm (HOTP) is generated based on HMAC algorithm using an authentication counter.
        /// </summary>
        /// <remarks>
        /// The algorithm is specified in RFC 4226.
        /// </remarks>
        Hotp = 0x10,

        /// <summary>
        /// A time-based one-time password (TOTP) is generated based on HMAC algorithm using the current time.
        /// </summary>
        /// <remarks>
        /// It expires after 15, 30 or 60 seconds.
        /// The algorithm is specified in RFC 6238.
        /// </remarks>
        Totp = 0x20
    }
}
