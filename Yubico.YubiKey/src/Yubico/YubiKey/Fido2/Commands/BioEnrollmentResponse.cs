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
    /// The response partner to the BioEnrollmentCommand.
    /// </summary>
    /// <remarks>
    /// The standard specifies that all BioEnrollment responses that return data,
    /// return a specified map consisting of several elements. This class will be
    /// able to return a data struct that contains all those elements. However,
    /// not every response contains all elements. Hence, some will be null.
    /// Individual subcommand response classes will call on this class to parse
    /// the response, then return only those elements it can.
    /// </remarks>
    public class BioEnrollmentResponse : Fido2Response, IYubiKeyResponseWithData<BioEnrollmentData>
    {
        /// <summary>
        /// Constructs a new instance of
        /// <see cref="BioEnrollmentResponse"/> based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorBioEnrollment</c> command.
        /// </param>
        public BioEnrollmentResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }

        /// <inheritdoc />
        public BioEnrollmentData GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            return new BioEnrollmentData(ResponseApdu.Data);
        }
    }
}
