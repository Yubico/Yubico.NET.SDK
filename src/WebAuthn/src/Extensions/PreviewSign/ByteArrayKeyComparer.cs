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

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Equality comparer for <see cref="ReadOnlyMemory{T}"/> of byte used as dictionary keys.
/// </summary>
/// <remarks>
/// Uses sequence equality for comparison and a simple hash based on the first 4 bytes
/// for hash code generation.
/// </remarks>
internal sealed class ByteArrayKeyComparer : IEqualityComparer<ReadOnlyMemory<byte>>
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly ByteArrayKeyComparer Instance = new();

    private ByteArrayKeyComparer()
    {
    }

    /// <summary>
    /// Determines whether two byte sequences are equal.
    /// </summary>
    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) =>
        x.Span.SequenceEqual(y.Span);

    /// <summary>
    /// Returns a hash code for a byte sequence.
    /// </summary>
    /// <remarks>
    /// Uses the first 4 bytes (or fewer if shorter) as a simple hash.
    /// This is not a cryptographic hash but provides reasonable distribution
    /// for credential IDs which are typically random.
    /// </remarks>
    public int GetHashCode(ReadOnlyMemory<byte> obj)
    {
        ReadOnlySpan<byte> span = obj.Span;
        if (span.Length == 0)
        {
            return 0;
        }

        if (span.Length >= 4)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(span[..4]);
        }

        // For shorter sequences, combine available bytes
        int hash = 0;
        for (int i = 0; i < span.Length; i++)
        {
            hash = (hash << 8) | span[i];
        }
        return hash;
    }
}
