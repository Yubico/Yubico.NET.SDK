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

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Equality comparer for <see cref="ReadOnlyMemory{T}"/> of byte used as dictionary keys.
/// </summary>
/// <remarks>
/// Compares byte sequences by content and hashes the full sequence.
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
    /// Uses full-content hashing via <see cref="HashCode.AddBytes"/> for robust distribution.
    /// </remarks>
    public int GetHashCode(ReadOnlyMemory<byte> obj)
    {
        ReadOnlySpan<byte> span = obj.Span;
        if (span.Length == 0)
        {
            return 0;
        }

        var hashCode = new HashCode();
        hashCode.AddBytes(span);
        return hashCode.ToHashCode();
    }
}