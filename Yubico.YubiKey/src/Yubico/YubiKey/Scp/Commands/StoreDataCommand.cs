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
using System.Collections.Generic;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands
{


    /// <summary>
    /// TODO
    /// </summary>
    internal class StoreDataCommand : IYubiKeyCommand<StoreDataCommandResponse>
    {
        private const byte InsStoreData = 0xE2;
        private readonly ReadOnlyMemory<byte> _data;

        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;

        public StoreDataCommand(ReadOnlyMemory<byte> data)
        {
            _data = data;
        }
        
        // The default constructor explicitly defined. We don't want it to be
        // used.
        private StoreDataCommand()
        {
            throw new NotImplementedException();
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Cla = 0,
            Ins = InsStoreData,
            P1 = 0x90,
            P2 = 0x00,
            Data = _data
        };

        public StoreDataCommandResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new StoreDataCommandResponse(responseApdu);
        
        
    }

    internal class StoreDataCommandResponse : ScpResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        public StoreDataCommandResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }

        public ReadOnlyMemory<byte> GetData() => ResponseApdu.Data;
    }
}
