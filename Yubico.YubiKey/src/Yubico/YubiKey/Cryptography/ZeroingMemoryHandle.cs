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
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

#pragma warning disable CA1710
internal class ZeroingMemoryHandle : IDisposable, IReadOnlyCollection<byte>, IEnumerable<byte>
#pragma warning restore CA1710
{
    private readonly byte[] _data;
    private bool _disposed;

    public int Length => _disposed ? 0 : _data.Length;
    public int Count => Length; // For IReadOnlyCollection

    public byte this[int index] => _disposed 
        ? throw new ObjectDisposedException(nameof(ZeroingMemoryHandle)) 
        : _data[index];

    public ZeroingMemoryHandle(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public ReadOnlySpan<byte> AsSpan() => _disposed 
        ? ReadOnlySpan<byte>.Empty 
        : _data.AsSpan();

    public byte[] Data => _disposed 
        ? throw new ObjectDisposedException(nameof(ZeroingMemoryHandle)) 
        : _data;

    public void CopyTo(byte[] destination, int destinationIndex = 0)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ZeroingMemoryHandle));
        }

        Buffer.BlockCopy(_data, 0, destination, destinationIndex, _data.Length);
    }

    public ReadOnlySpan<byte> Slice(int start, int length) => _disposed 
        ? ReadOnlySpan<byte>.Empty 
        : _data.AsSpan(start, length);

    public IEnumerator<byte> GetEnumerator()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ZeroingMemoryHandle));
        }

        for (int i = 0; i < _data.Length; i++)
        {
            yield return _data[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_data);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~ZeroingMemoryHandle()
    {
        Dispose();
    }
}
