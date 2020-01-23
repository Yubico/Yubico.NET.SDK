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
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents a 'relying party' - the entity that uses WebAuthn to register and authenticate users.
    /// </summary>
    [CborSerializable]
    internal class RelyingParty : IValidatable
    {
        /// <summary>
        /// The name of the relying party.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A valid domain string that identifies the WebAuthn Relying Party on whose behalf a given registration or authentication ceremony is being performed.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// A serialized URL which resolves to an image associated with the user. Maximum length is 128 bytes.
        /// </summary>
        public Uri? Icon { get; set; }

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

            if (!(Icon is null) && Icon.ToString().Length > 128)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidCtap2Data) { PropertyName = nameof(Icon) };
            }
        }
    }
}
