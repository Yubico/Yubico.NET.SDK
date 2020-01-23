// Copyright 2021 Yubico AB
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
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents additional user account attributes used when creating a credential with <see cref="MakeCredentialCommand"/>.
    /// </summary>
    [CborSerializable]
    internal class PublicKeyCredentialUserEntity : IValidatable
    {
        /// <summary>
        /// A human-palatable name for the user account. It is intended only for display.
        /// </summary>
        /// <example>"alex.p.mueller@example.com"</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The user handle of the user account entity. Maximum length is 64 bytes.
        /// </summary>
        public byte[] Id { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// A human-palatable name for the user account, chosen by the user, intended only for display.
        /// </summary>
        /// <example>"Alex P. Müller"</example>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// An optional serialized URL which resolves to an image associated with the user. Maximum length is 128 bytes.
        /// </summary>
        public Uri? Icon { get; set; }

        public const int MaximumUserIdLength = 64;

        /// <inheritdoc/>
        public void Validate()
        {
            if (Name is null || Name.Length == 0)
            {
                throw new Ctap2DataException(ExceptionMessages.MissingCtap2Data) { PropertyName = nameof(Name) };
            }

            if (Id is null || Id.Length == 0)
            {
                throw new Ctap2DataException(ExceptionMessages.MissingCtap2Data) { PropertyName = nameof(Id) };
            }

            if ( Id.Length > MaximumUserIdLength)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidCtap2Data) { PropertyName = nameof(Id) };
            }

            if (DisplayName.Length == 0)
            {
                throw new Ctap2DataException(ExceptionMessages.MissingCtap2Data) { PropertyName = nameof(DisplayName) };
            }
        }
    }
}
