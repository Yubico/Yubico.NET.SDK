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

using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

internal sealed class TestPinUvAuthProtocol : IPinUvAuthProtocol
{
    public int Version => 2;

    public int AuthenticationTagLength => 16;

    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message) => new byte[16];

    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext) => throw new NotImplementedException();

    public void Dispose() { }

    public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
        IReadOnlyDictionary<int, object?> peerCoseKey) => throw new NotImplementedException();

    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext) => throw new NotImplementedException();

    public byte[] Kdf(ReadOnlySpan<byte> z) => throw new NotImplementedException();

    public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature) =>
        throw new NotImplementedException();
}
