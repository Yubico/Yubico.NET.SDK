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
    /// Begins the process of enrolling a fingerprint. This is a subcommand of
    /// the CTAP command "authenticatorBioEnrollment".
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="BioEnrollBeginResponse"/>.
    /// </remarks>
    public sealed class BioEnrollBeginCommand : IYubiKeyCommand<BioEnrollBeginResponse>
    {
        private const int SubCmdEnrollBegin = 0x01;
        private const int KeyTimeout = 0x03;

        private readonly BioEnrollmentCommand _command;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private BioEnrollBeginCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="BioEnrollBeginCommand"/>.
        /// </summary>
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
        public BioEnrollBeginCommand(
            int? timeoutMilliseconds,
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol)
        {
            _command = new BioEnrollmentCommand(
                SubCmdEnrollBegin,
                EncodeParams(timeoutMilliseconds),
                pinUvAuthToken,
                authProtocol);
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public BioEnrollBeginResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new BioEnrollBeginResponse(responseApdu);

        // This method encodes the parameters. For
        // EnrollBegin, the parameters consist of the timeout in milliseconds. If
        // the caller does not specify a timeout (null input), then there are no
        // parameters.
        // It is encoded as
        //   map
        //     03 int
        private static byte[]? EncodeParams(int? timeoutMilliseconds)
        {
            return timeoutMilliseconds is null
                ? null
                : new CborMapWriter<int>()
                    .Entry(KeyTimeout, timeoutMilliseconds.Value)
                    .Encode();
        }
    }
}
