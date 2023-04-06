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
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Continue the process of enrolling a fingerprint. This is a subcommand of
    /// the CTAP command "authenticatorBioEnrollment".
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="BioEnrollNextSampleResponse"/>.
    /// </remarks>
    public sealed class BioEnrollNextSampleCommand : IYubiKeyCommand<BioEnrollNextSampleResponse>
    {
        private const int SubCmdEnrollNextSample = 0x02;
        private const int KeyTemplateId = 0x01;
        private const int KeyTimeout = 0x03;

        private readonly BioEnrollmentCommand _command;
        private readonly ReadOnlyMemory<byte> _templateId;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private BioEnrollNextSampleCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="BioEnrollNextSampleCommand"/>.
        /// </summary>
        /// <param name="templateId">
        /// The templateID returned by the YubiKey upon completion of the
        /// <see cref="BioEnrollBeginCommand"/>.
        /// </param>
        /// <param name="timeoutMilliseconds">
        /// The timeout the caller would like the YubiKey to enforce. This is
        /// optional and can be null.
        /// </param>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public BioEnrollNextSampleCommand(
            ReadOnlyMemory<byte> templateId,
            int? timeoutMilliseconds,
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol)
        {
            _templateId = templateId;
            _command = new BioEnrollmentCommand(
                SubCmdEnrollNextSample,
                EncodeParams(templateId, timeoutMilliseconds),
                pinUvAuthToken,
                authProtocol);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public BioEnrollNextSampleResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new BioEnrollNextSampleResponse(responseApdu, _templateId);

        // This method encodes the parameters. For
        // EnrollNextSample, the parameters consist of the template ID and the
        // timeout in milliseconds. If the caller does not specify a timeout
        // (null input), then there is only the template ID.
        // It is encoded as
        //   map
        //     01 byte string
        //     03 int
        private static byte[]? EncodeParams(ReadOnlyMemory<byte> templateId, int? timeoutMilliseconds)
        {
            return new CborMapWriter<int>()
                .Entry(KeyTemplateId, templateId)
                .OptionalEntry(KeyTimeout, timeoutMilliseconds)
                .Encode();
        }
    }
}
