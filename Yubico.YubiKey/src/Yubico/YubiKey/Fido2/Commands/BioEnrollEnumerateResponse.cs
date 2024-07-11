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

using System.Collections.Generic;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response partner to the BioEnrollEnumerateCommand.
    /// </summary>
    public class BioEnrollEnumerateResponse : Fido2Response, IYubiKeyResponseWithData<IReadOnlyList<TemplateInfo>>
    {
        private readonly BioEnrollmentResponse _response;

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="BioEnrollEnumerateResponse"/> based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorCredentialManagement</c> command.
        /// </param>
        public BioEnrollEnumerateResponse(ResponseApdu responseApdu)
            : base(responseApdu)
        {
            _response = new BioEnrollmentResponse(responseApdu);
        }

        /// <summary>
        /// Return the data returned by the YubiKey as a <c>List</c>.
        /// </summary>
        /// <remarks>
        /// If there are no fingerprints enrolled, this will return a <c>List</c>
        /// with zero elements.
        /// </remarks>
        public IReadOnlyList<TemplateInfo> GetData()
        {
            // If the return is InvalidOption, that means there were no enrolled
            // fingerprints, return a List of zero elements.
            if (CtapStatus == CtapStatus.InvalidOption)
            {
                return new List<TemplateInfo>();
            }

            BioEnrollmentData enrollData = _response.GetData();

            if (!(enrollData.TemplateInfos is null))
            {
                return enrollData.TemplateInfos;
            }

            throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
        }

        /// <inheritdoc />
        protected override ResponseStatusPair StatusCodeMap =>
            CtapStatus switch
            {
                CtapStatus.InvalidOption => new ResponseStatusPair(
                    ResponseStatus.Success, ResponseStatusMessages.BaseSuccess),
                _ => base.StatusCodeMap,
            };
    }
}
