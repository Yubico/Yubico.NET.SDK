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

using System;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Remove an enrolled fingerprint. This is a subcommand of the CTAP
    /// command "authenticatorBioEnrollment".
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="Fido2Response"/>. This command
    /// does not return any data, it only returns "success" or "failure", and has
    /// some FIDO2-specific error information.
    /// </remarks>
    public sealed class BioEnrollRemoveCommand : IYubiKeyCommand<Fido2Response>
    {
        private const int SubCmdRemoveEnrollment = 0x06;
        private const int KeyTemplateId = 0x01;

        private readonly BioEnrollmentCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        public BioEnrollRemoveCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs an instance of the <see cref="BioEnrollEnumerateCommand"/> class.
        /// </summary>
        /// <param name="templateId">
        /// The ID of the template to remove.
        /// </param>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public BioEnrollRemoveCommand(
            ReadOnlyMemory<byte> templateId,
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol)
        {
            _command = new BioEnrollmentCommand(
                SubCmdRemoveEnrollment,
                EncodeParams(templateId),
                pinUvAuthToken,
                authProtocol);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public Fido2Response CreateResponseForApdu(ResponseApdu responseApdu) =>
            new Fido2Response(responseApdu);

        // This method encodes the parameters. For RemoveEnrollment, the
        // parameters consist of the template ID.
        // It is encoded as
        //   map
        //     01 byte string
        private static byte[]? EncodeParams(ReadOnlyMemory<byte> templateId)
        {
            return new CborMapWriter<int>()
                .Entry(KeyTemplateId, templateId)
                .Encode();
        }
    }
}
