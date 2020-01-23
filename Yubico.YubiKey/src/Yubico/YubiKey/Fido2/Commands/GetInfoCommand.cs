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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Gets detailed information about the authenticator.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="GetInfoResponse"/>. 
    /// Specified in CTAP as "authenticatorGetInfo".
    /// </remarks>
    internal sealed class GetInfoCommand : IYubiKeyCommand<GetInfoResponse>
    {
        private const byte CtapGetInfoCmd = 0x04;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Fido2
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// Constructs an instance of the <see cref="GetInfoCommand" /> class.
        /// </summary>
        public GetInfoCommand()
        {

        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            byte[] payload = new byte[] { CtapGetInfoCmd };
            return new CommandApdu()
            {
                Ins = (byte)CtapHidCommand.Cbor,
                Data = payload
            };
        }

        /// <inheritdoc />
        public GetInfoResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetInfoResponse(responseApdu);
    }
}
