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
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
///     Covers the ceremony frame shared by MakeCredential and GetAssertion: UV
///     escalation happens at most once, the exclude-list pre-flight probes with up:false and narrows the
///     exclude list, probe failures keep their wrapping, CTAP statuses keep their mapping, and extension
///     outputs come from the attempt that succeeded.
/// </summary>
public class WebAuthnClientCeremonyFrameTests
{
    private const string RpId = "example.com";
    private static readonly byte[] TokenBytes = new byte[32];

    private readonly IWebAuthnBackend _backend = Substitute.For<IWebAuthnBackend>();
    private readonly WebAuthnClient _client;

    public WebAuthnClientCeremonyFrameTests()
    {
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockAuthenticatorInfo(
                clientPinSupported: true, extensions: MockCtap2Responses.AllKnownExtensions));
        _backend.GetPinUvTokenAsync(
                PinUvAuthMethod.Pin, Arg.Any<PinUvAuthTokenPermissions>(), RpId,
                Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), (byte[])TokenBytes.Clone()));
        _client = new WebAuthnClient(_backend, Assert.IsType<WebAuthnOrigin>(origin),
            new WebAuthnClientOptions { RpIdValidator = (_, rpId) => rpId == "example.com" });
    }

    private static PublicKeyCredentialCreationOptions Registration(UserVerificationPreference uv = UserVerificationPreference.Preferred) => new()
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        Rp = new PublicKeyCredentialRpEntity(RpId, "Example"),
        User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user", "User"),
        PubKeyCredParams = [CoseAlgorithm.Es256],
        UserVerification = uv
    };

    private static PublicKeyCredentialRequestOptions Authentication(UserVerificationPreference uv = UserVerificationPreference.Preferred) => new()
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        RpId = RpId,
        UserVerification = uv
    };

    private static ReadOnlyMemory<byte> Pin => "123456"u8.ToArray();

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task TokenlessUv_AssertionPreservesCandidatesAfterSilentMiss(bool silentHit, bool verifiedMiss)
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(uvSupported: true, maxCredentialCountInList: 2));
        var a = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var b = new PublicKeyCredentialDescriptor(new byte[] { 2 });
        var requests = new List<BackendGetAssertionRequest>();
        _backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(requests.Add), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            bool silent = request.Options?.GetValueOrDefault("up", true) == false;
            if ((silent && !silentHit) || (!silent && verifiedMiss))
                throw new CtapException(CtapStatus.NoCredentials);
            return MockCtap2Responses.CreateMockGetAssertionResponse(b.Id.ToArray());
        });

        await using var client = _client;
        var ct = TestContext.Current.CancellationToken;
        var matches = await client.GetAssertionAsync(WithAllowList(a, b) with
        {
            UserVerification = UserVerificationPreference.Required
        }, cancellationToken: ct);

        Assert.Equal(2, requests.Count);
        var real = requests[1];
        Assert.Equal(silentHit ? [b] : new[] { a, b }, real.AllowList);
        Assert.True(real.Options?.GetValueOrDefault("uv"));
        Assert.NotEqual(false, real.Options?.GetValueOrDefault("up", true));
        Assert.Null(real.PinUvAuthParam);
        Assert.Null(real.PinUvAuthProtocol);
        Assert.Contains(real.ClientDataHash.ToArray(), value => value != 0);
        if (verifiedMiss)
            Assert.Empty(matches);
        else
        {
            var response = await Assert.Single(matches).SelectAsync(ct);
            Assert.Equal(real.ClientDataHash.ToArray(), response.ClientData.Hash.ToArray());
            Assert.Equal(b.Id.ToArray(), response.CredentialId.ToArray());
        }
    }

    [Fact]
    public async Task TokenlessUv_RegistrationKeepsFittingProtectedExcludeList()
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(uvSupported: true, maxCredentialCountInList: 2));
        var a = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var b = new PublicKeyCredentialDescriptor(new byte[] { 2 });
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetAssertionResponse>(new CtapException(CtapStatus.NoCredentials)));
        var requests = new List<BackendMakeCredentialRequest>();
        _backend.MakeCredentialAsync(Arg.Do<BackendMakeCredentialRequest>(requests.Add), Arg.Any<CancellationToken>()).Returns(call =>
            call.Arg<BackendMakeCredentialRequest>().ExcludeList?.Contains(b) == true
                ? throw new CtapException(CtapStatus.CredentialExcluded)
                : MockCtap2Responses.CreateMockMakeCredentialResponse());

        await using var client = _client;
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(
            Registration(UserVerificationPreference.Required) with { ExcludeCredentials = [a, b] },
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
        var request = Assert.Single(requests);
        Assert.Equal(new[] { a, b }, request.ExcludeList);
        Assert.True(request.Options?.GetValueOrDefault("uv"));
        Assert.Null(request.PinUvAuthParam);
        await _backend.DidNotReceive().GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("foreign", WebAuthnClientErrorCode.InvalidState)]
    [InlineData("missing ID", WebAuthnClientErrorCode.InvalidState)]
    [InlineData("missing UV", WebAuthnClientErrorCode.InvalidState)]
    [InlineData("missing UP", WebAuthnClientErrorCode.InvalidState)]
    [InlineData("multiple", WebAuthnClientErrorCode.InvalidState)]
    [InlineData("zero count", WebAuthnClientErrorCode.InvalidState)]
    [InlineData("denied", WebAuthnClientErrorCode.NotAllowed)]
    public async Task TokenlessUv_BadInteractiveAssertionStopsBeforeNextChunk(string failure, WebAuthnClientErrorCode expected)
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(uvSupported: true, maxCredentialCountInList: 2));
        var a = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var b = new PublicKeyCredentialDescriptor(new byte[] { 2 });
        var c = new PublicKeyCredentialDescriptor(new byte[] { 3 });
        var response = MockCtap2Responses.CreateMockGetAssertionResponse(failure == "foreign" ? [3] : [1],
            numberOfCredentials: failure switch { "multiple" => 2u, "zero count" => 0u, _ => null }, omitCredential: failure == "missing ID",
            userVerified: failure != "missing UV", userPresent: failure != "missing UP", largeBlobKey: [9]);
        int real = 0;
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            if (request.Options?.GetValueOrDefault("up", true) == false)
                throw new CtapException(CtapStatus.NoCredentials);
            real++;
            Assert.Equal(new[] { a, b }, request.AllowList);
            if (failure == "denied")
                throw new CtapException(CtapStatus.NotAllowed);
            return response;
        });

        await using var client = _client;
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(WithAllowList(a, b, c) with
        {
            UserVerification = UserVerificationPreference.Required
        }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(expected, error.Code);
        Assert.Equal(1, real);
        await _backend.DidNotReceive().GetNextAssertionAsync(Arg.Any<CancellationToken>());
        if (failure != "denied")
        {
            Assert.Equal(-1, response.AuthenticatorDataRaw.Span.IndexOfAnyExcept((byte)0));
            Assert.Equal(new byte[1], response.LargeBlobKey?.ToArray());
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task TokenlessUv_PositiveSilentHitStillRequiresUvAndUpOnFinalAssertion(bool uv, bool up)
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(uvSupported: true, maxCredentialCountInList: 2));
        var a = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var b = new PublicKeyCredentialDescriptor(new byte[] { 2 });
        var rejected = MockCtap2Responses.CreateMockGetAssertionResponse(b.Id.ToArray(),
            userVerified: uv, userPresent: up, largeBlobKey: [9], signature: [4]);
        int real = 0;
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            if (request.Options?.GetValueOrDefault("up", true) == false)
                return MockCtap2Responses.CreateMockGetAssertionResponse(b.Id.ToArray(), userVerified: false, userPresent: false);

            real++;
            Assert.Equal(new[] { b }, request.AllowList);
            return rejected;
        });

        await using var client = _client;
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(WithAllowList(a, b) with
        {
            UserVerification = UserVerificationPreference.Required
        }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
        Assert.Equal(1, real);
        Assert.Equal(-1, rejected.AuthenticatorDataRaw.Span.IndexOfAnyExcept((byte)0));
        Assert.Equal(new byte[1], rejected.LargeBlobKey?.ToArray());
        Assert.Equal(new byte[1], rejected.Signature.ToArray());
        Assert.Equal(new byte[] { 2 }, b.Id.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TokenlessUv_RegistrationRefinesOversizedCheckWithoutSkipping(bool excluded)
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(uvSupported: true, maxCredentialCountInList: 2));
        var a = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var b = new PublicKeyCredentialDescriptor(new byte[] { 2 });
        var c = new PublicKeyCredentialDescriptor(new byte[] { 3 });
        var real = new List<BackendGetAssertionRequest>();
        var discarded = MockCtap2Responses.CreateMockGetAssertionResponse(omitCredential: true, largeBlobKey: [9]);
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            Assert.Null(request.Extensions);
            Assert.Null(request.PinUvAuthParam);
            Assert.Null(request.PinUvAuthProtocol);
            if (request.Options?.GetValueOrDefault("up", true) == false)
                throw new CtapException(CtapStatus.NoCredentials);

            real.Add(request);
            Assert.True(request.Options?.GetValueOrDefault("uv"));
            Assert.True(request.Options?.GetValueOrDefault("up"));
            if (request.AllowList?.Count > 1)
                throw new CtapException(CtapStatus.RequestTooLarge);
            if (excluded && request.AllowList?.Contains(b) == true)
                return discarded;
            throw new CtapException(CtapStatus.NoCredentials);
        });
        var creation = new List<BackendMakeCredentialRequest>();
        _backend.MakeCredentialAsync(Arg.Do<BackendMakeCredentialRequest>(creation.Add), Arg.Any<CancellationToken>()).Returns(call =>
            call.Arg<BackendMakeCredentialRequest>().ExcludeList?.Contains(b) == true
                ? throw new CtapException(CtapStatus.CredentialExcluded)
                : MockCtap2Responses.CreateMockMakeCredentialResponse());

        await using var client = _client;
        var options = Registration(UserVerificationPreference.Required) with { ExcludeCredentials = [a, b, c] };
        var ct = TestContext.Current.CancellationToken;
        if (excluded)
        {
            var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(options, cancellationToken: ct));
            Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
            Assert.Equal(-1, discarded.AuthenticatorDataRaw.Span.IndexOfAnyExcept((byte)0));
            Assert.Equal(new byte[1], discarded.LargeBlobKey?.ToArray());
        }
        else
            await client.MakeCredentialAsync(options, cancellationToken: ct);

        Assert.Equal(excluded ? 3 : 4, real.Count);
        Assert.Equal(new[] { a, b }, real[0].AllowList);
        Assert.Equal(new[] { a }, real[1].AllowList);
        Assert.Equal(new[] { b }, real[2].AllowList);
        if (!excluded)
            Assert.Equal(new[] { c }, real[3].AllowList);
        var sent = Assert.Single(creation);
        Assert.All(real, candidate => Assert.Equal(sent.ClientDataHash.ToArray(), candidate.ClientDataHash.ToArray()));
        Assert.Contains(sent.ClientDataHash.ToArray(), value => value != 0);
        Assert.Equal(excluded ? [b] : Array.Empty<PublicKeyCredentialDescriptor>(), sent.ExcludeList);
    }

    [Theory]
    [InlineData(195, false)]
    [InlineData(195, true)]
    [InlineData(32, false)]
    public async Task TokenlessUv_RegistrationDoesNotSendAnOversizedMakeCredential(int maxMsgSize, bool excluded)
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(uvSupported: true, maxMsgSize: maxMsgSize));
        var credential = new PublicKeyCredentialDescriptor(Enumerable.Repeat((byte)4, 100).ToArray());
        int verified = 0;
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            if (request.Options?.GetValueOrDefault("up", true) == false)
                throw new CtapException(CtapStatus.NoCredentials);
            verified++;
            Assert.Equal(credential, Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<PublicKeyCredentialDescriptor>>(request.AllowList)));
            return excluded ? MockCtap2Responses.CreateMockGetAssertionResponse(credential.Id.ToArray())
                : throw new CtapException(CtapStatus.NoCredentials);
        });
        var creation = new List<BackendMakeCredentialRequest>();
        _backend.MakeCredentialAsync(Arg.Do<BackendMakeCredentialRequest>(creation.Add), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockMakeCredentialResponse());

        await using var client = _client;
        var options = Registration(UserVerificationPreference.Required) with { ExcludeCredentials = [credential] };
        var ct = TestContext.Current.CancellationToken;
        if (excluded || maxMsgSize == 32)
        {
            var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(options, cancellationToken: ct));
            Assert.Equal(WebAuthnClientErrorCode.Constraint, error.Code);
            Assert.Empty(creation);
        }
        else
        {
            await client.MakeCredentialAsync(options, cancellationToken: ct);
            Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<PublicKeyCredentialDescriptor>>(Assert.Single(creation).ExcludeList));
        }
        Assert.Equal(maxMsgSize == 32 ? 0 : 1, verified);
    }

    [Fact]
    public async Task TokenlessUv_EscalatedRegistrationDoesNotTrustCachedSilentMiss()
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(new AuthenticatorInfo
        {
            Options = new Dictionary<string, bool> { ["uv"] = true, ["makeCredUvNotRqd"] = true }
        });
        var a = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var b = new PublicKeyCredentialDescriptor(new byte[] { 2 });
        int silent = 0;
        int verified = 0;
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            var descriptor = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<PublicKeyCredentialDescriptor>>(request.AllowList));
            if (request.Options?.GetValueOrDefault("up", true) == false)
            {
                silent++;
                throw new CtapException(CtapStatus.NoCredentials);
            }
            verified++;
            Assert.True(request.Options?.GetValueOrDefault("uv"));
            return descriptor == b ? MockCtap2Responses.CreateMockGetAssertionResponse(b.Id.ToArray())
                : throw new CtapException(CtapStatus.NoCredentials);
        });
        var attempts = new List<BackendMakeCredentialRequest>();
        _backend.MakeCredentialAsync(Arg.Do<BackendMakeCredentialRequest>(attempts.Add), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendMakeCredentialRequest>();
            if (request.Options?.GetValueOrDefault("uv") == true && request.ExcludeList?.Contains(b) == true)
                throw new CtapException(CtapStatus.CredentialExcluded);
            if (attempts.Count == 1)
                throw new CtapException(CtapStatus.PuatRequired);
            return MockCtap2Responses.CreateMockMakeCredentialResponse();
        });

        await using var client = _client;
        var options = Registration(UserVerificationPreference.Discouraged) with { ExcludeCredentials = [a, b] };
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(options,
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
        Assert.Equal(2, silent);
        Assert.Equal(2, verified);
        Assert.Equal(2, attempts.Count); // One preexisting escalation, not a replay of the built-in UV creation.
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<PublicKeyCredentialDescriptor>>(attempts[0].ExcludeList));
        Assert.Equal(new[] { b }, attempts[1].ExcludeList);
    }

    [Fact]
    public async Task Assertion_UnassociatedSuccessfulProbeIsNotAPrfRetryMiss()
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(clientPinSupported: true, extensions: ["hmac-secret"]));
        var a = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var b = new PublicKeyCredentialDescriptor(new byte[] { 2 });
        var wrong = MockCtap2Responses.CreateMockGetAssertionResponse(b.Id.ToArray(), largeBlobKey: Enumerable.Repeat((byte)0x45, 32).ToArray());
        var requests = new List<BackendGetAssertionRequest>();
        _backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(requests.Add), Arg.Any<CancellationToken>())
            .Returns(wrong);
        var options = WithAllowList(a, b) with
        {
            Extensions = new AuthenticationExtensionInputs(Prf: new PrfInput { First = "RP input"u8.ToArray() })
        };

        await using var client = _client;
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(options, Pin, TestContext.Current.CancellationToken));
        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
        var probe = Assert.Single(requests);
        Assert.False(probe.Options?.GetValueOrDefault("up", true));
        Assert.Equal(a, Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<PublicKeyCredentialDescriptor>>(probe.AllowList)));
        Assert.Null(probe.PinUvAuthParam);
        await _backend.DidNotReceive().GetPinUvTokenAsync(Arg.Any<PinUvAuthMethod>(), Arg.Any<PinUvAuthTokenPermissions>(),
            Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>());
        await _backend.DidNotReceive().EncapsulateAsync(Arg.Any<CancellationToken>());
        Assert.False(wrong.AuthenticatorDataRaw.Span.ContainsAnyExcept((byte)0));
        Assert.False(wrong.LargeBlobKey.GetValueOrDefault().Span.ContainsAnyExcept((byte)0));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
    public async Task ExtensionPreparationUsesGeneralMappingBeforeFirstOrRetriedCommand(bool registration, bool retry)
    {
        var ct = TestContext.Current.CancellationToken;
        var failure = new CtapException(CtapStatus.InvalidOption);
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(MockCtap2Responses.CreateMockAuthenticatorInfo(
            clientPinSupported: true, extensions: ["previewSign", "hmac-secret", "hmac-secret-mc"]));
        _backend.EncapsulateAsync(Arg.Any<CancellationToken>()).Throws(failure);
        var allowed = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var noPrf = new PublicKeyCredentialDescriptor(new byte[] { 2 });
        var commandCount = 0;
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            if (request.Options?.GetValueOrDefault("up", true) == false)
            {
                if (retry && request.PinUvAuthParam is null)
                    throw new CtapException(CtapStatus.NoCredentials);
                return MockCtap2Responses.CreateMockGetAssertionResponse([1]);
            }
            commandCount++;
            Assert.Null(request.Extensions); // The dummy request cannot carry the requested previewSign input.
            throw new CtapException(CtapStatus.PuatRequired);
        });
        var prf = retry ? new PrfInput
        {
            EvalByCredential = new Dictionary<string, PrfInputValues> { ["AQ"] = new() { First = "RP input"u8.ToArray() } }
        } : new PrfInput { First = "RP input"u8.ToArray() };
        var signingInputs = new Dictionary<ReadOnlyMemory<byte>, WebAuthn.Extensions.PreviewSign.PreviewSignSigningParams>
        {
            [allowed.Id] = new(new byte[] { 2 }, new byte[] { 3 })
        };
        if (retry)
            signingInputs[noPrf.Id] = new(new byte[] { 4 }, new byte[] { 5 });
        await using var client = _client;
        Task PerformAsync() => registration
            ? client.MakeCredentialAsync(Registration() with
            {
                Extensions = new RegistrationExtensionInputs(Prf: prf, PreviewSign: new([CoseAlgorithm.Es256]))
            }, Pin, ct)
            : client.GetAssertionAsync(WithAllowList(retry ? [allowed, noPrf] : [allowed]) with
            {
                Extensions = new AuthenticationExtensionInputs(Prf: prf, PreviewSign: new(signingInputs))
            }, Pin, ct);

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(PerformAsync);
        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
        Assert.Same(failure, error.InnerException);
        Assert.Equal(retry ? 1 : 0, commandCount);
        await _backend.DidNotReceive().MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>());
        await _backend.Received(1).EncapsulateAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task PrfEvaluationRequiresVerificationOnlyWhenSupported(bool registration, bool evaluationSupported)
    {
        string[] names = evaluationSupported ? ["hmac-secret", "hmac-secret-mc"]
            : registration ? ["hmac-secret"] : [];
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(extensions: names));
        _backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockMakeCredentialResponse());
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse());
        await using var client = _client;
        var ct = TestContext.Current.CancellationToken;
        var prf = new PrfInput { First = "RP input"u8.ToArray() };

        Task PerformAsync() => registration
            ? client.MakeCredentialAsync(Registration(UserVerificationPreference.Discouraged) with
            { Extensions = new RegistrationExtensionInputs(Prf: prf) }, cancellationToken: ct)
            : client.GetAssertionAsync(Authentication(UserVerificationPreference.Discouraged) with
            { Extensions = new AuthenticationExtensionInputs(Prf: prf) }, cancellationToken: ct);

        if (evaluationSupported)
            Assert.Equal(WebAuthnClientErrorCode.NotAllowed, (await Assert.ThrowsAsync<WebAuthnClientError>(PerformAsync)).Code);
        else
            await PerformAsync();

        await _backend.Received(!evaluationSupported && registration ? 1 : 0)
            .MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>());
        await _backend.Received(!evaluationSupported && !registration ? 1 : 0)
            .GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MakeCredential_MapsDiscoveryAndTokenFailuresWithoutEnteringCommandRetry(bool discovery)
    {
        await using var client = _client;
        var failure = new CtapException(CtapStatus.PuatRequired);
        if (discovery)
            _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Throws(failure);
        else
            _backend.GetPinUvTokenAsync(PinUvAuthMethod.Pin, Arg.Any<PinUvAuthTokenPermissions>(), RpId,
                Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>()).Throws(failure);

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(
            Registration(), Pin, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        Assert.Same(failure, error.InnerException);
        await _backend.Received(discovery ? 0 : 1).GetPinUvTokenAsync(PinUvAuthMethod.Pin,
            Arg.Any<PinUvAuthTokenPermissions>(), RpId, Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>());
        await _backend.DidNotReceive().MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_PuatRequiredTwice_EscalatesOnceThenMapsTheSecondFailure()
    {
        _backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.PuatRequired));

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.MakeCredentialAsync(Registration(), Pin, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        await _backend.Received(2).MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAssertion_PuatRequiredTwice_EscalatesOnceThenMapsTheSecondFailure()
    {
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.PuatRequired));

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.GetAssertionAsync(Authentication(), Pin, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        await _backend.Received(2).GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>The escalated attempt is a new attempt: a fresh extension session produces the outputs.</summary>
    [Fact]
    public async Task MakeCredential_AfterEscalation_ExtensionOutputsComeFromTheSucceedingAttempt()
    {
        var calls = 0;
        _backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++calls == 1
                ? throw new CtapException(CtapStatus.PuatRequired)
                : MockCtap2Responses.CreateMockMakeCredentialResponse());
        var options = Registration() with
        {
            ResidentKey = ResidentKeyPreference.Discouraged,
            Extensions = new RegistrationExtensionInputs(CredProps: new CredPropsInput())
        };

        var response = await _client.MakeCredentialAsync(options, Pin, TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
        Assert.NotNull(response.ClientExtensionResults?.CredProps);
        Assert.False(response.ClientExtensionResults.CredProps.ResidentKey);
    }

    [Fact]
    public async Task MakeCredential_ExcludeListWithToken_ProbesWithUpFalseThenSendsOnlyTheMatch()
    {
        var excluded = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        var other = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        var probes = new List<BackendGetAssertionRequest>();
        _backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(probes.Add), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<BackendGetAssertionRequest>().AllowList!.Any(d => d.Id.Span.SequenceEqual(excluded.Id.Span))
                ? MockCtap2Responses.CreateMockGetAssertionResponse(excluded.Id.ToArray())
                : throw new CtapException(CtapStatus.NoCredentials));
        BackendMakeCredentialRequest? sent = null;
        _backend.MakeCredentialAsync(Arg.Do<BackendMakeCredentialRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.CredentialExcluded));

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => _client.MakeCredentialAsync(
            Registration(UserVerificationPreference.Required) with { ExcludeCredentials = [other, excluded] },
            Pin, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code); // WebAuthn L3 §5.1.3: CredentialExcluded → InvalidStateError
        // No maxCredentialCountInList in GetInfo → chunks of one, in the relying party's order.
        Assert.Equal(2, probes.Count);
        Assert.All(probes, probe =>
        {
            Assert.False(probe.Options!["up"]);
            Assert.NotNull(probe.PinUvAuthParam);
        });
        Assert.Equal([other], probes[0].AllowList);
        Assert.Equal([excluded], probes[1].AllowList);
        Assert.Equal([excluded], sent!.ExcludeList);
    }

    [Fact]
    public async Task MakeCredential_ExcludeListProbeMatchesNothing_SendsEmptyExcludeList()
    {
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.NoCredentials));
        BackendMakeCredentialRequest? sent = null;
        _backend.MakeCredentialAsync(Arg.Do<BackendMakeCredentialRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockMakeCredentialResponse());

        await _client.MakeCredentialAsync(
            Registration(UserVerificationPreference.Required) with { ExcludeCredentials = [new PublicKeyCredentialDescriptor(new byte[] { 1 })] },
            Pin, TestContext.Current.CancellationToken);

        Assert.NotNull(sent);
        Assert.Empty(sent.ExcludeList!);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task MakeCredential_ExcludeListWithoutToken_ProbesInBatchesAndNarrowsTheFinalList(
        bool credentialExists, bool pinSupplied)
    {
        ArrangeDiscouragedUvAuthenticator();
        var absent = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        var candidate = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        var probes = new List<BackendGetAssertionRequest>();
        _backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(probes.Add), Arg.Any<CancellationToken>())
            .Returns(call => credentialExists && call.Arg<BackendGetAssertionRequest>().AllowList!.Contains(candidate)
                ? MockCtap2Responses.CreateMockGetAssertionResponse(candidate.Id.ToArray())
                : throw new CtapException(CtapStatus.NoCredentials));
        BackendMakeCredentialRequest? sent = null;
        _backend.MakeCredentialAsync(Arg.Do<BackendMakeCredentialRequest>(request => sent = request), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<BackendMakeCredentialRequest>().ExcludeList is { Count: > 0 }
                ? throw new CtapException(CtapStatus.CredentialExcluded)
                : MockCtap2Responses.CreateMockMakeCredentialResponse());
        var options = Registration(UserVerificationPreference.Discouraged) with
        {
            ExcludeCredentials = [absent, candidate]
        };

        if (credentialExists)
        {
            var error = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
                _client.MakeCredentialAsync(options, pinSupplied ? Pin : null, TestContext.Current.CancellationToken));
            Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
        }
        else
        {
            await _client.MakeCredentialAsync(options, pinSupplied ? Pin : null, TestContext.Current.CancellationToken);
        }

        Assert.Equal(2, probes.Count);
        Assert.Equal([absent], probes[0].AllowList);
        Assert.Equal([candidate], probes[1].AllowList);
        Assert.All(probes, probe =>
        {
            Assert.False(probe.Options!["up"]);
            Assert.Null(probe.PinUvAuthParam);
            Assert.Null(probe.PinUvAuthProtocol);
        });
        Assert.NotNull(sent);
        Assert.Equal<PublicKeyCredentialDescriptor>(credentialExists ? [candidate] : [], sent.ExcludeList!);
        Assert.Null(sent.PinUvAuthParam);
        Assert.Null(sent.PinUvAuthProtocol);
        Assert.Null(sent.Options);
        await _backend.DidNotReceive().GetPinUvTokenAsync(
            Arg.Any<PinUvAuthMethod>(), Arg.Any<PinUvAuthTokenPermissions>(), Arg.Any<string>(),
            Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_ExcludeListWithoutToken_UvEscalationReusesTheProbedMatch()
    {
        ArrangeDiscouragedUvAuthenticator();
        var excluded = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse(excluded.Id.ToArray()));
        var attempts = new List<BackendMakeCredentialRequest>();
        _backend.MakeCredentialAsync(Arg.Do<BackendMakeCredentialRequest>(attempts.Add), Arg.Any<CancellationToken>())
            .Throws(_ => new CtapException(attempts.Count == 1 ? CtapStatus.PuatRequired : CtapStatus.CredentialExcluded));

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => _client.MakeCredentialAsync(
            Registration(UserVerificationPreference.Discouraged) with { ExcludeCredentials = [excluded] },
            Pin, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
        Assert.Equal(2, attempts.Count);
        Assert.All(attempts, attempt => Assert.Equal([excluded], attempt.ExcludeList));
        Assert.Null(attempts[0].PinUvAuthParam);
        Assert.NotNull(attempts[1].PinUvAuthParam);
        await _backend.Received(1).GetAssertionAsync(
            Arg.Is<BackendGetAssertionRequest>(request => !request.PinUvAuthParam.HasValue && !request.PinUvAuthProtocol.HasValue),
            Arg.Any<CancellationToken>());
        await _backend.Received(1).GetPinUvTokenAsync(
            PinUvAuthMethod.Pin, Arg.Any<PinUvAuthTokenPermissions>(), RpId,
            Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>());
    }

    private void ArrangeDiscouragedUvAuthenticator() =>
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(new AuthenticatorInfo
        {
            Versions = ["FIDO_2_1"],
            Options = new Dictionary<string, bool> { ["clientPin"] = true, ["makeCredUvNotRqd"] = true },
            MaxCredentialCountInList = 1
        });

    /// <summary>A probe failure other than NoCredentials keeps its dedicated wrapping and does not escalate UV.</summary>
    [Fact]
    public async Task MakeCredential_ExcludeListProbeFails_ThrowsUnknownWithoutEscalating()
    {
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.PuatRequired));

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => _client.MakeCredentialAsync(
            Registration(UserVerificationPreference.Required) with { ExcludeCredentials = [new PublicKeyCredentialDescriptor(new byte[] { 1 })] },
            Pin, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.Unknown, error.Code);
        Assert.Contains("Pre-flight excludeList probe failed", error.Message, StringComparison.Ordinal);
        await _backend.Received(1).GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
        await _backend.DidNotReceive().MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Checks combined probe permissions followed by a new MakeCredential-only token request.</summary>
    /// <remarks>Earlier commentary attributes re-minting to used-token behavior on firmware 5.8.0.
    /// This fake-backend test does not measure or qualify that firmware behavior.</remarks>
    [Fact]
    public async Task MakeCredential_AfterExcludeListProbe_RemintsTokenWithMakeCredentialOnly()
    {
        var permissions = new List<PinUvAuthTokenPermissions>();
        _backend.GetPinUvTokenAsync(
                PinUvAuthMethod.Pin, Arg.Do<PinUvAuthTokenPermissions>(permissions.Add), RpId,
                Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), (byte[])TokenBytes.Clone()));
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.NoCredentials));
        _backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockMakeCredentialResponse());

        await _client.MakeCredentialAsync(
            Registration(UserVerificationPreference.Required) with { ExcludeCredentials = [new PublicKeyCredentialDescriptor(new byte[] { 1 })] },
            Pin, TestContext.Current.CancellationToken);

        Assert.Equal(
            [PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion, PinUvAuthTokenPermissions.MakeCredential],
            permissions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
    public async Task MakeCredential_UsesSpecialMappingOnlyWhenPreviewSignIsSent(bool supported)
    {
        if (!supported)
            ArrangeNoTokenAuthenticator();
        var failure = new CtapException(CtapStatus.InvalidOption);
        _backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Throws(failure);
        var options = Registration() with
        {
            Extensions = new RegistrationExtensionInputs(
                PreviewSign: new WebAuthn.Extensions.PreviewSign.PreviewSignRegistrationInput([CoseAlgorithm.Es256]))
        };

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.MakeCredentialAsync(options, Pin, TestContext.Current.CancellationToken));

        Assert.Equal(supported ? WebAuthnClientErrorCode.InvalidState : WebAuthnClientErrorCode.NotSupported, error.Code);
        Assert.Equal(supported, error.Message.StartsWith("previewSign:", StringComparison.Ordinal));
        Assert.Same(failure, error.InnerException);
    }

    [Fact]
    public async Task GetAssertion_WithoutToken_SendsNoPinUvAuthParamAndNoUvOption()
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockAuthenticatorInfo()); // no clientPin, no uv
        BackendGetAssertionRequest? sent = null;
        _backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse());

        await _client.GetAssertionAsync(Authentication(UserVerificationPreference.Discouraged), pinBytes: null, TestContext.Current.CancellationToken);

        Assert.NotNull(sent);
        Assert.Null(sent.PinUvAuthParam);
        Assert.Null(sent.PinUvAuthProtocol);
        Assert.Null(sent.Options);
    }

    // ---- Allow-list credential selection (extension-processing R4) ----

    private static readonly byte[] DummyCredentialId = [0x00]; // python-fido2 client/__init__.py L1012-1017: id=b"\0"

    private static PublicKeyCredentialRequestOptions WithAllowList(params PublicKeyCredentialDescriptor[] allowList) =>
        Authentication(UserVerificationPreference.Discouraged) with { AllowCredentials = allowList };

    private void ArrangeNoTokenAuthenticator() =>
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockAuthenticatorInfo()); // no clientPin: ceremony runs without a token

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
    public async Task AssertionUsesSpecialMappingOnlyWhenPreviewSignIsSent(bool match)
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(extensions: ["previewSign"]));
        var allowed = new PublicKeyCredentialDescriptor(new byte[] { 1 });
        var failure = new CtapException(CtapStatus.InvalidOption);
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            if (request.Options?.GetValueOrDefault("up", true) == false)
                return match ? MockCtap2Responses.CreateMockGetAssertionResponse([1]) : throw new CtapException(CtapStatus.NoCredentials);
            Assert.Equal(match, request.Extensions.HasValue);
            throw failure;
        });
        var options = WithAllowList(allowed) with
        {
            Extensions = new AuthenticationExtensionInputs(PreviewSign: new(
                new Dictionary<ReadOnlyMemory<byte>, WebAuthn.Extensions.PreviewSign.PreviewSignSigningParams>
                { [allowed.Id] = new(new byte[] { 2 }, new byte[] { 3 }) }))
        };
        await using var client = _client;
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(options,
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(match ? WebAuthnClientErrorCode.InvalidState : WebAuthnClientErrorCode.NotSupported, error.Code);
        Assert.Same(failure, error.InnerException);
    }

    [Fact]
    public async Task AssertionContinuationDoesNotRetryTheInitialCommand()
    {
        var failure = new CtapException(CtapStatus.PuatRequired);
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse(numberOfCredentials: 2));
        _backend.GetNextAssertionAsync(Arg.Any<CancellationToken>()).Throws(failure);
        await using var client = _client;
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(
            Authentication(UserVerificationPreference.Discouraged), Pin, TestContext.Current.CancellationToken));
        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        Assert.Same(failure, error.InnerException);
        await _backend.Received(1).GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
    public async Task GetAssertion_PreviewSignDoesNotPublishAnUnknownOrUnassociatedId(bool omitted)
    {
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(extensions: ["previewSign"]));
        ReadOnlyMemory<byte> allowed = new byte[] { 1 };
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<BackendGetAssertionRequest>().Options?.GetValueOrDefault("up") == false
                ? throw new CtapException(CtapStatus.NoCredentials)
                : MockCtap2Responses.CreateMockGetAssertionResponse([7], omitCredential: omitted));
        var input = new WebAuthn.Extensions.PreviewSign.PreviewSignAuthenticationInput(
            new Dictionary<ReadOnlyMemory<byte>, WebAuthn.Extensions.PreviewSign.PreviewSignSigningParams>
            {
                [allowed] = new(new byte[] { 2 }, new byte[] { 3 })
            });
        await using var client = _client;
        var options = WithAllowList(new PublicKeyCredentialDescriptor(allowed)) with
        {
            Extensions = new AuthenticationExtensionInputs(PreviewSign: input)
        };
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(options,
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAssertion_FinalCredentialIdUsesOnlyTheProbedSingleton(bool wrongId)
    {
        ArrangeNoTokenAuthenticator();
        await using var client = _client;
        byte[] first = [1];
        byte[] selected = [2];
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<BackendGetAssertionRequest>();
                Assert.NotNull(request.AllowList);
                if (request.Options?.GetValueOrDefault("up") == false)
                    return request.AllowList[0].Id.Span.SequenceEqual(selected)
                        ? MockCtap2Responses.CreateMockGetAssertionResponse(selected)
                        : throw new CtapException(CtapStatus.NoCredentials);
                Assert.Equal(selected, Assert.Single(request.AllowList).Id.Span);
                return MockCtap2Responses.CreateMockGetAssertionResponse(first, omitCredential: !wrongId);
            });
        var options = WithAllowList(new PublicKeyCredentialDescriptor(first), new PublicKeyCredentialDescriptor(selected));
        var ct = TestContext.Current.CancellationToken;

        if (wrongId)
        {
            var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(options, cancellationToken: ct));
            Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
        }
        else
        {
            var match = Assert.Single(await client.GetAssertionAsync(options, cancellationToken: ct));
            byte[] expected = [2];
            selected.AsSpan().Clear(); // Request memory is no longer borrowed after the awaited ceremony.
            Assert.Equal(expected, match.Id.Span);
            var response = await match.SelectAsync(ct);
            Assert.Equal(expected, response.CredentialId.Span);
        }
    }

    /// <summary>
    ///     With an allow list the client first finds the credential with an up:false probe, then asserts with
    ///     exactly that one descriptor.
    /// </summary>
    [Fact]
    public async Task GetAssertion_AllowList_ProbesWithUpFalse_ThenSendsOnlyTheSelectedDescriptor()
    {
        ArrangeNoTokenAuthenticator();
        var absent = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        var present = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        var requests = new List<BackendGetAssertionRequest>();
        _backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(requests.Add), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<BackendGetAssertionRequest>().AllowList!.Any(d => d.Id.Span.SequenceEqual(present.Id.Span))
                ? MockCtap2Responses.CreateMockGetAssertionResponse(present.Id.ToArray())
                : throw new CtapException(CtapStatus.NoCredentials));

        var matches = await _client.GetAssertionAsync(WithAllowList(absent, present), pinBytes: null, TestContext.Current.CancellationToken);

        Assert.Equal(present.Id.ToArray(), Assert.Single(matches).Id.ToArray());
        var real = requests[^1];
        Assert.Null(real.Options); // up is never sent as false on the real request
        Assert.Equal([present], real.AllowList);
        var probes = requests[..^1];
        Assert.NotEmpty(probes);
        Assert.All(probes, probe => Assert.False(probe.Options!["up"]));
        Assert.Equal(2, probes.Sum(probe => probe.AllowList!.Count)); // every candidate was probed once
    }

    /// <summary>
    /// After a silent-probe miss, the final request carries a dummy descriptor with normal required-UP
    /// policy rather than returning the probe miss directly. NoCredentials produces an empty match list.
    /// This fake checks the request and result, not a gesture or a no-disclosure guarantee.
    /// </summary>
    [Fact]
    public async Task GetAssertion_AllowList_NothingMatches_SendsDummyDescriptorAndReturnsEmpty()
    {
        ArrangeNoTokenAuthenticator();
        var requests = new List<BackendGetAssertionRequest>();
        _backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(requests.Add), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.NoCredentials));

        var matches = await _client.GetAssertionAsync(
            WithAllowList(new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32))), pinBytes: null, TestContext.Current.CancellationToken);

        Assert.Empty(matches);
        var real = requests[^1];
        Assert.Null(real.Options);
        var dummy = Assert.Single(real.AllowList!);
        Assert.Equal(DummyCredentialId, dummy.Id.ToArray());
        Assert.Equal("public-key", dummy.Type);
    }

    /// <summary>
    ///     Checks one GetAssertion permission-token acquisition and nonnull auth parameters on both the
    ///     probe and final assertion requests. The fake backend and noncryptographic protocol exercise the
    ///     SDK's token-reuse choice, not authenticator acceptance or command-specific token invalidation.
    /// </summary>
    [Fact]
    public async Task GetAssertion_AllowList_WithToken_SignsProbeAndAssertionWithOneToken()
    {
        var permissions = new List<PinUvAuthTokenPermissions>();
        _backend.GetPinUvTokenAsync(
                PinUvAuthMethod.Pin, Arg.Do<PinUvAuthTokenPermissions>(permissions.Add), RpId,
                Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), (byte[])TokenBytes.Clone()));
        var credential = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        var requests = new List<BackendGetAssertionRequest>();
        _backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(requests.Add), Arg.Any<CancellationToken>())
            .Returns(_ => MockCtap2Responses.CreateMockGetAssertionResponse(credential.Id.ToArray()));

        await _client.GetAssertionAsync(
            Authentication(UserVerificationPreference.Required) with { AllowCredentials = [credential] }, Pin, TestContext.Current.CancellationToken);

        Assert.Equal(2, requests.Count);
        Assert.NotNull(requests[0].PinUvAuthParam); // probe
        Assert.NotNull(requests[1].PinUvAuthParam); // real assertion, same token
        Assert.Equal([PinUvAuthTokenPermissions.GetAssertion], permissions);
    }

    [Fact]
    public async Task GetAssertion_NoAllowList_DoesNotProbe()
    {
        ArrangeNoTokenAuthenticator();
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse());

        await _client.GetAssertionAsync(Authentication(UserVerificationPreference.Discouraged), pinBytes: null, TestContext.Current.CancellationToken);

        await _backend.Received(1).GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }
}