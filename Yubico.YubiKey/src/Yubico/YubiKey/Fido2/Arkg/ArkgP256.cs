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

using Yubico.Core.Cryptography;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2.Arkg
{
    /// <summary>
    /// Provides ARKG-P256 (Asynchronous Remote Key Generation for P-256) operations.
    /// </summary>
    /// <remarks>
    /// Thin wrapper that routes to the OpenSSL-backed
    /// <see cref="IArkgPrimitives"/>. The full algorithm body lives in
    /// <c>Yubico.Core.Cryptography.ArkgPrimitivesOpenSsl.Derive</c> because
    /// it needs Yubico.Core's internal OpenSSL P/Invoke surface, which is not
    /// visible from Yubico.YubiKey. Conforms to draft-bradleylundberg-cfrg-arkg-09;
    /// reference implementation in
    /// cnh-authenticator-rs-extension/native/crates/hid-test/src/arkg.rs.
    /// </remarks>
    internal static class ArkgP256
    {
        /// <summary>
        /// Derives a public key using the ARKG-P256 algorithm.
        /// </summary>
        /// <param name="pkBl">The blinding public key (SEC1 uncompressed, 65 bytes).</param>
        /// <param name="pkKem">The KEM public key (SEC1 uncompressed, 65 bytes).</param>
        /// <param name="ikm">Input keying material for derivation.</param>
        /// <param name="ctx">Context string for derivation. Must be at most 64 bytes.</param>
        /// <returns>
        /// A tuple containing the SEC1 uncompressed derived public key
        /// (65 bytes) and the ARKG key handle to send to the authenticator.
        /// </returns>
        public static (byte[] derivedPk, byte[] arkgKeyHandle) DerivePublicKey(
            byte[] pkBl,
            byte[] pkKem,
            byte[] ikm,
            byte[] ctx)
        {
            return CryptographyProviders.ArkgPrimitivesCreator().Derive(pkBl, pkKem, ikm, ctx);
        }
    }
}
