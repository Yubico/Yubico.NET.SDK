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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// This is the partner response class to the <see cref="GetUvRetriesCommand"/> command class.
    /// </summary>
    public class GetUvRetriesResponse : Fido2Response, IYubiKeyResponseWithData<int>
    {
        private readonly ClientPinResponse _response;

        /// <summary>
        /// Constructs a new instance of the <see cref="GetUvRetriesResponse"/> class based on a response APDU provided
        /// by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response for the `getUvRetries` subcommand of the `authenticatorClientPIN`
        /// CTAP2 command.
        /// </param>
        public GetUvRetriesResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
            _response = new ClientPinResponse(responseApdu);
        }

        /// <summary>
        /// Returns the number of built-in User Verification (UV) retries remaining for this YubiKey's FIDO application.
        /// The only built-in UV method the YubiKey has is the fingerprint reader on the YubiKey Bio Series. The UV retry
        /// count is not applicable to non-biometric based YubiKeys.
        /// </summary>
        /// <returns>
        /// The number of UV tries remaining before disabling the built-in fingerprint sensor.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The UV retry count was missing from the response that was returned by the YubiKey.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The UV retries counter represents the number of attempts left using the YubiKey's biometric sensor. This only
        /// applies to YubiKeys that have an onboard biometric sensor, such as the YubiKey Bio Series.
        /// </para>
        /// <para>
        /// Applications should alert the user when the number of retries approaches zero. When the number of retries is
        /// exhausted, a user will no longer be able to use the fingerprint reader to authenticate. The YubiKey will fall
        /// back to the PIN for future authentication attempts. Once the correct PIN has been entered, the number of UV
        /// retries will be reset and the fingerprint reader will be unblocked.
        /// </para>
        /// </remarks>
        public int GetData()
        {
            var clientPinData = _response.GetData();
            if (clientPinData.UvRetries is null)
            {
                throw new Ctap2DataException(ExceptionMessages.Ctap2MissingRequiredField);
            }

            return clientPinData.UvRetries.Value;
        }
    }
}
