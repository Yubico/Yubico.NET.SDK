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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Obtain the next per-credential signature for a given <see cref="GetAssertionCommand"/> request.
    /// This command must follow a <see cref="GetAssertionCommand"/> or <see cref="GetNextAssertionCommand"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this command when the <see cref="GetAssertionCommand"/> response contains
    /// the <see cref="GetAssertionOutput.NumberOfCredentials"/> member and the number
    /// of credentials exceeds 1.
    /// </para>
    /// 
    /// <para>
    /// Responses to the <see cref="GetNextAssertionCommand"/> will never have the
    /// <see cref="GetAssertionOutput.NumberOfCredentials"/> member set.
    /// </para>
    /// </remarks>
    internal sealed class GetNextAssertionCommand : IYubiKeyCommand<GetAssertionResponse>
    {
        private const byte CtapGetNextAssertionCommand = 0x08;
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// Initializes a new instance of the GetNextAssertionCommand class.
        /// </summary>
        public GetNextAssertionCommand()
        {

        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu()
        {
            byte[] payload = { CtapGetNextAssertionCommand };

            return new CommandApdu()
            {
                Ins = (byte)CtapHidCommand.Cbor,
                Data = payload
            };
        }

        /// <inheritdoc/>
        public GetAssertionResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetAssertionResponse (responseApdu);
    }
}
