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

using System.Buffers;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Utils;

/// <summary>
/// A disposable buffer backed by <see cref="ArrayPool{T}"/> that securely clears memory on disposal.
/// Implements <see cref="IMemoryOwner{T}"/> for compatibility with async APIs.
/// </summary>
public sealed class DisposableArrayPoolBuffer : IMemoryOwner<byte>
{
    private byte[]? _rentedBuffer;
    private readonly int _length;

    /// <summary>
    /// Creates a new buffer of the specified size from the shared array pool.
    /// </summary>
    /// <param name="size">The size of the buffer to allocate.</param>
    /// <param name="clearOnCreate">If true, zeros the buffer after allocation. Defaults to true.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when size is zero or negative.</exception>
    public DisposableArrayPoolBuffer(int size, bool clearOnCreate = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        _rentedBuffer = ArrayPool<byte>.Shared.Rent(size);
        _length = size;
        if (clearOnCreate)
        {
            CryptographicOperations.ZeroMemory(_rentedBuffer);
        }
    }

    /// <inheritdoc />
    public Memory<byte> Memory => _rentedBuffer is not null 
        ? _rentedBuffer.AsMemory(0, _length) 
        : throw new ObjectDisposedException(nameof(DisposableArrayPoolBuffer));

    /// <summary>
    /// Gets a span over the buffer's memory.
    /// </summary>
    public Span<byte> Span => Memory.Span;

    /// <summary>
    /// Gets the logical length of the buffer.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Creates a buffer from a source span, copying the data.
    /// </summary>
    /// <param name="source">The source data to copy into the new buffer.</param>
    /// <returns>A new buffer containing a copy of the source data.</returns>
    public static DisposableArrayPoolBuffer CreateFromSpan(ReadOnlySpan<byte> source)
    {
        var buffer = new DisposableArrayPoolBuffer(source.Length, clearOnCreate: false);
        source.CopyTo(buffer.Span);
        return buffer;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_rentedBuffer is null)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_rentedBuffer);
        ArrayPool<byte>.Shared.Return(_rentedBuffer, clearArray: true);
        _rentedBuffer = null;
    }
}