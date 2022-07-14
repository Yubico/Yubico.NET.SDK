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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// An enumeration denoting the FIDO2 PIN/UV authentication protocol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A specific PIN/UV auth protocol defines an implementation of an interface to a set of cryptographic services.
    /// This service is used to facilitate authenticating with the YubiKey in such a way that the PIN and any other
    /// sensitive authentication data cannot be observed by an attacker.
    /// </para>
    /// <para>
    /// See the user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth guide
    /// to working with PINs within FIDO2.
    /// </para>
    /// </remarks>
    public enum PinUvAuthProtocol
    {
        None = 0,

        /// <summary>
        /// Identifier for PIN/UV auth protocol 1.
        /// </summary>
        ProtocolOne = 1,

        /// <summary>
        /// Identifier for PIN/UV auth protocol 2. This protocol contains certain provisions that make it more applicable
        /// to FIPS certified YubiKeys.
        /// </summary>
        ProtocolTwo = 2,
    }
}
