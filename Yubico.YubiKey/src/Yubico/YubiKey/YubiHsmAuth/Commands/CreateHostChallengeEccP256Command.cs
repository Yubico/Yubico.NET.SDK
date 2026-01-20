// Copyright 2026 Yubico AB
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
using System.Globalization;
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The command class for getting a challenge (EPK-OCE) for ECC P-256 credential.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This command sends the GET_CHALLENGE instruction (0x04) to the YubiKey device
    /// to retrieve the public part of a newly generated ephemeral ECC SECP256R1 key.
    /// For asymmetric (ECC) credentials, this returns a 65-byte uncompressed public key.
    /// </para>
    /// <para>
    /// The associated response class is <see cref="CreateHostChallengeEccP256Response"/>.
    /// </para>
    /// </remarks>
    public sealed class CreateHostChallengeEccP256Command : IYubiKeyCommand<CreateHostChallengeEccP256Response>
    {
        private const byte GetChallengeInstruction = 0x04;
        private readonly Credential _credential = new Credential();
        private readonly ReadOnlyMemory<byte> _credentialPassword;

        /// <summary>
        /// Gets the <see cref="YubiKeyApplication"/> to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.YubiHsmAuth"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

        /// <inheritdoc cref="Credential.Label"/>
        public string CredentialLabel
        {
            get => _credential.Label;
            set => _credential.Label = value;
        }

        /// <summary>
        /// Constructs an instance of the <see cref="CreateHostChallengeEccP256Command"/> class.
        /// </summary>
        /// <remarks>
        /// The <see cref="CredentialLabel"/> will need to be set before calling
        /// <see cref="CreateCommandApdu"/>.
        /// </remarks>
        public CreateHostChallengeEccP256Command()
        {
        }

        /// <summary>
        /// Constructs an instance of the <see cref="CreateHostChallengeEccP256Command"/> class
        /// with the credential.
        /// </summary>
        /// <param name="credential">
        /// The ECC P-256 credential for which to get the challenge.
        /// </param>
        public CreateHostChallengeEccP256Command(EccP256CredentialWithSecrets credential)
        {
            CredentialLabel = credential.Label;
            _credentialPassword = credential.CredentialPassword;
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = GetChallengeInstruction,
            Data = BuildDataField(),
        };

        /// <inheritdoc/>
        public CreateHostChallengeEccP256Response CreateResponseForApdu(ResponseApdu responseApdu) =>
            new CreateHostChallengeEccP256Response(responseApdu);
        public byte[] BuildDataField()
        {
            var tlvWriter = new TlvWriter();

            tlvWriter.WriteString(DataTagConstants.Label, CredentialLabel, Encoding.UTF8);
            tlvWriter.WriteValue(DataTagConstants.Password, _credentialPassword.Span);

            byte[] tlvBytes = tlvWriter.Encode();
            tlvWriter.Clear();

            return tlvBytes;
        }
    }
}
