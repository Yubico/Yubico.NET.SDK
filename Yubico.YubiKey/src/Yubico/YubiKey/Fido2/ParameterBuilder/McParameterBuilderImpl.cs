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
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.ParameterBuilder
{
    internal class McParameterBuilderImpl :
        IMcClientHashBuilder,
        IMcRelyingPartyBuilder,
        IMcUserBuilder,
        IMcOptionalBuilder
    {
        private ReadOnlyMemory<byte> _clientDataHash;
        private RelyingParty? _relyingParty;
        private MakeCredentialParameters? _makeCredentialParameters;

        /// <inheritdoc />
        public IMcRelyingPartyBuilder SetClientHash(ReadOnlyMemory<byte> clientHash)
        {
            _clientDataHash = clientHash;

            return this;
        }

        /// <inheritdoc />
        public IMcUserBuilder SetRelyingParty(string id) => SetRelyingParty(id, null);

        public IMcUserBuilder SetRelyingParty(string id, string? name) =>
            SetRelyingParty(new RelyingParty(id) { Name = name });

        public IMcUserBuilder SetRelyingParty(RelyingParty relyingParty)
        {
            _relyingParty = relyingParty;

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder SetUser(byte[] id) => SetUser(id, null, null);

        /// <inheritdoc />
        public IMcOptionalBuilder SetUser(byte[] id, string? name) => SetUser(id, name, null);

        /// <inheritdoc />
        public IMcOptionalBuilder SetUser(byte[] id, string? name, string? displayName)
        {
            if (id is null)
            {
                throw new ArgumentException(nameof(id));
            }

            var userEntity = new UserEntity(id) { Name = name, DisplayName = displayName };

            if (_relyingParty is null)
            {
                // We should never hit this. If we did, it's a bug in the SDK and likely means
                // that someone changed the order of the builder operations.
                throw new InvalidOperationException();
            }

            _makeCredentialParameters = new MakeCredentialParameters(_relyingParty, userEntity)
            {
                ClientDataHash = _clientDataHash
            };

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder AddAlgorithm(string type, CoseAlgorithmIdentifier algorithm)
        {
            _makeCredentialParameters!.AddAlgorithm(type, algorithm);

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder AddExtension(string extensionKey, byte[] encodedValue)
        {
            _makeCredentialParameters!.AddExtension(extensionKey, encodedValue);

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder ExcludeCredential(CredentialId credentialId)
        {
            _makeCredentialParameters!.ExcludeCredential(credentialId);

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder IsDiscoverableCredential(bool value = true)
        {
            _makeCredentialParameters!.AddOption("rk", value);

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder RequireUserPresence(bool value = true)
        {
            _makeCredentialParameters!.AddOption("up", value);

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder RequireUserVerification(bool value = true)
        {
            _makeCredentialParameters!.AddOption("uv", value);

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder AddOption(string optionName, bool value = true)
        {
            _makeCredentialParameters!.AddOption(optionName, value);

            return this;
        }

        /// <inheritdoc />
        public IMcOptionalBuilder UseAttestation(EnterpriseAttestation attestationType)
        {
            _makeCredentialParameters!.EnterpriseAttestation = attestationType;

            return this;
        }

        public MakeCredentialParameters Build() => _makeCredentialParameters!;
    }
}
