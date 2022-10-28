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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class Fido2Response : YubiKeyResponse
    {
        private const short CtapStatusMask = 0xFF;

        /// <summary>
        /// The CTAP status code.
        /// </summary>
        public CtapStatus CtapStatus { get; private set; }

        public Fido2Response(ResponseApdu responseApdu) : base(responseApdu)
        {
            CtapStatus = (CtapStatus)(StatusWord & CtapStatusMask);
        }

        /// <summary>
        /// Overridden to modify the messages associated with certain
        /// status words. The messages match the status words' meanings
        /// as described in the FIDO2 specifications.
        /// </summary>
        protected override ResponseStatusPair StatusCodeMap => CtapStatus switch
        {
            CtapStatus.NotAllowed => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.Fido2NotAllowed),
            CtapStatus.PinNotSet => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.Fido2PinNotSet),
            CtapStatus.PinInvalid => new ResponseStatusPair(ResponseStatus.ConditionsNotSatisfied, ResponseStatusMessages.Fido2PinNotVerified),
            CtapStatus.PinBlocked => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.Fido2PinBlocked),
            CtapStatus.ActionTimeout => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.Fido2Timeout),

            _ => base.StatusCodeMap,
        };
    }
}
