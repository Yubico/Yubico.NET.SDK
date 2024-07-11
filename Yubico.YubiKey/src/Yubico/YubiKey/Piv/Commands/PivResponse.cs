// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Base class for all PIV responses. Use this class to represent the status
    /// of a PIV command, or one of its derived classes to retrieve the full
    /// response.
    /// </summary>
    /// <seealso cref="Yubico.YubiKey.IYubiKeyResponse"/>
    public class PivResponse : YubiKeyResponse
    {
        // Overridden to add values of the StatusWord known to PIV responses.
        protected override ResponseStatusPair StatusCodeMap =>
            StatusWord switch
            {
                SWConstants.SecurityStatusNotSatisfied => new ResponseStatusPair(ResponseStatus.AuthenticationRequired, ResponseStatusMessages.PivSecurityStatusNotSatisfied),
                _ => base.StatusCodeMap,
            };

        /// <summary>
        /// Constructs a PivResponse based on a ResponseApdu received from the
        /// YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        public PivResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {

        }
    }
}
