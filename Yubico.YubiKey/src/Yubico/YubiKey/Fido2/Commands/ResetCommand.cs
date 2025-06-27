// Copyright 2025 Yubico AB
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
    /// Instruct the YubiKey to reset the FIDO2 application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This will delete all credentials and associated information from the FIDO2
    /// application and remove the PIN.
    /// </para>
    /// <para>
    /// Before attempting to reset a YubiKey Bio Multi-protocol Edition key with ResetCommand(), verify that the FIDO2 application is not blocked from using this method by checking the <see cref="IYubiKeyDeviceInfo.ResetBlocked"/> property. If the application is blocked, use <see cref="IYubiKeyDevice.DeviceReset"/>.
    /// </para>
    /// </remarks>
    public class ResetCommand : IYubiKeyCommand<ResetResponse>
    {
        private const int CtapResetCmd = 0x07;

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// Constructs an instance of the <see cref="ResetCommand"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This will delete all credentials and associated information from the FIDO2
        /// application and remove the PIN.
        /// </para>
        /// <para>
        /// Before attempting to reset a YubiKey Bio Multi-protocol Edition key with ResetCommand(), verify that the FIDO2 application is not blocked from using this method by checking the <see cref="IYubiKeyDeviceInfo.ResetBlocked"/> property. If the application is blocked, use <see cref="IYubiKeyDevice.DeviceReset"/>.
        /// </para>
        /// </remarks>
        public ResetCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            byte[] payload = new byte[] { CtapResetCmd };
            return new CommandApdu()
            {
                Ins = CtapConstants.CtapHidCbor,
                Data = payload
            };
        }

        /// <inheritdoc />
        public ResetResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ResetResponse(responseApdu);
    }
}
