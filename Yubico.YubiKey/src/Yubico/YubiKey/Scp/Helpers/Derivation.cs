// Copyright 2024 Yubico AB
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
using System.Security.Cryptography;
using Yubico.Core.Cryptography;
using Yubico.YubiKey.Cryptography;


namespace Yubico.YubiKey.Scp.Helpers
{
    /// <summary>
    /// Provides key derivation functionality for Secure Channel Protocol (SCP) operations.
    /// This class implements the key derivation functions used in SCP03 protocol for
    /// generating session keys and cryptograms.
    /// </summary>
    internal static class Derivation
    {
        /// <summary>
        /// Data Derivation Constant for Session Encryption Key
        /// </summary>
        internal const byte DDC_SENC = 0x04;

        /// <summary>
        /// Data Derivation Constant for Session MAC Key
        /// </summary>
        internal const byte DDC_SMAC = 0x06;

        /// <summary>
        /// Data Derivation Constant for Session RMAC Key
        /// </summary>
        internal const byte DDC_SRMAC = 0x07;

        /// <summary>
        /// Data Derivation Constant for Card Cryptogram
        /// </summary>
        internal const byte DDC_CARD_CRYPTOGRAM = 0x00;

        /// <summary>
        /// Data Derivation Constant for Host Cryptogram
        /// </summary>
        internal const byte DDC_HOST_CRYPTOGRAM = 0x01;

        /// <summary>
        /// Derives a key or cryptogram from the host and card challenges using the specified derivation parameters.
        /// </summary>
        /// <param name="dataDerivationConstant">The derivation constant indicating the type of key being derived (e.g., SENC, SMAC).</param>
        /// <param name="outputLenBits">The desired output length in bits (must be either 64 or 128).</param>
        /// <param name="kdfKey">The key derivation function key.</param>
        /// <param name="hostChallenge">The 8-byte challenge from the host.</param>
        /// <param name="cardChallenge">The 8-byte challenge from the card.</param>
        /// <returns>A derived key or cryptogram as a Memory{byte}.</returns>
        /// <exception cref="SecureChannelException">
        /// Thrown when:
        /// - The output length is not 64 or 128 bits
        /// - Either challenge is not exactly 8 bytes
        /// </exception>
        public static Memory<byte> Derive(
            byte dataDerivationConstant,
            byte outputLenBits,
            ReadOnlySpan<byte> kdfKey,
            ReadOnlySpan<byte> hostChallenge,
            ReadOnlySpan<byte> cardChallenge)
        {
            // Validate output length
            if (outputLenBits != 64 && outputLenBits != 128)
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectDerivationLength);
            }

            // Validate challenge lengths
            if (hostChallenge.Length != 8 || cardChallenge.Length != 8)
            {
                throw new SecureChannelException(ExceptionMessages.InvalidChallengeLength);
            }

            // Initialize MAC input buffer with zeros
            // Format according to GP spec 2.3.1:
            // [padding(11) || derivation constant || padding(2) || output length || counter=1 || host challenge || card challenge]
            Span<byte> macInputBuffer = stackalloc byte[32];
            macInputBuffer[11] = dataDerivationConstant;

            // Set output length and counter
            macInputBuffer[14] = outputLenBits;
            macInputBuffer[15] = 1; // Counter is always 1 for our use case

            // Copy host and cardchallenges to the end of the input buffer
            hostChallenge.CopyTo(macInputBuffer.Slice(16, 8));
            cardChallenge.CopyTo(macInputBuffer.Slice(24, 8));

            // Calculate CMAC using AES-128
            Span<byte> cmac = stackalloc byte[16];

            using var cmacObj = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);
            cmacObj.CmacInit(kdfKey);
            cmacObj.CmacUpdate(macInputBuffer);
            cmacObj.CmacFinal(cmac);

            return outputLenBits == 128
                ? cmac.ToArray() // If output length is 128 bits, return the full CMAC
                : cmac[..8].ToArray(); // For 64-bit output (cryptograms), use only the first 8 bytes
        }

        /// <summary>
        /// Derives a cryptogram using the specified derivation constant and challenges.
        /// This is a convenience method that calls Derive with a 64-bit output length.
        /// </summary>
        /// <param name="dataDerivationConstant">The derivation constant (<see cref="DDC_CARD_CRYPTOGRAM"/> or <see cref="DDC_HOST_CRYPTOGRAM"/>.</param>
        /// <param name="key">The key used for cryptogram derivation.</param>
        /// <param name="hostChallenge">The 8-byte host challenge.</param>
        /// <param name="cardChallenge">The 8-byte card challenge.</param>
        /// <returns>A 64-bit (8-byte) cryptogram.</returns>
        public static Memory<byte> DeriveCryptogram(
            byte dataDerivationConstant,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> hostChallenge,
            ReadOnlySpan<byte> cardChallenge) =>
            Derive(dataDerivationConstant, 64, key, hostChallenge, cardChallenge);

        /// <summary>
        /// Derives session keys from the static keys using host and card challenges.
        /// This method generates three session keys (SMAC, SENC, SRMAC) and includes the static DEK.
        /// </summary>
        /// <param name="staticKeys">The static key set containing channel MAC, encryption, and data encryption keys.</param>
        /// <param name="hostChallenge">The 8-byte host challenge.</param>
        /// <param name="cardChallenge">The 8-byte card challenge.</param>
        /// <returns>A SessionKeys object containing all derived session keys.</returns>
        /// <remarks>
        /// This method implements secure memory handling:
        /// - Uses stackalloc for temporary buffers
        /// - Securely clears sensitive data after use
        /// - Properly handles exceptions to ensure no sensitive data leaks
        /// </remarks>
        public static SessionKeys DeriveSessionKeysFromStaticKeys(
            StaticKeys staticKeys,
            ReadOnlySpan<byte> hostChallenge,
            ReadOnlySpan<byte> cardChallenge)
        {
            Span<byte> macKey = stackalloc byte[staticKeys.ChannelMacKey.Length];
            Span<byte> encKey = stackalloc byte[staticKeys.ChannelEncryptionKey.Length];
            Span<byte> dekKey = stackalloc byte[staticKeys.DataEncryptionKey.Length];

            staticKeys.ChannelMacKey.Span.CopyTo(macKey);
            staticKeys.ChannelEncryptionKey.Span.CopyTo(encKey);
            staticKeys.DataEncryptionKey.Span.CopyTo(dekKey);

            try
            {
                // If these calls succeed, then control of the created buffers
                // will be given to the new SessionKeys object created. That is,
                // if these succeed, don't overwrite the returned sensitive data.
                // The Derive call can throw an exception. Normally, we would
                // want to catch that exception just in case at least one call
                // succeeded and we have some sensitive data to overwrite. But
                // the Derive call fails if either the host or card challenge
                // is not exactly 8 bytes. In that case, the first call would
                // fail before generating a result, so there will be no data to
                // overwrite.
                var SMAC = Derive(DDC_SMAC, 128, macKey, hostChallenge, cardChallenge);
                var SENC = Derive(DDC_SENC, 128, encKey, hostChallenge, cardChallenge);
                var SRMAC = Derive(DDC_SRMAC, 128, macKey, hostChallenge, cardChallenge);

                return new SessionKeys(SMAC, SENC, SRMAC, dekKey.ToArray());
            }
            finally
            {
                CryptographicOperations.ZeroMemory(macKey);
                CryptographicOperations.ZeroMemory(encKey);
                CryptographicOperations.ZeroMemory(dekKey);
            }
        }
    }
}
