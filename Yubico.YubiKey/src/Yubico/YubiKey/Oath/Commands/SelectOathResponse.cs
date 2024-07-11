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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// The response to the <see cref="SelectOathCommand"/> command, containing the YubiKey's OATH application info.
    /// </summary>
    public class SelectOathResponse : OathResponse,
                                      InterIndustry.Commands.ISelectApplicationResponse<OathApplicationData>
    {
        private const byte VersionTag = 0x79;
        private const byte NameTag = 0x71;
        private const byte ChallengeTag = 0x74;
        private const byte AlgorithmTag = 0x7B;

        /// <summary>
        /// Constructs a SelectResponse instance based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The ResponseApdu returned by the YubiKey.
        /// </param>
        public SelectOathResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <summary>
        /// Gets the instance of the <see cref="OathApplicationData"/> class.
        /// </summary>
        /// <returns>
        /// The data in the response APDU, presented as a OATH application info.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="IYubiKeyResponse.Status"/> is not equal to <see cref="ResponseStatus.Success"/>.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data provided does not meet the expectations, and cannot be parsed.
        /// </exception>
        public OathApplicationData GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            FirmwareVersion? version = null;
            ReadOnlyMemory<byte> salt = ReadOnlyMemory<byte>.Empty;
            ReadOnlyMemory<byte> challenge = ReadOnlyMemory<byte>.Empty;
            HashAlgorithm algorithm = HashAlgorithm.Sha1;

            var tlvReader = new TlvReader(ResponseApdu.Data);
            while (tlvReader.HasData)
            {
                switch (tlvReader.PeekTag())
                {
                    case VersionTag:
                        ReadOnlySpan<byte> firmwareValue = tlvReader.ReadValue(VersionTag).Span;
                        version = new FirmwareVersion
                        {
                            Major = firmwareValue[0],
                            Minor = firmwareValue[1],
                            Patch = firmwareValue[2]
                        };

                        break;

                    case NameTag:
                        salt = tlvReader.ReadValue(NameTag);
                        break;

                    case ChallengeTag:
                        challenge = tlvReader.ReadValue(ChallengeTag);
                        break;

                    case AlgorithmTag:
                        algorithm = (HashAlgorithm)tlvReader.ReadByte(AlgorithmTag);
                        break;

                    default:
                        throw new MalformedYubiKeyResponseException()
                        {
                            ResponseClass = nameof(SelectOathResponse),
                            ActualDataLength = ResponseApdu.Data.Length,
                        };
                }
            }

            if (version is null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            return new OathApplicationData(ResponseApdu.Data, version, salt, challenge, algorithm);
        }
    }
}
