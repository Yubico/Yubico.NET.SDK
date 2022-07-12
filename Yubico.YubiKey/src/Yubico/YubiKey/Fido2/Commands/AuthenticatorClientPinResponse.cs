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

using System.Formats.Cbor;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class AuthenticatorClientPinResponse : YubiKeyResponse, IYubiKeyResponseWithData<AuthenticatorClientPinData>
    {
        private const int TagKeyAgreement = 0x01;
        private const int TagPinUvAuthToken = 0x02;
        private const int TagPinRetries = 0x03;
        private const int TagPowerCycleState = 0x04;
        private const int TagUvRetries = 0x05;

        public AuthenticatorClientPinResponse(ResponseApdu responseApdu) : base(responseApdu)
        {

        }

        /// <inheritdoc />
        public AuthenticatorClientPinData GetData()
        {
            var cbor = new CborReader(ResponseApdu.Data, CborConformanceMode.Ctap2Canonical);

            int? entries = cbor.ReadStartMap();

            if (entries is null)
            {
                throw new Ctap2DataException(); // TODO
            }

            var data = new AuthenticatorClientPinData();
            for (int entry = entries.Value; entry > 0; entry--)
            {
                uint key = cbor.ReadUInt32();

                switch (key)
                {
                    case TagKeyAgreement:
                        data.KeyAgreement = cbor.ReadByteString();
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
                        throw new Ctap2DataException(); // TODO
                }
            }

            return data;
        }
    }
}
