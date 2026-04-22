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

using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;

namespace Yubico.YubiKit.WebAuthn.UnitTests;

public class WebAuthnAuthenticatorDataTests
{
    [Fact]
    public void Decode_WithCredProtectExtension_ParsesExtensionMap()
    {
        // Arrange - Build authenticator data with credProtect extension
        var authData = BuildAuthDataWithCredProtectExtension();

        // Act
        var decoded = WebAuthnAuthenticatorData.Decode(authData);

        // Assert
        Assert.NotNull(decoded.ParsedExtensions);
        Assert.True(decoded.ParsedExtensions.ContainsKey("credProtect"));

        var credProtectValue = decoded.ParsedExtensions["credProtect"];
        Assert.False(credProtectValue.IsEmpty);

        // Verify it's a CBOR integer (value 3)
        var reader = new CborReader(credProtectValue, CborConformanceMode.Lax);
        var value = reader.ReadInt32();
        Assert.Equal(3, value);
    }

    [Fact]
    public void Decode_NoExtensions_EmptyParsedExtensionsMap()
    {
        // Arrange - Minimal auth data with no extensions
        var authData = BuildMinimalAuthData();

        // Act
        var decoded = WebAuthnAuthenticatorData.Decode(authData);

        // Assert
        Assert.NotNull(decoded.ParsedExtensions);
        Assert.Empty(decoded.ParsedExtensions);
    }

    [Fact]
    public void Decode_MultipleExtensions_ParsesAllIdentifiers()
    {
        // Arrange - Auth data with two extensions
        var authData = BuildAuthDataWithMultipleExtensions();

        // Act
        var decoded = WebAuthnAuthenticatorData.Decode(authData);

        // Assert
        Assert.Equal(2, decoded.ParsedExtensions.Count);
        Assert.True(decoded.ParsedExtensions.ContainsKey("credProtect"));
        Assert.True(decoded.ParsedExtensions.ContainsKey("credBlob"));
    }

    // Helper: Build minimal auth data (no extensions, no attested credential)
    private static byte[] BuildMinimalAuthData()
    {
        var authData = new byte[37];

        // rpIdHash (32 bytes)
        SHA256.HashData("example.com"u8, authData.AsSpan(0, 32));

        // flags (1 byte) - UP only
        authData[32] = 0x01;

        // signCount (4 bytes, big-endian) - already zero

        return authData;
    }

    // Helper: Build auth data with credProtect extension
    private static byte[] BuildAuthDataWithCredProtectExtension()
    {
        var baseAuthData = new byte[37];

        // rpIdHash
        SHA256.HashData("example.com"u8, baseAuthData.AsSpan(0, 32));

        // flags - UP + ED (extension data)
        baseAuthData[32] = 0x01 | 0x80;

        // signCount = 0

        // Build extension CBOR map
        var extWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extWriter.WriteStartMap(1);
        extWriter.WriteTextString("credProtect");
        extWriter.WriteInt32(3); // credProtect level 3
        extWriter.WriteEndMap();

        var extensionBytes = extWriter.Encode();

        // Combine base + extensions
        var result = new byte[baseAuthData.Length + extensionBytes.Length];
        baseAuthData.CopyTo(result, 0);
        extensionBytes.CopyTo(result, baseAuthData.Length);

        return result;
    }

    // Helper: Build auth data with multiple extensions
    private static byte[] BuildAuthDataWithMultipleExtensions()
    {
        var baseAuthData = new byte[37];

        // rpIdHash
        SHA256.HashData("example.com"u8, baseAuthData.AsSpan(0, 32));

        // flags - UP + ED
        baseAuthData[32] = 0x01 | 0x80;

        // Build extension CBOR map with two extensions
        var extWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extWriter.WriteStartMap(2);

        extWriter.WriteTextString("credBlob");
        extWriter.WriteByteString([0x01, 0x02, 0x03]);

        extWriter.WriteTextString("credProtect");
        extWriter.WriteInt32(2);

        extWriter.WriteEndMap();

        var extensionBytes = extWriter.Encode();

        var result = new byte[baseAuthData.Length + extensionBytes.Length];
        baseAuthData.CopyTo(result, 0);
        extensionBytes.CopyTo(result, baseAuthData.Length);

        return result;
    }
}
