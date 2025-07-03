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
using System.Formats.Cbor;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Gets the large blob out of the YubiKey.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="GetLargeBlobResponse"/>.
    /// Specified in CTAP as "authenticatorLargeBlobs".
    /// <para>
    /// The standard specifies one command called
    /// "<c>authenticatorLargeBlobs</c>". It takes input that specifies whether
    /// to get or set. The SDK breaks this into two commands.
    /// </para>
    /// </remarks>
    public sealed class GetLargeBlobCommand : IYubiKeyCommand<GetLargeBlobResponse>
    {
        private const byte CtapGetLargeBlobCmd = 0x0C;
        private const int CborMapCount = 2;
        private const int CborKeyGet = 1;
        private const int CborKeyOffset = 3;

        private readonly int _offset;
        private readonly int _count;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Fido2
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// Constructs an instance of the <see cref="GetLargeBlobCommand" /> class.
        /// </summary>
        /// <param name="offset">
        /// The offset into the stored large blob where the returned data should
        /// begin.
        /// </param>
        /// <param name="count">
        /// The number of bytes to return.
        /// </param>
        public GetLargeBlobCommand(int offset, int count)
        {
            _offset = offset;
            _count = count;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var cborWriter = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cborWriter.WriteStartMap(CborMapCount);
            cborWriter.WriteInt32(CborKeyGet);
            cborWriter.WriteInt32(_count);
            cborWriter.WriteInt32(CborKeyOffset);
            cborWriter.WriteInt32(_offset);
            cborWriter.WriteEndMap();
            byte[] encodedParams = cborWriter.Encode();

            byte[] payload = new byte[encodedParams.Length + 1];
            payload[0] = CtapGetLargeBlobCmd;
            Array.Copy(encodedParams, 0, payload, 1, encodedParams.Length);

            return new CommandApdu()
            {
                Ins = CtapConstants.CtapHidCbor,
                Data = payload
            };
        }

        /// <inheritdoc />
        public GetLargeBlobResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetLargeBlobResponse(responseApdu);
    }
}
