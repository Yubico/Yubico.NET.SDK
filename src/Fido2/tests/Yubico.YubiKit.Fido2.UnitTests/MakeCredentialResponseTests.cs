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

using System.Formats.Cbor;
using Xunit;
using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.Fido2.UnitTests;

public class MakeCredentialResponseTests
{
    [Fact(Timeout = 5000)]
    public void Decode_WithUnsignedExtensionOutputs_CapturesExtensionMap()
    {
        // Arrange - Build a minimal MakeCredential response with key 6 (unsignedExtensionOutputs).
        // Per CTAP 2.2 / WebAuthn L3, key 6 is the canonical position for unsignedExtensionOutputs,
        // aligned with yubikit-swift, yubikit-android, and yubikit-python.
        byte[] cbor = BuildMakeCredentialResponseCbor(unsignedExtensionOutputsKey: 6);

        // Act
        var response = MakeCredentialResponse.Decode(cbor);

        // Assert
        Assert.NotNull(response.UnsignedExtensionOutputs);
        Assert.True(response.UnsignedExtensionOutputs.ContainsKey("previewSign"));
        Assert.True(response.UnsignedExtensionOutputs["previewSign"].Length > 0);
    }

    [Fact(Timeout = 5000)]
    public void Decode_WithUnsignedExtensionOutputsAtLegacyKey8_IsSilentlyDropped()
    {
        // Regression guard: an early CTAP v4 draft used key 8 for unsignedExtensionOutputs,
        // and v2 .NET historically parsed at that key (silent data loss against real firmware
        // emitting key 6). This test pins the new behavior: a response carrying the legacy
        // key 8 must NOT populate UnsignedExtensionOutputs (the parser should ignore it).
        byte[] cbor = BuildMakeCredentialResponseCbor(unsignedExtensionOutputsKey: 8);

        var response = MakeCredentialResponse.Decode(cbor);

        Assert.Null(response.UnsignedExtensionOutputs);
    }

    private static byte[] BuildMakeCredentialResponseCbor(int unsignedExtensionOutputsKey)
    {
        var writer = new CborWriter(CborConformanceMode.Lax);
        writer.WriteStartMap(4);

        writer.WriteInt32(1);
        writer.WriteTextString("none");

        writer.WriteInt32(2);
        var authData = new byte[37];
        authData[32] = 0x01;
        writer.WriteByteString(authData);

        writer.WriteInt32(3);
        writer.WriteStartMap(0);
        writer.WriteEndMap();

        writer.WriteInt32(unsignedExtensionOutputsKey);
        writer.WriteStartMap(1);
        writer.WriteTextString("previewSign");
        writer.WriteStartMap(1);
        writer.WriteInt32(7);
        writer.WriteByteString(new byte[] { 0xAA, 0xBB });
        writer.WriteEndMap();
        writer.WriteEndMap();

        writer.WriteEndMap();

        return writer.Encode();
    }
}
