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

public class DisposableArrayPoolBuffer : IDisposable
{
    private readonly Memory<byte> _buffer;
    private byte[]? _rentedBuffer;

    public DisposableArrayPoolBuffer(int size, bool clear = true)
    {
        _rentedBuffer = ArrayPool<byte>.Shared.Rent(size);
        if (clear) CryptographicOperations.ZeroMemory(_rentedBuffer);
        _buffer = _rentedBuffer.AsMemory(0, size);
    }

    public Span<byte> Span => _buffer.Span;

    #region IDisposable Members

    public void Dispose()
    {
        if (_rentedBuffer == null) return;

        CryptographicOperations.ZeroMemory(_rentedBuffer);
        ArrayPool<byte>.Shared.Return(_rentedBuffer);
        _rentedBuffer = null;
    }

    #endregion
}