// Copyright 2025 Yubico AB
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
using System.Text;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

public class WebAuthnClientDataTests
{
    [Fact]
    public void Create_UsesExactCcdToStringEscapesAndHash()
    {
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        var type = "test" + new string(Enumerable.Range(0, 32).Select(value => (char)value).ToArray())
            + "\"\\\u007f\u0085é😀";
        var expected = """
            {"type":"test\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000a\u000b\u000c\u000d\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f\"\\
            """ + "\u007f\u0085é😀" + """
            ","challenge":"AQ","origin":"https://example.com","crossOrigin":false}
            """;

        var data = WebAuthnClientData.Create(type, new byte[] { 1 }, origin);

        Assert.Equal(expected, Encoding.UTF8.GetString(data.JsonBytes.Span));
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), data.Hash.Span);
    }

    [Fact]
    public void Create_ProducesCorrectJson_WithKeyOrdering()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        var challenge = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var clientData = WebAuthnClientData.Create(
            "webauthn.create",
            challenge,
            origin,
            crossOrigin: false);

        var json = Encoding.UTF8.GetString(clientData.JsonBytes.Span);

        // Assert - Exact JSON with key order: type, challenge, origin, crossOrigin
        var expectedJson = "{\"type\":\"webauthn.create\",\"challenge\":\"AQIDBA\",\"origin\":\"https://example.com\",\"crossOrigin\":false}";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Create_HashIsExactly32Bytes()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        var challenge = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var clientData = WebAuthnClientData.Create(
            "webauthn.get",
            challenge,
            origin);

        // Assert
        Assert.Equal(32, clientData.Hash.Length);
    }

    [Fact]
    public void Create_HashMatchesSHA256OfJson()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        var challenge = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var clientData = WebAuthnClientData.Create(
            "webauthn.create",
            challenge,
            origin,
            crossOrigin: false);

        // Compute expected hash
        Span<byte> expectedHash = stackalloc byte[32];
        SHA256.HashData(clientData.JsonBytes.Span, expectedHash);

        // Assert
        Assert.True(expectedHash.SequenceEqual(clientData.Hash.Span));
    }

    [Fact]
    public void Create_WithCrossOriginTrue_IncludesTrue()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        var challenge = new byte[] { 0xAA, 0xBB };

        // Act
        var clientData = WebAuthnClientData.Create(
            "webauthn.get",
            challenge,
            origin,
            crossOrigin: true);

        var json = Encoding.UTF8.GetString(clientData.JsonBytes.Span);

        // Assert
        Assert.Contains("\"crossOrigin\":true", json);
    }

    [Fact]
    public void Create_WithTopOrigin_AppendsTopOriginField()
    {
        // Intentionally inconsistent crossOrigin/topOrigin values exercise only the low-level builder.
        // The public ceremony methods reject this combination; it is not a valid ceremony example.
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        var challenge = new byte[] { 0xCC };

        // Act
        var clientData = WebAuthnClientData.Create(
            "webauthn.create",
            challenge,
            origin,
            crossOrigin: false,
            topOrigin: "https://top.example.com");

        var json = Encoding.UTF8.GetString(clientData.JsonBytes.Span);

        // Assert
        Assert.Contains("\"topOrigin\":\"https://top.example.com\"", json);

        // Verify key order: type, challenge, origin, crossOrigin, topOrigin
        var expectedJson = "{\"type\":\"webauthn.create\",\"challenge\":\"zA\",\"origin\":\"https://example.com\",\"crossOrigin\":false,\"topOrigin\":\"https://top.example.com\"}";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Create_EscapesSpecialCharactersInStrings()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://example.com", out var o) ? o : throw new InvalidOperationException();
        var challenge = new byte[] { 0x01 };

        // Use a type string with special characters (contrived for testing escaping)
        var typeWithQuote = "test\"type";

        // Act - Though normally type is fixed, this tests the escaping logic
        var clientData = WebAuthnClientData.Create(
            typeWithQuote,
            challenge,
            origin);

        var json = Encoding.UTF8.GetString(clientData.JsonBytes.Span);

        // Assert - Backslash-escaped quote
        Assert.Contains("\"test\\\"type\"", json);
    }

    [Fact]
    public void Create_ByteIdenticalForFixedInputs()
    {
        // Arrange
        var origin = WebAuthnOrigin.TryParse("https://login.example.com:8443", out var o) ? o : throw new InvalidOperationException();
        var challenge = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        // Act
        var clientData1 = WebAuthnClientData.Create("webauthn.create", challenge, origin, crossOrigin: false);
        var clientData2 = WebAuthnClientData.Create("webauthn.create", challenge, origin, crossOrigin: false);

        // Assert - Multiple calls produce identical bytes
        Assert.True(clientData1.JsonBytes.Span.SequenceEqual(clientData2.JsonBytes.Span));
        Assert.True(clientData1.Hash.Span.SequenceEqual(clientData2.Hash.Span));
    }
}