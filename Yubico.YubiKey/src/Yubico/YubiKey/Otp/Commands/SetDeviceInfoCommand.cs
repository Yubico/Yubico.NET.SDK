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
using Yubico.YubiKey.Management.Commands;

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Configures device-wide settings on the YubiKey.
    /// </summary>
    public class SetDeviceInfoCommand : SetDeviceInfoBaseCommand, IYubiKeyCommand<ReadStatusResponse>
    {
        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Otp;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDeviceInfoCommand"/> class.
        /// </summary>
        public SetDeviceInfoCommand()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDeviceInfoCommand"/> class.
        /// </summary>
        /// <param name="baseCommand">
        /// An instance of the base class to use for initialization.
        /// </param>
        public SetDeviceInfoCommand(SetDeviceInfoBaseCommand baseCommand) : base(baseCommand)
        {

        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = OtpConstants.RequestSlotInstruction,
                P1 = OtpConstants.SetDeviceInfoSlot,
                Data = GetDataForApdu(),
            };

        /// <inheritdoc />
        public ReadStatusResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ReadStatusResponse(responseApdu);
    }
}
