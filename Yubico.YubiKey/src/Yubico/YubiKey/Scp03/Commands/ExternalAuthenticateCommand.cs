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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp03.Commands
{
    /// <summary>
    /// Represents the second command in the SCP03 authentication handshake, 'EXTERNAL_AUTHENTICATE'
    /// </summary>
    internal class ExternalAuthenticateCommand : IYubiKeyCommand<ExternalAuthenticateResponse>
    {
        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;
        const byte GpExternalAuthenticateCla = 0b1000_0100;
        const byte GpExternalAuthenticateIns = 0x82;
        const byte GpHighestSecurityLevel = 0b0011_0011;

        private readonly byte[] _data;

        /// <summary>
        /// Constructs an EXTERNAL_AUTHENTICATE command, containing the provided data.
        /// </summary>
        /// <remarks>
        /// Clients should not generally build this manaully. See <see cref="Pipelines.Scp03ApduTransform"/> for more.
        /// </remarks>
        /// <param name="data">Data for the command</param>
        public ExternalAuthenticateCommand(byte[] data)
        {
            _data = data;
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Cla = GpExternalAuthenticateCla,
            Ins = GpExternalAuthenticateIns,
            P1 = GpHighestSecurityLevel,
            Data = _data
        };
        public ExternalAuthenticateResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ExternalAuthenticateResponse(responseApdu);
    }
}
