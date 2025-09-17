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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Permission flags used to scope the abilities of the PIN / UV auth token.
    /// </summary>
    [Flags]
    public enum PinUvAuthTokenPermissions
    {
        /// <summary>
        /// No permissions were specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Allow the auth token to be used for <see cref="MakeCredentialCommand"/> operations with the provided relying
        /// party ID parameter.
        /// </summary>
        MakeCredential = 0x01,

        /// <summary>
        /// Allow the auth token to be used for <see cref="GetAssertionCommand"/> operations with the provided relying
        /// party ID parameter.
        /// </summary>
        GetAssertion = 0x02,

        /// <summary>
        /// Allow the auth token to be used with the <see cref="CredentialManagementCommand"/> command. The relying party ID parameter is
        /// optional.
        /// </summary>
        CredentialManagement = 0x04,

        /// <summary>
        /// Allow the auth token to be used with the <see cref="BioEnrollmentCommand"/> command. The relying party ID parameter is ignored
        /// for this permission.
        /// </summary>
        BioEnrollment = 0x08,

        /// <summary>
        /// Allow the auth token to be used with the LargeBlob commands. The relying party ID parameter is ignored for
        /// this permission.
        /// </summary>
        LargeBlobWrite = 0x10,

        /// <summary>
        /// Allow the auth token to be used with the <see cref="ConfigCommand"/> Config command. The relying party ID parameter is ignored for this
        /// permission.
        /// </summary>
        AuthenticatorConfiguration = 0x20,
        
        /// <summary>
        /// This allows the auth token to be used with the 
        /// <see cref="CredentialManagementCommand"/>,
        /// <see cref="GetCredentialMetadataCommand"/>,
        /// <see cref="EnumerateCredentialsBeginCommand"/>,
        /// <see cref="EnumerateRpsBeginCommand"/>,
        /// The relying party ID parameter is ignored for this permission.
        /// </summary>
        PersistentCredentialManagementReadOnly = 0x40,
    }
}
