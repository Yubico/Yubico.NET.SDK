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

using System;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The results of a call to the <see cref="ClientPinCommand" /> command class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class contains all of the data that can be returned in a <see cref="ClientPinResponse" /> response. Since
    /// this response may represent one of several different sub-responses, not all of the properties will be set to
    /// a value. Which property is set depends on what kind of client PIN sub-command was issued.
    /// </para>
    /// <para>
    /// It is often more convenient to issue a sub-command directly through its command class representation. That
    /// command class will have a partner response class that will only return the set of information that is relevant
    /// to that sub-command. It is recommended that you use this approach rather than using <see cref="ClientPinCommand"/>,
    /// <see cref="ClientPinResponse"/>, and <see cref="ClientPinData"/> directly.
    /// </para>
    /// </remarks>
    public class ClientPinData
    {
        /// <summary>
        /// Used to convey the authenticator's public key to the client / platform.
        /// </summary>
        public CoseKey? KeyAgreement { get; set; }

        /// <summary>
        /// The pinUvAuthToken, encrypted by calling `encrypt` on the PIN/UV auto protocol with the shared secret as
        /// the key.
        /// </summary>
        public Memory<byte>? PinUvAuthToken { get; set; }

        /// <summary>
        /// The number of PIN attemps remaining before the YubiKey's FIDO2 application is locked out.
        /// </summary>
        public int? PinRetries { get; set; }

        /// <summary>
        /// Indicates whether the YubiKey requires a power cycle before any future PIN operations can continue.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Present and `true` if the YubiKey needs to be power cycled (unplugged and re-plugged in), `false` if no
        /// power cycle is needed, and `null` if no information was given.
        /// </para>
        /// <para>
        /// This field is only valid in response to the `getRetries` sub-command. The YubiKey will return this when an
        /// authentication has been blocked due to excessive retries. The power cycle behavior is a security property
        /// of the FIDO2 PIN protocol, and helps prevent automated attacks against the PIN.
        /// </para>
        /// </remarks>
        public bool? PowerCycleState { get; set; }

        /// <summary>
        /// The number of User Verification retries remaining before a lockout of the YubiKey will occur.
        /// </summary>
        public int? UvRetries { get; set; }
    }
}
