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
    /// Base class for all U2F responses. Use this class to represent the status of a U2F command,
    /// or one of its derived classes to retrieve the full response.
    /// </summary>
    /// <seealso cref="Yubico.YubiKey.IYubiKeyResponse" />
    public class U2fResponse : YubiKeyResponse
    {
        /// <summary>
        /// Overridden to modify the messages associated with certain
        /// status words. The messages match the status words' meanings
        /// as described in the FIDO U2F specifications.
        /// </summary>
        protected override ResponseStatusPair StatusCodeMap => StatusWord switch
        {
            // U2F raw message status codes - FIDO U2F Raw Message Formats, section 3.3
            SWConstants.ConditionsNotSatisfied => new ResponseStatusPair(ResponseStatus.ConditionsNotSatisfied, ResponseStatusMessages.U2fConditionsNotSatisfied),
            SWConstants.InvalidCommandDataParameter => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.U2fWrongData),
            SWConstants.VerifyFail => new ResponseStatusPair(ResponseStatus.AuthenticationRequired, ResponseStatusMessages.U2fPinNotVerified),

            // U2FHID_ERROR - FIDO U2F HID Protocol, section 4.1.4
            SWConstants.CommandNotAllowed => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.U2fHidErrorInvalidCommand),
            SWConstants.InvalidParameter => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.U2fHidErrorInvalidParameter),
            SWConstants.WrongLength => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.U2fHidErrorInvalidLength),
            SWConstants.NoPreciseDiagnosis => GetU2fHidErrorStatusPair(),

            _ => base.StatusCodeMap,
        };

        /// <summary>
        /// Bind a new instance of U2FResponse from the given response APDU
        /// </summary>
        /// <param name="responseApdu">
        /// The response from the YubiKey to the partner Command.
        /// </param>
        public U2fResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// For response APDUs where the Status Word is
        /// <see cref="SWConstants.NoPreciseDiagnosis"/>, this method
        /// translates the U2F HID errors into an appropriate status and
        /// message.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <see cref="Pipelines.FidoTransform"/> receives
        /// a U2F HID error, it will transform it into a response APDU where
        /// <see cref="ResponseApdu.Data"/> contains the original one-byte
        /// error code, and <see cref="ResponseApdu.SW"/> is set to the most
        /// similar value in <see cref="SWConstants"/>. If there isn't a
        /// good match, then the Status Word will be set to
        /// <see cref="SWConstants.NoPreciseDiagnosis"/>.
        /// </para>
        /// <para>
        /// This method examines the original U2F HID error code, and returns
        /// the appropriate status and message which best describe the error.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A status and message which best describe the U2F HID error.
        /// </returns>
        /// <seealso cref="Pipelines.FidoTransform.Invoke(CommandApdu, Type, Type)"/>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.StatusWord"/> is not set
        /// to <see cref="SWConstants.NoPreciseDiagnosis"/>.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the <see cref="YubiKeyResponse.ResponseApdu"/>'s
        /// data field does not contain exactly one byte.
        /// </exception>
        private ResponseStatusPair GetU2fHidErrorStatusPair()
        {
            if (StatusWord != SWConstants.NoPreciseDiagnosis)
            {
                throw new InvalidOperationException(
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidStatusWordMustBeNoPreciseDiagnosis,
                        StatusWord));
            }

            if (ResponseApdu.Data.Length != 1)
            {
                throw new MalformedYubiKeyResponseException(
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidU2fHidErrorCodeLength,
                        ResponseApdu.Data.Length));
            }

            byte errorCode = ResponseApdu.Data.Span[0];

            string responseMessage =
                errorCode switch
                {
                    (byte)U2fHidStatus.Ctap1ErrInvalidSequencing => ResponseStatusMessages.U2fHidErrorInvalidSequence,
                    (byte)U2fHidStatus.Ctap1ErrTimeout => ResponseStatusMessages.U2fHidErrorMessageTimeout,
                    (byte)U2fHidStatus.Ctap1ErrChannelBusy => ResponseStatusMessages.U2fHidErrorChannelBusy,
                    _ => string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            ResponseStatusMessages.U2fHidErrorUnknown,
                            errorCode),
                };

            return new ResponseStatusPair(ResponseStatus.Failed, responseMessage);
        }
    }
}
