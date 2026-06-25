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

using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Fido2.Arkg;

/// <summary>
/// Provides ARKG-P256 (Asynchronous Remote Key Generation for P-256) operations.
/// </summary>
/// <remarks>
/// Thin wrapper that routes to the OpenSSL-backed
/// <see cref="IArkgPrimitives"/> (via <see cref="CryptographyProviders.ArkgPrimitivesCreator"/>).
/// The full algorithm body lives in <c>Yubico.YubiKit.Core.Cryptography.ArkgPrimitivesOpenSsl.Derive</c>
/// because it needs Core's internal OpenSSL P/Invoke surface.
/// Conforms to draft-bradleylundberg-cfrg-arkg-10; reference implementation in
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
    /// A tuple containing the SEC1 uncompressed derived public key (65 bytes)
    /// and the ARKG key handle to send to the authenticator.
    /// </returns>
    internal static (byte[] derivedPk, byte[] arkgKeyHandle) DerivePublicKey(
        ReadOnlySpan<byte> pkBl,
        ReadOnlySpan<byte> pkKem,
        ReadOnlySpan<byte> ikm,
        ReadOnlySpan<byte> ctx)
    {
        return CryptographyProviders.ArkgPrimitivesCreator().Derive(pkBl, pkKem, ikm, ctx);
    }
}
