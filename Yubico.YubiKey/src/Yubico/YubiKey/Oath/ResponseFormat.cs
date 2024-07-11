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

namespace Yubico.YubiKey.Oath
{
    /// <summary>
    /// The response format from OATH application.
    /// </summary>
    /// <remarks>
    /// When the OATH application calculates an one-time password (OTP),
    /// the full value it calculates depends on the <see cref="HashAlgorithm"/> type set when the
    /// credential was created. The first 4 bytes of that value are needed to derive the one-time
    /// password (OTP), as OATH is a truncated hmac verification scheme.
    /// </remarks>
    public enum ResponseFormat
    {
        /// <summary>
        /// A full response is the full hash, where the first 4 bytes contain the generated one-time password (OTP).
        /// </summary>
        Full = 0x00,

        /// <summary>
        /// A truncated response only contains the generated four-byte one-time password (OTP).
        /// </summary>
        Truncated = 0x01,
    }
}
