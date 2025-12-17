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

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Cryptography;

internal static class X964Kdf
{
    /// <summary>
    ///     Core X9.63-2001 KDF implementation using SHA-256.
    ///     Derives key material from shared secret Z and optional SharedInfo.
    /// </summary>
    /// <param name="z">Shared secret material (input keying material)</param>
    /// <param name="sharedInfo">Optional shared information (can be empty)</param>
    /// <param name="keyDataLength">Desired output length in bytes</param>
    /// <returns>Derived key material of exactly keyDataLength bytes</returns>
    /// <exception cref="ArgumentException">Thrown if keyDataLength is invalid</exception>
    internal static byte[] DeriveKeyMaterial(
        ReadOnlySpan<byte> z,
        ReadOnlySpan<byte> sharedInfo,
        int keyDataLength)
    {
        if (keyDataLength <= 0)
            throw new ArgumentException("Key data length must be positive", nameof(keyDataLength));

        // ANS X9.63-2001 KDF with SHA-256
        // Output = Hash(Z || Counter || SharedInfo) for Counter = 1, 2, 3, ...
        // Counter is 32-bit big-endian starting at 1

        const int hashSize = 32; // SHA-256 output size
        var result = new byte[keyDataLength];
        var counter = 1;
        var bytesGenerated = 0;

        Span<byte> counterBytes = stackalloc byte[4];
        Span<byte> hash = stackalloc byte[hashSize];

        while (bytesGenerated < keyDataLength)
        {
            using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            // Write counter as big-endian 32-bit integer
            BinaryPrimitives.WriteInt32BigEndian(counterBytes, counter);

            // Hash(Z || Counter || SharedInfo)
            sha256.AppendData(z);
            sha256.AppendData(counterBytes);
            sha256.AppendData(sharedInfo);

            var bytesWritten = sha256.GetHashAndReset(hash);
            if (bytesWritten != hashSize)
                throw new InvalidOperationException("Hash computation failed");

            // Copy hash bytes to result (may be partial for last iteration)
            var bytesToCopy = Math.Min(hashSize, keyDataLength - bytesGenerated);
            hash[..bytesToCopy].CopyTo(result.AsSpan(bytesGenerated, bytesToCopy));

            bytesGenerated += bytesToCopy;
            counter++;
        }

        return result;
    }
}