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

namespace Yubico.YubiKey.Scp.Commands
{
    /// <summary>
    /// This class is used to get data from the YubiKey associated with the given tag.
    /// <para>For getting data in the Security Domain, it is recommended to use the methods provided by <see cref="SecurityDomainSession"/> instead, such as
    /// <see cref="SecurityDomainSession.GetData"/>, <see cref="SecurityDomainSession.GetCertificates"/>, <see cref="SecurityDomainSession.GetSupportedCaIdentifiers"/>, and <see cref="SecurityDomainSession.GetKeyInformation"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.
    /// </para>
    /// </remarks>
    /// <returns>The encoded tlv data retrieved from the YubiKey.</returns>
    /// <exception cref="SecureChannelException">Thrown when there was an SCP error, described in the exception message.</exception>
    internal class GetDataCommand : IYubiKeyCommand<GetDataCommandResponse>
    {
        internal const byte GetDataIns = 0xCA;
        private readonly int _tag;
        private readonly ReadOnlyMemory<byte> _data;

        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetDataCommand"/> class.
        /// </summary>
        /// <param name="tag">The tag of the data to retrieve from the YubiKey.</param>
        /// <param name="data">
        /// Optional data to be used in the GetData command. This might be used for
        /// certain YubiKey applications, such as the OpenPGP application.
        /// </param>
        /// <remarks>
        /// <para>For getting data in the Security Domain,
        /// it is recommended to use the methods provided by <see cref="SecurityDomainSession"/> instead, such as
        /// <see cref="SecurityDomainSession.GetData"/>, <see cref="SecurityDomainSession.GetCertificates"/>, <see cref="SecurityDomainSession.GetSupportedCaIdentifiers"/>, and <see cref="SecurityDomainSession.GetKeyInformation"/>.
        /// </para>
        /// <para>
        /// See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.
        /// </para>
        /// </remarks>
        public GetDataCommand(int tag, ReadOnlyMemory<byte>? data = null)
        {
            _tag = tag;
            _data = data ?? ReadOnlyMemory<byte>.Empty;
        }

        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Cla = 0,
                Ins = GetDataIns,
                P1 = (byte)(_tag >> 8),
                P2 = (byte)(_tag & 0xFF),
                Data = _data
            };

        public GetDataCommandResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetDataCommandResponse(responseApdu);
    }

    internal class GetDataCommandResponse : ScpResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        /// <summary>
        /// The response to the GetData command.
        /// </summary>
        /// <param name="responseApdu">The response APDU from the YubiKey.</param>
        public GetDataCommandResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the data retrieved from the YubiKey.
        /// </summary>
        /// <returns>The data retrieved from the YubiKey.</returns>
        public ReadOnlyMemory<byte> GetData() => ResponseApdu.Data;
    }
}
