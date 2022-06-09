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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// The response to the U2F Authenticate command.
    /// </summary>
    /// <remarks>
    /// This is the partner response class to <see cref="AuthenticateCommand"/>.
    /// </remarks>
    public sealed class AuthenticateResponse : U2fResponse, IYubiKeyResponseWithData<AuthenticationData>
    {
        /// <summary>
        /// Constructs an AuthenticateResponse from the given ResponseApdu.
        /// </summary>
        /// <param name="responseApdu">The response to a
        /// <see cref="AuthenticateCommand"/>.
        /// </param>
        public AuthenticateResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the authentication data from the response.
        /// </summary>
        /// <returns>
        /// The data in the response APDU, presented as an AuthenticationData object.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public AuthenticationData GetData() => Status switch
        {
            ResponseStatus.Success => new AuthenticationData(ResponseApdu.Data),
            _ => throw new InvalidOperationException(StatusMessage),
        };
    }
}
