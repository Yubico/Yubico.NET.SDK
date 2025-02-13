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
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// The response to the <see cref="ValidateCommand"/> command, containing the response from the oath application.
    /// </summary>
    public class ValidateResponse : OathResponse, IYubiKeyResponseWithData<bool>
    {
        private const byte ResponseTag = 0x75;

        /// <inheritdoc/>
        protected override ResponseStatusPair StatusCodeMap =>
           StatusWord switch
           {
               OathSWConstants.NoSuchObject => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.OathAuthNotEnabled),
               _ => base.StatusCodeMap,
           };
        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <value>
        /// The response that was calculated with a new generated challenge.
        /// </value>
        public ReadOnlyMemory<byte> Response { get; }

        /// <summary>
        /// Constructs a ValidateResponse instance based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        /// <param name="calculatedResponse">
        /// The response that was calculated with a new generated challenge in ValidateComand.
        /// </param>
        public ValidateResponse(ResponseApdu responseApdu, ReadOnlyMemory<byte> calculatedResponse) :
            base(responseApdu)
        {
            Response = calculatedResponse;
        }

        /// <summary>
        /// Gets the response data.
        /// </summary>
        /// <returns>
        /// True if validation succeeded.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="IYubiKeyResponse.Status"/> is not equal to <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public bool GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            var tlvReader = new TlvReader(ResponseApdu.Data);
            var value = tlvReader.ReadValue(ResponseTag);

            return value.Span.SequenceEqual(Response.Span);
        }
    }
}

