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

using Yubico.YubiKey.Management.Commands;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Configures device-wide settings on the YubiKey.
    /// </summary>
    public sealed class SetDeviceInfoCommand : SetDeviceInfoBaseCommand, IYubiKeyCommand<SetDeviceInfoResponse>
    {
        private const byte SetDeviceInfoInstruction = 0xC3;

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDeviceInfoCommand"/> class.
        /// </summary>
        public SetDeviceInfoCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDeviceInfoCommand"/> class.
        /// </summary>
        public SetDeviceInfoCommand(SetDeviceInfoBaseCommand baseCommand) : base(baseCommand)
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = SetDeviceInfoInstruction,
            Data = GetDataForApdu()
        };

        /// <inheritdoc />
        public SetDeviceInfoResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new SetDeviceInfoResponse(responseApdu);
    }
}
