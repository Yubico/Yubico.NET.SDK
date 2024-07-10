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
    ///     Gets the modality of the BioEnrollment. This is a subcommand of the CTAP
    ///     command "authenticatorBioEnrollment".
    /// </summary>
    /// <remarks>
    ///     The partner Response class is <see cref="GetBioModalityResponse" />.
    ///     <para>
    ///         The modality describes what the Bio authentication (biometric) is.
    ///         Currently, the only modality listed in the FIDO2 standard is fingerprint.
    ///     </para>
    /// </remarks>
    public sealed class GetBioModalityCommand : IYubiKeyCommand<GetBioModalityResponse>
    {
        private readonly BioEnrollmentCommand _command;

        /// <summary>
        ///     Constructs an instance of the <see cref="GetBioModalityCommand" /> class.
        /// </summary>
        public GetBioModalityCommand()
        {
            // Get Bio Modality is not a defined subcommand, so we pass 0 as the
            // subcommand. The base class knows how to handle this case.
            _command = new BioEnrollmentCommand(0);
        }

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public GetBioModalityResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetBioModalityResponse(responseApdu);
    }
}
