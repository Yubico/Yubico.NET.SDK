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

namespace Yubico.YubiKey.Fido2.ParameterBuilder
{
    /// <summary>
    /// A fluent builder interface for specifying the client hash data required for a <see cref="Fido2Session.MakeCredential"/>
    /// operation.
    /// </summary>
    public interface IMcClientHashBuilder
    {
        /// <summary>
        /// Collects the client hash data portion required for making a FIDO2 credential.
        /// </summary>
        /// <param name="clientHash">
        /// The challenge provided to the client by the relying party.
        /// </param>
        /// <returns>
        /// The next interface in the fluent builder chain.
        /// </returns>
        public IMcRelyingPartyBuilder SetClientHash(ReadOnlyMemory<byte> clientHash);
    }
}
