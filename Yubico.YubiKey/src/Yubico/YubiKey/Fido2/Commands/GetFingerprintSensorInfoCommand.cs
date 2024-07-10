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
    ///     Gets information about the fingerprint sensor. This is a subcommand of
    ///     the CTAP command "authenticatorBioEnrollment".
    /// </summary>
    /// <remarks>
    ///     The partner Response class is <see cref="GetFingerprintSensorInfoResponse" />.
    /// </remarks>
    public sealed class GetFingerprintSensorInfoCommand : IYubiKeyCommand<GetFingerprintSensorInfoResponse>
    {
        private const int SubCmdSensorInfo = 0x07;

        private readonly BioEnrollmentCommand _command;

        /// <summary>
        ///     Constructs a new instance of <see cref="GetFingerprintSensorInfoCommand" />.
        /// </summary>
        public GetFingerprintSensorInfoCommand()
        {
            _command = new BioEnrollmentCommand(SubCmdSensorInfo);
        }

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public GetFingerprintSensorInfoResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetFingerprintSensorInfoResponse(responseApdu);
    }
}
