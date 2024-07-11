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
    /// The response to the <see cref="ConfigCommand"/>, containing the response
    /// from the YubiKey.
    /// </summary>
    /// <remarks>
    /// This is the partner response class to <see cref="ConfigCommand"/>.
    /// <para>
    /// After executing the <c>ConfigCommand</c>, the result is an
    /// instance of this class. There is no data to return. Simply check the
    /// <c>Status</c> property. If it is <c>ResponseStatus.Success</c> the
    /// operation succeeded. If it is anything else, then the operation failed
    /// and you can check the CtapStatus and StatusMessage.
    /// </para>
    /// <para>
    /// This class is really used only to override the StatusCodeMap and provide
    /// error messages specific to AuthenticatorConfig.
    /// </para>
    /// </remarks>
    public sealed class ConfigResponse : Fido2Response, IYubiKeyResponse
    {
        /// <summary>
        /// Constructs a ConfigResponse based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU returned by the YubiKey.
        /// </param>
        public ConfigResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <inheritdoc />
        protected override ResponseStatusPair StatusCodeMap => CtapStatus switch
        {
            CtapStatus.InvalidParameter => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseFunctionNotSupported),
            _ => base.StatusCodeMap,
        };
    }
}
