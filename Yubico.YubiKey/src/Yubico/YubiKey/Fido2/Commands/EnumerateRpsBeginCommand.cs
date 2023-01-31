// Copyright 2023 Yubico AB
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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Begin the process of getting information on all the relying parties
    /// represented in credentials on the YubiKey.
    /// </summary>
    /// <remarks>
    /// This returns information on one of the relying parties, and the total
    /// number of relying parties represented in the set of credentials. If there
    /// is only one RP, then you have all the information you need. If there are
    /// more RPs, then you can get information on all of them by calling the
    /// <c>enumerateRPsGetNextRP</c> sub-command.
    /// <para>
    /// The return from this command is the
    /// <c>authenticatorCredentialManagement</c> response, but only three of the
    /// elements are included: <c>rp</c>, <c>rpIDHash</c> and <c>totalRPs</c>.
    /// </para>
    /// </remarks>
    public class EnumerateRpsBeginCommand : CredentialManagementCommand
    {
        private const int SubCmdGetEnumerateRpsBegin = 0x02;

        /// <summary>
        /// Constructs a new instance of <see cref="EnumerateRpsBeginCommand"/>.
        /// </summary>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public EnumerateRpsBeginCommand(
            ReadOnlyMemory<byte> pinUvAuthToken, PinUvAuthProtocolBase authProtocol)
            : base(SubCmdGetEnumerateRpsBegin, null, pinUvAuthToken, authProtocol)
        {
        }
    }
}
