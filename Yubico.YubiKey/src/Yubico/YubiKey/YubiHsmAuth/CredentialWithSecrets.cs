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

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    ///     This <see cref="Credential" /> subclass is used when adding new
    ///     credentials to the YubiHSM Auth application. See
    ///     <see cref="Commands.AddCredentialCommand" /> for more information.
    /// </summary>
    /// <remarks>
    ///     Every credential in the YubiHSM Auth application contains two
    ///     secrets: the credential password, and the cryptographic key(s). The
    ///     requirements for the credential password are the same for every
    ///     credential. The caller is responsible for generating the cryptographic
    ///     key(s) with an appropriate RNG. The requirements for the
    ///     key(s) vary based on the <see cref="Credential.KeyType" />. Classes
    ///     that inherit from this one will implement functionality related to the
    ///     key(s) specific to the <see cref="CryptographicKeyType" /> it represents.
    ///     See <see cref="Aes128CredentialWithSecrets" /> for an example
    ///     implementation.
    /// </remarks>
    public abstract class CredentialWithSecrets : Credential
    {
        /// <summary>
        ///     The credential password must be exactly 16 bytes.
        /// </summary>
        public const int RequiredCredentialPasswordLength = 16;

        private ReadOnlyMemory<byte> _credentialPassword;

        /// <summary>
        ///     Create a credential with the secrets to be stored in the
        ///     application.
        /// </summary>
        /// <param name="credentialPassword">
        ///     Sets <see cref="CredentialPassword" />.
        /// </param>
        /// <param name="keyType">
        ///     Sets <see cref="Credential.KeyType" />.
        /// </param>
        /// <param name="label">
        ///     Sets <see cref="Credential.Label" />.
        /// </param>
        /// <param name="touchRequired">
        ///     Sets <see cref="Credential.TouchRequired" />.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     The credential password does not meet the length requirements.
        /// </exception>
        protected CredentialWithSecrets(
            ReadOnlyMemory<byte> credentialPassword,
            CryptographicKeyType keyType,
            string label,
            bool touchRequired) : base(keyType, label, touchRequired)
        {
            CredentialPassword = credentialPassword;
        }

        /// <summary>
        ///     The credential password is required when performing operations
        ///     that access the key(s), such as calculating session keys. Its
        ///     length must be equal to <see cref="RequiredCredentialPasswordLength" />.
        /// </summary>
        /// <remarks>
        ///     The caller is responsible for controlling the buffer which holds
        ///     this value, and should overwrite the data after the command
        ///     (see <see cref="Commands.AddCredentialCommand" />) is sent.
        ///     The user's manual entry
        ///     <xref href="UsersManualSensitive">"Sensitive Data"</xref> has further
        ///     details and recommendations for handling this kind of data.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///     The credential password does not meet the length requirements.
        /// </exception>
        public ReadOnlyMemory<byte> CredentialPassword
        {
            get => _credentialPassword;

            set =>
                _credentialPassword = value.Length == RequiredCredentialPasswordLength
                    ? value
                    : throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.YubiHsmAuthInvalidPasswordLength,
                            value.Length));
        }
    }
}
