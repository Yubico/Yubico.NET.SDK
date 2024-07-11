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

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    ///     Resets the YubiKey's OATH application back to a factory default state.
    /// </summary>
    public class ResetCommand : IYubiKeyCommand<OathResponse>
    {
        private const byte ResetInstruction = 0x04;

        /// <summary>
        ///     Constructs an instance of the <see cref="ResetCommand" /> class.
        /// </summary>
        public ResetCommand()
        {
        }

        /// <summary>
        ///     Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        ///     YubiKeyApplication.Oath
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Oath;

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = ResetInstruction,
                P1 = 0xDE,
                P2 = 0xAD
            };

        /// <inheritdoc />
        public OathResponse CreateResponseForApdu(ResponseApdu responseApdu) => new OathResponse(responseApdu);
    }
}
