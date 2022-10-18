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
    /// A fluent builder interface for specifying the user entity data required for a
    /// <see cref="Fido2Session.MakeCredential(Yubico.YubiKey.Fido2.Fido2Session.MakeCredentialParamBuilderFn)"/>
    /// operation.
    /// </summary>
    public interface IMcUserBuilder
    {
        /// <summary>
        /// Sets the user for which the credential is to be created.
        /// </summary>
        /// <param name="id">
        /// The identifier for the user.
        /// </param>
        /// <returns>
        /// The next interface in the fluent builder chain.
        /// </returns>
        public IMcOptionalBuilder SetUser(byte[] id);

        /// <summary>
        /// Sets the user for which the credential is to be created.
        /// </summary>
        /// <param name="id">
        /// The identifier for the user.
        /// </param>
        /// <param name="name">
        /// The name, typically the username, that identifies this user.
        /// </param>
        /// <returns>
        /// The next interface in the fluent builder chain.
        /// </returns>
        public IMcOptionalBuilder SetUser(byte[] id, string? name);

        /// <summary>
        /// Sets the user for which the credential is to be created.
        /// </summary>
        /// <param name="id">
        /// The identifier for the user.
        /// </param>
        /// <param name="name">
        /// The name, typically the username, that identifies this user.
        /// </param>
        /// <param name="displayName">
        /// A display name that can be used by UIs to represent the user.
        /// </param>
        /// <returns>
        /// The next interface in the fluent builder chain.
        /// </returns>
        public IMcOptionalBuilder SetUser(byte[] id, string? name, string? displayName);
    }
}
