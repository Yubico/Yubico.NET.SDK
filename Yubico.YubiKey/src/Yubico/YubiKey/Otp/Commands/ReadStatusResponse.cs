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

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// A shared response for many OTP commands, containing the YubiKey's current OTP application
    /// status. This is often used to verify whether a configuration was successfully applied or not.
    /// </summary>
    public class ReadStatusResponse : OtpResponse, IYubiKeyResponseWithData<OtpStatus>
    {
        private const int OtpStatusLength = 6;

        private const byte ShortPressValidMask = 0b0000_0001;
        private const byte LongPressValidMask = 0b0000_0010;
        private const byte ShortPressTouchMask = 0b0000_0100;
        private const byte LongPressTouchMask = 0b0000_1000;
        private const byte LedInvertedMask = 0b0001_0000;

        public ReadStatusResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the OTP application status.
        /// </summary>
        /// <returns>The data in the ResponseAPDU, presented as an OtpStatus class.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data received from the YubiKey does not
        /// match the expectations of the parser.
        /// </exception>
        public OtpStatus GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            if (ResponseApdu.Data.Length != OtpStatusLength)
            {
                throw new MalformedYubiKeyResponseException()
                {
                    ResponseClass = nameof(ReadStatusResponse),
                    ExpectedDataLength = OtpStatusLength,
                    ActualDataLength = ResponseApdu.Data.Length
                };
            }

            ReadOnlySpan<byte> responseApduData = ResponseApdu.Data.Span;

            return new OtpStatus
            {
                FirmwareVersion = new FirmwareVersion
                {
                    Major = responseApduData[0],
                    Minor = responseApduData[1],
                    Patch = responseApduData[2]
                },
                SequenceNumber = responseApduData[3],
                ShortPressConfigured = (responseApduData[4] & ShortPressValidMask) != 0,
                LongPressConfigured = (responseApduData[4] & LongPressValidMask) != 0,
                ShortPressRequiresTouch = (responseApduData[4] & ShortPressTouchMask) != 0,
                LongPressRequiresTouch = (responseApduData[4] & LongPressTouchMask) != 0,
                LedBehaviorInverted = (responseApduData[4] & LedInvertedMask) != 0,
                TouchLevel = responseApduData[5],
            };
        }
    }
}
