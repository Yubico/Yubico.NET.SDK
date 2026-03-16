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
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The command class for retrieving the public key from an asymmetric
    /// credential in the YubiHSM Auth application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This command retrieves the public key (PK-OCE) from an asymmetric
    /// credential. This public key is typically used in asymmetric authentication
    /// protocols such as ECC P-256.
    /// </para>
    /// <para>
    /// The partner response class is
    /// <see cref="GetPublicKeyResponse"/>.
    /// </para>
    /// </remarks>
    public sealed class GetPublicKeyCommand : IYubiKeyCommand<GetPublicKeyResponse>
    {
        private const byte GetPublicKeyInstruction = 0x0A;

        private readonly Credential _credential = new Credential();

        /// <inheritdoc/>
        public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

        /// <inheritdoc cref="Credential.Label"/>
        // We're saving to a Credential field so we can leverage its parameter
        // validation.
        public string CredentialLabel
        {
            get => _credential.Label;
            set => _credential.Label = value;
        }

        /// <summary>
        /// Retrieve the public key from an asymmetric credential.
        /// </summary>
        /// <remarks>
        /// This command retrieves the public key (PK-OCE) from the specified
        /// asymmetric credential. The returned key is a 65-byte uncompressed
        /// elliptic curve point (for ECC P-256 credentials).
        /// </remarks>
        /// <param name="credentialLabel">
        /// The label of the credential for which to retrieve the public key. The
        /// string must meet the same requirements as
        /// <see cref="Credential.Label"/>.
        /// </param>
        public GetPublicKeyCommand(string credentialLabel)
        {
            CredentialLabel = credentialLabel;
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = GetPublicKeyInstruction,
            Data = BuildDataField(),
        };

        /// <inheritdoc/>
        public GetPublicKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetPublicKeyResponse(responseApdu);

        /// <summary>
        /// Build the <see cref="CommandApdu.Data"/> field from the given data.
        /// </summary>
        /// <returns>
        /// Data formatted as a TLV.
        /// </returns>
        private byte[] BuildDataField()
        {
            var tlvWriter = new TlvWriter();

            tlvWriter.WriteString(DataTagConstants.Label, CredentialLabel, Encoding.UTF8);

            byte[] tlvBytes = tlvWriter.Encode();
            tlvWriter.Clear();

            return tlvBytes;
        }
    }
}
