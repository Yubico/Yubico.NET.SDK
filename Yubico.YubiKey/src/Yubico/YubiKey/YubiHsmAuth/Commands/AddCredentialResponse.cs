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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The response class for adding a credential to the YubiHSM Auth
    /// application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If authentication failed, the <see cref="YubiKeyResponse.Status"/>
    /// will be set to
    /// <see cref="ResponseStatus.AuthenticationRequired"/> and
    /// <see cref="BaseYubiHsmAuthResponseWithRetries.RetriesRemaining"/>
    /// will contain the number of retries remaining for the management key.
    /// </para>
    /// <para>
    /// The associated command class is <see cref="AddCredentialCommand"/>.
    /// </para>
    /// </remarks>
    public class AddCredentialResponse : BaseYubiHsmAuthResponseWithRetries
    {
        protected override ResponseStatusPair StatusCodeMap
        {
            get => StatusWord switch
            {
                // A credential with that label already exists
                SWConstants.AuthenticationMethodBlocked =>
                    new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.YubiHsmAuthLabelConflict),

                _ => base.StatusCodeMap,
            };
        }

        /// <summary>
        /// Constructs an AddCredentialResponse based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public AddCredentialResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }
    }
}
