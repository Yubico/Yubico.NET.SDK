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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents a derived public key produced by ARKG-P256 derivation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class contains the derived public key and handles needed for
    /// authentication via the previewSign extension. Instances are obtained
    /// by calling <see cref="PreviewSignGeneratedKey.DerivePublicKey"/> with
    /// application-provided input keying material and a context string.
    /// </para>
    /// <para>
    /// The derived public key can be used to verify signatures produced by the
    /// YubiKey when signing with the corresponding ARKG key handle and context.
    /// Use <see cref="VerifySignature"/> to validate signatures against this key.
    /// </para>
    /// <para>
    /// To request a signature from the YubiKey using this derived key, pass this
    /// object to <see cref="GetAssertionParameters.AddPreviewSignByCredentialExtension"/>.
    /// </para>
    /// </remarks>
    public sealed class PreviewSignDerivedKey
    {
        /// <summary>
        /// Gets the derived public key.
        /// </summary>
        public ReadOnlyMemory<byte> PublicKey { get; init; }

        /// <summary>
        /// Gets the ARKG key handle.
        /// </summary>
        public ReadOnlyMemory<byte> ArkgKeyHandle { get; init; }

        /// <summary>
        /// Gets the device key handle from the original registration.
        /// </summary>
        public ReadOnlyMemory<byte> DeviceKeyHandle { get; init; }

        /// <summary>
        /// Gets the context string used for derivation.
        /// </summary>
        public ReadOnlyMemory<byte> Context { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewSignDerivedKey"/> class.
        /// </summary>
        /// <param name="publicKey">The derived public key.</param>
        /// <param name="arkgKeyHandle">The ARKG key handle.</param>
        /// <param name="deviceKeyHandle">The device key handle.</param>
        /// <param name="context">The context string.</param>
        internal PreviewSignDerivedKey(
            ReadOnlyMemory<byte> publicKey,
            ReadOnlyMemory<byte> arkgKeyHandle,
            ReadOnlyMemory<byte> deviceKeyHandle,
            ReadOnlyMemory<byte> context)
        {
            PublicKey = publicKey;
            ArkgKeyHandle = arkgKeyHandle;
            DeviceKeyHandle = deviceKeyHandle;
            Context = context;
        }

        /// <summary>
        /// Verifies a signature against the derived public key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method verifies that a signature produced by the YubiKey (obtained via
        /// <see cref="AuthenticatorData.GetPreviewSignSignature"/>) is valid for the
        /// given message using the derived public key from ARKG-P256 derivation.
        /// </para>
        /// <para>
        /// The signature must be in DER-encoded ECDSA format, as returned by the
        /// YubiKey's previewSign extension. The message is the raw data that was
        /// signed, not a hash.
        /// </para>
        /// </remarks>
        /// <param name="message">
        /// The message that was signed. This method will hash the message internally
        /// before verifying the signature.
        /// </param>
        /// <param name="signature">
        /// The DER-encoded ECDSA signature to verify, as returned by
        /// <see cref="AuthenticatorData.GetPreviewSignSignature"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the signature is valid for the message using the derived
        /// public key; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="message"/> or <paramref name="signature"/> is null.
        /// </exception>
        public bool VerifySignature(byte[] message, byte[] signature)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (signature is null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            // PublicKey is SEC1 uncompressed: 0x04 || X(32) || Y(32).
            if (PublicKey.Length != 65 || PublicKey.Span[0] != 0x04)
            {
                return false;
            }

            var verifier = new EcdsaVerify(PublicKey);
            return verifier.VerifyData(message, signature, isStandardSignature: true);
        }
    }
}
