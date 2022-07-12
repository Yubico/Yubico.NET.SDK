// Copyright 2022 Yubico AB
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
    public class GetPinRetriesCommand : IYubiKeyCommand<GetPinRetriesResponse>
    {
        private readonly AuthenticatorClientPinCommand _command;

        private const int SubCmdGetPinRetries = 0x01;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        public GetPinRetriesCommand()
        {
            _command = new AuthenticatorClientPinCommand()
            {
                SubCommand = SubCmdGetPinRetries
            };
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public GetPinRetriesResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetPinRetriesResponse(responseApdu);
    }

    public class GetPinRetriesResponse : IYubiKeyResponse
    {
        public GetPinRetriesResponse(ResponseApdu responseApdu)
        {

        }

        /// <inheritdoc />
        public ResponseStatus Status { get; }

        /// <inheritdoc />
        public short StatusWord { get; }

        /// <inheritdoc />
        public string StatusMessage { get; }
    }
}
