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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// The response to the <see cref="CalculateAllCredentialsCommand"/> command, containing the response from the oath application.
    /// </summary>
    public class CalculateAllCredentialsResponse : OathResponse, IYubiKeyResponseWithData<IDictionary<Credential, Code>>
    {
        private const byte FullResponseTag = 0x75;
        private const byte TruncatedResponseTag = 0x76;
        private const byte HotpTag = 0x77;
        private const byte TouchTag = 0x7C;
        private const byte NameTag = 0x71;

        /// <summary>
        /// Constructs an instance of the <see cref="CalculateAllCredentialsResponse" /> class based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public CalculateAllCredentialsResponse(ResponseApdu responseApdu) :
             base(responseApdu)
        {

        }

        /// <summary>
        /// Gets the dictionary of <see cref="Credential"/> and <see cref="Code"/> pair.
        /// </summary>
        /// <returns>
        /// Returns name + response for TOTP and just name for HOTP and credentials requiring touch.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="IYubiKeyResponse.Status"/> is not equal to <see cref="ResponseStatus.Success"/>
        /// or the credential's period is invalid.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data provided does not meet the expectations, and cannot be parsed.
        /// </exception>
        public IDictionary<Credential, Code> GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            var calculatedCredentials = new Dictionary<Credential, Code>();

            if (ResponseApdu.Data.Length == 0)
            {
                return calculatedCredentials;
            }

            var tlvReader = new TlvReader(ResponseApdu.Data);

            while (tlvReader.HasData)
            {
                (string? OtpString, int? Digits) response = (null, null);
                var credentialType = CredentialType.Totp;
                bool requiresTouch = false;

                string label = tlvReader.PeekTag() switch
                {
                    NameTag => tlvReader.ReadString(NameTag, Encoding.UTF8),
                    _ => throw new MalformedYubiKeyResponseException()
                    {
                        ResponseClass = nameof(CalculateAllCredentialsResponse),
                        ActualDataLength = ResponseApdu.Data.Length,
                    }
                };

                switch (tlvReader.PeekTag())
                {
                    case HotpTag:
                        _ = tlvReader.ReadByte(HotpTag);
                        credentialType = CredentialType.Hotp;
                        break;

                    case TouchTag:
                        _ = tlvReader.ReadByte(TouchTag);
                        requiresTouch = true;
                        break;

                    case FullResponseTag:
                        var fullValue = tlvReader.ReadValue(FullResponseTag);
                        response = GetOtpValue(fullValue);
                        break;

                    case TruncatedResponseTag:
                        var truncatedValue = tlvReader.ReadValue(TruncatedResponseTag);
                        response = GetOtpValue(truncatedValue);
                        break;

                    default:
                        throw new MalformedYubiKeyResponseException()
                        {
                            ResponseClass = nameof(CalculateAllCredentialsResponse),
                            ActualDataLength = ResponseApdu.Data.Length,
                        };
                }

                var credential = FromLabelAndType(label, credentialType);
                credential.RequiresTouch = requiresTouch;
                credential.Digits = response.Digits;

                if (credential.Period is null)
                {
                    throw new InvalidOperationException(ExceptionMessages.InvalidCredentialPeriod);
                }

                var code = new Code(response.OtpString, (CredentialPeriod)credential.Period);

                calculatedCredentials.Add(credential, code);
            }

            return calculatedCredentials;
        }

        private static (string otpString, int digits) GetOtpValue(ReadOnlyMemory<byte> value)
        {
            if (value.Length < 5)
            {
                throw new MalformedYubiKeyResponseException()
                {
                    ResponseClass = nameof(CalculateCredentialResponse),
                    ActualDataLength = value.Length,
                };
            }

            int digits = value.Span[0];
            uint otpValue = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(1).Span);
            otpValue %= (uint)Math.Pow(10, digits);
            string otpString = otpValue.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');

            return (otpString, digits);
        }

        private static Credential FromLabelAndType(string label, CredentialType type)
        {
            if (label is null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            (var credentialPeriod, string? issuer, string account) = Credential.ParseLabel(label, type);
            return new Credential(issuer, account, type, credentialPeriod);
        }
    }
}

