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
    /// The response partner to the BioEnrollNextSampleCommand.
    /// </summary>
    public class BioEnrollNextSampleResponse : Fido2Response, IYubiKeyResponseWithData<BioEnrollSampleResult>
    {
        private readonly BioEnrollmentResponse _response;
        private readonly ReadOnlyMemory<byte> _templateId;

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="BioEnrollNextSampleResponse"/> based on a response APDU
        /// provided by the YubiKey, along with the template ID of the
        /// fingerprint being enrolled.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorCredentialManagement</c> command.
        /// </param>
        /// <param name="templateId">
        /// The template ID returned by the BioEnrollBeginCommand.
        /// </param>
        public BioEnrollNextSampleResponse(ResponseApdu responseApdu, ReadOnlyMemory<byte> templateId)
            : base(responseApdu)
        {
            _response = new BioEnrollmentResponse(responseApdu);
            _templateId = templateId;
        }

        /// <inheritdoc/>
        public BioEnrollSampleResult GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            BioEnrollmentData enrollData = _response.GetData();

            if (!(enrollData.LastEnrollSampleStatus is null)
                && !(enrollData.RemainingSampleCount is null))
            {
                return new BioEnrollSampleResult(
                    _templateId,
                    enrollData.LastEnrollSampleStatus.Value,
                    enrollData.RemainingSampleCount.Value);
            }

            throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
        }

        /// <inheritdoc />
        protected override ResponseStatusPair StatusCodeMap =>
            CtapStatus switch
            {
                CtapStatus.ErrOther => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2OperationCanceled),
                _ => base.StatusCodeMap,
            };
    }
}
