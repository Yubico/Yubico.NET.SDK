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
using System.Globalization;
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The command class for calculating session keys from an ECC P-256
    /// credential. These session keys are used to establish a secure session
    /// with a YubiHSM 2 device.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some steps must be performed prior to calling this command. First,
    /// retrieve a 65-byte host challenge from the YubiKey device, and
    /// retrieve a 65-byte challenge from the YubiHSM 2 device.
    /// These two challenges are combined into a single 130-byte context
    /// and then used to construct this command.
    /// </para>
    /// <para>
    /// There is a limit of 8 attempts to authenticate with the credential's
    /// password before the credential is deleted. Once the credential is
    /// deleted, it cannot be recovered. Supplying the correct password before the
    /// credential is deleted will reset the retry counter to 8.
    /// </para>
    /// <para>
    /// If the credential requires touch (see <see cref="Credential.TouchRequired"/>),
    /// then the user must also touch the YubiKey as part of the authentication
    /// procedure. See <see cref="GetEccP256SessionKeysResponse"/> for more
    /// information on response statuses.
    /// </para>
    /// <para>
    /// The secure session protocol is based on Secure Channel Protocol 3
    /// (SCP03). The session keys returned by the application are the
    /// Session Secure Channel Encryption Key (S-ENC),
    /// Secure Channel Message Authentication Code Key for Command (S-MAC),
    /// and Secure Channel Message Authentication Code Key for Response
    /// (S-RMAC). These session-specific keys are used to encrypt and
    /// authenticate commands and responses with a YubiHSM 2 device during
    /// a single session. The session keys are discarded afterwards.
    /// </para>
    /// <para>
    /// The partner response class is
    /// <see cref="GetEccP256SessionKeysResponse"/>.
    /// </para>
    /// </remarks>
    public sealed class GetEccP256SessionKeysCommand : IYubiKeyCommand<GetEccP256SessionKeysResponse>
    {
        private const byte GetSessionKeysInstruction = 0x03;

        private readonly Credential _credential = new Credential();
        private readonly ReadOnlyMemory<byte> _credentialPassword;
        private readonly ReadOnlyMemory<byte> _hostChallenge;
        private readonly ReadOnlyMemory<byte> _hsmDeviceChallenge;
        private readonly ReadOnlyMemory<byte> _cardPublicKey;
        private readonly ReadOnlyMemory<byte> _cardCrypto;

        /// <summary>
        /// The challenge must be exactly 65 bytes.
        /// </summary>
        /// <remarks>
        /// The host challenge and HSM device challenge are both supplied to
        /// the constructor, and each challenge must meet this length
        /// requirement.
        /// </remarks>
        public const int RequiredChallengeLength = 65;

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
        /// Calculate session keys from an ECC P-256 credential. These session
        /// keys are used to encrypt and authenticate commands and responses
        /// with a YubiHSM 2 device during a single session.
        /// </summary>
        /// <remarks>
        /// The caller is responsible for controlling the buffers which hold
        /// the <paramref name="credentialPassword"/>,
        /// <paramref name="hostChallenge"/>, <paramref name="hsmDeviceChallenge"/>,
        /// <paramref name="cardPublicKey"/>, and
        /// <paramref name="cardCrypto"/>. The caller should overwrite
        /// the data after the command is sent. The user's manual entry
        /// <xref href="UsersManualSensitive">"Sensitive Data"</xref> has further
        /// details and recommendations for handling this kind of data.
        /// </remarks>
        /// <param name="credentialLabel">
        /// The label of the credential for calculating the session keys. The
        /// string must meet the same requirements as
        /// <see cref="Credential.Label"/>.
        /// </param>
        /// <param name="credentialPassword">
        /// The password of the credential for calculating the session keys.
        /// It must meet the same requirements as
        /// <see cref="CredentialWithSecrets.CredentialPassword"/>.
        /// </param>
        /// <param name="hostChallenge">
        /// The 65 byte challenge generated by the host.
        /// </param>
        /// <param name="hsmDeviceChallenge">
        /// The 65 byte challenge generated by the YubiHSM 2 device.
        /// </param>
        /// <param name="cardPublicKey">
        /// The 65-byte public key from the YubiHSM 2 device (uncompressed EC point for P-256).
        /// </param>
        /// <param name="cardCrypto">
        /// The cryptographic response from the YubiHSM 2 device (16 bytes).
        /// </param>
        public GetEccP256SessionKeysCommand(string credentialLabel,
            ReadOnlyMemory<byte> credentialPassword,
            ReadOnlyMemory<byte> hostChallenge,
            ReadOnlyMemory<byte> hsmDeviceChallenge,
            ReadOnlyMemory<byte> cardPublicKey,
            ReadOnlyMemory<byte> cardCrypto)
        {
            CredentialLabel = credentialLabel;

            _credentialPassword =
                credentialPassword.Length == CredentialWithSecrets.RequiredCredentialPasswordLength
                ? credentialPassword
                : throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidPasswordLength,
                        credentialPassword.Length));

            _hostChallenge = hostChallenge.Length == RequiredChallengeLength
                ? hostChallenge
                : throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidHostChallengeLength,
                        hostChallenge.Length));

            _hsmDeviceChallenge = hsmDeviceChallenge.Length == RequiredChallengeLength
                ? hsmDeviceChallenge
                : throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidHsmDeviceChallengeLength,
                        hsmDeviceChallenge.Length));

            _cardPublicKey = cardPublicKey;
            _cardCrypto = cardCrypto;
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = GetSessionKeysInstruction,
            Data = BuildDataField(),
        };

        /// <inheritdoc/>
        public GetEccP256SessionKeysResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetEccP256SessionKeysResponse(responseApdu);

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

            byte[] context = new byte[130];
            _hostChallenge.CopyTo(context.AsMemory(0));
            _hsmDeviceChallenge.CopyTo(context.AsMemory(65));
            tlvWriter.WriteValue(DataTagConstants.Context, context);

            if (_cardPublicKey.Length > 0)
            {
                tlvWriter.WriteValue(DataTagConstants.PubKey, _cardPublicKey.Span);
            }

            if (_cardCrypto.Length > 0)
            {
                tlvWriter.WriteValue(DataTagConstants.Response, _cardCrypto.Span);
            }

            tlvWriter.WriteValue(DataTagConstants.Password, _credentialPassword.Span);

            byte[] tlvBytes = tlvWriter.Encode();
            tlvWriter.Clear();

            return tlvBytes;
        }
    }
}
