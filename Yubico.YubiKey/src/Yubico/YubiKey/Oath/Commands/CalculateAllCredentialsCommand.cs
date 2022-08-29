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
    /// Performs CALCULATE of OTP (One-Time Password) values for all available credentials on the YubiKey.
    /// </summary>
    public class CalculateAllCredentialsCommand : OathChallengeResponseBaseCommand, IYubiKeyCommand<CalculateAllCredentialsResponse>
    {
        private const byte CalculateAllInstruction = 0xA4;
        private const byte ChallengeTag = 0x74;
        
        /// <summary>
        /// Full or truncated response to receive back.
        /// </summary>
        /// <value>
        /// The default value for the response is truncated.
        /// </value>
        public ResponseFormat ResponseFormat { get; set; } = ResponseFormat.Truncated;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Oath
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Oath;

        /// <summary>
        /// Constructs an instance of the <see cref="CalculateAllCredentialsCommand" /> class.
        /// The ResponseFormat will be set to its default value which is truncated.
        /// </summary>
        public CalculateAllCredentialsCommand()
        {
        }

        /// <summary>
        /// Constructs an instance of the <see cref="CalculateAllCredentialsCommand" /> class.
        /// </summary>
        /// <param name="responseFormat">
        /// Full or truncated response to receive back.
        /// </param>
        public CalculateAllCredentialsCommand(ResponseFormat responseFormat)
        {
            ResponseFormat = responseFormat;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var tlvWriter = new TlvWriter();
            
            // Using default period which is 30 seconds for calculating all credentials.
            // Credentials that have different period are recalculated later in CalculateAllCredentialsResponse.
            tlvWriter.WriteValue(ChallengeTag, GenerateTotpChallenge(CredentialPeriod.Period30));

            return new CommandApdu
            {
                Ins = CalculateAllInstruction,
                P2 = (byte)ResponseFormat,
                Data = tlvWriter.Encode()
            };
        }

        /// <inheritdoc />
        public CalculateAllCredentialsResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new CalculateAllCredentialsResponse(responseApdu);
    }
}


