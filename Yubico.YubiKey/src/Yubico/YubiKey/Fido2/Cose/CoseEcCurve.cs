// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.Fido2.Cose
{
    /// <summary>
    ///     An enumeration of the elliptic curves that are supported by COSE representations.
    /// </summary>
    public enum CoseEcCurve
    {
        /// <summary>
        ///     The EC curve could not be determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     NIST P-256 (also known as secp256r1)
        /// </summary>
        P256 = 1,

        /// <summary>
        ///     NIST P-384 (also known as secp384r1)
        /// </summary>
        P384 = 2,

        /// <summary>
        ///     NIST P-521 (also known as secp521r1)
        /// </summary>
        P521 = 3,

        /// <summary>
        ///     X25519 for use with ECDH only
        /// </summary>
        X25519 = 4,

        /// <summary>
        ///     X448 for use with ECDH only
        /// </summary>
        X448 = 5,

        /// <summary>
        ///     Ed25519 for use with EdDSA only
        /// </summary>
        Ed25519 = 6,

        /// <summary>
        ///     Ed448 for use with EdDSA only
        /// </summary>
        Ed448 = 7
    }
}
