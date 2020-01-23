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
    /// Describes a public key credential, for input into commands like <see cref="MakeCredentialCommand"/> and <see cref="GetAssertionCommand"/>.
    /// </summary>
    [CborSerializable]
    internal class PublicKeyCredentialDescriptor : IValidatable
    {
        /// <summary>
        /// Credential type; only currently standardized value is 'public-key'.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The credential ID: a probabilistically-unique at least 16 bytes long identifying a public key credential.
        /// </summary>
        public byte[] Id { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// An optional list of transport hints. See <see cref="AuthenticatorTransportHints"/> for the set of known hints.
        /// </summary>
        public string[]? Transports { get; set; }

        private static string PublicKeyType => "public-key";

        /// <inheritdoc/>
        public void Validate()
        {
            if (Type != PublicKeyType)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidCtap2Data) { PropertyName = nameof(Type) };
            }

            if (Id.Length == 0)
            {
                throw new Ctap2DataException(ExceptionMessages.MissingCtap2Data) { PropertyName = nameof(Id) };
            }
        }
    }
}
