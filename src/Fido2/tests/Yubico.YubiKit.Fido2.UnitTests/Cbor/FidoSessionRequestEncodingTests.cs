// Copyright 2026 Yubico AB
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
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.UnitTests.Cbor;

public sealed class FidoSessionRequestEncodingTests
{
    [Fact]
    public void BuildMakeCredentialRequest_WithRequiredFields_WritesCommandAndRequiredParameters()
    {
        var request = FidoSessionRequestEncoding.BuildMakeCredentialRequest(
            ClientDataHash(),
            new PublicKeyCredentialRpEntity("example.com", "Example"),
            new PublicKeyCredentialUserEntity(new byte[] { 0x01, 0x02, 0x03, 0x04 }, "alice", "Alice"),
            [new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.ES256)],
            options: null);

        Assert.Equal(CtapCommand.MakeCredential, request[0]);
        var reader = CborPayloadReader(request);

        Assert.Equal(4, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(ClientDataHash().ToArray(), reader.ReadByteString());
        Assert.Equal(2, reader.ReadInt32());
        SkipMap(reader);
        Assert.Equal(3, reader.ReadInt32());
        SkipMap(reader);
        Assert.Equal(4, reader.ReadInt32());
        Assert.Equal(1, reader.ReadStartArray());
        SkipMap(reader);
        reader.ReadEndArray();
        reader.ReadEndMap();
    }

    [Fact]
    public void BuildMakeCredentialRequest_WithAllOptions_WritesOptionalParametersInOrder()
    {
        var options = new MakeCredentialOptions
        {
            ExcludeList = [new PublicKeyCredentialDescriptor(new byte[] { 0xAA, 0xBB })],
            Extensions = EncodeSingleExtension("credProtect", 2),
            ResidentKey = true,
            UserPresence = false,
            UserVerification = true,
            PinUvAuthParam = new byte[] { 0x10, 0x11, 0x12, 0x13 },
            PinUvAuthProtocol = 1,
            EnterpriseAttestation = 2
        };

        var request = FidoSessionRequestEncoding.BuildMakeCredentialRequest(
            ClientDataHash(),
            new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
            [new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.ES256)],
            options);

        var reader = CborPayloadReader(request);
        Assert.Equal(10, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        reader.ReadByteString();
        Assert.Equal(2, reader.ReadInt32());
        SkipMap(reader);
        Assert.Equal(3, reader.ReadInt32());
        SkipMap(reader);
        Assert.Equal(4, reader.ReadInt32());
        SkipArray(reader);
        Assert.Equal(5, reader.ReadInt32());
        SkipArray(reader);
        Assert.Equal(6, reader.ReadInt32());
        SkipMap(reader);
        Assert.Equal(7, reader.ReadInt32());
        AssertOptionMap(reader, ("rk", true), ("up", false), ("uv", true));
        Assert.Equal(8, reader.ReadInt32());
        Assert.Equal([0x10, 0x11, 0x12, 0x13], reader.ReadByteString());
        Assert.Equal(9, reader.ReadInt32());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(10, reader.ReadInt32());
        Assert.Equal(2, reader.ReadInt32());
        reader.ReadEndMap();
    }

    [Fact]
    public void BuildGetAssertionRequest_WithRequiredFields_WritesCommandAndRequiredParameters()
    {
        var request = FidoSessionRequestEncoding.BuildGetAssertionRequest(
            "example.com",
            ClientDataHash(),
            options: null);

        Assert.Equal(CtapCommand.GetAssertion, request[0]);
        var reader = CborPayloadReader(request);

        Assert.Equal(2, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal("example.com", reader.ReadTextString());
        Assert.Equal(2, reader.ReadInt32());
        Assert.Equal(ClientDataHash().ToArray(), reader.ReadByteString());
        reader.ReadEndMap();
    }

    [Fact]
    public void BuildGetAssertionRequest_WithAllOptions_WritesOptionalParametersInOrder()
    {
        var options = new GetAssertionOptions
        {
            AllowList = [new PublicKeyCredentialDescriptor(new byte[] { 0xAA, 0xBB })],
            Extensions = EncodeSingleExtension("hmac-secret", true),
            UserPresence = false,
            UserVerification = true,
            PinUvAuthParam = new byte[] { 0x20, 0x21, 0x22, 0x23 },
            PinUvAuthProtocol = 1
        };

        var request = FidoSessionRequestEncoding.BuildGetAssertionRequest(
            "example.com",
            ClientDataHash(),
            options);

        var reader = CborPayloadReader(request);
        Assert.Equal(7, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal("example.com", reader.ReadTextString());
        Assert.Equal(2, reader.ReadInt32());
        reader.ReadByteString();
        Assert.Equal(3, reader.ReadInt32());
        SkipArray(reader);
        Assert.Equal(4, reader.ReadInt32());
        SkipMap(reader);
        Assert.Equal(5, reader.ReadInt32());
        AssertOptionMap(reader, ("up", false), ("uv", true));
        Assert.Equal(6, reader.ReadInt32());
        Assert.Equal([0x20, 0x21, 0x22, 0x23], reader.ReadByteString());
        Assert.Equal(7, reader.ReadInt32());
        Assert.Equal(1, reader.ReadInt32());
        reader.ReadEndMap();
    }

    private static ReadOnlyMemory<byte> ClientDataHash()
    {
        var hash = new byte[32];
        for (byte i = 0; i < hash.Length; i++)
        {
            hash[i] = i;
        }

        return hash;
    }

    private static ReadOnlyMemory<byte> EncodeSingleExtension(string key, object value)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteTextString(key);
        switch (value)
        {
            case bool boolValue:
                writer.WriteBoolean(boolValue);
                break;
            case int intValue:
                writer.WriteInt32(intValue);
                break;
        }

        writer.WriteEndMap();
        return writer.Encode();
    }

    private static CborReader CborPayloadReader(byte[] request) =>
        new(request.AsSpan(1).ToArray(), CborConformanceMode.Ctap2Canonical);

    private static void AssertOptionMap(CborReader reader, params (string Key, bool Value)[] expected)
    {
        Assert.Equal(expected.Length, reader.ReadStartMap());
        foreach (var (key, value) in expected)
        {
            Assert.Equal(key, reader.ReadTextString());
            Assert.Equal(value, reader.ReadBoolean());
        }

        reader.ReadEndMap();
    }

    private static void SkipArray(CborReader reader)
    {
        var length = reader.ReadStartArray();
        for (var i = 0; i < length; i++)
        {
            reader.SkipValue();
        }

        reader.ReadEndArray();
    }

    private static void SkipMap(CborReader reader)
    {
        var length = reader.ReadStartMap();
        for (var i = 0; i < length; i++)
        {
            reader.SkipValue();
            reader.SkipValue();
        }

        reader.ReadEndMap();
    }
}