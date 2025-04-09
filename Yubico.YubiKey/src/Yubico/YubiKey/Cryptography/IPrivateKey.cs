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

namespace Yubico.YubiKey.Cryptography;

public interface IPrivateKey : IKeyBase
{
    /// <summary>
    /// Exports the current key in the PKCS#8 PrivateKeyInfo format.
    /// </summary>
    /// <returns>
    /// A byte array containing the PKCS#8 PrivateKeyInfo representation of this key.
    /// </returns>
    public byte[] ExportPkcs8PrivateKey();

    /// <summary>
    /// Clears the buffers containing private key data.
    /// </summary>
    public void Clear();
}

public abstract class PrivateKey : IPrivateKey, IDisposable
{
    private bool _disposed;

    /// <inheritdoc /> 
    public abstract KeyType KeyType { get; }

    /// <inheritdoc />
    public abstract byte[] ExportPkcs8PrivateKey();

    /// <inheritdoc /> 
    public abstract void Clear();

    /// <summary>
    /// Clears the private key data and disposes the object
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}
