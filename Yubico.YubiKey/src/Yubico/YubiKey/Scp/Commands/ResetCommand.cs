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
    /// TODO
    /// </summary>
    internal class ResetCommand : IYubiKeyCommand<YubiKeyResponse>
    {
        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;
        private readonly byte[] _data;
        private readonly byte _keyVersionNumber;
        private readonly byte _kid;
        private readonly byte _ins;

        /// <summary>
        /// TODO
        /// </summary>
        /// <remarks>
        /// Clients should not generally build this manually.
        /// </remarks>
        /// <param name="ins"></param>
        /// <param name="keyVersionNumber">Which key set to use.</param>
        /// <param name="kid"></param>
        /// <param name="data"></param>
        public ResetCommand(byte ins, byte keyVersionNumber, byte kid, byte[] data)
        {
            _ins = ins;
            _data = data;
            _keyVersionNumber = keyVersionNumber;
            _kid = kid;
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Cla = 0x80,
            Ins = _ins,
            P1 = _keyVersionNumber,
            P2 = _kid,
            Data = _data,
            Ne = 0
        };
        public YubiKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) => new YubiKeyResponse(responseApdu);
    }
}
