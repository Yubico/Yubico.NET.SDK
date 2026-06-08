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

using NSubstitute;
using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.CredentialManagement;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.UnitTests.CredentialManagement;

public sealed class CredentialManagementWireTests
{
    private readonly IFidoSession _session = Substitute.For<IFidoSession>();
    private readonly WirePinUvAuthProtocol _protocol = new();
    private readonly byte[] _pinUvAuthToken = [0x01, 0x02, 0x03, 0x04];

    [Fact]
    public async Task GetCredentialsMetadataAsync_SendsCanonicalCredentialManagementRequest()
    {
        byte[]? capturedRequest = null;
        _session.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(request => capturedRequest = request.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(CreateCredentialMetadataResponse());

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        await credentialManagement.GetCredentialsMetadataAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(capturedRequest);
        Assert.Equal(CtapCommand.CredentialManagement, capturedRequest[0]);
        var reader = CborPayloadReader(capturedRequest);
        Assert.Equal(3, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(CredManagementSubCommand.GetCredsMetadata, reader.ReadInt32());
        Assert.Equal(3, reader.ReadInt32());
        Assert.Equal(_protocol.Version, reader.ReadInt32());
        Assert.Equal(4, reader.ReadInt32());
        Assert.Equal(_protocol.AuthenticationTagLength, reader.ReadByteString().Length);
        reader.ReadEndMap();
    }

    [Fact]
    public async Task DeleteCredentialAsync_SendsCanonicalCredentialManagementRequest()
    {
        byte[]? capturedRequest = null;
        _session.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(request => capturedRequest = request.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(ReadOnlyMemory<byte>.Empty);

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        await credentialManagement.DeleteCredentialAsync(
            new PublicKeyCredentialDescriptor(new byte[] { 0xA1, 0xA2 }),
            TestContext.Current.CancellationToken);

        Assert.NotNull(capturedRequest);
        Assert.Equal(CtapCommand.CredentialManagement, capturedRequest[0]);
        var reader = CborPayloadReader(capturedRequest);
        Assert.Equal(4, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(CredManagementSubCommand.DeleteCredential, reader.ReadInt32());
        Assert.Equal(2, reader.ReadInt32());
        AssertCredentialIdParam(reader, [0xA1, 0xA2]);
        Assert.Equal(3, reader.ReadInt32());
        Assert.Equal(_protocol.Version, reader.ReadInt32());
        Assert.Equal(4, reader.ReadInt32());
        Assert.Equal(_protocol.AuthenticationTagLength, reader.ReadByteString().Length);
        reader.ReadEndMap();
    }

    private static CborReader CborPayloadReader(byte[] request) =>
        new(request.AsMemory(1), CborConformanceMode.Ctap2Canonical);

    private static void AssertCredentialIdParam(CborReader reader, byte[] expectedId)
    {
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal(2, reader.ReadInt32());
        Assert.Equal(2, reader.ReadStartMap());
        Assert.Equal("id", reader.ReadTextString());
        Assert.Equal(expectedId, reader.ReadByteString());
        Assert.Equal("type", reader.ReadTextString());
        Assert.Equal("public-key", reader.ReadTextString());
        reader.ReadEndMap();
        reader.ReadEndMap();
    }

    private static ReadOnlyMemory<byte> CreateCredentialMetadataResponse()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(1);
        writer.WriteInt32(1);
        writer.WriteInt32(2);
        writer.WriteInt32(24);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private sealed class WirePinUvAuthProtocol : IPinUvAuthProtocol
    {
        public int Version => 2;

        public int AuthenticationTagLength => 16;

        public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message) => new byte[AuthenticationTagLength];

        public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext) => plaintext.ToArray();

        public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext) => ciphertext.ToArray();

        public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(IReadOnlyDictionary<int, object?> peerCoseKey) =>
            (new Dictionary<int, object?>(), new byte[32]);

        public byte[] Kdf(ReadOnlySpan<byte> z) => new byte[32];

        public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature) => true;

        public void Dispose()
        {
        }
    }
}