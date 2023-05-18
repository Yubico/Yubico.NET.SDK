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
using Yubico.YubiKey.Fido2;

namespace Yubico.YubiKey.Pipelines
{
    internal static class CtapToApduResponse
    {
        /// <summary>
        /// Converts a U2FHID_ERROR response into a ResponseApdu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Takes in the "data" field of a U2FHID_ERROR response, and returns a
        /// response APDU where the data field contains the original error code,
        /// and the status word is the closest matching ISO7816 status word.
        /// </para>
        /// <para>
        /// Supports error codes defined in FIDO U2Fv1.0 section 4.1.4. For all
        /// other error codes, the status word will be set to 0x6F00 (no precise
        /// diagnosis).
        /// </para>
        /// </remarks>
        /// <param name="responseData">
        /// The "data" field of the U2FHID_ERROR response message.
        /// </param>
        /// <returns>
        /// A <see cref="ResponseApdu"/> where <see cref="ResponseApdu.Data"/>
        /// contains the original one-byte U2FHID error code, and
        /// <see cref="ResponseApdu.SW"/> is set to the most appropriate ISO7816
        /// status word (or 0x6F00 "NoPreciseDiagnosis" if there isn't a close
        /// match).
        /// </returns>
        /// <exception cref="MalformedYubiKeyResponseException"></exception>
        public static ResponseApdu ToCtap1ResponseApdu(Span<byte> responseData)
        {
            if (responseData.Length != 1)
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.Ctap2MalformedResponse);
            }

            byte errorCode = responseData[0];

            short statusWord =
                errorCode switch
                {
                    (byte)U2f.U2fHidStatus.Ctap1ErrInvalidCommand => SWConstants.CommandNotAllowed,
                    (byte)U2f.U2fHidStatus.Ctap1ErrInvalidParameter => SWConstants.InvalidParameter,
                    (byte)U2f.U2fHidStatus.Ctap1ErrInvalidLength => SWConstants.WrongLength,
                    _ => SWConstants.NoPreciseDiagnosis,
                };

            return new ResponseApdu(responseData.ToArray(), statusWord);
        }

        /// <summary>
        /// Takes a CTAP2 response message and transforms that into a ResponseApdu that the SDK can understand.
        /// </summary>
        /// <param name="responseData">The CTAP2 response returned by the YubiKey.</param>
        /// <returns>An IOS7186 ResponseApdu that matches the form used by the FIDO2 command layer.</returns>
        /// <remarks>
        /// Unlike U2F responses, which are already formatted as APDUs - CTAP2 responses are of the form `Status || Data`.
        /// Status is a single byte that contains the CTAP2 status of the operation. This means two things: First, an
        /// error response may only be a single byte long. Second, on success, we should expect a leading `00` (the
        /// success status) before the start of the data. This function removes the leading byte from the APDU data and
        /// transfers that value to the SW part of the ResponseAPDU. This is the form that the FIDO2 command layer
        /// expects.
        /// </remarks>
        public static ResponseApdu ToCtap2ResponseApdu(Span<byte> responseData)
        {
            if (responseData.Length == 1)
            {
                return new ResponseApdu(Array.Empty<byte>(), GetSwForCtapError((CtapStatus)responseData[0]));
            }

            return new ResponseApdu(responseData[1..].ToArray(), SWConstants.Success);
        }

        private static short GetSwForCtapError(CtapStatus ctapStatus) =>
            ctapStatus switch
            {
                CtapStatus.Ok => SWConstants.Success,
                _ => unchecked((short)((SW1Constants.NoPreciseDiagnosis << 8) | (byte)ctapStatus))
            };
    }
}
