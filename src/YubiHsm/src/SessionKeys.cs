// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Holds the three 16-byte session keys (S-ENC, S-MAC, S-RMAC) derived from
///     a YubiHSM Auth CALCULATE response. Disposal clears the three arrays owned by this instance.
/// </summary>
/// <remarks>
///     Key properties expose spans over the owned arrays. Use these views within the instance's lifetime;
///     disposal also clears the storage seen by previously obtained spans. Accessing a key property
///     after disposal throws <see cref="ObjectDisposedException" />. The caller clears the original
///     <see cref="Parse" /> input and any additional copies it makes separately.
///     Parsing accepts exactly the 48-byte key region. A full applet CALCULATE response can contain
///     additional bytes: vendor host code has handled 56-byte responses since 2023, and a supplied
///     symmetric cryptogram produced that length on a tested development YubiKey. The session currently
///     passes the full response here, so that path raises <see cref="ArgumentException" />. This parser
///     leaves the additional bytes' meaning and firmware applicability to a separately verified response contract.
/// </remarks>
public sealed class SessionKeys : IDisposable
{
    /// <summary>
    ///     The length accepted by this key parser: S-ENC, S-MAC and S-RMAC (3 x 16 bytes).
    /// </summary>
    public const int ExpectedResponseLength = 48;

    private const int KeyLength = 16;

    private readonly byte[] _sEnc;
    private readonly byte[] _sMac;
    private readonly byte[] _sRmac;
    private bool _disposed;

    private SessionKeys(byte[] sEnc, byte[] sMac, byte[] sRmac)
    {
        _sEnc = sEnc;
        _sMac = sMac;
        _sRmac = sRmac;
    }

    /// <summary>
    ///     Gets the session encryption key (S-ENC).
    /// </summary>
    public ReadOnlySpan<byte> SEnc
    {
        get
        {
            ThrowIfDisposed();
            return _sEnc;
        }
    }

    /// <summary>
    ///     Gets the session MAC key (S-MAC).
    /// </summary>
    public ReadOnlySpan<byte> SMac
    {
        get
        {
            ThrowIfDisposed();
            return _sMac;
        }
    }

    /// <summary>
    ///     Gets the session response MAC key (S-RMAC).
    /// </summary>
    public ReadOnlySpan<byte> SRmac
    {
        get
        {
            ThrowIfDisposed();
            return _sRmac;
        }
    }

    /// <summary>
    ///     Copies a 48-byte CALCULATE response into three independently owned key arrays.
    /// </summary>
    /// <param name="response">The borrowed 48-byte key region. The caller retains responsibility for clearing its storage.</param>
    /// <returns>A new <see cref="SessionKeys" /> instance. The caller must dispose it.</returns>
    /// <exception cref="ArgumentException">Thrown when the response is not exactly 48 bytes.</exception>
    public static SessionKeys Parse(ReadOnlySpan<byte> response)
    {
        if (response.Length != ExpectedResponseLength)
            throw new ArgumentException(
                $"CALCULATE response must be exactly {ExpectedResponseLength} bytes, got {response.Length}.",
                nameof(response));

        var sEnc = response[..KeyLength].ToArray();
        var sMac = response[KeyLength..(KeyLength * 2)].ToArray();
        var sRmac = response[(KeyLength * 2)..].ToArray();

        return new SessionKeys(sEnc, sMac, sRmac);
    }

    /// <summary>
    ///     Clears the owned key arrays and rejects subsequent key-property access. Repeated disposal is safe.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographicOperations.ZeroMemory(_sEnc);
        CryptographicOperations.ZeroMemory(_sMac);
        CryptographicOperations.ZeroMemory(_sRmac);
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}