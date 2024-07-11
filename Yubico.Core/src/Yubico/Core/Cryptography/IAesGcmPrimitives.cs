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
using System.Security.Cryptography;

namespace Yubico.Core.Cryptography
{
    /// <summary>
    ///     An interface exposing AES-GCM primitive operations.
    /// </summary>
    public interface IAesGcmPrimitives
    {
        /// <summary>
        ///     Encrypt the <c>plaintext</c> using AES-GCM with the given
        ///     <c>keyData</c>, <c>nonce</c>, and <c>associatedData</c>. Place the
        ///     resulting encrypted data into the <c>ciphertext</c> Span and the
        ///     authentication tag into the <c>tag</c> Span.
        /// </summary>
        /// <remarks>
        ///     The key data must be either 128, 192, or 256 bits (16, 24, or 32
        ///     bytes).
        ///     <para>
        ///         The nonce must be exactly 12 bytes. The ciphertext will be the same
        ///         length as the plaintext and the authentication tag will be exactly 16
        ///         bytes. Note that this method will throw an exception if
        ///         <c>ciphertext.Length</c> is not exactly <c>plaintext.Length</c> and
        ///         <c>tag.Length</c> is not exactly 16.
        ///     </para>
        ///     <para>
        ///         Note also that the plaintext can be any length. That is, it is not
        ///         necessary to to supply data that is a length which is a multiple of
        ///         the AES block size.
        ///     </para>
        /// </remarks>
        /// <param name="keyData">
        ///     The key data that will be used to encrypt, either 16, 24, or 32 bytes.
        /// </param>
        /// <param name="nonce">
        ///     The 12-byte "IV". A GCM nonce should be random bytes and should be
        ///     different for each key.
        /// </param>
        /// <param name="plaintext">
        ///     The data to encrypt.
        /// </param>
        /// <param name="ciphertext">
        ///     Where the encrypted data will be placed.
        /// </param>
        /// <param name="tag">
        ///     Where the 16-byte authentication tag will be placed.
        /// </param>
        /// <param name="associatedData">
        ///     The "extra" data used to compute the authentication tag.
        /// </param>
        /// <exception cref="CryptographicException">
        ///     The key data is not a valid length.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     One of the arguments was not valid (e.g. nonce is not exactly 12
        ///     bytes).
        /// </exception>
        public void EncryptAndAuthenticate(
            ReadOnlySpan<byte> keyData,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData);

        /// <summary>
        ///     Decrypt the <c>ciphertext</c> using AES-GCM with the given
        ///     <c>keyData</c>, <c>nonce</c>, and <c>associatedData</c>. Verify the
        ///     authentication tag in the <c>tag</c> Span. Place the resulting
        ///     decrypted data into the <c>plaintext</c> Span. Return the result of
        ///     the authentication verification.
        /// </summary>
        /// <remarks>
        ///     The key data must be either 128, 192, or 256 bits (16, 24, or 32
        ///     bytes).
        ///     <para>
        ///         The nonce must be exactly 12 bytes, and the tag must be exactly 16
        ///         bytes. The plaintext result will be the same length as the
        ///         ciphertext. Note that this method will throw an exception if
        ///         <c>plaintext.Length</c> is not exactly <c>ciphertext.Length</c>.
        ///     </para>
        ///     <para>
        ///         If the input tag matches the tag computed during decryption, this
        ///         method will return <c>true</c>. If the input tag does not match the
        ///         tag computed during decryption, this method will return <c>false</c>.
        ///         In this case, the method will still fill the <c>plaintext</c> buffer
        ///         with the decrypted data.
        ///     </para>
        /// </remarks>
        /// <param name="keyData">
        ///     The key data that will be used to decrypt, either 16, 24, or 32 bytes.
        /// </param>
        /// <param name="nonce">
        ///     The 12-byte "IV". A GCM nonce should be random bytes and should be
        ///     different for each key.
        /// </param>
        /// <param name="ciphertext">
        ///     The data to decrypt.
        /// </param>
        /// <param name="plaintext">
        ///     Where the decrypted data will be placed.
        /// </param>
        /// <param name="tag">
        ///     The 16-byte authentication tag computed during encryption. This is
        ///     the value this method will authenticate.
        /// </param>
        /// <para>
        ///     Note also that the ciphertext can be any length, it is not necessary
        ///     to supply data that is a length which is a multiple of the AES block
        ///     size.
        /// </para>
        /// <param name="associatedData">
        ///     The "extra" data used to compute the authentication tag.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the authentication tag is verified and
        ///     <c>false</c> if it is not.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///     The key data is not a valid length.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     One of the arguments was not valid (e.g. tag is not exactly 16
        ///     bytes).
        /// </exception>
        public bool DecryptAndVerify(
            ReadOnlySpan<byte> keyData,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData);
    }
}
