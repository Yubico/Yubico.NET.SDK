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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    ///     Get the public properties of all <see cref="Credential" />s in the
    ///     YubiHSM Auth application, along with the number of retries remaining
    ///     for each.
    /// </summary>
    /// <remarks>
    ///     The associated response class is <see cref="ListCredentialsResponse" />.
    /// </remarks>
    public sealed class ListCredentialsCommand : IYubiKeyCommand<ListCredentialsResponse>
    {
        private const byte ListCredentialsInstruction = 0x05;

        /// <summary>
        ///     Constructs an instance of the <see cref="ListCredentialsCommand" /> class.
        /// </summary>
        public ListCredentialsCommand()
        {
        }

        /// <summary>
        ///     Gets the <see cref="YubiKeyApplication" /> to which this command belongs.
        /// </summary>
        /// <value>
        ///     <see cref="YubiKeyApplication.YubiHsmAuth" />
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = ListCredentialsInstruction
            };

        /// <inheritdoc />
        public ListCredentialsResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ListCredentialsResponse(responseApdu);
    }
}
