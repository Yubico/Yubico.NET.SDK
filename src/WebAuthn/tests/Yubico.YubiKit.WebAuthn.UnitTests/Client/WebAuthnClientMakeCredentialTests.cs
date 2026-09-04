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

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

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
        var mockInfo = MockFido2Responses.CreateMockAuthenticatorInfo();
        _mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(mockInfo);

        _client = new WebAuthnClient(
            _mockBackend,
            _origin,
            isPublicSuffix: domain => domain == "com",
            new WebAuthnClientOptions());
    }

    [Fact]
    public async Task MakeCredential_BuildsClientDataHash_PassedToBackend()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        var options = new RegistrationOptions
        {
            Challenge = challenge,
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        BackendMakeCredentialRequest? capturedRequest = null;
        _mockBackend.MakeCredentialAsync(
            Arg.Do<BackendMakeCredentialRequest>(r => capturedRequest = r),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

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
            Rp = new PublicKeyCredentialRpEntity("evil.com", "Evil"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
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
            new WebAuthnClientOptions());

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        // Act (should not throw)
        await client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert - verify backend was called
        await _mockBackend.Received(1).MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
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
            new WebAuthnClientOptions { EnterpriseRpIds = new HashSet<string> { "partner.test" } });

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("partner.test", "Partner"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        // Act (should not throw)
        await client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert - verify backend was called
        await _mockBackend.Received(1).MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_ResidentKeyRequired_SetsRkOption()
    {
        // Arrange
        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            ResidentKey = ResidentKeyPreference.Required
        };

        BackendMakeCredentialRequest? capturedRequest = null;
        _mockBackend.MakeCredentialAsync(
            Arg.Do<BackendMakeCredentialRequest>(r => capturedRequest = r),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

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
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        var expectedGuid = Guid.NewGuid();
        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse(expectedGuid));

        // Act
        var response = await _client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert
        Assert.Equal(expectedGuid, response.Aaguid.Value);
        Assert.IsType<CoseEc2Key>(response.PublicKey);
    }

    [Fact]
    public async Task MakeCredential_ExcludeListWithoutToken_PreservesOriginalExcludeList()
    {
        // Arrange
        var excludeCredential = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            ExcludeCredentials = [excludeCredential]
        };

        BackendMakeCredentialRequest? capturedRequest = null;
        _mockBackend.MakeCredentialAsync(
            Arg.Do<BackendMakeCredentialRequest>(r => capturedRequest = r),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        // Act
        await _client.MakeCredentialAsync(options, pinBytes: null, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Same(options.ExcludeCredentials, capturedRequest.ExcludeList);
        await _mockBackend.DidNotReceive().GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_PuatRequired_RetriesWithUserVerificationRequired()
    {
        // Arrange
        _mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(clientPinSupported: true));
        _mockBackend.GetPinUvTokenAsync(
            PinUvAuthMethod.Pin,
            Arg.Any<PinUvAuthTokenPermissions>(),
            "example.com",
            Arg.Any<ReadOnlyMemory<byte>?>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]));

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        var requests = new List<BackendMakeCredentialRequest>();
        var callCount = 0;
        _mockBackend.MakeCredentialAsync(
            Arg.Do<BackendMakeCredentialRequest>(r => requests.Add(r)),
            Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new CtapException(CtapStatus.PuatRequired);
                }

                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });

        // Act
        await _client.MakeCredentialAsync(options, Encoding.UTF8.GetBytes("123456"), CancellationToken.None);

        // Assert
        Assert.Equal(2, requests.Count);
        Assert.Null(requests[0].Options);
        Assert.NotNull(requests[1].Options);
        Assert.True(requests[1].Options!.TryGetValue("uv", out var uv) && uv);
        Assert.NotNull(requests[1].PinUvAuthParam);
        Assert.Equal((byte)2, requests[1].PinUvAuthProtocol.GetValueOrDefault());
    }

    [Fact]
    public async Task MakeCredential_PuatRequired_WithExcludeList_RetriesWithGetAssertionPermissionForPreflight()
    {
        // Arrange
        var tokenPermissions = new List<PinUvAuthTokenPermissions>();
        _mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(clientPinSupported: true));
        _mockBackend.GetPinUvTokenAsync(
            PinUvAuthMethod.Pin,
            Arg.Do<PinUvAuthTokenPermissions>(p => tokenPermissions.Add(p)),
            "example.com",
            Arg.Any<ReadOnlyMemory<byte>?>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]));
        _mockBackend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.NoCredentials));

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            ExcludeCredentials = [new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32))]
        };

        var callCount = 0;
        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new CtapException(CtapStatus.PuatRequired);
                }

                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });

        // Act
        await _client.MakeCredentialAsync(options, Encoding.UTF8.GetBytes("123456"), CancellationToken.None);

        // Assert
        Assert.Equal(4, tokenPermissions.Count);
        Assert.Equal(
            PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion,
            tokenPermissions[0]);
        Assert.Equal(PinUvAuthTokenPermissions.MakeCredential, tokenPermissions[1]);
        Assert.Equal(
            PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion,
            tokenPermissions[2]);
        Assert.Equal(PinUvAuthTokenPermissions.MakeCredential, tokenPermissions[3]);
        await _mockBackend.Received(2).GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_WithExcludeList_ZeroesEveryIssuedTokenBuffer()
    {
        // Arrange
        var issued = ArrangePinTokenAcquisition();
        ArrangePreflightMiss();
        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        // Act
        _ = await _client.MakeCredentialAsync(
            CreateExcludeListUvRequiredOptions(),
            "123456"u8.ToArray(),
            TestContext.Current.CancellationToken);

        // Assert
        AssertEveryIssuedTokenZeroed(issued, expectedCount: 2);
    }

    [Fact]
    public async Task MakeCredential_WithExcludeList_WhenBackendThrows_ZeroesEveryIssuedTokenBuffer()
    {
        // Arrange
        var issued = ArrangePinTokenAcquisition();
        ArrangePreflightMiss();
        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.OperationDenied));

        // Act
        var ex = await Assert.ThrowsAsync<WebAuthnClientError>(() => _client.MakeCredentialAsync(
            CreateExcludeListUvRequiredOptions(),
            "123456"u8.ToArray(),
            TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, ex.Code);
        AssertEveryIssuedTokenZeroed(issued, expectedCount: 2);
    }

    [Fact]
    public async Task MakeCredential_WithExcludeList_PuatRequiredRetry_ZeroesEveryIssuedTokenBuffer()
    {
        // Arrange - the retry re-runs acquisition and pre-flight, so the ceremony holds four
        // separate token buffers and each one has its own disposal site.
        var issued = ArrangePinTokenAcquisition();
        ArrangePreflightMiss();
        var callCount = 0;
        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1
                    ? throw new CtapException(CtapStatus.PuatRequired)
                    : MockFido2Responses.CreateMockMakeCredentialResponse();
            });

        // Act - UV is left unspecified so the PuatRequired retry path is the one taken.
        _ = await _client.MakeCredentialAsync(
            CreateExcludeListOptions(),
            "123456"u8.ToArray(),
            TestContext.Current.CancellationToken);

        // Assert
        AssertEveryIssuedTokenZeroed(issued, expectedCount: 4);
    }

    [Fact]
    public async Task MakeCredential_WhenBackendFails_ThrowsTypedWebAuthnClientError()
    {
        // A raw CTAP status must never escape the client surface.
        _mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>())
            .Returns<MakeCredentialResponse>(_ => throw new CtapException(CtapStatus.OperationDenied));

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        var ex = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.MakeCredentialAsync(options, pinBytes: null, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, ex.Code);
    }

    [Fact]
    public async Task MakeCredential_WhenPinNeededAndNoPromptConfigured_ThrowsNotAllowed()
    {
        _mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(
                clientPinSupported: true, uvSupported: false));

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            UserVerification = UserVerificationPreference.Required
        };

        var ex = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.MakeCredentialAsync(options, pinBytes: null, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, ex.Code);
        await _mockBackend.DidNotReceive().MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>());
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

    /// <summary>
    /// Arranges an authenticator with a PIN set and hands out a fresh sentinel-filled buffer for
    /// every token acquisition, so the caller can check each one individually afterwards.
    /// </summary>
    /// <remarks>
    /// One buffer per acquisition matters: a shared buffer would let a single disposal mask every
    /// other token's missing one.
    /// </remarks>
    private List<byte[]> ArrangePinTokenAcquisition()
    {
        var issued = new List<byte[]>();

        _mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(clientPinSupported: true));

        _mockBackend.GetPinUvTokenAsync(
            PinUvAuthMethod.Pin,
            Arg.Any<PinUvAuthTokenPermissions>(),
            "example.com",
            Arg.Any<ReadOnlyMemory<byte>?>(),
            Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var token = TokenBufferAssert.CreateSentinelToken();
                TokenBufferAssert.NotZeroed(token, "a token must be live when the client receives it");
                issued.Add(token);
                return new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), token);
            });

        return issued;
    }

    /// <summary>
    /// Makes the silent pre-flight probe report no match, so pre-flight runs to completion and
    /// re-mints the token.
    /// </summary>
    private void ArrangePreflightMiss() =>
        _mockBackend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.NoCredentials));

    private static void AssertEveryIssuedTokenZeroed(IReadOnlyList<byte[]> issued, int expectedCount)
    {
        // Guards the zeroing assertions against passing for the wrong reason: a ceremony that
        // minted fewer tokens than expected never reached the paths under test, and a ceremony
        // that reused one buffer would let a single disposal cover for every missing one.
        Assert.Equal(expectedCount, issued.Count);
        Assert.Equal(expectedCount, issued.Cast<object>().Distinct(ReferenceEqualityComparer.Instance).Count());

        for (var i = 0; i < issued.Count; i++)
        {
            TokenBufferAssert.Zeroed(issued[i], $"token buffer {i} of the ceremony must be zeroed");
        }
    }

    private static RegistrationOptions CreateExcludeListOptions(
        UserVerificationPreference userVerification = UserVerificationPreference.Preferred) => new()
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            ExcludeCredentials = [new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32))],
            UserVerification = userVerification
        };

    private static RegistrationOptions CreateExcludeListUvRequiredOptions() =>
        CreateExcludeListOptions(UserVerificationPreference.Required);
}