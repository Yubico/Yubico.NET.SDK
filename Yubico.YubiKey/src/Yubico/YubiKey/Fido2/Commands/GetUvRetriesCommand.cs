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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    ///     Gets the number of UV retries remaining for FIDO2.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The number of UV attempts remaining may vary based the configured maximum and the number of failed attempts
    ///         since
    ///         the last successful verification. Use this command to query for the current number of retries remaining for
    ///         this
    ///         specific YubiKey.
    ///     </para>
    ///     <para>
    ///         The number of remaining UV attempts should also be displayed to the user, so that they know that they may be
    ///         reaching the limit. Exhausting the number of UV retries is not as catastrophic as exhausting the number of PIN
    ///         attempts. When the number of UV retries reaches 0, the authenticator will no longer attempt UV with the
    ///         on-board
    ///         sensor, and will instead require PIN entry. Once the PIN has been successfully entered, the UV retry count will
    ///         be reset - even if it was previous blocked.
    ///     </para>
    ///     <para>
    ///         See the user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth
    ///         guide to working with user verification within FIDO2.
    ///     </para>
    /// </remarks>
    public class GetUvRetriesCommand : IYubiKeyCommand<GetUvRetriesResponse>
    {
        private const int SubCmdGetUvRetries = 0x07;
        private readonly ClientPinCommand _command;

        /// <summary>
        ///     Constructs a new instance of <see cref="GetUvRetriesCommand" />.
        /// </summary>
        public GetUvRetriesCommand()
        {
            _command = new ClientPinCommand
            {
                SubCommand = SubCmdGetUvRetries
            };
        }

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public GetUvRetriesResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetUvRetriesResponse(responseApdu);
    }
}
