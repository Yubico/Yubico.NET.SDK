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
    /// The response to the U2F Register command.
    /// </summary>
    /// <remarks>
    /// This is the partner response class to <see cref="RegisterCommand"/>.
    /// <p>
    /// Registration on most devices will first fail with <see cref="ResponseStatus.ConditionsNotSatisfied"/>
    /// and then the device will begin waiting for a touch to verify user presence.
    /// See <see cref="RegisterCommand"/> for more details.
    /// </p>
    /// </remarks>
    public sealed class RegisterResponse : U2fResponse, IYubiKeyResponseWithData<RegistrationData>
    {
        /// <summary>
        /// Constructs a RegisterResponse from the given ResponseApdu.
        /// </summary>
        /// <param name="responseApdu">The response to a <see cref="RegisterCommand"/>.</param>
        public RegisterResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the registration data from the response.
        /// </summary>
        /// <remarks>
        /// If the status of the response is not 'Success', this method will fail. If the
        /// status of the response is <see cref="ResponseStatus.ConditionsNotSatisfied"/> then
        /// clients should retry the command until it succeeds (when user presence is confirmed,
        /// generally through touch).
        /// <p>
        /// Throws a <see cref="ArgumentException"/> in the event of an error
        /// parsing the device response.
        /// </p>
        /// </remarks>
        /// <returns>
        /// The data in the response APDU, presented as a <see cref="RegistrationData"/> object.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public RegistrationData GetData() => Status switch
        {
            ResponseStatus.Success => new RegistrationData(ResponseApdu.Data),
            _ => throw new InvalidOperationException(StatusMessage),
        };
    }
}
