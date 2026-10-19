// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using NSubstitute;
using System.Buffers;
using System.Formats.Cbor;
using System.Text.Json;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.AuditV2;

public class WebAuthnAuditReproTests
{
    private static WebAuthnClient CreateClient(IWebAuthnBackend backend)
    {
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        return new WebAuthnClient(backend, Assert.IsType<WebAuthnOrigin>(origin),
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain is "com" });
    }

    private static RegistrationOptions Registration() => new()
    {
        Challenge = new byte[32],
        Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
        User = new PublicKeyCredentialUserEntity(new byte[] { 1 }, "user", "User"),
        PubKeyCredParams = [CoseAlgorithm.Es256],
        UserVerification = UserVerificationPreference.Discouraged
    };

    private static AuthenticationOptions Authentication() => new()
    {
        Challenge = new byte[32],
        RpId = "example.com",
        UserVerification = UserVerificationPreference.Discouraged
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Audit", "YESDK-1610")]
    public async Task YESDK1610_RegistrationTokenDoesNotAlsoRequestUv(bool builtInUv)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(
                clientPinSupported: !builtInUv, uvSupported: builtInUv));
        backend.GetPinUvTokenAsync(Arg.Any<PinUvAuthMethod>(), Arg.Any<PinUvAuthTokenPermissions>(),
                Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]));
        BackendMakeCredentialRequest? sent = null;
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<BackendMakeCredentialRequest>();
                Assert.NotNull(sent.PinUvAuthParam);
                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });

        await using var client = CreateClient(backend);
        _ = await client.MakeCredentialAsync(Registration() with { UserVerification = UserVerificationPreference.Required },
            builtInUv ? null : "123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.NotNull(sent);
        Assert.False(sent.Options?.ContainsKey("uv") ?? false,
            "CTAP makeCredential must not contain both the uv option and pinUvAuthParam");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Audit", "YESDK-1610")]
    public async Task YESDK1610_AssertionTokenDoesNotAlsoRequestUv(bool builtInUv)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(
                clientPinSupported: !builtInUv, uvSupported: builtInUv));
        backend.GetPinUvTokenAsync(Arg.Any<PinUvAuthMethod>(), Arg.Any<PinUvAuthTokenPermissions>(),
                Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]));
        BackendGetAssertionRequest? sent = null;
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<BackendGetAssertionRequest>();
                Assert.NotNull(sent.PinUvAuthParam);
                return MockFido2Responses.CreateMockGetAssertionResponse();
            });

        await using var client = CreateClient(backend);
        _ = await client.GetAssertionAsync(Authentication() with { UserVerification = UserVerificationPreference.Required },
            builtInUv ? null : "123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.NotNull(sent);
        Assert.False(sent.Options?.ContainsKey("uv") ?? false,
            "CTAP getAssertion must not contain both the uv option and pinUvAuthParam");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Audit", "YESDK-1610")]
    public async Task YESDK1610_BackendPreservesAbsentUvOption(bool registration)
    {
        var session = Substitute.For<IFidoSession>();
        bool? uv = null;
        session.MakeCredentialAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<PublicKeyCredentialRpEntity>(),
                Arg.Any<PublicKeyCredentialUserEntity>(), Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
                Arg.Any<MakeCredentialOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                uv = call.Arg<MakeCredentialOptions>().UserVerification;
                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });
        session.GetAssertionAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<GetAssertionOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                uv = call.Arg<GetAssertionOptions>().UserVerification;
                return MockFido2Responses.CreateMockGetAssertionResponse();
            });
        await using var backend = new WebAuthnBackend(session);
        if (registration)
        {
            _ = await backend.MakeCredentialAsync(new BackendMakeCredentialRequest
            {
                ClientDataHash = new byte[32],
                Rp = Registration().Rp,
                User = Registration().User,
                PubKeyCredParams = [PublicKeyCredentialParameters.CreateES256()]
            }, TestContext.Current.CancellationToken);
        }
        else
        {
            _ = await backend.GetAssertionAsync(new BackendGetAssertionRequest
            {
                ClientDataHash = new byte[32],
                RpId = "example.com"
            }, TestContext.Current.CancellationToken);
        }

        Assert.Null(uv); // nullable false is encoded as an actual uv:false option by FidoSessionRequestEncoding
    }

    [Fact]
    [Trait("Audit", "YESDK-1614")]
    public async Task YESDK1614_NoneAttestationRemovesAuthenticatorStatement()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteTextString("alg"); writer.WriteInt32(-7);
        writer.WriteTextString("sig"); writer.WriteByteString([1, 2, 3]);
        writer.WriteEndMap();
        var statement = writer.Encode();
        writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(1); writer.WriteTextString("packed");
        writer.WriteInt32(2); writer.WriteByteString(MockFido2Responses.BuildAuthDataWithAttestedCredential(Guid.NewGuid()));
        writer.WriteInt32(3); writer.WriteEncodedValue(statement);
        writer.WriteEndMap();
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MakeCredentialResponse.Decode(writer.Encode()));

        await using var client = CreateClient(backend);
        var response = await client.MakeCredentialAsync(Registration() with { Attestation = AttestationPreference.None },
            cancellationToken: TestContext.Current.CancellationToken);
        var reader = new CborReader(response.RawAttestationObject, CborConformanceMode.Lax);
        _ = reader.ReadStartMap();
        string? format = null;
        int? statementLength = null;
        for (var index = 0; index < 3; index++)
        {
            var key = reader.ReadTextString();
            if (key == "fmt")
            {
                format = reader.ReadTextString();
            }
            else if (key == "attStmt")
            {
                statementLength = reader.ReadStartMap();
                for (var item = 0; item < statementLength; item++)
                {
                    reader.SkipValue();
                    reader.SkipValue();
                }
                reader.ReadEndMap();
            }
            else
            {
                reader.SkipValue();
            }
        }
        Assert.Equal("none", format);
        Assert.Equal(0, statementLength); // must not forward original signature
    }

    /// <summary>
    ///     WebAuthn L3 §5.1.3 "none": a self attestation (zero AAGUID, packed, no x5c) is left alone; any other
    ///     preference passes the statement through; after replacement every attestation view agrees and the
    ///     AAGUID is not zeroed (L3 change log).
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1614")]
    [InlineData(AttestationPreference.None, false, true, "none")] // non-self, x5c present → replaced
    [InlineData(AttestationPreference.None, true, false, "packed")] // self attestation → kept
    [InlineData(AttestationPreference.Direct, false, true, "packed")] // direct → kept
    public async Task YESDK1614_NonePreferenceKeepsSelfAttestation_AndOtherPreferencesPassThrough(
        AttestationPreference preference, bool zeroAaguid, bool includeX5c, string expectedFormat)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        var aaguid = zeroAaguid ? Guid.Empty : Guid.NewGuid();
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(includeX5c ? 3 : 2);
        writer.WriteTextString("alg"); writer.WriteInt32(-7);
        writer.WriteTextString("sig"); writer.WriteByteString([1, 2, 3]);
        if (includeX5c)
        {
            writer.WriteTextString("x5c"); writer.WriteStartArray(1); writer.WriteByteString([0x30, 0x00]); writer.WriteEndArray();
        }
        writer.WriteEndMap();
        var statement = writer.Encode();
        writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(1); writer.WriteTextString("packed");
        writer.WriteInt32(2); writer.WriteByteString(MockFido2Responses.BuildAuthDataWithAttestedCredential(aaguid));
        writer.WriteInt32(3); writer.WriteEncodedValue(statement);
        writer.WriteEndMap();
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MakeCredentialResponse.Decode(writer.Encode()));
        await using var client = CreateClient(backend);

        var response = await client.MakeCredentialAsync(Registration() with { Attestation = preference },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(expectedFormat, response.AttestationObject.Statement.Format.Value);
        Assert.Equal(expectedFormat == "none", response.AttestationStatement is NoneAttestationStatement);
        Assert.Equal(response.AttestationObject.RawCbor.ToArray(), response.RawAttestationObject.ToArray());
        Assert.Equal(aaguid, response.Aaguid.Value); // never zeroed, whatever the preference
    }

    [Fact]
    [Trait("Audit", "YESDK-1615")]
    public async Task YESDK1615_PreferredResidentKeyOnCapableAuthenticatorRequestsDiscoverable()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(new AuthenticatorInfo { Options = new Dictionary<string, bool> { ["rk"] = true } });
        BackendMakeCredentialRequest? sent = null;
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<BackendMakeCredentialRequest>();
                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });
        await using var client = CreateClient(backend);
        _ = await client.MakeCredentialAsync(Registration() with { ResidentKey = ResidentKeyPreference.Preferred },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(sent);
        Assert.True(sent.Options?.TryGetValue("rk", out var rk) == true && rk,
            "a capable authenticator must receive rk:true when residentKey is preferred");
    }

    [Fact]
    [Trait("Audit", "YESDK-1615")]
    public async Task YESDK1615_BackendPreservesAbsentResidentKeyOption()
    {
        var session = Substitute.For<IFidoSession>();
        bool? residentKey = null;
        session.MakeCredentialAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<PublicKeyCredentialRpEntity>(),
                Arg.Any<PublicKeyCredentialUserEntity>(), Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
                Arg.Any<MakeCredentialOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                residentKey = call.Arg<MakeCredentialOptions>().ResidentKey;
                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });
        await using var backend = new WebAuthnBackend(session);
        _ = await backend.MakeCredentialAsync(new BackendMakeCredentialRequest
        {
            ClientDataHash = new byte[32],
            Rp = Registration().Rp,
            User = Registration().User,
            PubKeyCredParams = [PublicKeyCredentialParameters.CreateES256()]
        }, TestContext.Current.CancellationToken);

        Assert.Null(residentKey); // FidoSessionRequestEncoding writes rk:false if HasValue is true
    }

    /// <summary>
    ///     WebAuthn L3 §5.8.1: topOrigin "is set only if ... crossOrigin is true". The client refuses the
    ///     contradictory input instead of emitting it (maintainer decision: reject, not silently drop).
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1616")]
    [InlineData(false)]
    [InlineData(null)]
    public async Task YESDK1616_SameOriginRegistrationWithTopOriginIsRejected(bool? crossOrigin)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        await using var client = CreateClient(backend);

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(Registration() with
        {
            CrossOrigin = crossOrigin,
            TopOrigin = "https://other.example"
        }, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, error.Code);
        await backend.DidNotReceive().MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [Trait("Audit", "YESDK-1616")]
    [InlineData(false)]
    [InlineData(null)]
    public async Task YESDK1616_SameOriginAssertionWithTopOriginIsRejected(bool? crossOrigin)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        await using var client = CreateClient(backend);

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(Authentication() with
        {
            CrossOrigin = crossOrigin,
            TopOrigin = "https://other.example"
        }, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, error.Code);
        await backend.DidNotReceive().GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Controls for the serialization WebAuthn L3 §5.8.1.1 requires: crossOrigin is always present (false when
    ///     unset) and topOrigin appears for a genuine cross-origin request.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1616")]
    public async Task YESDK1616_CrossOriginRegistrationCarriesTopOrigin_AndCrossOriginIsAlwaysSerialized()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());
        await using var client = CreateClient(backend);

        var crossOrigin = await client.MakeCredentialAsync(Registration() with
        {
            CrossOrigin = true,
            TopOrigin = "https://other.example"
        }, cancellationToken: TestContext.Current.CancellationToken);
        using (var json = JsonDocument.Parse(crossOrigin.ClientData.JsonBytes))
        {
            Assert.True(json.RootElement.GetProperty("crossOrigin").GetBoolean());
            Assert.Equal("https://other.example", json.RootElement.GetProperty("topOrigin").GetString());
        }

        var sameOrigin = await client.MakeCredentialAsync(Registration(),
            cancellationToken: TestContext.Current.CancellationToken);
        using (var json = JsonDocument.Parse(sameOrigin.ClientData.JsonBytes))
        {
            Assert.False(json.RootElement.GetProperty("crossOrigin").GetBoolean());
            Assert.False(json.RootElement.TryGetProperty("topOrigin", out _));
        }
    }

    // WebAuthn L3 §5.1.3 lifetimeTimer tests. The guard is the test's own safety net: it must never be
    // the one that cancels, otherwise the SDK did not honour the deadline.
    private static readonly TimeSpan DeadlineThatExpires = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan DeadlineThatDoesNotExpire = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TestGuard = TimeSpan.FromSeconds(1);

    private sealed class HangingPrompt : ICredentialPrompt
    {
        public TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ObservedToken { get; private set; }

        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken)
        {
            ObservedToken = cancellationToken;
            Requested.TrySetResult();
            return new ValueTask<IMemoryOwner<byte>?>(new TaskCompletionSource<IMemoryOwner<byte>?>().Task);
        }
    }

    private static (IWebAuthnBackend Backend, TaskCompletionSource Entered) HangingMakeCredentialBackend()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                entered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });
        return (backend, entered);
    }

    /// <summary>
    ///     WebAuthn L3 §5.1.3: "If lifetimeTimer expires ... invoke the authenticatorCancel operation ... Throw a
    ///     "NotAllowedError" DOMException." The backend's token is cancelled by the deadline, not by the caller.
    /// </summary>
    [Fact(Timeout = 5000)]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_RegistrationTimeoutCancelsPendingAuthenticator()
    {
        var (backend, entered) = HangingMakeCredentialBackend();
        await using var client = CreateClient(backend);
        using var guard = new CancellationTokenSource(TestGuard);

        var operation = client.MakeCredentialAsync(Registration() with { Timeout = DeadlineThatExpires },
            cancellationToken: guard.Token);
        await entered.Task;

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => operation);
        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        Assert.IsAssignableFrom<OperationCanceledException>(error.InnerException);
        Assert.False(guard.IsCancellationRequested, "the ceremony deadline, not the test guard, must cancel");
    }

    [Fact(Timeout = 5000)]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_AuthenticationTimeoutCancelsPendingAuthenticator()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                entered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
                return MockFido2Responses.CreateMockGetAssertionResponse();
            });
        await using var client = CreateClient(backend);
        using var guard = new CancellationTokenSource(TestGuard);

        var operation = client.GetAssertionAsync(Authentication() with { Timeout = DeadlineThatExpires },
            cancellationToken: guard.Token);
        await entered.Task;

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => operation);
        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        Assert.False(guard.IsCancellationRequested, "the ceremony deadline, not the test guard, must cancel");
    }

    /// <summary>Caller cancellation is unchanged: OperationCanceledException, never a WebAuthnClientError.</summary>
    [Fact(Timeout = 5000)]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_CallerCancellationStillSurfacesAsOperationCanceled()
    {
        var (backend, entered) = HangingMakeCredentialBackend();
        await using var client = CreateClient(backend);
        using var caller = new CancellationTokenSource();

        var operation = client.MakeCredentialAsync(Registration() with { Timeout = DeadlineThatDoesNotExpire },
            cancellationToken: caller.Token);
        await entered.Task;
        caller.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }

    /// <summary>A null Timeout starts no timer (python-fido2 parity; differs from the L3 §15.1 300 s default).</summary>
    [Fact]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_NullTimeoutStartsNoDeadline()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        CancellationToken observed = default;
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Do<CancellationToken>(token => observed = token))
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());
        await using var client = CreateClient(backend);

        var response = await client.MakeCredentialAsync(Registration(), cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.False(observed.CanBeCanceled, "no linked token source may be created when Timeout is null");
    }

    /// <summary>An unexpired deadline does not disturb a successful ceremony.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_UnexpiredTimeoutReturnsNormally()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());
        await using var client = CreateClient(backend);

        var response = await client.MakeCredentialAsync(Registration() with { Timeout = DeadlineThatDoesNotExpire },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(response);
    }

    /// <summary>No clamping (maintainer decision): a non-positive Timeout is a malformed request.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_NonpositiveTimeoutIsRejected(int milliseconds)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        await using var client = CreateClient(backend);

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(
            Authentication() with { Timeout = TimeSpan.FromMilliseconds(milliseconds) },
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, error.Code);
        await backend.DidNotReceive().GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>The timer covers user interaction too: a PIN prompt still waiting at the deadline is cancelled.</summary>
    [Fact(Timeout = 5000)]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_TimeoutCancelsPendingPinPrompt()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(clientPinSupported: true));
        backend.GetPinRetriesAsync(Arg.Any<CancellationToken>()).Returns(8);
        var prompt = new HangingPrompt();
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(backend, Assert.IsType<WebAuthnOrigin>(origin),
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain is "com", CredentialPrompt = prompt });
        using var guard = new CancellationTokenSource(TestGuard);

        var operation = client.MakeCredentialAsync(
            Registration() with { Timeout = DeadlineThatExpires, UserVerification = UserVerificationPreference.Required },
            cancellationToken: guard.Token);
        await prompt.Requested.Task;

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => operation);
        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        Assert.True(prompt.ObservedToken.IsCancellationRequested, "the prompt must see the deadline on its own token");
        Assert.False(guard.IsCancellationRequested, "the ceremony deadline, not the test guard, must cancel");
        await backend.DidNotReceive().MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     CTAP 2.1-2.3 §6.4 maxCredentialCountInList: "MUST be greater than zero if present." A non-positive
    ///     value is refused with a typed error before any probe is sent (maintainer decision: no clamping).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Audit", "YESDK-1633")]
    public async Task YESDK1633_NonpositiveGetInfoListLimitIsRefusedBeforeAnyProbe(int maxCount)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = maxCount };
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns<GetAssertionResponse>(_ => throw new CtapException(CtapStatus.NoCredentials));
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            CredentialFilter.SelectAsync(
                backend, "example.com", [new PublicKeyCredentialDescriptor(new byte[] { 1 }), new PublicKeyCredentialDescriptor(new byte[] { 2 })],
                info, new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]), guard.Token));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
        await backend.DidNotReceive().GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Control: an absent limit probes one credential at a time; a positive limit chunks by it.</summary>
    [Theory]
    [InlineData(null, 2)]
    [InlineData(8, 1)]
    [Trait("Audit", "YESDK-1633")]
    public async Task YESDK1633_PositiveOrAbsentGetInfoListLimitChunksTheExcludeList(int? maxCount, int expectedProbes)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = maxCount };
        var probes = 0;
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns<GetAssertionResponse>(call =>
            {
                Assert.NotEmpty(Assert.IsAssignableFrom<IReadOnlyList<PublicKeyCredentialDescriptor>>(call.Arg<BackendGetAssertionRequest>().AllowList));
                probes++;
                throw new CtapException(CtapStatus.NoCredentials);
            });
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var match = await CredentialFilter.SelectAsync(
            backend, "example.com", [new PublicKeyCredentialDescriptor(new byte[] { 1 }), new PublicKeyCredentialDescriptor(new byte[] { 2 })],
            info, new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]), guard.Token);

        Assert.Null(match);
        Assert.Equal(expectedProbes, probes);
    }
}