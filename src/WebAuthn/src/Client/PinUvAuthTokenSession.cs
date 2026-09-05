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
/// <para>
/// This session takes ownership of the token array and zeroes it on disposal. It deliberately
/// does not copy: the array is the decrypted PIN/UV auth token that
/// <c>ClientPin.GetPinUvAuthTokenUsing*Async</c> allocates and returns to a single caller, so
/// copying would leave a second live plaintext token that nothing zeroes. There must be exactly
/// one live copy of a decrypted token, and this session is its owner.
/// </para>
/// <para>
/// The protocol instance is NOT disposed by this session (owned by backend).
/// </para>
/// </remarks>
internal sealed class PinUvAuthTokenSession : IDisposable
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
    /// <param name="token">
    /// The token bytes. Ownership transfers to this session; the caller must not keep using or
    /// separately zero the array.
    /// </param>
    /// <remarks>
    /// The token is adopted before anything that can throw, so a failed construction still leaves
    /// the array reachable only from this instance, whose finalizer clears it.
    /// </remarks>
    internal PinUvAuthTokenSession(IPinUvAuthProtocol protocol, byte[] token)
    {
        _token = token ?? throw new ArgumentNullException(nameof(token));
        Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
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
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer fallback: zeroes the token bytes if Dispose was not called.
    /// CLAUDE.md mandates IDisposable + defensive zeroing for owned sensitive byte[].
    /// </summary>
    ~PinUvAuthTokenSession()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_token);
        }
    }
}