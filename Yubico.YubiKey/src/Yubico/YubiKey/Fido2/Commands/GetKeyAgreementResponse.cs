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
using System.Globalization;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// This is the partner response class to the
    /// <see cref="GetKeyAgreementCommand" /> command class.
    /// </summary>
    public class GetKeyAgreementResponse : IYubiKeyResponseWithData<CosePublicEcKey>
    {
        private readonly ClientPinResponse _response;

        /// <summary>
        /// Constructs a new instance of the
        /// <see cref="GetKeyAgreementResponse"/> class based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response for the
        /// `getKeyAgreement` sub-command of the `authenticatorClientPIN` CTAP2
        /// command.
        /// </param>
        public GetKeyAgreementResponse(ResponseApdu responseApdu)
        {
            _response = new ClientPinResponse(responseApdu);
        }

        /// <summary>
        /// Returns the YubiKey's public key for the key agreement algorithm of
        /// the PIN/UV auth protocol specified in the command.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public CosePublicEcKey GetData()
        {
            ClientPinData data = _response.GetData();

            if (data.KeyAgreement is null)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }

            return new CosePublicEcKey(data.KeyAgreement.Value);
        }

        /// <inheritdoc />
        public ResponseStatus Status => _response.Status;

        /// <inheritdoc />
        public short StatusWord => _response.StatusWord;

        /// <inheritdoc />
        public string StatusMessage => _response.StatusMessage;
    }
}
