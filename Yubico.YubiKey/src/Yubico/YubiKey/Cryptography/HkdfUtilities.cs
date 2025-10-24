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

namespace Yubico.YubiKey.Cryptography;

internal static class HkdfUtilities
{
    private const int Sha256HashByteLength = 32; // SHA-256 hash length in bytes

    /// <summary>
    /// Derives a key using the HKDF (HMAC-based Key Derivation Function)
    /// as specified in RFC 5869 using SHA-256.
    /// </summary>
    /// <param name="inputKeyMaterial">The input key material (IKM).</param>
    /// <param name="salt">Optional salt value. If not provided, a zero-length
    ///     salt will be used.</param>
    /// <param name="contextInfo">Optional context information (info).</param>
    /// <param name="length">The desired length of the output key material (OKM).
    ///     If not specified, defaults to 32 bytes.</param>
    /// <returns>A Memory&lt;byte&gt; containing the derived key.</returns>
    public static Memory<byte> DeriveKey(
        ReadOnlySpan<byte> inputKeyMaterial,
        ReadOnlySpan<byte> salt = default,
        ReadOnlySpan<byte> contextInfo = default,
        int length = Sha256HashByteLength)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");
        }

        if (length > 255 * Sha256HashByteLength) // 8160 bytes
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length exceeds maximum output size.");
        }

        var pseudoRandomKey = HkdfExtract(inputKeyMaterial, salt);
        return HkdfExpand(pseudoRandomKey, contextInfo, length);
    }

    private static ReadOnlyMemory<byte> HkdfExtract(ReadOnlySpan<byte> inputKeyMaterial, ReadOnlySpan<byte> salt)
    {
        using var hmac = CryptographyProviders.HmacCreator("HMACSHA256");
        hmac.Key = salt.IsEmpty ? new byte[Sha256HashByteLength] : salt.ToArray();
        return hmac.ComputeHash(inputKeyMaterial.ToArray());
    }

    private static Memory<byte> HkdfExpand(
        ReadOnlyMemory<byte> pseudoRandomKey,
        ReadOnlySpan<byte> contextInfo,
        int length)
    {
        int numberOfBlocks = (length / Sha256HashByteLength) + (length % Sha256HashByteLength == 0 ? 0 : 1);
        byte[] outputKeyMaterial = new byte[length];
        Span<byte> previousBlock = Array.Empty<byte>();

        using var hmac = CryptographyProviders.HmacCreator("HMACSHA256");

        hmac.Key = pseudoRandomKey.ToArray();
        
        for (byte index = 1; index <= numberOfBlocks; index++)
        {
            hmac.Initialize();

            int inputSize = previousBlock.Length + contextInfo.Length + 1;
            byte[] input = new byte[inputSize];
            Span<byte> indexBytes = [index];

            // Copy components: block || contextInfo || index
            previousBlock.CopyTo(input.AsSpan());
            contextInfo.CopyTo(input.AsSpan(previousBlock.Length));
            indexBytes.CopyTo(input.AsSpan(previousBlock.Length + contextInfo.Length));

            // Compute the HMAC for the current block
            byte[] currentBlock = hmac.ComputeHash(input);

            // Copy the relevant part of the current block to the output
            // If the output length is less than the full block size, copy only the required bytes
            int blockOffset = (index - 1) * Sha256HashByteLength;
            int bytesToCopy = Math.Min(Sha256HashByteLength, length - blockOffset);
            currentBlock
                .AsSpan(0, bytesToCopy)
                .CopyTo(outputKeyMaterial.AsSpan(blockOffset));
            
            previousBlock = currentBlock;
        }

        return outputKeyMaterial;
    }
}
