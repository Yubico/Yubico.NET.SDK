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
    /// Configures Authentication. If length 0 is sent, authentication is removed.
    /// The key to be set is expected to be a user-supplied UTF-8 encoded password passed through 1000 rounds of PBKDF2 with the ID from SelectOathResponse used as salt.
    /// 16 bytes of that are used. When configuring authentication you are required to send an 8 byte challenge and one authentication-response with that key,
    /// in order to confirm that the application and the host software can calculate the same response for that key.
    /// </summary>
    public class SetPasswordCommand : OathChallengeResponseBaseCommand, IYubiKeyCommand<SetPasswordResponse>
    {
        private const byte SetPasswordInstruction = 0x03;
        private const byte SecretTag = 0x73;
        private const byte ChallengeTag = 0x74;
        private const byte ResponseTag = 0x75;

        /// <summary>
        /// Gets the OATH application information.
        /// </summary>
        public OathApplicationData OathData { get; }

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>
        /// A user-supplied password to validate.
        /// </value>
        public ReadOnlyMemory<byte> Password { get; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Oath
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Oath;

        // We explicitly do not want a default constructor for this command.
        private SetPasswordCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs an instance of the <see cref="SetPasswordCommand" /> class.
        /// </summary>
        /// <param name="password">
        /// The user-supplied password to validate.
        /// </param>
        /// <param name="oathData">
        ///  An implementation of <c>OathApplicationData</c>.
        /// </param>
        public SetPasswordCommand(ReadOnlyMemory<byte> password, OathApplicationData oathData)
        {
            if (oathData is null)
            {
                throw new ArgumentNullException(nameof(oathData));
            }

            Password = password;
            OathData = oathData;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var tlvWriter = new TlvWriter();

            if (Password.Length == 0)
            {
                tlvWriter.WriteValue(SecretTag, null);
            }
            else
            {
                byte[] secret = CalculateSecret(Password, OathData.Salt);
                byte[] challenge = GenerateChallenge();
                byte[] response = CalculateResponse(secret, challenge);

                byte[] fullKey = new byte[1 + secret.Length];
                fullKey[0] = (byte)HashAlgorithm.Sha1;
                Array.Copy(secret, 0, fullKey, 1, secret.Length);

                tlvWriter.WriteValue(SecretTag, fullKey);
                tlvWriter.WriteValue(ChallengeTag, challenge);
                tlvWriter.WriteValue(ResponseTag, response);
            }

            return new CommandApdu
            {
                Ins = SetPasswordInstruction,
                Data = tlvWriter.Encode()
            };
        }

        /// <inheritdoc />
        public SetPasswordResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new SetPasswordResponse(responseApdu);
    }
}
