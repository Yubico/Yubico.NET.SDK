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
using CommunityToolkit.Diagnostics;
using Yubico.Core.Cryptography;
using Yubico.YubiKey.Fido2;

namespace Yubico.YubiKey.TestUtilities.Fido2
{
    /// <summary>
    /// Extension methods for <see cref="PreviewSignGeneratedKey"/> that provide
    /// RP-side verification functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These methods enable Relying Party (RP) side public key derivation and
    /// signature verification for the previewSign WebAuthn extension. These are
    /// test/verification utilities and not part of the authenticator-side SDK surface.
    /// </para>
    /// <para>
    /// In production deployments, RP-side verification logic belongs on the
    /// application server, not in the YubiKey SDK. These methods are provided
    /// for integration testing and demonstration purposes.
    /// </para>
    /// </remarks>
    public static class PreviewSignGeneratedKeyExtensions
    {
        /// <summary>
        /// Derives a public key using the ARKG-P256 algorithm.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs offline key derivation using the ARKG-P256 algorithm.
        /// The derived public key can be used to verify signatures created by the
        /// YubiKey when provided with the corresponding ARKG key handle and context.
        /// </para>
        /// <para>
        /// Multiple independent public keys can be derived from the same generated
        /// key by using different context strings. Each context produces a unique
        /// derived key pair.
        /// </para>
        /// <para>
        /// To use the derived key for signing, pass the returned
        /// <see cref="PreviewSignDerivedKey"/> to
        /// <see cref="GetAssertionParameters.AddPreviewSignExtension"/>.
        /// The YubiKey will produce a signature that can be verified using
        /// <see cref="PreviewSignDerivedKey.VerifySignature(byte[], byte[])"/>.
        /// </para>
        /// </remarks>
        /// <param name="generatedKey">The generated key from which to derive.</param>
        /// <param name="ikm">
        /// Input keying material for HKDF derivation. This should be random data
        /// unique to the derivation context.
        /// </param>
        /// <param name="ctx">
        /// Context string for domain separation. Different contexts produce
        /// different derived keys from the same input keying material.
        /// </param>
        /// <returns>
        /// A <see cref="PreviewSignDerivedKey"/> containing the derived public key,
        /// ARKG key handle, device key handle, and context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="generatedKey"/>, <paramref name="ikm"/>,
        /// or <paramref name="ctx"/> is null.
        /// </exception>
        public static PreviewSignDerivedKey DerivePublicKey(
            this PreviewSignGeneratedKey generatedKey,
            byte[] ikm,
            byte[] ctx)
        {
            Guard.IsNotNull(generatedKey, nameof(generatedKey));
            Guard.IsNotNull(ikm, nameof(ikm));
            Guard.IsNotNull(ctx, nameof(ctx));

            return DerivePublicKey(generatedKey, (ReadOnlySpan<byte>)ikm, (ReadOnlySpan<byte>)ctx);
        }

        /// <summary>
        /// Derives a public key using the ARKG-P256 algorithm.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs offline key derivation using the ARKG-P256 algorithm.
        /// The derived public key can be used to verify signatures created by the
        /// YubiKey when provided with the corresponding ARKG key handle and context.
        /// </para>
        /// <para>
        /// Multiple independent public keys can be derived from the same generated
        /// key by using different context strings. Each context produces a unique
        /// derived key pair.
        /// </para>
        /// <para>
        /// To use the derived key for signing, pass the returned
        /// <see cref="PreviewSignDerivedKey"/> to
        /// <see cref="GetAssertionParameters.AddPreviewSignExtension"/>.
        /// The YubiKey will produce a signature that can be verified using
        /// <see cref="PreviewSignDerivedKey.VerifySignature(byte[], byte[])"/>.
        /// </para>
        /// </remarks>
        /// <param name="generatedKey">The generated key from which to derive.</param>
        /// <param name="ikm">
        /// Input keying material for HKDF derivation. This should be random data
        /// unique to the derivation context.
        /// </param>
        /// <param name="ctx">
        /// Context string for domain separation. Different contexts produce
        /// different derived keys from the same input keying material.
        /// </param>
        /// <returns>
        /// A <see cref="PreviewSignDerivedKey"/> containing the derived public key,
        /// ARKG key handle, device key handle, and context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="generatedKey"/> is null.
        /// </exception>
        public static PreviewSignDerivedKey DerivePublicKey(
            this PreviewSignGeneratedKey generatedKey,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> ctx)
        {
            Guard.IsNotNull(generatedKey, nameof(generatedKey));

            // Access internal fields via public properties
            byte[] blindingPublicKey = generatedKey.BlindingPublicKey.ToArray();
            byte[] kemPublicKey = generatedKey.KemPublicKey.ToArray();

            (byte[] derivedPk, byte[] arkgKeyHandle) = ArkgPrimitives.Create().Derive(
                blindingPublicKey,
                kemPublicKey,
                ikm,
                ctx);

            return new PreviewSignDerivedKey(
                derivedPk,
                arkgKeyHandle,
                generatedKey.KeyHandle,
                ctx.ToArray());
        }
    }
}
