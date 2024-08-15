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
    /// Represents the second command in the SCP03 authentication handshake, 'EXTERNAL_AUTHENTICATE' TODO Fix better docu
    /// </summary>
    internal class ExternalAuthenticateCommand : IYubiKeyCommand<ExternalAuthenticateResponse>
    {
        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;

        private const byte GpExternalAuthenticateCla = 0x80;
        private const byte GpExternalAuthenticateIns = 0x82;
        private const byte GpHighestSecurityLevel = 0x33;
        private readonly ReadOnlyMemory<byte> _data;
        private readonly byte _keyVersionNumber;
        private readonly byte _keyId;
        
        /// <summary>
        /// Constructs an EXTERNAL_AUTHENTICATE command, containing the provided data.
        /// </summary>
        /// <remarks>
        /// Clients should not generally build this manually. See <see cref="Pipelines.ScpApduTransform"/> for more.
        /// </remarks>
        /// <param name="data">Data for the command. E.g. a host cryptogram when authenticating with SCP03</param>
        public ExternalAuthenticateCommand(ReadOnlyMemory<byte> data)
        {
            _data = data;
        }

        public ExternalAuthenticateCommand(byte keyVersionNumber, byte keyId, byte[] data)
        {
            _keyVersionNumber = keyVersionNumber;
            _keyId = keyId;
            _data = data;
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Cla = GpExternalAuthenticateCla,
            Ins = GpExternalAuthenticateIns,
            P1 = _keyVersionNumber > 0 ? _keyVersionNumber : GpHighestSecurityLevel,
            P2 = _keyId > 0 ? _keyId : default,
            Data = _data
        };
        
        public ExternalAuthenticateResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ExternalAuthenticateResponse(responseApdu);
    }
}
