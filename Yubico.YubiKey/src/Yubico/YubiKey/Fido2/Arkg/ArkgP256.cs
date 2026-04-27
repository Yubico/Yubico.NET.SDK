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

namespace Yubico.YubiKey.Fido2.Arkg
{
    /// <summary>
    /// Provides ARKG-P256 (Asynchronous Remote Key Generation for P-256) operations.
    /// </summary>
    /// <remarks>
    /// This implementation follows draft-bradleylundberg-cfrg-arkg-09 and performs
    /// offline public key derivation without requiring YubiKey interaction.
    /// </remarks>
    internal static class ArkgP256
    {
        /// <summary>
        /// Derives a public key using ARKG-P256 algorithm.
        /// </summary>
        /// <param name="pkBl">The blinding public key.</param>
        /// <param name="pkKem">The KEM public key.</param>
        /// <param name="ikm">Input keying material for derivation.</param>
        /// <param name="ctx">Context string for derivation.</param>
        /// <returns>
        /// A tuple containing the derived public key and the ARKG key handle.
        /// </returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public static (byte[] derivedPk, byte[] arkgKeyHandle) DerivePublicKey(
            byte[] pkBl,
            byte[] pkKem,
            byte[] ikm,
            byte[] ctx)
        {
            throw new NotImplementedException();
        }
    }
}
