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

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// The result of one CTAP key agreement (§6.5.5.4 getKeyAgreement + §6.5.6 encapsulate): the platform's
/// COSE key to send as <c>keyAgreement</c> and the shared secret that encrypts and authenticates
/// extension material such as hmac-secret salts. Dispose zeroes the secret.
/// </summary>
internal sealed class PinUvSharedSecret : IDisposable
{
    private readonly byte[] _sharedSecret;
    private bool _disposed;

    internal PinUvSharedSecret(IReadOnlyDictionary<int, object?> keyAgreement, byte[] sharedSecret)
    {
        KeyAgreement = keyAgreement;
        _sharedSecret = sharedSecret;
    }

    /// <summary>The platform's COSE_Key for the authenticator (hmac-secret key 1).</summary>
    public IReadOnlyDictionary<int, object?> KeyAgreement { get; }

    public ReadOnlyMemory<byte> SharedSecret
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _sharedSecret;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_sharedSecret);
            _disposed = true;
        }
    }
}