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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// Gets additional response data from the previously issued command.
    /// </summary>
    public class GetResponseCommand : IYubiKeyCommand<YubiKeyResponse>
    {
        private const byte GetResponseInstruction = 0xA5;

        private readonly byte _Cla;
        private readonly int _SW2;

        /// <summary>
        /// Gets the YubiKeyApplication (e.g. PIV, OATH, etc.) that this command applies to.
        /// </summary>
        /// <value>
        /// The value will always be `YubiKeyApplication.Oath` for this command.
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Oath;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetResponseCommand"/> class.
        /// </summary>
        /// <param name="originatingCommand">
        /// The original command APDU that was sent that is now indicating that more data is in the
        /// response.
        /// </param>
        /// <param name="SW2">
        /// The SW2 byte of the last response. It indicates the number of bytes left for the next
        /// GetResponseCommand.
        /// </param>
        public GetResponseCommand(CommandApdu originatingCommand, short SW2)
        {
            if (originatingCommand is null)
            {
                throw new ArgumentNullException(nameof(originatingCommand));
            }

            _Cla = originatingCommand.Cla;
            _SW2 = SW2;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Cla = _Cla,
            Ins = GetResponseInstruction,
            Ne = _SW2 == 0 ? 256 : _SW2,
        };

        /// <inheritdoc />
        public YubiKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new YubiKeyResponse(responseApdu);
    }
}
