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
using System.Text.Json;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

public class RpAuthorizationTests
{
    [Theory]
    [InlineData("partner.test")]
    [InlineData("https://partner.test/native-client")]
    [InlineData("com")]
    [InlineData("x:credential")]
    [InlineData("x:/credential")]
    [InlineData("x://partner.test/credential")]
    [InlineData("x:")]
    public async Task Ceremonies_ApplicationAuthorizesNonSuffix_PreserveRpIdAndOrigin(string rpId)
    {
        var backend = CreateBackend();
        var origin = Origin();
        var calls = new List<(WebAuthnOrigin Origin, string RpId)>();
        await using var client = new WebAuthnClient(backend, origin, Options((actualOrigin, actualRpId) =>
        {
            calls.Add((actualOrigin, actualRpId));
            return true;
        }));

        var registration = await client.MakeCredentialAsync(Registration(rpId), cancellationToken: TestContext.Current.CancellationToken);
        var matches = await client.GetAssertionAsync(Authentication(rpId), cancellationToken: TestContext.Current.CancellationToken);
        var assertion = await Assert.Single(matches).SelectAsync(TestContext.Current.CancellationToken);

        Assert.Equal([(origin, rpId), (origin, rpId)], calls);
        using var registrationData = JsonDocument.Parse(registration.ClientData.JsonBytes);
        using var assertionData = JsonDocument.Parse(assertion.ClientData.JsonBytes);
        Assert.Equal(origin.StringValue, registrationData.RootElement.GetProperty("origin").GetString());
        Assert.Equal(origin.StringValue, assertionData.RootElement.GetProperty("origin").GetString());
        await backend.Received(1).MakeCredentialAsync(Arg.Is<BackendMakeCredentialRequest>(r => r.Rp.Id == rpId), Arg.Any<CancellationToken>());
        await backend.Received(1).GetAssertionAsync(Arg.Is<BackendGetAssertionRequest>(r => r.RpId == rpId), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ceremonies_CallbackRejectsOrThrows_BlockAllBackendCalls(bool throws)
    {
        var backend = CreateBackend();
        var failure = new InvalidOperationException("Application authorization unavailable");
        int calls = 0;
        await using var client = new WebAuthnClient(backend, Origin(), Options((_, _) =>
        {
            calls++;
            return throws ? throw failure : false;
        }));
        backend.ClearReceivedCalls();

        var registrationError = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(
            Registration("example.com"), cancellationToken: TestContext.Current.CancellationToken));
        var authenticationError = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(
            Authentication("example.com"), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.Security, registrationError.Code);
        Assert.Equal(WebAuthnClientErrorCode.Security, authenticationError.Code);
        Assert.Equal(throws ? failure : null, registrationError.InnerException);
        Assert.Equal(throws ? failure : null, authenticationError.InnerException);
        Assert.Equal(2, calls);
        Assert.Empty(backend.ReceivedCalls());
    }

    [Theory]
    [InlineData("bad rp id")]
    [InlineData("example.com/invalid")]
    [InlineData("example..com")]
    [InlineData("example.com\0")]
    [InlineData("example-.com")]
    [InlineData("example_foo.com")]
    [InlineData("ab--cd.com")]
    [InlineData("example.123")]
    [InlineData("example.0x")]
    [InlineData("example.0xdead")]
    [InlineData("x:bad%GG")]
    [InlineData("x:bad%")]
    [InlineData("x:bad value")]
    [InlineData("x:\\credential")]
    [InlineData("x://[bad]/credential")]
    [InlineData("x:credential[bad]")]
    [InlineData("x://partner.test:invalid/credential")]
    public async Task Ceremonies_MalformedProtocolRpId_EvenPermissiveCallbackCannotReachBackend(string rpId)
    {
        var backend = CreateBackend();
        int calls = 0;
        await using var client = new WebAuthnClient(backend, Origin(), Options((_, _) =>
        {
            calls++;
            return true;
        }));
        backend.ClearReceivedCalls();

        var registrationError = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(
            Registration(rpId), cancellationToken: TestContext.Current.CancellationToken));
        var authenticationError = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(
            Authentication(rpId), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, registrationError.Code);
        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, authenticationError.Code);
        Assert.Equal(0, calls);
        Assert.Empty(backend.ReceivedCalls());
    }

    [Fact]
    public async Task Ceremonies_CallbackCancellation_PropagatesAndBlocksAllBackendCalls()
    {
        var backend = CreateBackend();
        await using var client = new WebAuthnClient(backend, Origin(), Options((_, _) => throw new OperationCanceledException()));
        backend.ClearReceivedCalls();

        await Assert.ThrowsAsync<OperationCanceledException>(() => client.MakeCredentialAsync(
            Registration("example.com"), cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.GetAssertionAsync(
            Authentication("example.com"), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Empty(backend.ReceivedCalls());
    }

    [Fact]
    public async Task Ceremonies_InvalidUnicodeCannotBeReplacedDuringEncoding()
    {
        // Build at runtime: runner serialization can replace unpaired UTF-16 surrogates in theory data.
        await Ceremonies_MalformedProtocolRpId_EvenPermissiveCallbackCannotReachBackend(new string('\ud800', 1));
        await Ceremonies_MalformedProtocolRpId_EvenPermissiveCallbackCannotReachBackend("https://partner.test/" + new string('\udc00', 1));
    }

    [Fact]
    public async Task Ceremonies_ValidIdn_UsesActualAsciiLabelLengthButPreservesInput()
    {
        // 32 Unicode characters encode to a 38-byte ASCII label, not Uri's estimated 68 bytes.
        await Ceremonies_ApplicationAuthorizesNonSuffix_PreserveRpIdAndOrigin(new string('例', 32) + ".com");
    }

    [Fact]
    public async Task Ceremonies_DomainLabelAndNameLengthLimits_CannotBeOverridden()
    {
        await Ceremonies_MalformedProtocolRpId_EvenPermissiveCallbackCannotReachBackend(new string('a', 64) + ".com");
        await Ceremonies_MalformedProtocolRpId_EvenPermissiveCallbackCannotReachBackend(
            string.Join('.', Enumerable.Repeat(new string('a', 63), 4)));
    }

    [Fact]
    public async Task Ceremonies_DocumentedUriSubset_RejectsUnsupportedSchemeSpecificForm()
    {
        // Valid generic RFC3986 syntax, but not supported by System.Uri's HTTP parser.
        await Ceremonies_MalformedProtocolRpId_EvenPermissiveCallbackCannotReachBackend("http:credential");
    }

    private static WebAuthnClientOptions Options(Func<WebAuthnOrigin, string, bool> validator)
        => new() { RpIdValidator = validator.Invoke };

    private static WebAuthnOrigin Origin()
    {
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        return origin;
    }

    private static PublicKeyCredentialCreationOptions Registration(string rpId) => new()
    {
        Rp = new PublicKeyCredentialRpEntity(rpId, "Application"),
        User = new PublicKeyCredentialUserEntity(new byte[16], "user", "User"),
        Challenge = new byte[32],
        PubKeyCredParams = [CoseAlgorithm.Es256]
    };

    private static PublicKeyCredentialRequestOptions Authentication(string rpId) => new()
    {
        RpId = rpId,
        Challenge = new byte[32]
    };

    private static IWebAuthnBackend CreateBackend()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(MockCtap2Responses.CreateMockAuthenticatorInfo());
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockMakeCredentialResponse());
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse());
        return backend;
    }
}