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

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// The response to the <see cref="SetPinCommand"/> command, containing the
    /// response from the YubiKey.
    /// </summary>
    /// <remarks>
    /// This is the partner response class to <see cref="SetPinCommand"/>.
    /// <para>
    /// After executing the <c>SetPinCommand</c>, the result is an
    /// instance of this class. There is no data to return. Simply check the
    /// <c>Status</c> property. If it is <c>ResponseStatus.Success</c> the
    /// PIN was changed. If it is <c>ResponseStatus.ConditionsNotSatisfied</c>,
    /// then the current PIN was incorrect.
    /// </para>
    /// </remarks>
    public sealed class SetPinResponse : U2fResponse, IYubiKeyResponse
    {
        /// <summary>
        /// Constructs a SetPinResponse based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU returned by the YubiKey.
        /// </param>
        public SetPinResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <inheritdoc/>
        protected override ResponseStatusPair StatusCodeMap => StatusWord switch
        {
            SWConstants.VerifyFail => new ResponseStatusPair(ResponseStatus.ConditionsNotSatisfied, ResponseStatusMessages.BaseConditionsNotSatisfied),
            _ => base.StatusCodeMap,
        };
    }
}
