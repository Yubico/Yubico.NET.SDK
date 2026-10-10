// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Formats.Cbor;
using System.Text.Json;
using NSubstitute;
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
        Challenge = new byte[32], RpId = "example.com",
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
                ClientDataHash = new byte[32], Rp = Registration().Rp, User = Registration().User,
                PubKeyCredParams = [PublicKeyCredentialParameters.CreateES256()]
            }, TestContext.Current.CancellationToken);
        }
        else
        {
            _ = await backend.GetAssertionAsync(new BackendGetAssertionRequest
            {
                ClientDataHash = new byte[32], RpId = "example.com"
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
            ClientDataHash = new byte[32], Rp = Registration().Rp, User = Registration().User,
            PubKeyCredParams = [PublicKeyCredentialParameters.CreateES256()]
        }, TestContext.Current.CancellationToken);

        Assert.Null(residentKey); // FidoSessionRequestEncoding writes rk:false if HasValue is true
    }

    [Fact]
    [Trait("Audit", "YESDK-1616")]
    public async Task YESDK1616_SameOriginRegistrationOmitsTopOriginFromClientData()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());
        await using var client = CreateClient(backend);
        var result = await client.MakeCredentialAsync(Registration() with
        {
            CrossOrigin = false, TopOrigin = "https://other.example"
        }, cancellationToken: TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(result.ClientData.JsonBytes);
        Assert.False(json.RootElement.GetProperty("crossOrigin").GetBoolean());
        Assert.False(json.RootElement.TryGetProperty("topOrigin", out _));
    }

    [Fact]
    [Trait("Audit", "YESDK-1616")]
    public async Task YESDK1616_SameOriginAssertionOmitsTopOriginFromClientData()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo());
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockGetAssertionResponse());
        await using var client = CreateClient(backend);
        var matches = await client.GetAssertionAsync(Authentication() with
        {
            CrossOrigin = false, TopOrigin = "https://other.example"
        }, cancellationToken: TestContext.Current.CancellationToken);
        var result = await Assert.Single(matches).SelectAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(result.ClientData.JsonBytes);
        Assert.False(json.RootElement.GetProperty("crossOrigin").GetBoolean());
        Assert.False(json.RootElement.TryGetProperty("topOrigin", out _));
    }

    [Fact(Timeout = 5000)]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_RegistrationTimeoutCancelsPendingAuthenticator()
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
        await using var client = CreateClient(backend);
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var operation = client.MakeCredentialAsync(Registration() with { Timeout = TimeSpan.FromMilliseconds(50) },
            cancellationToken: guard.Token);
        await entered.Task;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
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
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var operation = client.GetAssertionAsync(Authentication() with { Timeout = TimeSpan.FromMilliseconds(50) },
            cancellationToken: guard.Token);
        await entered.Task;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.False(guard.IsCancellationRequested, "the ceremony deadline, not the test guard, must cancel");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_NonpositiveGetInfoListLimitStillProbesAllCredentials(int maxCount)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = maxCount };
        var probes = 0;
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns<GetAssertionResponse>(call =>
            {
                var request = call.Arg<BackendGetAssertionRequest>();
                Assert.NotEmpty(Assert.IsAssignableFrom<IReadOnlyList<PublicKeyCredentialDescriptor>>(request.AllowList));
                probes++;
                throw new CtapException(CtapStatus.NoCredentials);
            });
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var match = await Yubico.YubiKit.WebAuthn.Internal.ExcludeListPreflight.FindFirstMatchAsync(
            backend, "example.com", [new PublicKeyCredentialDescriptor(new byte[] { 1 }), new PublicKeyCredentialDescriptor(new byte[] { 2 })],
            info, new byte[32], new TestPinUvAuthProtocol(), guard.Token);
        Assert.Null(match);
        Assert.Equal(2, probes);
    }
}
