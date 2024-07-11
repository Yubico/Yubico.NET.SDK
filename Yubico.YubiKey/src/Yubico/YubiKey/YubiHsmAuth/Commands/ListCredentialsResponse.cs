// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The response to the <see cref="ListCredentialsCommand"/> command, containing
    /// the credentials present in the YubiHSM Auth application, and the number
    /// of retries remaining for each.
    /// </summary>
    public sealed class ListCredentialsResponse :
        BaseYubiHsmAuthResponse,
        IYubiKeyResponseWithData<List<CredentialRetryPair>>
    {
        private readonly Index CryptoKeyTypeIndex = 0;
        private readonly Index TouchIndex = 1;
        private readonly Range LabelRange = 2..^1;
        private readonly Index RetryIndex = ^1;

        // CryptoKeyType (1) + Touch (1) + Retry (1) + Label (min/max)
        private const int MinElementSize = 3 + Credential.MinLabelByteCount;
        private const int MaxElementSize = 3 + Credential.MaxLabelByteCount;

        /// <summary>
        /// Constructs a ListCredentialsResponse instance based on a ResponseApdu
        /// received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public ListCredentialsResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the list of <see cref="Credential"/>s present in the YubiHSM Auth
        /// application, and the number of retries remaining.
        /// </summary>
        /// <returns>
        /// The data in the response APDU, as a list of credentials and retry count.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the <see cref="IYubiKeyResponse.Status"/> is not equal to
        /// <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public List<CredentialRetryPair> GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            List<CredentialRetryPair> credentialRetryPairs = new List<CredentialRetryPair>();

            var tlvReader = new TlvReader(ResponseApdu.Data);

            // Parse data by iterating over each LabelList element, parsing it into a
            // CredentialRetryPair, and adding it to the returned List.
            while (tlvReader.HasData)
            {
                int nextTagValue = tlvReader.PeekTag();

                if (nextTagValue != DataTagConstants.LabelList)
                {
                    throw new MalformedYubiKeyResponseException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidDataTag,
                            nextTagValue));
                }

                ReadOnlySpan<byte> credentialRetryElement =
                    tlvReader.ReadValue(DataTagConstants.LabelList).Span;

                // Check that it's formatted correctly
                if (credentialRetryElement.Length < MinElementSize
                    || credentialRetryElement.Length > MaxElementSize)
                {
                    throw new MalformedYubiKeyResponseException(
                        ExceptionMessages.InvalidCredentialRetryDataLength);
                }

                Credential credential = new Credential(
                    (CryptographicKeyType)credentialRetryElement[CryptoKeyTypeIndex],
                    Encoding.UTF8.GetString(credentialRetryElement[LabelRange].ToArray()),
                    credentialRetryElement[TouchIndex] != 0);

                int retries = credentialRetryElement[RetryIndex];

                credentialRetryPairs.Add(new CredentialRetryPair(credential, retries));
            }

            return credentialRetryPairs;
        }
    }
}
