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
    /// The command class for getting a challenge for credential authentication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This command sends the GET_CHALLENGE instruction (0x04) to the YubiKey device
    /// to retrieve a challenge based on the credential type.
    /// </para>
    /// <para>
    /// For symmetric credentials this generates an 8-byte 'Host Challenge', a random value
    /// used for authentication with AES-based symmetric key protocols.
    /// </para>
    /// <para>
    /// For asymmetric credentials this returns 'EPK-OCE', the public part of a newly generated
    /// ephemeral ECC SECP256R1 key (65 bytes uncompressed).
    /// </para>
    /// <para>
    /// The associated response class is <see cref="CreateHostChallengeResponse"/>.
    /// </para>
    /// </remarks>
    public sealed class CreateHostChallengeCommand : IYubiKeyCommand<CreateHostChallengeResponse>
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
        /// <inheritdoc cref="CryptographicKeyType"/>
        public CryptographicKeyType KeyType
        {
            get => _credential.KeyType;
            set => _credential.KeyType = value;
        }

        /// <summary>
        /// Constructs an instance of the <see cref="CreateHostChallengeCommand"/> class.
        /// </summary>
        /// <remarks>
        /// The <see cref="CredentialLabel"/> will need to be set before calling
        /// <see cref="CreateCommandApdu"/>.
        /// </remarks>
        public CreateHostChallengeCommand()
        {
        }

        /// <summary>
        /// Constructs an instance of the <see cref="CreateHostChallengeCommand"/> class
        /// with the credential.
        /// </summary>
        /// <param name="credentialLabel">
        /// The <see cref="Credential"/> for which to get the challenge.
        /// </param>
        /// <param name="keytype">
        /// The type of cryptographic key.
        /// </param>
        /// <param name="credentialPassword">
        /// The password for the credential.
        /// </param>
        public CreateHostChallengeCommand(CryptographicKeyType keytype, string credentialLabel, ReadOnlyMemory<byte>? credentialPassword = null)
        {
            CredentialLabel = credentialLabel;
            KeyType = keytype;
            if (credentialPassword == null)
            {
                _credentialPassword = ReadOnlyMemory<byte>.Empty;
            }
            else
            {
                _credentialPassword = credentialPassword.Value;
            }
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = GetChallengeInstruction,
            Data = BuildDataField(),
        };

        /// <inheritdoc/>
        public CreateHostChallengeResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new CreateHostChallengeResponse(responseApdu);
        private byte[] BuildDataField()
        {
            var tlvWriter = new TlvWriter();
            tlvWriter.WriteString(DataTagConstants.Label, CredentialLabel, Encoding.UTF8);

            if (_credential.KeyType is CryptographicKeyType.SecP256R1 && _credentialPassword.Length > 0)
            {
                tlvWriter.WriteValue(DataTagConstants.Password, _credentialPassword.Span);

            }
            
            byte[] tlvBytes = tlvWriter.Encode();
            tlvWriter.Clear();

            return tlvBytes;
        }
    }
}
