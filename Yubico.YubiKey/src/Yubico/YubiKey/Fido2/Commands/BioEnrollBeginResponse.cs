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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response partner to the GetBioModalityCommand, containing
    /// information about the biometric technique of the YubiKey.
    /// </summary>
    public class BioEnrollBeginResponse : Fido2Response, IYubiKeyResponseWithData<BioEnrollSampleResult>
    {
        private readonly BioEnrollmentResponse _response;

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="BioEnrollBeginResponse"/> based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorBioEnrollment</c> command.
        /// </param>
        public BioEnrollBeginResponse(ResponseApdu responseApdu)
            : base(responseApdu)
        {
            _response = new BioEnrollmentResponse(responseApdu);
        }

        /// <inheritdoc/>
        public BioEnrollSampleResult GetData()
        {
            var bioEnrollmentData = _response.GetData();

            if (!(bioEnrollmentData.TemplateId is null)
                && !(bioEnrollmentData.LastEnrollSampleStatus is null)
                && !(bioEnrollmentData.RemainingSampleCount is null))
            {
                return new BioEnrollSampleResult(
                    bioEnrollmentData.TemplateId.Value,
                    bioEnrollmentData.LastEnrollSampleStatus.Value,
                    bioEnrollmentData.RemainingSampleCount.Value);
            }

            throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
        }

        protected override ResponseStatusPair StatusCodeMap => CtapStatus switch
        {
            CtapStatus.FpDatabaseFull => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseNoMoreSpaceInFile),
            _ => base.StatusCodeMap,
        };
    }
}
