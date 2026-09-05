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

using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
/// Pins the ownership contract of <see cref="PinUvAuthTokenSession"/>: it adopts the caller's
/// token array rather than copying it.
/// </summary>
/// <remarks>
/// The array handed to the constructor is the decrypted PIN/UV auth token that
/// <c>ClientPin.GetPinUvAuthTokenUsing*Async</c> allocates and returns. Nothing else holds a
/// reference to it, so the session must be its single owner. If the session copied instead,
/// the caller's array would stay live and unzeroed until garbage collection.
/// </remarks>
public class PinUvAuthTokenSessionTests
{
    [Fact]
    public void Dispose_ZeroesTheCallerSuppliedArray()
    {
        var token = TokenBufferAssert.CreateSentinelToken();

        var session = new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), token);
        TokenBufferAssert.NotZeroed(token, "the session must not zero the token before disposal");

        session.Dispose();

        TokenBufferAssert.Zeroed(token, "disposing the session must zero the array it was given");
    }

    [Fact]
    public void Token_IsTheCallerSuppliedArray_NotACopy()
    {
        var token = TokenBufferAssert.CreateSentinelToken();

        using var session = new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), token);

        // A copy would leave a second live plaintext token that no one zeroes.
        Assert.True(
            session.Token == token.AsSpan(),
            "the session must expose the caller's array, not a private copy of it");
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var token = TokenBufferAssert.CreateSentinelToken();
        var session = new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), token);

        session.Dispose();
        session.Dispose();

        TokenBufferAssert.Zeroed(token);
        _ = Assert.Throws<ObjectDisposedException>(() => _ = session.Token.Length);
    }
}