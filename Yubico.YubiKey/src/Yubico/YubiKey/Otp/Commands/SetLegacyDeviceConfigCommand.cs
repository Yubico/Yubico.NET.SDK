﻿// Copyright 2021 Yubico AB
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
    /// <inheritdoc/>
    public class SetLegacyDeviceConfigCommand : SetLegacyDeviceConfigBase, IYubiKeyCommand<OtpResponse>
    {
        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Otp;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetLegacyDeviceConfigCommand"/> class.
        /// </summary>
        /// <inheritdoc/>
        public SetLegacyDeviceConfigCommand(
            YubiKeyCapabilities yubiKeyInterfaces,
            byte challengeResponseTimeout,
            bool touchEjectEnabled,
            int autoEjectTimeout)
            : base(yubiKeyInterfaces, challengeResponseTimeout, touchEjectEnabled, autoEjectTimeout)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SetLegacyDeviceConfigCommand"/> from another object which derives from
        /// <see cref="SetLegacyDeviceConfigBase"/>.
        /// </summary>
        /// <remarks>
        /// This constructor can be useful to switch between different application-specific
        /// implementations of the same base command.
        /// </remarks>
        /// <param name="baseCommand">
        /// The SetLegacyDeviceConfig base command object to copy from.
        /// </param>
        public SetLegacyDeviceConfigCommand(SetLegacyDeviceConfigBase baseCommand) : base(baseCommand)
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = OtpConstants.RequestSlotInstruction,
                P1 = OtpConstants.WriteDeviceConfig,
                Data = GetDataForApdu(),
            };

        /// <inheritdoc />
        public OtpResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ReadStatusResponse(responseApdu);
    }
}
