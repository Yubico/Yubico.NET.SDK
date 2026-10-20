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
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

public class WebAuthnClientConstructionTests
{
    [Fact]
    public async Task Constructor_WithCtap2Session_DelegatesMakeCredentialToSession()
    {
        var ctap2Session = Substitute.For<ICtap2Session>();
        ctap2Session.GetInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockAuthenticatorInfo());
        ctap2Session.MakeCredentialAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<PublicKeyCredentialRpEntity>(),
                Arg.Any<PublicKeyCredentialUserEntity>(),
                Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
                Arg.Any<MakeCredentialOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockMakeCredentialResponse());

        var client = new WebAuthnClient(
            ctap2Session,
            ParseOrigin("https://example.com"),
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain == "com" });

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [CoseAlgorithm.Es256]
        };

        await client.MakeCredentialAsync(options, pinBytes: null, TestContext.Current.CancellationToken);

        await ctap2Session.Received(1).MakeCredentialAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<PublicKeyCredentialRpEntity>(),
            Arg.Any<PublicKeyCredentialUserEntity>(),
            Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
            Arg.Any<MakeCredentialOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisposeAsync_WithCtap2SessionConstructor_DisposesSession()
    {
        var ctap2Session = Substitute.For<ICtap2Session>();
        var client = new WebAuthnClient(
            ctap2Session,
            ParseOrigin("https://example.com"),
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain == "com" });

        await client.DisposeAsync();

        await ctap2Session.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Constructor_WithNullOrigin_DoesNotAdoptSession()
    {
        var ctap2Session = Substitute.For<ICtap2Session>();

        Assert.Throws<ArgumentNullException>(
            () => new WebAuthnClient(
                ctap2Session,
                origin: null!,
                new WebAuthnClientOptions { PublicSuffixChecker = domain => domain == "com" }));

        await ctap2Session.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task Constructor_WithNullOptions_DoesNotAdoptSession()
    {
        var ctap2Session = Substitute.For<ICtap2Session>();

        Assert.Throws<ArgumentNullException>(
            () => new WebAuthnClient(
                ctap2Session,
                ParseOrigin("https://example.com"),
                options: null!));

        await ctap2Session.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task GetAssertion_WithCtap2SessionConstructor_RejectsPublicSuffixRpId()
    {
        var ctap2Session = Substitute.For<ICtap2Session>();
        var client = new WebAuthnClient(
            ctap2Session,
            ParseOrigin("https://login.example.com"),
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain == "com" });

        var options = new AuthenticationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = "com"
        };

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(
            () => client.GetAssertionAsync(options, pinBytes: null, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, error.Code);
        await ctap2Session.DidNotReceive().GetInfoAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ceremonies_WithReusedOptions_InvokeCheckerAndRejectBeforeBackendCalls()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        var checkedDomains = new List<string>();
        var clientOptions = new WebAuthnClientOptions
        {
            PublicSuffixChecker = domain =>
            {
                checkedDomains.Add(domain);
                return domain == "com";
            }
        };
        await using var client = new WebAuthnClient(
            backend,
            ParseOrigin("https://login.example.com"),
            clientOptions);

        var registrationOptions = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("com", "Public suffix"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [CoseAlgorithm.Es256]
        };
        var authenticationOptions = new AuthenticationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = "com"
        };

        WebAuthnClientError registrationError = await Assert.ThrowsAsync<WebAuthnClientError>(
            () => client.MakeCredentialAsync(registrationOptions, cancellationToken: TestContext.Current.CancellationToken));
        WebAuthnClientError authenticationError = await Assert.ThrowsAsync<WebAuthnClientError>(
            () => client.GetAssertionAsync(authenticationOptions, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, registrationError.Code);
        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, authenticationError.Code);
        Assert.Equal(["com", "com"], checkedDomains);
        await backend.DidNotReceive().GetCachedInfoAsync(Arg.Any<CancellationToken>());
        await backend.DidNotReceive().MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<CancellationToken>());
        await backend.DidNotReceive().GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_WithCtap2SessionConstructor_AllowsEnterpriseRpId()
    {
        var ctap2Session = Substitute.For<ICtap2Session>();
        ctap2Session.GetInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockAuthenticatorInfo());
        ctap2Session.MakeCredentialAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<PublicKeyCredentialRpEntity>(),
                Arg.Any<PublicKeyCredentialUserEntity>(),
                Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
                Arg.Any<MakeCredentialOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockMakeCredentialResponse());

        var client = new WebAuthnClient(
            ctap2Session,
            ParseOrigin("https://example.com"),
            new WebAuthnClientOptions
            {
                PublicSuffixChecker = domain => domain == "com" || domain == "test",
                EnterpriseRpIds = new HashSet<string> { "partner.test" }
            });

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("partner.test", "Partner"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@partner.test", "User"),
            PubKeyCredParams = [CoseAlgorithm.Es256]
        };

        await client.MakeCredentialAsync(options, pinBytes: null, TestContext.Current.CancellationToken);

        await ctap2Session.Received(1).MakeCredentialAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Is<PublicKeyCredentialRpEntity>(rp => rp.Id == "partner.test"),
            Arg.Any<PublicKeyCredentialUserEntity>(),
            Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
            Arg.Any<MakeCredentialOptions?>(),
            Arg.Any<CancellationToken>());
    }

    private static WebAuthnOrigin ParseOrigin(string url)
    {
        Assert.True(WebAuthnOrigin.TryParse(url, out var origin));
        return origin;
    }
}