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
    /// Validates authentication (mutually).
    /// The challenge for this comes from the SelectOathResponse.
    /// The response computed by performing the correct HMAC function of that challenge with the correct secret.
    /// A new challenge is then sent to the application along with the response.
    /// The application will then respond with a similar calculation that the host software can verify.
    /// </summary>
    public class ValidateCommand : OathChallengeResponseBaseCommand, IYubiKeyCommand<ValidateResponse>
    {
        private const byte ValidateInstruction = 0xA3;
        private const byte ChallengeTag = 0x74;
        private const byte ResponseTag = 0x75;

        /// <summary>
        /// Gets the OATH application information.
        /// </summary>
        private readonly OathApplicationData _oathData;

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>
        /// A user-supplied password to validate.
        /// </value>
        private readonly ReadOnlyMemory<byte> _password;

        /// <summary>
        /// Gets and privately sets.
        /// </summary>
        /// <value>
        /// The response that is calculated with a new generated challenge.
        /// </value>
        public ReadOnlyMemory<byte> CalculatedResponse { get; private set; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Oath
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Oath;

        /// <summary>
        /// Constructs an instance of the <see cref="ValidateCommand" /> class.
        /// </summary>
        /// /// <param name="password">
        /// The user-supplied password to validate.
        /// </param>
        /// <param name="oathData">
        /// An implementation of <c>OathApplicationData</c>.
        /// </param>
        public ValidateCommand(ReadOnlyMemory<byte> password, OathApplicationData oathData)
        {
            if (oathData is null)
            {
                throw new ArgumentNullException(nameof(oathData));
            }

            _password = password;
            _oathData = oathData;
            CalculatedResponse = ReadOnlyMemory<byte>.Empty;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            byte[] secret = CalculateSecret(_password, _oathData.Salt);
            byte[] response = CalculateResponse(secret, _oathData.Challenge);
            byte[] challenge = GenerateRandomChallenge();

            CalculatedResponse = CalculateResponse(secret, challenge);

            var tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(ResponseTag, response);
            tlvWriter.WriteValue(ChallengeTag, challenge);

            return new CommandApdu
            {
                Ins = ValidateInstruction,
                Data = tlvWriter.Encode()
            };
        }

        /// <inheritdoc />
        public ValidateResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ValidateResponse(responseApdu, CalculatedResponse);
    }
}
