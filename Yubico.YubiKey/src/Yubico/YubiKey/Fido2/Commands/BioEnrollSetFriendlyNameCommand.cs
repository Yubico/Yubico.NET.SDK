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

using System;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Set the friendly name of an enrolled fingerprint. If there is a friendly
    /// name already, this replaces it. This is a subcommand of the CTAP
    /// command "authenticatorBioEnrollment".
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="Fido2Response"/>. This command
    /// does not return any data, it only returns "success" or "failure", and has
    /// some FIDO2-specific error information.
    /// </remarks>
    public sealed class BioEnrollSetFriendlyNameCommand : IYubiKeyCommand<Fido2Response>
    {
        private const int SubCmdSetName = 0x05;
        private const int KeyTemplateId = 0x01;
        private const int KeyFriendlyName = 0x02;

        private readonly BioEnrollmentCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        public BioEnrollSetFriendlyNameCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs an instance of the <see cref="BioEnrollSetFriendlyNameCommand" /> class.
        /// </summary>
        /// <param name="templateId">
        /// The ID of the fingerprint template for which the friendly name is
        /// being set.
        /// </param>
        /// <param name="friendlyName">
        /// The name that will be associated with the template.
        /// </param>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public BioEnrollSetFriendlyNameCommand(
            ReadOnlyMemory<byte> templateId,
            string friendlyName,
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol)
        {
            _command = new BioEnrollmentCommand(
                SubCmdSetName,
                EncodeParams(templateId, friendlyName),
                pinUvAuthToken,
                authProtocol);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public Fido2Response CreateResponseForApdu(ResponseApdu responseApdu) => new Fido2Response(responseApdu);

        // This method encodes the parameters. For SetFriendlyName, the
        // parameters consist of the template ID and the name.
        // It is encoded as
        //   map
        //     01 byte string
        //     02 text string
        private static byte[]? EncodeParams(ReadOnlyMemory<byte> templateId, string friendlyName)
        {
            return new CborMapWriter<int>()
                .Entry(KeyTemplateId, templateId)
                .Entry(KeyFriendlyName, friendlyName)
                .Encode();
        }
    }
}
