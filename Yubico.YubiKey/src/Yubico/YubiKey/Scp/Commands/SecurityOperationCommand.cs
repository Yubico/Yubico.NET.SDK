// Copyright 2024 Yubico AB
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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands
{
    /// <summary>
    /// Implements the PERFORM SECURITY OPERATION command for SCP (Secure Channel Protocol) operations.
    /// This command is used to perform security-related operations such as certificate verification
    /// and key agreement during SCP11a/b/c authentication.
    /// </summary>
    /// <remarks>
    /// The PERFORM SECURITY OPERATION command is part of the SCP protocol suite and is used
    /// specifically for operations that involve certificate handling and key establishment.
    /// It is typically used in conjunction with other SCP commands like Initialize Update
    /// and External/Internal Authenticate to establish a secure channel.
    /// </remarks>
    internal class SecurityOperationCommand : IYubiKeyCommand<SecurityOperationResponse>
    {
        public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;
        internal const byte GpPerformSecurityOperationIns = 0x2A;
        private readonly ReadOnlyMemory<byte> _oceCertificates;
        private readonly byte _oceRefVersionNumber;
        private readonly byte _oceKeyId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityOperationCommand"/> class.
        /// </summary>
        /// <param name="oceKeyVersionNumer">The Off-Card Entity key version number.</param>
        /// <param name="oceKeyId">The Off-Card Entity key ID.</param>
        /// <param name="oceCertificates">The certificate chain for the off-card entity</param>
        /// <remarks>
        /// This command is used as part of the SCP11 protocol suite for presenting the off-card entity's certificate chain to the YubiKey.
        /// </remarks>
        public SecurityOperationCommand(byte oceKeyVersionNumer, byte oceKeyId, ReadOnlyMemory<byte> oceCertificates)
        {
            _oceRefVersionNumber = oceKeyVersionNumer;
            _oceKeyId = oceKeyId;
            _oceCertificates = oceCertificates;
        }

        /// <summary>
        /// Creates the APDU for the PERFORM SECURITY OPERATION command.
        /// </summary>
        /// <returns>
        /// A <see cref="CommandApdu"/> object containing the formatted command APDU.
        /// </returns>
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Cla = 0x80,
                Ins = GpPerformSecurityOperationIns,
                P1 = _oceRefVersionNumber,
                P2 = _oceKeyId,
                Data = _oceCertificates
            };

        public SecurityOperationResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new SecurityOperationResponse(responseApdu);
    }

    internal class SecurityOperationResponse : ScpResponse
    {
        public SecurityOperationResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }
    }
}
