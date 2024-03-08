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
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using Yubico.PlatformInterop;

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// An OpenSSL implementation of the IAesGcmPrimitives interface, exposing
    /// AES-GCM primitives to the SDK.
    /// </summary>
    internal class AesGcmPrimitivesOpenSsl : IAesGcmPrimitives
    {
        private const int NonceLength = 12;
        private const int AuthTagLength = 16;

        /// <inheritdoc />
        public void EncryptAndAuthenticate(
            ReadOnlySpan<byte> keyData,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData)
        {
            if ((nonce.Length != NonceLength) || (ciphertext.Length != plaintext.Length)
                || (tag.Length != AuthTagLength))
            {
                throw new ArgumentException(ExceptionMessages.InvalidAesGcmInput);
            }

            int outputLength;
            byte[] keyBytes = keyData.ToArray();
            byte[] dataToEncrypt = plaintext.ToArray();
            byte[] encryptedData = new byte[plaintext.Length];
            byte[] tagBytes = new byte[AuthTagLength];

            try
            {
                using SafeEvpCipherCtx ctx = NativeMethods.EvpCipherCtxNew();

                int status = NativeMethods.EvpAes256GcmInit(true, ctx, keyBytes, nonce.ToArray());
                if (status != 0)
                {
                    // The OpenSSL Wiki documents AES-GCM, and says to pass in
                    // the AAD with a null output buffer.
                    status = NativeMethods.EvpUpdate(
                        ctx, null, out outputLength, associatedData.ToArray(), associatedData.Length);
                }
                if (status != 0)
                {
                    status = NativeMethods.EvpUpdate(
                        ctx, encryptedData, out outputLength, dataToEncrypt, dataToEncrypt.Length);
                    if (outputLength != dataToEncrypt.Length)
                    {
                        status = 0;
                    }
                }
                if (status != 0)
                {
                    encryptedData.CopyTo(ciphertext);

                    // The doc says this computes the tag, but it doesn't return it.
                    status = NativeMethods.EvpFinal(ctx, tagBytes, out outputLength);
                    if (outputLength != 0)
                    {
                        status = 0;
                    }
                }
                if (status != 0)
                {
                    status = NativeMethods.EvpCipherCtxCtrl(ctx, NativeMethods.CtrlFlag.GetTag, AuthTagLength, tagBytes);
                }

                if (status == 0)
                {
                    throw new SecurityException(ExceptionMessages.AesGcmFailed);
                }

                tagBytes.CopyTo(tag);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBytes);
                CryptographicOperations.ZeroMemory(dataToEncrypt);
            }
        }

        /// <inheritdoc />
        public bool DecryptAndVerify(
            ReadOnlySpan<byte> keyData,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            if ((nonce.Length != NonceLength) || (plaintext.Length != ciphertext.Length)
                || (tag.Length != AuthTagLength))
            {
                throw new ArgumentException(ExceptionMessages.InvalidAesGcmInput);
            }

            bool returnValue = false;
            int outputLength;

            byte[] keyBytes = keyData.ToArray();
            byte[] decryptedData = new byte[ciphertext.Length];
            byte[] tagBytes = tag.ToArray();

            try
            {
                using SafeEvpCipherCtx ctx = NativeMethods.EvpCipherCtxNew();

                int status = NativeMethods.EvpAes256GcmInit(false, ctx, keyBytes, nonce.ToArray());

                if (status != 0)
                {
                    // The OpenSSL Wiki documents AES-GCM, and says to pass in
                    // the AAD with a null output buffer.
                    status = NativeMethods.EvpUpdate(
                        ctx, null, out outputLength, associatedData.ToArray(), associatedData.Length);
                }
                if (status != 0)
                {
                    status = NativeMethods.EvpUpdate(
                        ctx, decryptedData, out outputLength, ciphertext.ToArray(), ciphertext.Length);
                    if (outputLength != ciphertext.Length)
                    {
                        status = 0;
                    }
                }
                if (status != 0)
                {
                    decryptedData.CopyTo(plaintext);

                    // Set the expected tag, the Final call will compute from the
                    // decryption and AAD, then compare to the input.
                    status = NativeMethods.EvpCipherCtxCtrl(ctx, NativeMethods.CtrlFlag.SetTag, AuthTagLength, tagBytes);
                }
                if (status != 0)
                {
                    // This will check the tag. If the tag verifies, it will
                    // return 1. If the tag does not verify, it will return 0.
                    // If it returns 0, leave the returnValue to false
                    // (indicating the tag did not verify), but set status back
                    // to 1 so we don't throw an exception (we want to throw an
                    // exception on errors, but a tag that does not verify is not
                    // an error, because that's what this method is supposed to
                    // do, determine if the tag verifies. The method did what it
                    // was supposed to do, it succeeded in its mission, that is
                    // not an error).
                    status = NativeMethods.EvpFinal(ctx, tagBytes, out outputLength);
                    if (status != 0)
                    {
                        returnValue = true;
                    }
                    else
                    {
                        status = 1;
                    }
                }

                if (status == 0)
                {
                    throw new SecurityException(ExceptionMessages.AesGcmFailed);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBytes);
                CryptographicOperations.ZeroMemory(decryptedData);
            }

            return returnValue;
        }
    }
}
