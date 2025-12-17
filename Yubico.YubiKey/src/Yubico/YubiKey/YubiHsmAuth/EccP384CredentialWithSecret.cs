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

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// This class is used when adding a new credential with an ECC P-384
    /// private key to the YubiHSM Auth application.
    /// </summary>
    public class EccP384CredentialWithSecrets : CredentialWithSecrets
    {
        // ECC P-384 private key
        private ReadOnlyMemory<byte> _privateKey;

        /// <summary>
        /// An ECC P-384 private key must be exactly 48 bytes.
        /// </summary>
        public const int RequiredKeySize = 48;

        /// <summary>
        /// The ECC P-384 private key. Its length must be equal to
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
        public ReadOnlyMemory<byte> PrivateKey
        {
            get => _privateKey;

            set => _privateKey = value.Length == RequiredKeySize
                ? value
                : throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.YubiHsmAuthInvalidKeySize,
                        value.Length,
                        RequiredKeySize));
        }

        /// <summary>
        /// Create an ECC P-384 credential to be stored in the YubiHSM Auth
        /// application.
        /// </summary>
        /// <param name="credentialPassword">
        /// The credential password is required when performing operations
        /// that access the key, such as calculating session keys. Its
        /// length must be equal to
        /// <see cref="CredentialWithSecrets.RequiredCredentialPasswordLength"/>.
        /// </param>
        /// <param name="privateKey">
        /// Sets <see cref="PrivateKey"/>.
        /// </param>
        /// <param name="label">
        /// Sets <see cref="Credential.Label"/>.
        /// </param>
        /// <param name="touchRequired">
        /// Sets <see cref="Credential.TouchRequired"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the ECC P-384 private key does not have the required size (see
        /// <see cref="RequiredKeySize"/>).
        /// </exception>
        public EccP384CredentialWithSecrets(
            ReadOnlyMemory<byte> credentialPassword,
            ReadOnlyMemory<byte> privateKey,
            string label,
            bool touchRequired)
            : base(credentialPassword, CryptographicKeyType.SecP384R1, label, touchRequired)
        {
            PrivateKey = privateKey;
        }
    }
}
