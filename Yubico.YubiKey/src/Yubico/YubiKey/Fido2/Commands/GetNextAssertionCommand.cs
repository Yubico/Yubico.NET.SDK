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
    /// Instruct the YubiKey to get the next assertion associated with the
    /// relying party specified in the previous call to
    /// <see cref="GetAssertionCommand"/>.
    /// </summary>
    public class GetNextAssertionCommand : IYubiKeyCommand<GetAssertionResponse>
    {
        private const int CtapGetNextAssertionCmd = 0x08;

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// Constructs an instance of the <see cref="GetNextAssertionCommand"/>.
        /// </summary>
        /// <remarks>
        /// The <c>GetNextAssertionCommand</c> will retrieve the next assertion
        /// in the list of assertions associated with a relying party. The
        /// relying party (and parameters) were sent to the YubiKey in a
        /// previous <see cref="GetAssertionCommand"/>.
        /// </remarks>
        public GetNextAssertionCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            byte[] payload = new byte[] { CtapGetNextAssertionCmd };
            return new CommandApdu()
            {
                Ins = CtapConstants.CtapHidCbor,
                Data = payload
            };
        }

        /// <inheritdoc />
        public GetAssertionResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetAssertionResponse(responseApdu);
    }
}
