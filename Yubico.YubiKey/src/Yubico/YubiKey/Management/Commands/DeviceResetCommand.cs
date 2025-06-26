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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Management.Commands
{
    /// <summary>
    /// Execute the device-wide reset. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resets ALL YubiKey applications (including FIDO and PIV) on the key to factory settings. This type of reset is only available on YubiKey Bio Multi-protocol Edition keys.
    /// </para>
    /// <para>
    /// A reset will delete all FIDO2 credentials, fingerprints, and associated information, remove the shared PIN, delete all PIV keys and certificates from PIV slots (except the F9 attestation slot), remove any information added to the PIV data elements, and set the PIV PUK and management key back to their factory default states.
    /// </para>
    /// <para>
    /// This class has a corresponding partner class <see cref="DeviceResetResponse"/>
    /// </para>
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
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = DeviceResetInstruction
        };

        /// <inheritdoc />
        public DeviceResetResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new DeviceResetResponse(responseApdu);
    }
}
