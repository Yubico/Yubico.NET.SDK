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

using System.Security.Cryptography;
using System.Text;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;
using Xunit;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
/// Tests for PIN retry behavior in WebAuthnClient.
/// </summary>
/// <remarks>
/// These tests verify that PinAuthInvalid errors are handled correctly without
/// burning YubiKey PIN attempts through unsafe retry loops.
/// </remarks>
public sealed class WebAuthnClientPinRetryTests
{
    private readonly IWebAuthnBackend _mockBackend;
    private readonly WebAuthnOrigin _origin;
    private readonly WebAuthnClient _client;
    private int _tokenCallCount;

    public WebAuthnClientPinRetryTests()
    {
        _mockBackend = Substitute.For<IWebAuthnBackend>();
        if (!WebAuthnOrigin.TryParse("https://example.com", out _origin!))
            throw new InvalidOperationException("Failed to parse origin");

        // Setup default mock responses
        var mockInfo = MockFido2Responses.CreateMockAuthenticatorInfo(clientPinSupported: true, uvSupported: true);
        _mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(mockInfo);

        _client = new WebAuthnClient(
            _mockBackend,
            _origin,
            isPublicSuffix: domain => domain == "com",
            enterpriseRpIds: new HashSet<string>());
    }

    [Fact(Timeout = 5000)]
    public async Task MakeCredential_PinAuthInvalid_ThrowsNotAllowed_WithoutRetry()
    {
        // Arrange: Configure backend to throw PinAuthInvalid on token request
        _tokenCallCount = 0;
        _mockBackend.GetPinUvTokenAsync(
            Arg.Any<PinUvAuthMethod>(),
            Arg.Any<PinUvAuthTokenPermissions>(),
            Arg.Any<string?>(),
            Arg.Any<ReadOnlyMemory<byte>?>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.PinAuthInvalid))
            .AndDoes(_ => _tokenCallCount++);

        var pinBytes = Encoding.UTF8.GetBytes("123456");

        var options = new RegistrationOptions
        {
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user", DisplayName = "User" },
            Challenge = RandomNumberGenerator.GetBytes(32),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            UserVerification = UserVerificationPreference.Required
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.MakeCredentialAsync(options, pinBytes, CancellationToken.None));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, ex.Code);
        Assert.Contains("PIN authentication failed", ex.Message);

        // CRITICAL: Verify GetPinUvTokenAsync was called EXACTLY once (no retry)
        Assert.Equal(1, _tokenCallCount);
    }

    [Fact(Timeout = 5000)]
    public async Task GetAssertion_PinAuthInvalid_ThrowsNotAllowed_WithoutRetry()
    {
        // Arrange: Configure backend to throw PinAuthInvalid on token request
        _tokenCallCount = 0;
        _mockBackend.GetPinUvTokenAsync(
            Arg.Any<PinUvAuthMethod>(),
            Arg.Any<PinUvAuthTokenPermissions>(),
            Arg.Any<string?>(),
            Arg.Any<ReadOnlyMemory<byte>?>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.PinAuthInvalid))
            .AndDoes(_ => _tokenCallCount++);

        var pinBytes = Encoding.UTF8.GetBytes("123456");

        var options = new AuthenticationOptions
        {
            RpId = "example.com",
            Challenge = RandomNumberGenerator.GetBytes(32),
            UserVerification = UserVerificationPreference.Required,
            AllowCredentials =
            [
                new WebAuthnCredentialDescriptor(RandomNumberGenerator.GetBytes(64))
            ]
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.GetAssertionAsync(options, pinBytes, CancellationToken.None));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, ex.Code);
        Assert.Contains("PIN authentication failed", ex.Message);

        // CRITICAL: Verify GetPinUvTokenAsync was called EXACTLY once (no retry)
        Assert.Equal(1, _tokenCallCount);
    }
}
