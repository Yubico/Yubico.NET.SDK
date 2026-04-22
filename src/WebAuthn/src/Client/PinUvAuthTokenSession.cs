// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Holds a PIN/UV auth token and associated protocol instance.
/// </summary>
/// <remarks>
/// This session owns the token bytes and zeroes them on disposal.
/// The protocol instance is NOT disposed by this session (owned by backend).
/// </remarks>
public sealed class PinUvAuthTokenSession : IDisposable
{
    private readonly byte[] _token;
    private bool _disposed;

    /// <summary>
    /// Gets the PIN/UV auth protocol instance.
    /// </summary>
    public IPinUvAuthProtocol Protocol { get; }

    /// <summary>
    /// Gets the token bytes as a read-only span.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    public ReadOnlySpan<byte> Token
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _token;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PinUvAuthTokenSession"/>.
    /// </summary>
    /// <param name="protocol">The PIN/UV auth protocol instance (not owned by this session).</param>
    /// <param name="token">The token bytes (copied and owned by this session).</param>
    internal PinUvAuthTokenSession(IPinUvAuthProtocol protocol, ReadOnlySpan<byte> token)
    {
        Protocol = protocol;
        _token = token.ToArray();
    }

    /// <summary>
    /// Disposes the session and zeroes the token bytes.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_token);
            _disposed = true;
        }
    }
}
