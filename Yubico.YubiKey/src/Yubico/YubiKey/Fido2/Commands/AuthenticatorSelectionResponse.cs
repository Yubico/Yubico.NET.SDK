// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response to the <see cref="AuthenticatorSelectionCommand"/>.
    /// </summary>
    /// <remarks>
    /// On success there is no response payload. If the authenticator does not implement
    /// <c>authenticatorSelection</c>, expect <see cref="Yubico.YubiKey.Fido2.CtapStatus.InvalidCommand"/> or another CTAP error;
    /// that reflects authenticator support, not an SDK defect.
    /// </remarks>
    public sealed class AuthenticatorSelectionResponse : Fido2Response, IYubiKeyResponse
    {
        /// <summary>
        /// Constructs an <see cref="AuthenticatorSelectionResponse"/> from the YubiKey APDU response.
        /// </summary>
        /// <param name="responseApdu">The response APDU returned by the YubiKey.</param>
        public AuthenticatorSelectionResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <inheritdoc />
        protected override ResponseStatusPair StatusCodeMap => CtapStatus switch
        {
            CtapStatus.OperationDenied => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.Fido2AuthenticatorSelectionDenied),
            _ => base.StatusCodeMap,
        };
    }
}