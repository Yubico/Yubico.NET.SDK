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

using System;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Instruct the YubiKey to reset the FIDO2 application.
    /// </summary>
    /// <remarks>
    /// This will delete all credentials and associated information from the FIDO2
    /// application and remove the PIN.
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
        /// This will delete all credentials and associated information from the FIDO2
        /// application and remove the PIN.
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
