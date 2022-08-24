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
using System.Globalization;
using System.Text;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// The command class for adding a credential to the YubiHSM Auth
    /// application.
    /// </summary>
    /// <remarks>
    /// The partner class is <see cref="AddCredentialResponse"/>. See
    /// <see cref="CredentialWithSecrets"/> for further information on the
    /// requirements of the new credential.
    /// </remarks>
    public sealed class AddCredentialCommand : IYubiKeyCommand<AddCredentialResponse>
    {
        private const byte AddCredentialInstruction = 0x01;

        private readonly ReadOnlyMemory<byte> _managementKey;
        private readonly CredentialWithSecrets _credentialWithSecrets;

        /// <summary>
        /// The management key must be exactly 16 bytes.
        /// </summary>
        /// <remarks>
        /// The management key is supplied as an argument to the constructor
        /// <see cref="AddCredentialCommand(ReadOnlyMemory{byte}, CredentialWithSecrets)"/>.
        /// </remarks>
        public const int ValidManagementKeyLength = 16;

        /// <inheritdoc/>
        public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

        /// <summary>
        /// Add a credential to the YubiHSM Auth application.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The application can store up to 32 credentials, and each credential
        /// must have a unique label. See <see cref="Credential.Label"/> for
        /// more information on encodings and requirements.
        /// </para>
        /// <para>
        /// To list the credentials currently stored in the application, use
        /// <see cref="ListCredentialsCommand"/>.
        /// </para>
        /// <para>
        /// The caller is responsible for controlling the buffer which holds
        /// the management key, and should overwrite the data after the command
        /// is sent. The user's manual entry
        /// <xref href="UsersManualSensitive">"Sensitive Data"</xref> has further
        /// details and recommendations for handling this kind of data.
        /// </para>
        /// </remarks>
        /// <param name="managementKey">
        /// The secret used to authenticate to the application prior to adding
        /// or removing credentials. See <see cref="ValidManagementKeyLength"/>
        /// for its required length. The application has a default management
        /// key of all zeros.
        /// </param>
        /// <param name="credentialWithSecrets">
        /// The credential to be added.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="managementKey"/> has an invalid length.
        /// </exception>
        public AddCredentialCommand(ReadOnlyMemory<byte> managementKey,
            CredentialWithSecrets credentialWithSecrets)
        {
            _managementKey = managementKey.Length == ValidManagementKeyLength
                ? managementKey
                : throw new ArgumentOutOfRangeException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidMgmtKeyLength,
                        managementKey.Length));

            _credentialWithSecrets = credentialWithSecrets;
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = AddCredentialInstruction,
            Data = BuildDataField(),
        };

        /// <inheritdoc/>
        public AddCredentialResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new AddCredentialResponse(responseApdu);

        /// <summary>
        /// Build the <see cref="CommandApdu.Data"/> field from the given data.
        /// </summary>
        /// <returns>
        /// Data formatted as a TLV.
        /// </returns>
        private byte[] BuildDataField()
        {
            TlvWriter tlvWriter = new TlvWriter();
            tlvWriter.WriteValue(DataTagConstants.ManagementKey, _managementKey.Span);
            tlvWriter.WriteString(DataTagConstants.Label,
                _credentialWithSecrets.Label, Encoding.UTF8);

            WriteCryptographicKeyType(tlvWriter);
            WriteKeys(tlvWriter);
            WriteCredentialPassword(tlvWriter);

            tlvWriter.WriteByte(DataTagConstants.Touch,
                _credentialWithSecrets.TouchRequired ? (byte)1 : (byte)0);

            byte[] returnValue = tlvWriter.Encode();
            tlvWriter.Clear();

            return returnValue;
        }

        /// <summary>
        /// Write credential password as a TLV.
        /// </summary>
        /// <remarks>
        /// Commands sent to the YubiHSM Auth application must send their data
        /// formatted as a TLV.
        /// </remarks>
        /// <param name="tlvWriter">
        /// The writer to use to build the TLV.
        /// </param>
        private void WriteCredentialPassword(TlvWriter tlvWriter)
        {
            tlvWriter.WriteValue(
                DataTagConstants.Password,
                _credentialWithSecrets.CredentialPassword.Span);
        }

        /// <summary>
        /// Write key type as a TLV.
        /// </summary>
        /// <remarks>
        /// Commands sent to the YubiHSM Auth application must send their data
        /// formatted as a TLV.
        /// </remarks>
        /// <param name="tlvWriter">
        /// The writer to use to build the TLV.
        /// </param>
        private void WriteCryptographicKeyType(TlvWriter tlvWriter)
        {
            tlvWriter.WriteByte(
                DataTagConstants.CryptographicKeyType,
                (byte)_credentialWithSecrets.KeyType);
        }

        /// <summary>
        /// Write the key(s) as a TLV.
        /// </summary>
        /// <remarks>
        /// This method will cast the <see cref="_credentialWithSecrets"/>
        /// to the matching subclass, and then retrieve the appropriate keys.
        /// </remarks>
        /// <param name="tlvWriter">
        /// The writer to use to build the TLV.
        /// </param>
        private void WriteKeys(TlvWriter tlvWriter)
        {
            if (_credentialWithSecrets is Aes128CredentialWithSecrets aes128Credential)
            {
                WriteKeys(tlvWriter, aes128Credential);
            }
            else
            {
                throw new NotImplementedException(ExceptionMessages.YubiHsmAuthKeyTypeNotSupported);
            }
        }

        private static void WriteKeys(
            TlvWriter tlvWriter,
            Aes128CredentialWithSecrets credentialWithSecrets)
        {
            tlvWriter.WriteValue(
                DataTagConstants.EncryptionKey,
                credentialWithSecrets.EncryptionKey.Span);
            tlvWriter.WriteValue(
                DataTagConstants.MacKey,
                credentialWithSecrets.MacKey.Span);
        }
    }
}
