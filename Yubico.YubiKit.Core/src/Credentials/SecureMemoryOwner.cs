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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// Internal memory owner that securely zeros its contents when disposed.
/// </summary>
/// <remarks>
/// This class wraps an ArrayPool-rented buffer and ensures:
/// <list type="bullet">
/// <item>Memory is zeroed on disposal using <see cref="CryptographicOperations.ZeroMemory"/></item>
/// <item>Buffer is returned to ArrayPool with <c>clearArray: true</c> for defense-in-depth</item>
/// <item>Throws <see cref="ObjectDisposedException"/> if accessed after disposal</item>
/// </list>
/// </remarks>
internal sealed class SecureMemoryOwner : IMemoryOwner<byte>
{
    private byte[]? _buffer;
    private readonly int _length;

    internal SecureMemoryOwner(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        _buffer = ArrayPool<byte>.Shared.Rent(size);
        _length = size;
        CryptographicOperations.ZeroMemory(_buffer);
    }

    /// <inheritdoc />
    public Memory<byte> Memory
    {
        get
        {
            if (_buffer is null)
            {
                throw new ObjectDisposedException(nameof(SecureMemoryOwner));
            }

            return _buffer.AsMemory(0, _length);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_buffer is null)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_buffer);
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _buffer = null;
    }
}
