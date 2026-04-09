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

using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Utils;

/// <summary>
///     Wraps a <see cref="Memory{T}"/> buffer and zeroes it when disposed.
/// </summary>
/// <remarks>
///     <b>Ownership transfer:</b> This type takes ownership of the provided buffer.
///     The buffer is zeroed via <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory"/>
///     when <see cref="Dispose"/> is called. Callers must not read from or write to
///     the original <paramref name="data"/> after disposing this handle.
///     Always pass a freshly-allocated or rented buffer — never wrap caller-held memory
///     that must survive beyond the scope of this handle.
/// </remarks>
public class DisposableBufferHandle(Memory<byte> data) : IDisposable
{
    public int Length => _disposed ? 0 : data.Length;
    public int Count => Length;
    private bool _disposed;

    public Memory<byte> Data => _disposed 
        ? throw new ObjectDisposedException(nameof(DisposableBufferHandle)) 
        : data;
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(data.Span);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~DisposableBufferHandle()
    {
        Dispose();
    }
}
