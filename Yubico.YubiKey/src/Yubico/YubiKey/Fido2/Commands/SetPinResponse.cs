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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// This is the partner response class to the <see cref="SetPinCommand"/>
    /// class.
    /// </summary>
    /// <remarks>
    /// Note that this response has no data to return (there is no <c>GetData</c>
    /// method). If the PIN is successfully set, the <see cref="CtapStatus"/>
    /// property will be <c>ResponseStatus.Success</c>. If the PIN is not set,
    /// the <c>Status</c> property will indicate the error.
    /// </remarks>
    public class SetPinResponse : Fido2Response, IYubiKeyResponse
    {
        private readonly ClientPinResponse _response;

        /// <summary>
        /// Constructs a new instance of the
        /// <see cref="SetPinResponse"/> class based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response for the
        /// `setPin` subcommand of the `authenticatorClientPIN` CTAP2
        /// command.
        /// </param>
        public SetPinResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
            _response = new ClientPinResponse(responseApdu);
        }
    }
}
