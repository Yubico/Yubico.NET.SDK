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

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Get the U2F protocol version implemented by the application.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="GetProtocolVersionResponse"/>.
    /// </remarks>
    public sealed class GetProtocolVersionCommand : IYubiKeyCommand<GetProtocolVersionResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte AppVersionInstruction = 0x03;

        /// <summary>
        /// The YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.FidoU2f"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Constructs an instance of the <see cref="GetProtocolVersionCommand"/> class.
        /// </summary>
        public GetProtocolVersionCommand()
        {
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu()
        {
            var innerCommand = new CommandApdu()
            {
                Ins = AppVersionInstruction,
            };

            return new CommandApdu()
            {
                Ins = Ctap1MessageInstruction,
                Data = innerCommand.AsByteArray(ApduEncoding.ExtendedLength),
            };
        }

        /// <inheritdoc/>
        public GetProtocolVersionResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetProtocolVersionResponse(responseApdu);
    }
}
