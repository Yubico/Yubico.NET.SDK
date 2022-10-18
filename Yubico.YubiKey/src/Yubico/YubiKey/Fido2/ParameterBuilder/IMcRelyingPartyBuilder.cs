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

namespace Yubico.YubiKey.Fido2.ParameterBuilder
{
    /// <summary>
    /// A fluent builder interface for specifying the relying party information for a
    /// <see cref="Fido2Session.MakeCredential(Yubico.YubiKey.Fido2.Fido2Session.MakeCredentialParamBuilderFn)" />
    /// operation.
    /// </summary>
    public interface IMcRelyingPartyBuilder
    {
        /// <summary>
        /// Sets the relying party for the credential.
        /// </summary>
        /// <param name="id">
        /// The relying party ID (RPID).
        /// </param>
        /// <returns>
        /// The next interface in the fluent builder chain.
        /// </returns>
        public IMcUserBuilder SetRelyingParty(string id);

        /// <summary>
        /// Sets the relying party for the credential.
        /// </summary>
        /// <param name="id">
        /// The relying party ID (RPID).
        /// </param>
        /// <param name="name">
        /// An optional name to give this relying party. Setting `null`
        /// will omit the name.
        /// </param>
        /// <returns>
        /// The next interface in the fluent builder chain.
        /// </returns>
        public IMcUserBuilder SetRelyingParty(string id, string? name);

        /// <summary>
        /// Sets the relying party for the credential.
        /// </summary>
        /// <param name="relyingParty">
        /// The relying party.
        /// </param>
        /// <returns>
        /// The next interface in the fluent builder chain.
        /// </returns>
        public IMcUserBuilder SetRelyingParty(RelyingParty relyingParty);
    }
}
