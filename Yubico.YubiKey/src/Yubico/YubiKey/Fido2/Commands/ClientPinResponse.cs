// Copyright 2022 Yubico AB
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
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response partner to <see cref="ClientPinCommand"/>.
    /// </summary>
    /// <remarks>
    /// Like <see cref="ClientPinCommand"/>, this response represents all of the possible outputs of all sub-commands
    /// supported by `authenticatorClientPin`. It is recommended that you use the command class that corresponds with
    /// the particular sub-command you care about. Doing so will return a more specific response partner class that will
    /// only contain the information relevant to that sub-command.
    /// </remarks>
    public class ClientPinResponse : Fido2Response, IYubiKeyResponseWithData<ClientPinData>
    {
        // Response constants
        private const int TagKeyAgreement = 0x01;
        private const int TagPinUvAuthToken = 0x02;
        private const int TagPinRetries = 0x03;
        private const int TagPowerCycleState = 0x04;
        private const int TagUvRetries = 0x05;

        /// <summary>
        /// Constructs a new instance of <see cref="ClientPinResponse"/> based on a response APDU provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the `authenticatorClientPin` command.
        /// </param>
        public ClientPinResponse(ResponseApdu responseApdu) : base(responseApdu)
        {

        }

        /// <inheritdoc />
        public ClientPinData GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            var cbor = new CborReader(ResponseApdu.Data, CborConformanceMode.Ctap2Canonical);

            int? entries = cbor.ReadStartMap();

            if (entries is null || entries.Value == 0)
            {
                throw new Ctap2DataException(ExceptionMessages.CborMapEntriesExpected);
            }

            // Any of the output parameters may be present. We know how many map entries there are, so iterate through
            // them and pull out the tags that are known to us.
            var data = new ClientPinData();
            for (int entry = entries.Value; entry > 0; entry--)
            {
                uint key = cbor.ReadUInt32();

                switch (key)
                {
                    case TagKeyAgreement:
                        data.KeyAgreement = CoseKey.Create(cbor.ReadEncodedValue(), out int _);
                        break;

                    case TagPinUvAuthToken:
                        data.PinUvAuthToken = cbor.ReadByteString();
                        break;

                    case TagPinRetries:
                        data.PinRetries = (int)cbor.ReadUInt32();
                        break;

                    case TagPowerCycleState:
                        data.PowerCycleState = cbor.ReadBoolean();
                        break;

                    case TagUvRetries:
                        data.UvRetries = (int)cbor.ReadUInt32();
                        break;

                    default:
                        throw new Ctap2DataException(ExceptionMessages.CborUnexpectedMapTag);
                }
            }

            return data;
        }
    }
}
