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

    [Fact]
    public async Task EnumerateRelyingPartiesAsync_ReturnsReportedTotalAndPreservesOrder()
    {
        _session.SendCborRequestAsync(default, TestContext.Current.CancellationToken).ReturnsForAnyArgs(
            CreateRelyingPartyResponse("first.example", total: 2),
            CreateRelyingPartyResponse("second.example"));

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        var result = await credentialManagement.EnumerateRelyingPartiesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.ReportedTotal);
        Assert.Equal(["first.example", "second.example"], result.RelyingParties.Select(rp => rp.RelyingParty.Id));
        Assert.Equal(2, result.RelyingParties[0].TotalRpCount);
        Assert.Null(result.RelyingParties[1].TotalRpCount);
        var mutableView = Assert.IsAssignableFrom<IList<RelyingPartyInfo>>(result.RelyingParties);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(result.RelyingParties[0]));
        await _session.Received(2).SendCborRequestAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnumerateCredentialsAsync_ReturnsReportedTotalAndPreservesOrder()
    {
        _session.SendCborRequestAsync(default, TestContext.Current.CancellationToken).ReturnsForAnyArgs(
            CreateStoredCredentialResponse(0x11, total: 3),
            CreateStoredCredentialResponse(0x22),
            CreateStoredCredentialResponse(0x33));

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        using var result = await credentialManagement.EnumerateCredentialsAsync(
            new byte[32],
            TestContext.Current.CancellationToken);

        Assert.Equal(3, result.ReportedTotal);
        Assert.Equal([0x11, 0x22, 0x33], result.Credentials.Select(credential => credential.CredentialId.Id.Span[0]));
        Assert.Equal(3, result.Credentials[0].TotalCredentials);
        Assert.Null(result.Credentials[1].TotalCredentials);
        Assert.Null(result.Credentials[2].TotalCredentials);
        var mutableView = Assert.IsAssignableFrom<IList<StoredCredentialInfo>>(result.Credentials);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(result.Credentials[0]));
        ReadOnlyMemory<byte>[] capturedRawData = result.Credentials.Select(credential => credential.RawData).ToArray();
        result.Dispose();
        Assert.All(capturedRawData, rawData => Assert.All(rawData.ToArray(), value => Assert.Equal(0, value)));
        Assert.All(result.Credentials, credential =>
            Assert.Throws<ObjectDisposedException>(() => _ = credential.RawData));
        await _session.Received(3).SendCborRequestAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnumerateCredentialsAsync_WithoutReportedTotal_ReturnsOneCredentialAndNullTotal()
    {
        _session.SendCborRequestAsync(default, TestContext.Current.CancellationToken).ReturnsForAnyArgs(CreateStoredCredentialResponse(0x11));

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        using var result = await credentialManagement.EnumerateCredentialsAsync(
            new byte[32],
            TestContext.Current.CancellationToken);

        Assert.Null(result.ReportedTotal);
        Assert.Single(result.Credentials);
        Assert.Equal(0x11, result.Credentials[0].CredentialId.Id.Span[0]);
        Assert.Null(result.Credentials[0].TotalCredentials);
    }

    [Fact]
    public async Task EnumerateCredentialsAsync_WhenNoCredentials_ReturnsEmptyResultWithNullTotal()
    {
        _session.SendCborRequestAsync(default, TestContext.Current.CancellationToken).ReturnsForAnyArgs<ReadOnlyMemory<byte>>(
            _ => throw new CtapException(CtapStatus.NoCredentials));

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        using var result = await credentialManagement.EnumerateCredentialsAsync(
            new byte[32],
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Credentials);
        Assert.Null(result.ReportedTotal);
        result.Dispose();
        result.Dispose();
    }

    [Fact]
    public async Task EnumerateCredentialsAsync_WhenContinuationIsCancelled_PropagatesCancellationAndLeavesResponsesUnchanged()
    {
        ReadOnlyMemory<byte> firstResponse = CreateStoredCredentialResponse(0x11, total: 2);
        byte[] expectedFirstResponse = firstResponse.ToArray();
        _session.SendCborRequestAsync(default, TestContext.Current.CancellationToken).ReturnsForAnyArgs(
            _ => Task.FromResult(firstResponse),
            _ => throw new OperationCanceledException(TestContext.Current.CancellationToken));

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() => credentialManagement.EnumerateCredentialsAsync(
            new byte[32],
            TestContext.Current.CancellationToken));

        Assert.Equal(expectedFirstResponse, firstResponse.ToArray());
    }

    [Fact]
    public async Task EnumerateCredentialsAsync_WhenContinuationCannotBeDecoded_PropagatesAndLeavesResponsesUnchanged()
    {
        ReadOnlyMemory<byte> firstResponse = CreateStoredCredentialResponse(0x11, total: 2);
        byte[] expectedFirstResponse = firstResponse.ToArray();
        ReadOnlyMemory<byte> malformedResponse = new byte[] { 0xA1, 0x06, 0xA0 };
        byte[] expectedMalformedResponse = malformedResponse.ToArray();
        _session.SendCborRequestAsync(default, TestContext.Current.CancellationToken).ReturnsForAnyArgs(
            firstResponse,
            malformedResponse);

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => credentialManagement.EnumerateCredentialsAsync(
            new byte[32],
            TestContext.Current.CancellationToken));

        Assert.Equal(expectedFirstResponse, firstResponse.ToArray());
        Assert.Equal(expectedMalformedResponse, malformedResponse.ToArray());
    }

    [Fact]
    public async Task EnumerateRelyingPartiesAsync_NonNoCredentialsFailure_Propagates()
    {
        _session.SendCborRequestAsync(default, TestContext.Current.CancellationToken).ReturnsForAnyArgs<ReadOnlyMemory<byte>>(
            _ => throw new CtapException(CtapStatus.InvalidCbor));

        using var credentialManagement = new Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement(
            _session,
            _protocol,
            _pinUvAuthToken);

        var exception = await Assert.ThrowsAsync<CtapException>(
            () => credentialManagement.EnumerateRelyingPartiesAsync(TestContext.Current.CancellationToken));

        Assert.Equal(CtapStatus.InvalidCbor, exception.Status);
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

    private static ReadOnlyMemory<byte> CreateRelyingPartyResponse(string id, int? total = null)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(total.HasValue ? 3 : 2);
        writer.WriteInt32(3);
        writer.WriteStartMap(1);
        writer.WriteTextString("id");
        writer.WriteTextString(id);
        writer.WriteEndMap();
        writer.WriteInt32(4);
        writer.WriteByteString(new byte[32]);
        if (total.HasValue)
        {
            writer.WriteInt32(5);
            writer.WriteInt32(total.Value);
        }
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static ReadOnlyMemory<byte> CreateStoredCredentialResponse(byte credentialId, int? total = null)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(total.HasValue ? 4 : 3);
        writer.WriteInt32(6);
        writer.WriteStartMap(1);
        writer.WriteTextString("id");
        writer.WriteByteString([0x01]);
        writer.WriteEndMap();
        writer.WriteInt32(7);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteByteString([credentialId]);
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();
        writer.WriteInt32(8);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        if (total.HasValue)
        {
            writer.WriteInt32(9);
            writer.WriteInt32(total.Value);
        }
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