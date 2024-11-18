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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands
{
    /// <summary>
    /// Represents the first command in the SCP03 authentication handshake, 'INITIALIZE_UPDATE'
    /// </summary>
    internal class InitializeUpdateCommand : IYubiKeyCommand<InitializeUpdateResponse>
    {
        public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;
        internal const byte GpInitializeUpdateIns = 0x50;
        private const byte GpInitializeUpdateCla = 0x84;
        private readonly ReadOnlyMemory<byte> _hostChallenge;
        private readonly int _keyVersionNumber;

        /// <summary>
        /// Constructs an INITIALIZE_UPDATE command, containing the provided data.
        /// </summary>
        /// <remarks>
        /// Clients should not generally build this manually. See <see cref="YubiKey.Pipelines.Scp03ApduTransform"/> for more.
        /// </remarks>
        /// <param name="keyVersionNumber">Which key set to use.</param>
        /// <param name="hostChallenge">An 8-byte randomly-generated challenge from the host to the device.</param>
        public InitializeUpdateCommand(int keyVersionNumber, ReadOnlyMemory<byte> hostChallenge)
        {
            if (hostChallenge.Length != 8)
            {
                throw new ArgumentException("Invalid size, must be 8 bytes", nameof(_hostChallenge));
            }

            _hostChallenge = hostChallenge;
            _keyVersionNumber = keyVersionNumber;
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Cla = GpInitializeUpdateCla,
            Ins = GpInitializeUpdateIns,
            P1 = (byte)_keyVersionNumber,
            Data = _hostChallenge
        };
        public InitializeUpdateResponse CreateResponseForApdu(ResponseApdu responseApdu) => new InitializeUpdateResponse(responseApdu);
    }
}
