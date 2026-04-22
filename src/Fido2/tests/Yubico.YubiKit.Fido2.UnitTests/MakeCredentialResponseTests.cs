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
        // Arrange - Build a minimal MakeCredential response with key 8 (unsignedExtensionOutputs)
        var writer = new CborWriter(CborConformanceMode.Lax);
        writer.WriteStartMap(4); // fmt, authData, attStmt, unsignedExtensionOutputs

        // Key 1: fmt
        writer.WriteInt32(1);
        writer.WriteTextString("none");

        // Key 2: authData (minimal: rpIdHash + flags + signCount = 37 bytes)
        writer.WriteInt32(2);
        var authData = new byte[37];
        authData[32] = 0x01; // UP bit set
        writer.WriteByteString(authData);

        // Key 3: attStmt (empty for "none" format)
        writer.WriteInt32(3);
        writer.WriteStartMap(0);
        writer.WriteEndMap();

        // Key 8: unsignedExtensionOutputs
        writer.WriteInt32(8);
        writer.WriteStartMap(1); // One extension
        writer.WriteTextString("previewSign");
        // Nested CBOR value (just a simple map for test)
        writer.WriteStartMap(1);
        writer.WriteInt32(7); // att-obj key
        writer.WriteByteString(new byte[] { 0xAA, 0xBB }); // Mock attestation object
        writer.WriteEndMap();
        writer.WriteEndMap();

        writer.WriteEndMap();

        byte[] cbor = writer.Encode();

        // Act
        var response = MakeCredentialResponse.Decode(cbor);

        // Assert
        Assert.NotNull(response.UnsignedExtensionOutputs);
        Assert.True(response.UnsignedExtensionOutputs.ContainsKey("previewSign"));
        Assert.True(response.UnsignedExtensionOutputs["previewSign"].Length > 0);
    }
}
