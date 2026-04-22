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

using System.Formats.Cbor;
using System.Security.Cryptography;
using NSubstitute;
using Xunit;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

public class WebAuthnClientMakeCredentialTests
{
    private readonly IWebAuthnBackend _mockBackend;
    private readonly WebAuthnOrigin _origin;
    private readonly WebAuthnClient _client;

    public WebAuthnClientMakeCredentialTests()
    {
        _mockBackend = Substitute.For<IWebAuthnBackend>();
        if (!WebAuthnOrigin.TryParse("https://example.com", out _origin!))
            throw new InvalidOperationException("Failed to parse origin");

        // Setup default mock responses
        var mockInfo = CreateMockAuthenticatorInfo();
        _mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(mockInfo);

        _client = new WebAuthnClient(
            _mockBackend,
            _origin,
            isPublicSuffix: domain => domain == "com",
            enterpriseRpIds: new HashSet<string>());
    }

    [Fact]
    public async Task MakeCredential_BuildsClientDataHash_PassedToBackend()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        var options = new RegistrationOptions
        {
            Challenge = challenge,
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        BackendMakeCredentialRequest? capturedRequest = null;
        _mockBackend.MakeCredentialAsync(
            Arg.Do<BackendMakeCredentialRequest>(r => capturedRequest = r),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateMockResponse());

        // Act
        await _client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        var expectedClientData = WebAuthnClientData.Create("webauthn.create", challenge, _origin, crossOrigin: null, topOrigin: null);
        Assert.Equal(expectedClientData.Hash.ToArray(), capturedRequest.ClientDataHash.ToArray());
    }

    [Fact]
    public async Task MakeCredential_RpIdMismatch_ThrowsInvalidRequest()
    {
        // Arrange
        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "evil.com", Name = "Evil" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
    }

    [Fact]
    public async Task MakeCredential_RpIdSuffix_Allowed()
    {
        // Arrange
        WebAuthnOrigin.TryParse("https://login.example.com", out var origin);
        var client = new WebAuthnClient(
            _mockBackend,
            origin!,
            isPublicSuffix: domain => domain == "com",
            enterpriseRpIds: new HashSet<string>());

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateMockResponse());

        // Act (should not throw)
        await client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert - verify backend was called
        await _mockBackend.Received(1).MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_EnterpriseRpId_Bypasses_SuffixCheck()
    {
        // Arrange
        var client = new WebAuthnClient(
            _mockBackend,
            _origin,
            isPublicSuffix: domain => domain == "com",
            enterpriseRpIds: new HashSet<string> { "partner.test" });

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "partner.test", Name = "Partner" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateMockResponse());

        // Act (should not throw)
        await client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert - verify backend was called
        await _mockBackend.Received(1).MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_ResidentKeyRequired_SetsRkOption()
    {
        // Arrange
        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            ResidentKey = ResidentKeyPreference.Required
        };

        BackendMakeCredentialRequest? capturedRequest = null;
        _mockBackend.MakeCredentialAsync(
            Arg.Do<BackendMakeCredentialRequest>(r => capturedRequest = r),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateMockResponse());

