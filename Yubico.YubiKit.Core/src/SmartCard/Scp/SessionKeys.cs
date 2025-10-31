// Copyright (C) 2024 Yubico.
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

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     Holds the session keys derived for an SCP session.
/// </summary>
internal sealed class SessionKeys : IDisposable
{
    private byte[]? _dek;
    private bool _disposed;
    private byte[]? _senc;
    private byte[]? _smac;
    private byte[]? _srmac;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SessionKeys" /> class.
    /// </summary>
    /// <param name="senc">The session encryption key (must be 16 bytes).</param>
    /// <param name="smac">The session MAC key (must be 16 bytes).</param>
    /// <param name="srmac">The session response MAC key (must be 16 bytes).</param>
    /// <param name="dek">The optional data encryption key (must be 16 bytes if provided).</param>
    /// <exception cref="ArgumentException">Thrown if any key is not 16 bytes.</exception>
    public SessionKeys(ReadOnlySpan<byte> senc, ReadOnlySpan<byte> smac, ReadOnlySpan<byte> srmac,
        ReadOnlySpan<byte> dek = default)
    {
        if (senc.Length != 16) throw new ArgumentException("S-ENC key must be 16 bytes", nameof(senc));
        if (smac.Length != 16) throw new ArgumentException("S-MAC key must be 16 bytes", nameof(smac));
        if (srmac.Length != 16) throw new ArgumentException("S-RMAC key must be 16 bytes", nameof(srmac));
        if (dek.Length != 0 && dek.Length != 16)
            throw new ArgumentException("DEK must be 16 bytes if provided", nameof(dek));

        _senc = senc.ToArray();
        _smac = smac.ToArray();
        _srmac = srmac.ToArray();
        _dek = dek.Length == 16 ? dek.ToArray() : null;
    }

    /// <summary>
    ///     Gets the session encryption key (S-ENC).
    /// </summary>
    public ReadOnlySpan<byte> Senc => _senc;

    /// <summary>
    ///     Gets the session MAC key (S-MAC).
    /// </summary>
    public ReadOnlySpan<byte> Smac => _smac;

    /// <summary>
    ///     Gets the session response MAC key (S-RMAC).
    /// </summary>
    public ReadOnlySpan<byte> Srmac => _srmac;

    /// <summary>
    ///     Gets the data encryption key (DEK), if available.
    ///     Only needed for SecurityDomainSession.PutKey operations.
    /// </summary>
    public ReadOnlySpan<byte> Dek => _dek;

    #region IDisposable Members

    /// <summary>
    ///     Releases the resources used by this instance and securely zeroes all key material.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        if (_senc != null)
        {
            CryptographicOperations.ZeroMemory(_senc);
            _senc = null;
        }

        if (_smac != null)
        {
            CryptographicOperations.ZeroMemory(_smac);
            _smac = null;
        }

        if (_srmac != null)
        {
            CryptographicOperations.ZeroMemory(_srmac);
            _srmac = null;
        }

        if (_dek != null)
        {
            CryptographicOperations.ZeroMemory(_dek);
            _dek = null;
        }

        _disposed = true;
    }

    #endregion
}