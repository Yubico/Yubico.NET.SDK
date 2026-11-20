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
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2.UnitTests;

public class MakeCredentialResponseTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Decode_TransfersWritableSecretBuffersWithoutChangingInput(bool makeCredential)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        byte[] authData = new byte[41];
        authData[32] = 0x80;
        // ED suffix: {"x": true}.
        byte[] extension = [0xA1, 0x61, 0x78, 0xF5];
        extension.CopyTo(authData.AsSpan(37));
        writer.WriteStartMap(makeCredential ? 5 : 3);
        if (makeCredential)
        {
            writer.WriteInt32(1);
            writer.WriteTextString("none");
        }
        writer.WriteInt32(2);
        writer.WriteByteString(authData);
        writer.WriteInt32(3);
        if (makeCredential)
        {
            writer.WriteEncodedValue([0xA0]);
        }
        else
        {
            writer.WriteByteString([0x01]);
        }
        writer.WriteInt32(makeCredential ? 5 : 7);
        writer.WriteByteString([0x12, 0x34]);
        if (makeCredential)
        {
            writer.WriteInt32(6);
            writer.WriteEncodedValue(extension);
        }
        writer.WriteEndMap();
        byte[] input = writer.Encode();
        byte[] original = input.ToArray();

        (Memory<byte> signed, Memory<byte>? key, Memory<byte>? outputs, AuthenticatorData parsed) = makeCredential
            ? Inspect(MakeCredentialResponse.Decode(input))
            : InspectAssertion(GetAssertionResponse.Decode(input));

        Assert.Equal(authData, signed.ToArray());
        Memory<byte> signedOutputs = Assert.IsType<Memory<byte>>(outputs);
        Memory<byte> largeBlobKey = Assert.IsType<Memory<byte>>(key);
        Assert.Equal(extension, signedOutputs.ToArray());
        Assert.Equal(new byte[] { 0x12, 0x34 }, largeBlobKey.ToArray());

        CryptographicOperations.ZeroMemory(signedOutputs.Span);
        CryptographicOperations.ZeroMemory(largeBlobKey.Span);
        Assert.Equal(new byte[4], signed[37..].ToArray());
        Assert.Equal(new byte[4], parsed.RawData[37..].ToArray());
        Assert.Equal(original, input);
    }

    private static (Memory<byte>, Memory<byte>?, Memory<byte>?, AuthenticatorData) Inspect(MakeCredentialResponse response)
    {
        Memory<byte> unsigned = Assert.Single(Assert.IsAssignableFrom<IReadOnlyDictionary<string, Memory<byte>>>(
            response.UnsignedExtensionOutputs)).Value;
        Assert.Equal(new byte[] { 0xF5 }, unsigned.ToArray());
        CryptographicOperations.ZeroMemory(unsigned.Span);
        return (response.AuthenticatorDataRaw, response.LargeBlobKey, response.ExtensionOutputs, response.AuthenticatorData);
    }

    private static (Memory<byte>, Memory<byte>?, Memory<byte>?, AuthenticatorData) InspectAssertion(GetAssertionResponse response) =>
        (response.AuthenticatorDataRaw, response.LargeBlobKey, response.ExtensionOutputs, response.AuthenticatorData);

    [Fact(Timeout = 5000)]
    public void Decode_WithUnsignedExtensionOutputs_CapturesExtensionMap()
    {
        // Arrange - Build a minimal MakeCredential response with key 6 (unsignedExtensionOutputs).
        // CTAP 2.3 §6.1 assigns makeCredential unsigned outputs to key 6. This fixture checks map-key
        // presence and a nonempty encoded value, not valid previewSign attestation or firmware behavior.
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
        // Pins ignored key 8 in this makeCredential decoder. CTAP 2.3 uses key 8 for getAssertion,
        // not makeCredential; this is not evidence of a general "CTAP v4" or live-firmware behavior.
        byte[] cbor = BuildMakeCredentialResponseCbor(unsignedExtensionOutputsKey: 8);

        var response = MakeCredentialResponse.Decode(cbor);

        Assert.Null(response.UnsignedExtensionOutputs);
        Assert.Null(response.LargeBlobKey);
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
