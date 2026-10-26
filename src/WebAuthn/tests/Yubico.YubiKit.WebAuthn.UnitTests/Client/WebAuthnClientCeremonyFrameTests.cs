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
///     Pins the ceremony frame shared by MakeCredential and GetAssertion before it is refactored: UV
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
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain is "com" });
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

    /// <summary>After the probe the token is re-minted with MakeCredential only (5.8.0: a used token no longer authorises).</summary>
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

    [Fact]
    public async Task MakeCredential_PreviewSignRequested_MapsCtapErrorsThroughPreviewSignMapper()
    {
        _backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.UnsupportedAlgorithm));
        var options = Registration() with
        {
            Extensions = new RegistrationExtensionInputs(
                PreviewSign: new WebAuthn.Extensions.PreviewSign.PreviewSignRegistrationInput([CoseAlgorithm.Es256]))
        };

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            _client.MakeCredentialAsync(options, Pin, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
        Assert.StartsWith("previewSign:", error.Message, StringComparison.Ordinal);
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

    /// <summary>Nothing matched: a dummy descriptor still goes to the authenticator so user presence is requested
    /// and the relying party cannot learn "credential absent" without a gesture; the result is empty as before.</summary>
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
    ///     With a token the probe is signed and the same token signs the assertion: a getAssertion accepts a token
    ///     already used for an up:false probe (only makeCredential does not, see the registration test).
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
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse(credential.Id.ToArray()));

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