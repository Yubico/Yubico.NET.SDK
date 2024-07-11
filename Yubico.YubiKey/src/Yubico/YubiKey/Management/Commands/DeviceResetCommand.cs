// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Management.Commands
{
    /// <summary>
    /// Execute device reset.
    /// </summary>
    /// <remarks>
    /// This class has a corresponding partner class <see cref="DeviceResetResponse"/>
    /// </remarks>
    public class DeviceResetCommand : IYubiKeyCommand<DeviceResetResponse>
    {
        private const byte DeviceResetInstruction = 0x1F;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.Management"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Management;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceResetCommand"/> class.
        /// </summary>
        public DeviceResetCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = DeviceResetInstruction
            };

        /// <inheritdoc />
        public DeviceResetResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new DeviceResetResponse(responseApdu);
    }
}
