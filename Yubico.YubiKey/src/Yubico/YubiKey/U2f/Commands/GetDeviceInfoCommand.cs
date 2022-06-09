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
    /// Gets detailed information about the YubiKey and its current configuration.
    /// </summary>
    public sealed class GetDeviceInfoCommand : IYubiKeyCommand<GetDeviceInfoResponse>
    {
        private const byte GetDeviceInfoInstruction = 0xC2;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.FidoU2f
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Constructs an instance of the <see cref="GetDeviceInfoCommand" /> class.
        /// </summary>
        public GetDeviceInfoCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = GetDeviceInfoInstruction
        };

        /// <inheritdoc />
        public GetDeviceInfoResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetDeviceInfoResponse(responseApdu);
    }
}
