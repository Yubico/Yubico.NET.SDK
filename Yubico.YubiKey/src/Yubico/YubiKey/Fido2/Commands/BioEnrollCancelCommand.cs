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
    /// Cancel the current BioEnrollment process. This is a subcommand of the
    /// CTAP command "authenticatorBioEnrollment".
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="Fido2Response"/>.
    /// <para>
    /// This does not return data, simply an indication whether it succeeded or
    /// not.
    /// </para>
    /// </remarks>
    public sealed class BioEnrollCancelCommand : IYubiKeyCommand<Fido2Response>
    {
        private const int SubCmdEnrollCancel = 0x03;

        private readonly BioEnrollmentCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        /// <summary>
        /// Constructs an instance of the <see cref="BioEnrollCancelCommand" /> class.
        /// </summary>
        public BioEnrollCancelCommand()
        {
            _command = new BioEnrollmentCommand(SubCmdEnrollCancel);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public Fido2Response CreateResponseForApdu(ResponseApdu responseApdu) =>
            new Fido2Response(responseApdu);
    }
}
