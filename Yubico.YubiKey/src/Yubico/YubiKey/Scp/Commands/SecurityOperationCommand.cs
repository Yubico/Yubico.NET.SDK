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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands
{
    internal class SecurityOperationCommand : IYubiKeyCommand<SecurityOperationResponse> //todo visibility of classes?
    {
        public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;

        private readonly byte _oceRefVersionNumber;
        private readonly byte _p2;
        private readonly byte[] _certificates;

        public SecurityOperationCommand(byte oceRefVersionNumber, byte p2, byte[] certificates)
        {
            _oceRefVersionNumber = oceRefVersionNumber;
            _p2 = p2;
            _certificates = certificates;
        }

        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Cla = 0x80,
                Ins = 0x2A,
                P1 = _oceRefVersionNumber,
                P2 = _p2,
                Data = _certificates
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
