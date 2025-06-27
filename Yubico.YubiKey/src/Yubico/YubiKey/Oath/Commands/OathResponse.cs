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

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// Base class for all OATH responses. Use this class to represent the status of an OATH command,
    /// or one of its derived classes to retrieve the full response.
    /// </summary>
    /// <seealso cref="Yubico.YubiKey.IYubiKeyResponse" />
    public class OathResponse : YubiKeyResponse
    {
        public OathResponse(ResponseApdu responseApdu) :
               base(responseApdu)
        {

        }

        /// <inheritdoc/>
        protected override ResponseStatusPair StatusCodeMap =>
           StatusWord switch
           {
               OathSWConstants.GenericError => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.OathGenericError),
               OathSWConstants.WrongSyntax => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.OathWrongSyntax),
               SWConstants.SecurityStatusNotSatisfied => new ResponseStatusPair(ResponseStatus.AuthenticationRequired, ResponseStatusMessages.OathSecurityStatusNotSatisfied),
               _ => base.StatusCodeMap,
           };
    }
}
