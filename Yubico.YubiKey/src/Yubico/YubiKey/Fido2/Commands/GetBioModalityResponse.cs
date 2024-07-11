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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response partner to the GetBioModalityCommand, containing
    /// information about the biometric technique of the YubiKey.
    /// </summary>
    public class GetBioModalityResponse : Fido2Response, IYubiKeyResponseWithData<int>
    {
        private readonly BioEnrollmentResponse _response;

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="GetBioModalityResponse"/> based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorBioEnrollment</c> command.
        /// </param>
        public GetBioModalityResponse(ResponseApdu responseApdu)
            : base(responseApdu)
        {
            _response = new BioEnrollmentResponse(responseApdu);
        }

        /// <inheritdoc/>
        /// <returns>
        /// An int, specifying the modality. The standard defines the meaning of
        /// each possible integer returned. Currently only one integer is
        /// defined: the value "1" means fingerprint.
        /// </returns>
        public int GetData()
        {
            BioEnrollmentData enrollData = _response.GetData();

            if (!(enrollData.Modality is null))
            {
                return enrollData.Modality.Value;
            }

            throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
        }
    }
}
