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
using Yubico.YubiKey.Scp.Commands;

namespace Yubico.YubiKey.Scp
{
    internal class InternalAuthenticateCommand : IYubiKeyCommand<InternalAuthenticateResponse>
    {
        public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;

        private readonly byte _keyReferenceVersionNumber;
        private readonly byte _keyReferenceId;
        private readonly byte[] _data;

        public InternalAuthenticateCommand(byte keyReferenceVersionNumber, byte keyReferenceId, byte[] data)
        {
            _keyReferenceVersionNumber = keyReferenceVersionNumber;
            _keyReferenceId = keyReferenceId;
            _data = data;
        }
        
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Cla = 0x80,
                Ins = 0x88,
                P1 = _keyReferenceVersionNumber,
                P2 = _keyReferenceId,
                Data = _data
            };

        public InternalAuthenticateResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new InternalAuthenticateResponse(responseApdu);
    }
    
    internal class InternalAuthenticateResponse : ScpResponse
    {
        public InternalAuthenticateResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }
    }
}
