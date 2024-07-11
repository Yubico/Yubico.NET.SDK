// Copyright 2023 Yubico AB
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
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains the info about a user in one credential on the YubiKey. This is
    /// the class returned when calling
    /// <see cref="Fido2Session.EnumerateCredentialsForRelyingParty"/>.
    /// </summary>
    public class CredentialUserInfo
    {
        /// <summary>
        /// The user entity for a credential returned.
        /// </summary>
        public UserEntity User { get; private set; }

        /// <summary>
        /// The credential ID for a credential returned.
        /// </summary>
        public CredentialId CredentialId { get; private set; }

        /// <summary>
        /// The public key for a credential returned.
        /// </summary>
        public CoseKey CredentialPublicKey { get; private set; }

        /// <summary>
        /// The credential protection policy. See section 12.1.1 of the FIDO2
        /// standard for a description of the meanings of the number returned.
        /// </summary>
        public CredProtectPolicy CredProtectPolicy { get; private set; }

        /// <summary>
        /// The large blob key for a credential. If this property is null, either
        /// the credential does not have a large blob key, or it does have a
        /// large blob key but it is not requested.
        /// </summary>
        public ReadOnlyMemory<byte>? LargeBlobKey { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private CredentialUserInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="CredentialUserInfo"/> based on the
        /// given objects related to a user.
        /// </summary>
        /// <param name="user">
        /// The user associated with the credential.
        /// </param>
        /// <param name="credentialId">
        /// The credential ID, which includes the Id and Type.
        /// </param>
        /// <param name="credentialPublicKey">
        /// The public key to use when verifying a credential.
        /// </param>
        /// <param name="credProtectPolicy">
        /// An int specifying the credential protection policy.
        /// </param>
        /// <param name="largeBlobKey">
        /// If null, there is no large blob key. If not null, this is the key
        /// that can be used to encrypt or decrypt large blob data for the
        /// credential.
        /// </param>
        public CredentialUserInfo(
            UserEntity user,
            CredentialId credentialId,
            CoseKey credentialPublicKey,
            int credProtectPolicy,
            ReadOnlyMemory<byte>? largeBlobKey = null)
        {
            User = user;
            CredentialId = credentialId;
            CredentialPublicKey = credentialPublicKey;
            CredProtectPolicy = (CredProtectPolicy)credProtectPolicy;
            LargeBlobKey = largeBlobKey;
        }
    }
}
