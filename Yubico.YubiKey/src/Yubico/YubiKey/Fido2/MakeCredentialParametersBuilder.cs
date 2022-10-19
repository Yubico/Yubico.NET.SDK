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

using Yubico.YubiKey.Fido2.ParameterBuilder;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// A builder to aid in the construction of a valid MakeCredentialParameters instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to create a fluent builder for <see cref="MakeCredentialParameters"/>. This builder will
    /// guide you through the mandatory parameters, as well as give you the ability to set optional parameters as well.
    /// Many of the builder methods will allow you to pass dependent data types in directly, or construct them inline
    /// using the builder.
    /// </para>
    /// <para>
    /// For example:
    /// <code language="csharp">
    /// using var fido2 = new Fido2Session(yubiKeyDevice);
    ///
    /// // Collect the PIN or UV prior to calling MakeCredential
    ///
    /// var builder = MakeCredentialParametersBuilder.Create();
    ///
    /// fido2.MakeCredential(builder
    ///     .SetClientHash(clientHashData)
    ///     .SetRelyingParty("my-rp-id")
    ///     .SetUser(userId, "username", "Display Name")
    ///     .Build());
    /// </code>
    /// </para>
    /// <para>
    /// More information about the parameters and their meanings can be found in the documentation for
    /// <see cref="MakeCredentialParameters"/>.
    /// </para>
    /// </remarks>
    public static class MakeCredentialParametersBuilder
    {
        /// <summary>
        /// Creates a new instance of a MakeCredentialParameters builder.
        /// </summary>
        /// <returns>
        /// The first builder interface in the chain.
        /// </returns>
        public static IMcClientHashBuilder Create() => new McParameterBuilderImpl();
    }
}