        // Act
        await _client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Options);
        Assert.True(capturedRequest.Options.TryGetValue("rk", out var rk) && rk);
    }

    [Fact]
    public async Task MakeCredential_ResponsePopulatesAaguidAndPublicKey()
    {
        // Arrange
        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        var expectedGuid = Guid.NewGuid();
        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(CreateMockResponse(expectedGuid));

        // Act
        var response = await _client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert
        Assert.Equal(expectedGuid, response.Aaguid.Value);
        Assert.IsType<CoseEc2Key>(response.PublicKey);
    }

    [Fact]
    public async Task MakeCredential_BackendDisposed_OnClientDisposeAsync()
    {
        // Arrange
        var mockBackend = Substitute.For<IWebAuthnBackend>();
        var client = new WebAuthnClient(
            mockBackend,
            _origin,
            isPublicSuffix: domain => domain == "com");

        // Act
        await client.DisposeAsync();

        // Assert
        await mockBackend.Received(1).DisposeAsync();
    }

    private static MakeCredentialResponse CreateMockResponse(Guid? aaguid = null)
    {
        var guid = aaguid ?? Guid.NewGuid();
        var authData = BuildAuthDataWithAttestedCredential(guid);
        var cborBytes = BuildMakeCredentialResponseCbor(authData, "none");
        return MakeCredentialResponse.Decode(cborBytes);
    }

    private static byte[] BuildAuthDataWithAttestedCredential(Guid aaguid)
    {
        // rpIdHash (32) + flags (1) + signCount (4) + AAGUID (16) + credIdLen (2) + credId + publicKey
        var data = new List<byte>();

        // rpIdHash (32 bytes)
        var rpIdHash = new byte[32];
        SHA256.HashData("example.com"u8, rpIdHash);
        data.AddRange(rpIdHash);

        // flags = UP | UV | AT (0x45)
        data.Add(0x45);

        // signCount (4 bytes, big-endian)
        data.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x01 });

        // AAGUID (16 bytes, big-endian network byte order)
        data.AddRange(EncodeAaguidBigEndian(aaguid));

        // Credential ID length (2 bytes, big-endian) = 32
        data.AddRange(new byte[] { 0x00, 0x20 });

        // Credential ID (32 bytes)
        var credId = new byte[32];
        RandomNumberGenerator.Fill(credId);
        data.AddRange(credId);

        // COSE public key (minimal EC2 key in CBOR)
        var keyWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        keyWriter.WriteStartMap(5);

        // kty = 2 (EC2)
        keyWriter.WriteInt32(1);
        keyWriter.WriteInt32(2);

        // alg = -7 (ES256)
        keyWriter.WriteInt32(3);
        keyWriter.WriteInt32(-7);

        // crv = 1 (P-256)
        keyWriter.WriteInt32(-1);
        keyWriter.WriteInt32(1);

        // x coordinate (32 bytes)
        keyWriter.WriteInt32(-2);
        keyWriter.WriteByteString(new byte[32]);

        // y coordinate (32 bytes)
        keyWriter.WriteInt32(-3);
        keyWriter.WriteByteString(new byte[32]);

        keyWriter.WriteEndMap();
        data.AddRange(keyWriter.Encode());

        return data.ToArray();
    }

    private static byte[] BuildMakeCredentialResponseCbor(byte[] authData, string format)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        writer.WriteStartMap(3);

        // 0x01: fmt
        writer.WriteInt32(1);
        writer.WriteTextString(format);

        // 0x02: authData
        writer.WriteInt32(2);
        writer.WriteByteString(authData);

        // 0x03: attStmt (empty for "none" format)
        writer.WriteInt32(3);
        writer.WriteStartMap(0);
        writer.WriteEndMap();

        writer.WriteEndMap();

        return writer.Encode();
    }

    private static AuthenticatorInfo CreateMockAuthenticatorInfo()
    {
        // Create minimal authenticatorInfo CBOR for testing
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        writer.WriteStartMap(3);

        // 0x01: versions
        writer.WriteInt32(1);
        writer.WriteStartArray(2);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteTextString("FIDO_2_1");
        writer.WriteEndArray();

        // 0x02: extensions
        writer.WriteInt32(2);
        writer.WriteStartArray(1);
        writer.WriteTextString("hmac-secret");
        writer.WriteEndArray();

        // 0x03: aaguid
        writer.WriteInt32(3);
        writer.WriteByteString(Guid.NewGuid().ToByteArray());

        writer.WriteEndMap();

        return AuthenticatorInfo.Decode(writer.Encode());
    }

    private static byte[] EncodeAaguidBigEndian(Guid guid)
    {
        // AAGUID must be in big-endian (network byte order)
        // .NET Guid.ToByteArray() gives little-endian on little-endian systems
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        // Convert first 3 components from little-endian to big-endian
        if (BitConverter.IsLittleEndian)
        {
            // Reverse Data1 (4 bytes)
            (bytes[0], bytes[1], bytes[2], bytes[3]) =
                (bytes[3], bytes[2], bytes[1], bytes[0]);

            // Reverse Data2 (2 bytes)
            (bytes[4], bytes[5]) = (bytes[5], bytes[4]);

            // Reverse Data3 (2 bytes)
            (bytes[6], bytes[7]) = (bytes[7], bytes[6]);
        }

        return bytes.ToArray();
    }
}
