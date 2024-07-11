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
    ///     The response to the <see cref="ChallengeResponseCommand" /> command, containing the YubiKey's
    ///     response to the issued challenge.
    /// </summary>
    public class ChallengeResponseResponse : OtpResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        private const int HmacResponseLength = 20;
        private const int YubicoOtpResponseLength = 16;

        private readonly ChallengeResponseAlgorithm _algorithm;

        /// <summary>
        ///     Constructs a <see cref="ChallengeResponseResponse" /> instance based on a <see cref="ResponseApdu" />
        ///     received from the YubiKey, and the algorithm requested for generating the response.
        /// </summary>
        /// <param name="responseApdu">The <see cref="ResponseApdu" /> returned by the YubiKey.</param>
        /// <param name="algorithm">
        ///     The algorithm used when the <see cref="ChallengeResponseCommand" /> was sent.
        /// </param>
        public ChallengeResponseResponse(ResponseApdu responseApdu, ChallengeResponseAlgorithm algorithm) :
            base(responseApdu)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        ///     Gets the response to the issued challenge.
        /// </summary>
        /// <returns>
        ///     The response to the challenge. The size of the response will be 16 bytes if the YubicoOtp
        ///     algorithm was used, or 20 bytes if the HMAC-SHA1 algorithm was used.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when <see cref="YubiKeyResponse.Status" /> is not <see cref="ResponseStatus.Success" />.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        ///     Thrown when the data received from the YubiKey does not
        ///     match the expectations of the parser.
        /// </exception>
        public ReadOnlyMemory<byte> GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            int expectedLength = _algorithm switch
            {
                ChallengeResponseAlgorithm.YubicoOtp => YubicoOtpResponseLength,
                ChallengeResponseAlgorithm.HmacSha1 => HmacResponseLength,
                _ => throw new InvalidOperationException(ExceptionMessages.InvalidOtpChallengeResponseAlgorithm)
            };

            if (ResponseApdu.Data.Length < expectedLength)
            {
                throw new MalformedYubiKeyResponseException
                {
                    ResponseClass = nameof(ChallengeResponseResponse),
                    ActualDataLength = ResponseApdu.Data.Length
                };
            }

            return ResponseApdu.Data.Slice(start: 0, expectedLength);
        }
    }
}
