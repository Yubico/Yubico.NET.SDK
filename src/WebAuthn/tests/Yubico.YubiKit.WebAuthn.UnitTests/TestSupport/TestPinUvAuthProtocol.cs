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
    /// <summary>
    /// Fills issued authentication tags with a recognisable non-zero pattern.
    /// </summary>
    /// <remarks>
    /// An all-zero tag would make "this buffer was zeroed" assertions pass without the production
    /// code doing anything, which is the failure mode <see cref="TokenBufferAssert"/> exists to
    /// avoid. The value is arbitrary; only its being non-zero matters.
    /// </remarks>
    private const byte TagSentinel = 0x5A;

    public int Version => 2;

    public int AuthenticationTagLength => 16;

    /// <summary>
    /// Every tag handed out by <see cref="Authenticate"/>, in call order.
    /// </summary>
    /// <remarks>
    /// Recorded so a test can assert on what secret-derived material a ceremony actually produced.
    /// An empty list after a failed ceremony is a meaningful result rather than an absent one: it
    /// says the tag was never computed, so there was nothing left live to clean up.
    /// </remarks>
    public List<byte[]> IssuedAuthTags { get; } = [];

    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        var tag = new byte[AuthenticationTagLength];
        Array.Fill(tag, TagSentinel);
        IssuedAuthTags.Add(tag);
        return tag;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext) => throw new NotImplementedException();

    public void Dispose() { }

    public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
        IReadOnlyDictionary<int, object?> peerCoseKey) => throw new NotImplementedException();

    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext) => throw new NotImplementedException();

    public byte[] Kdf(ReadOnlySpan<byte> z) => throw new NotImplementedException();

    public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature) =>
        throw new NotImplementedException();
}