// Copyright 2023 Yubico AB
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
    /// Continue the process of getting information on all the relying parties
    /// represented in credentials on the YubiKey, by getting the next RP.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="EnumerateRpsGetNextResponse"/>.
    /// <para>
    /// This returns information on one of the relying parties. If there is only
    /// one RP, then the call to the <c>enumerateRPsBegin</c> subcommand gave
    /// you all the information you need. It also indicated that there was only
    /// one RP. If there are more RPs, then you can get information on all of
    /// them by calling this subcommand, calling it once for every RP after the
    /// first one.
    /// </para>
    /// <para>
    /// The return from this command is the next relying party.
    /// </para>
    /// <para>
    /// Note that this command does not need the <c>pinUvAuthToken</c> nor the
    /// <c>authProtocol</c>. This command picks up where the
    /// <see cref="EnumerateRpsBeginCommand"/> left off, and can only operate
    /// successfully after that "Begin" command has successfully completed.
    /// </para>
    /// </remarks>
    public class EnumerateRpsGetNextCommand : IYubiKeyCommand<EnumerateRpsGetNextResponse>
    {
        private const int SubCmdGetEnumerateRpsGetNext = 0x03;

        private readonly CredentialManagementCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        /// <summary>
        /// Constructs a new instance of <see cref="EnumerateRpsGetNextCommand"/>.
        /// </summary>
        public EnumerateRpsGetNextCommand()
        {
            _command = new CredentialManagementCommand(SubCmdGetEnumerateRpsGetNext);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public EnumerateRpsGetNextResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new EnumerateRpsGetNextResponse(responseApdu);
    }
}
