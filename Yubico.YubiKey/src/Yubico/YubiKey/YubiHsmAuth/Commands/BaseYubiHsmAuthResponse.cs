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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The base class of YubiHSM Auth response types.
    /// </summary>
    /// <remarks>
    /// This base class adds mappings for status words which have new meanings
    /// in the YubiHSM Auth application.
    /// </remarks>
    public abstract class BaseYubiHsmAuthResponse : YubiKeyResponse
    {
        /// <inheritdoc/>
        protected override ResponseStatusPair StatusCodeMap
        {
            get => StatusWord switch
            {
                // Overriding these SW for meanings specific to the YubiHSM Auth application
                SWConstants.SecurityStatusNotSatisfied => new ResponseStatusPair(ResponseStatus.RetryWithTouch, ResponseStatusMessages.YubiHsmAuthTouchRequired),
                SWConstants.AuthenticationMethodBlocked => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.YubiHsmAuthInvalidEntry),
                SWConstants.ReferenceDataUnusable => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.YubiHsmAuthInvalidAuthData),

                _ => base.StatusCodeMap,
            };
        }

        protected BaseYubiHsmAuthResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }
    }
}
