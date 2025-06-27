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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// The response to the <see cref="ListCommand"/> command, containing the YubiKey's
    /// configured credentials list.
    /// </summary>
    public class ListResponse : OathResponse, IYubiKeyResponseWithData<List<Credential>>
    {
        private const byte NameListTag = 0x72;

        /// <summary>
        /// Constructs a ListResponse instance based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public ListResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the list of <see cref="Credential"/> objects.
        /// </summary>
        /// <returns>
        /// The list of credentials.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="IYubiKeyResponse.Status"/> is not equal to <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data provided does not meet the expectations, and cannot be parsed.
        /// </exception>
        public List<Credential> GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            if (ResponseApdu.Data.Length == 0)
            {
                return new List<Credential>();
            }

            var credentialList = new List<Credential>();

            var tlvReader = new TlvReader(ResponseApdu.Data);

            while (tlvReader.HasData)
            {
                switch (tlvReader.PeekTag())
                {
                    case NameListTag:
                        credentialList.Add(_GetCredential(tlvReader.ReadValue(NameListTag)));
                        break;
                    default:
                        throw new MalformedYubiKeyResponseException()
                        {
                            ResponseClass = nameof(ListResponse),
                            ActualDataLength = ResponseApdu.Data.Length,
                        };
                }
            }

            return credentialList;
        }

        /// <returns>
        /// Credential presented as a type, algorithm and name as "issuer:account".
        /// </returns>
        private static Credential _GetCredential(ReadOnlyMemory<byte> value)
        {
            ThrowIfNotLength(value, 2);

            byte algorithmType = value.Span[0];
            var algorithm = (HashAlgorithm)(algorithmType & 0x0F);
            var type = (CredentialType)(algorithmType & 0xF0);
            string label = Encoding.UTF8.GetString(value.Slice(1).ToArray());
            (var credentialPeriod, string? issuer, string account) = Credential.ParseLabel(label, type);

            return new Credential(issuer, account, credentialPeriod, type, algorithm);
        }

        private static void ThrowIfNotLength(ReadOnlyMemory<byte> value, int minLength)
        {
            if (value.Length < minLength)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.ValueConversionFailed,
                        minLength,
                        value.Length));
            }
        }
    }
}

