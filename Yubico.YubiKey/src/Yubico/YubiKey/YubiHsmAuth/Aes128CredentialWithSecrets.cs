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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// This class is used when adding a new credential with AES-128 keys
    /// to the YubiHSM Auth application.
    /// </summary>
    public class Aes128CredentialWithSecrets : CredentialWithSecrets
    {
        // AES-128 keys
        private ReadOnlyMemory<byte> _encryptionKey;
        private ReadOnlyMemory<byte> _macKey;

        /// <summary>
        /// An AES-128 key must be exactly 16 bytes. This applies
        /// to both the Encryption and MAC key.
        /// </summary>
        public const int RequiredKeySize = 16;

        /// <summary>
        /// The AES-128 key used for encryption. Its length must be equal to
        /// <see cref="RequiredKeySize"/>.
        /// </summary>
        /// <remarks>
        /// The caller is responsible for controlling the buffer which holds
        /// this value, and should overwrite the data after the command
        /// (see <see cref="Commands.AddCredentialCommand"/>) is sent.
        /// The user's manual entry
        /// <xref href="UsersManualSensitive">"Sensitive Data"</xref> has further
        /// details and recommendations for handling this kind of data.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the key does not have the required size.
        /// </exception>
        public ReadOnlyMemory<byte> EncryptionKey
        {
            get => _encryptionKey;

            set => _encryptionKey = value.Length == RequiredKeySize
                ? value
                : throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidEncSize,
                        value.Length));
        }

        /// <summary>
        /// The AES-128 key used for message authentication (MAC). Its length
        /// must be equal to <see cref="RequiredKeySize"/>.
        /// </summary>
        /// <remarks>
        /// The caller is responsible for controlling the buffer which holds
        /// this value, and should overwrite the data after the command
        /// (see <see cref="Commands.AddCredentialCommand"/>) is sent.
        /// The user's manual entry
        /// <xref href="UsersManualSensitive">"Sensitive Data"</xref> has further
        /// details and recommendations for handling this kind of data.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the key does not have the required size.
        /// </exception>
        public ReadOnlyMemory<byte> MacKey
        {
            get => _macKey;

            set => _macKey = value.Length == RequiredKeySize
                ? value
                : throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidMacSize,
                        value.Length));
        }

        /// <summary>
        /// Create an AES-128 credential to be stored in the YubiHSM Auth
        /// application.
        /// </summary>
        /// <param name="credentialPassword">
        /// The credential password is required when performing operations
        /// that access the key(s), such as calculating session keys. Its
        /// length must be equal to
        /// <see cref="CredentialWithSecrets.RequiredCredentialPasswordLength"/>.
        /// </param>
        /// <param name="encryptionKey">
        /// Sets <see cref="EncryptionKey"/>.
        /// </param>
        /// <param name="macKey">
        /// Sets <see cref="MacKey"/>.
        /// </param>
        /// <param name="label">
        /// Sets <see cref="Credential.Label"/>.
        /// </param>
        /// <param name="touchRequired">
        /// Sets <see cref="Credential.TouchRequired"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when an AES-128 key does not have the required size (see
        /// <see cref="RequiredKeySize"/>).
        /// </exception>
        public Aes128CredentialWithSecrets(
            ReadOnlyMemory<byte> credentialPassword,
            ReadOnlyMemory<byte> encryptionKey,
            ReadOnlyMemory<byte> macKey,
            string label,
            bool touchRequired)
            : base(credentialPassword, CryptographicKeyType.Aes128, label, touchRequired)
        {
            EncryptionKey = encryptionKey;
            MacKey = macKey;
        }
    }
}
