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

namespace Yubico.YubiKey.Scp03.Commands
{
    /// <summary>
    /// Represents the first command in the SCP03 authentication handshake, 'INITIALIZE_UPDATE'
    /// </summary>
    internal class InitializeUpdateCommand : IYubiKeyCommand<InitializeUpdateResponse>
    {
        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;
        const byte GpInitializeUpdateCla = 0b1000_0000;
        const byte GpInitializeUpdateIns = 0x50;
        private readonly byte[] _challenge;
        private readonly int _keyVersionNumber;

        /// <summary>
        /// Constructs an EXTERNAL_AUTHENTICATE command, containing the provided data.
        /// </summary>
        /// <remarks>
        /// Clients should not generally build this manually. See <see cref="YubiKey.Pipelines.Scp03ApduTransform"/> for more.
        /// </remarks>
        /// <param name="keyVersionNumber">Which key set to use.</param>
        /// <param name="challenge">An 8-byte randomly-generated challenge from the host to the device.</param>
        public InitializeUpdateCommand(int keyVersionNumber, byte[] challenge)
        {
            if (challenge is null)
            {
                throw new ArgumentNullException(nameof(challenge));
            }

            _challenge = challenge;
            _keyVersionNumber = keyVersionNumber;
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Cla = GpInitializeUpdateCla,
            Ins = GpInitializeUpdateIns,
            P1 = (byte)_keyVersionNumber,
            Data = _challenge
        };
        public InitializeUpdateResponse CreateResponseForApdu(ResponseApdu responseApdu) => new InitializeUpdateResponse(responseApdu);
    }
}
