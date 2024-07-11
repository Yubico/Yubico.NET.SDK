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

        public Fido2Response(ResponseApdu responseApdu) : base(responseApdu)
        {
            CtapStatus = (CtapStatus)(StatusWord & CtapStatusMask);
        }

        /// <summary>
        ///     The CTAP status code.
        /// </summary>
        public CtapStatus CtapStatus { get; }

        /// <summary>
        ///     Overridden to modify the messages associated with certain
        ///     status words. The messages match the status words' meanings
        ///     as described in the FIDO2 specifications.
        /// </summary>
        protected override ResponseStatusPair StatusCodeMap =>
            CtapStatus switch
            {
                CtapStatus.MissingParameter => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.BaseInvalidParameter),
                CtapStatus.NoCredentials => new ResponseStatusPair(
                    ResponseStatus.NoData, ResponseStatusMessages.Fido2NoCredentials),
                CtapStatus.NotAllowed => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2NotAllowed),
                CtapStatus.PinRequired => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2PinNotVerified),
                CtapStatus.PinPolicyViolation => new ResponseStatusPair(
                    ResponseStatus.ConditionsNotSatisfied, ResponseStatusMessages.Fido2PinComplexityViolation),
                CtapStatus.PinNotSet => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2PinNotSet),
                CtapStatus.PinInvalid => new ResponseStatusPair(
                    ResponseStatus.ConditionsNotSatisfied, ResponseStatusMessages.Fido2PinNotVerified),
                CtapStatus.PinBlocked => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2PinBlocked),
                CtapStatus.PinAuthInvalid => new ResponseStatusPair(
                    ResponseStatus.AuthenticationRequired, ResponseStatusMessages.Fido2AuthInvalid),
                CtapStatus.UvBlocked => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2PinBlocked),
                CtapStatus.UvInvalid => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2PinNotVerified),
                CtapStatus.ActionTimeout => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2Timeout),
                CtapStatus.UserActionTimeout => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2Timeout),
                CtapStatus.UnsupportedExtension => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2OptionExtension),
                CtapStatus.UnsupportedOption => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2OptionExtension),
                CtapStatus.InvalidOption => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2OptionExtension),
                CtapStatus.CredentialExcluded => new ResponseStatusPair(
                    ResponseStatus.Failed, ResponseStatusMessages.Fido2CredentialExcluded),

                _ => base.StatusCodeMap
            };
    }
}
