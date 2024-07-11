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
using System.Buffers.Binary;
using System.Globalization;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// The response to the <see cref="CalculateCredentialCommand"/> command, containing the response from the oath application.
    /// </summary>
    public class CalculateCredentialResponse : OathResponse, IYubiKeyResponseWithData<Code>
    {
        private const byte FullResponseTag = 0x75;
        private const byte TruncatedResponseTag = 0x76;

        /// <inheritdoc/>
        protected override ResponseStatusPair StatusCodeMap =>
            StatusWord switch
            {
                OathSWConstants.NoSuchObject => new ResponseStatusPair(
                    ResponseStatus.NoData, ResponseStatusMessages.OathNoSuchObject),
                _ => base.StatusCodeMap
            };

        /// <summary>
        /// The credential that was sent to calculate in CalculateCredentialCommand.
        /// </summary>
        public Credential Credential { get; }

        /// <summary> 
        /// Constructs an instance of the <see cref="CalculateCredentialResponse" /> class based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        /// <param name="credential">
        /// The credential that was sent to calculate in CalculateCredentialCommand.
        /// </param>
        public CalculateCredentialResponse(ResponseApdu responseApdu, Credential credential) :
            base(responseApdu)
        {
            Credential = credential;
        }

        /// <summary>
        /// Gets the instance <see cref="Code"/> class.
        /// </summary>
        /// <returns>
        /// The data in the response APDU, presented as one-time password.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="IYubiKeyResponse.Status"/> is not equal to <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data provided does not meet the expectations, and cannot be parsed.
        /// </exception>
        public Code GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            var tlvReader = new TlvReader(ResponseApdu.Data);

            ReadOnlyMemory<byte> bytes = tlvReader.PeekTag() switch
            {
                FullResponseTag => tlvReader.ReadValue(FullResponseTag),
                TruncatedResponseTag => tlvReader.ReadValue(TruncatedResponseTag),
                _ => throw new MalformedYubiKeyResponseException()
                {
                    ResponseClass = nameof(CalculateCredentialResponse),
                    ActualDataLength = ResponseApdu.Data.Length
                }
            };

            if (bytes.Length < 5)
            {
                throw new MalformedYubiKeyResponseException()
                {
                    ResponseClass = nameof(CalculateCredentialResponse),
                    ActualDataLength = ResponseApdu.Data.Length
                };
            }

            int digits = bytes.Span[0];
            Credential.Digits = digits;

            uint otpValue = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(1).Span);
            otpValue %= (uint)Math.Pow(x: 10, digits);
            string response = otpValue.ToString(CultureInfo.InvariantCulture).PadLeft(digits, paddingChar: '0');

            if (Credential.Period is null)
            {
                Credential.Period = Credential.Type == CredentialType.Totp
                    ? CredentialPeriod.Period30
                    : CredentialPeriod.Undefined;
            }

            return new Code(response, (CredentialPeriod)Credential.Period);
        }
    }
}
