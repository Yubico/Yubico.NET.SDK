// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Text.Unicode;

namespace Yubico.YubiKit.YubiHsm;

/// <summary>Owns the 16-byte password used by a YubiHSM Auth credential.</summary>
/// <remarks>
///     The factories copy borrowed input without changing it. The caller clears the source separately
///     and keeps this holder alive until every operation using it has completed.
/// </remarks>
public sealed class CredentialPassword : IDisposable
{
    private const int Length = 16;
    private readonly byte[] _bytes = new byte[Length];
    private bool _disposed;

    private CredentialPassword(ReadOnlySpan<byte> bytes) => bytes.CopyTo(_bytes);

    /// <summary>Copies UTF-8 encoded password bytes and zero-pads the copy to 16 bytes.</summary>
    /// <param name="utf8">Borrowed UTF-8 bytes, at most 16 bytes.</param>
    /// <returns>A holder the caller must dispose after use.</returns>
    /// <exception cref="ArgumentException">The input exceeds 16 bytes or is not valid UTF-8.</exception>
    public static CredentialPassword FromUtf8(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > Length || !Utf8.IsValid(utf8))
            throw new ArgumentException("The credential password must contain at most 16 valid UTF-8 bytes.", nameof(utf8));

        return new CredentialPassword(utf8);
    }

    /// <summary>Copies an exact 16-byte applet password without text decoding or padding.</summary>
    /// <param name="raw">Borrowed applet password bytes.</param>
    /// <returns>A holder the caller must dispose after use.</returns>
    /// <exception cref="ArgumentException">The input is not exactly 16 bytes.</exception>
    public static CredentialPassword FromRaw(ReadOnlySpan<byte> raw)
    {
        if (raw.Length != Length)
            throw new ArgumentException("The raw credential password must contain exactly 16 bytes.", nameof(raw));

        return new CredentialPassword(raw);
    }

    internal ReadOnlySpan<byte> Bytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _bytes;
        }
    }

    /// <summary>Clears the owned password bytes. Repeated disposal is harmless.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographicOperations.ZeroMemory(_bytes);
        _disposed = true;
    }
}
