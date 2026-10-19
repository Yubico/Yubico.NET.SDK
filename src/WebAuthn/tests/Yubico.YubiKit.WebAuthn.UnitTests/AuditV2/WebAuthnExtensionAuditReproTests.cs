using NSubstitute;
using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Codec;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.AuditV2;

public class WebAuthnExtensionAuditReproTests
{
    private static ExtensionContext PrfContext()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.EncapsulateAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvSharedSecret(new Dictionary<int, object?> { [1] = 2 }, new byte[64]));
        return new ExtensionContext(
            MockFido2Responses.CreateMockAuthenticatorInfo(extensions: MockFido2Responses.AllKnownExtensions),
            new PinUvAuthProtocolV2(), backend);
    }

    /// <summary>WebAuthn L3 §10.1.4 over CTAP §12.7: the PRF request is an encrypted hmac-secret map, never a literal "prf" key.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1609")]
    public async Task YESDK1609_AuthenticationPrfUsesEncryptedHmacSecretNotLiteralPrf()
    {
        var inputs = new AuthenticationExtensionInputs(Prf: new PrfInput { First = new byte[] { 0xAA } });
        using var session = AuthenticationExtensionSession.Create(inputs, allowCredentials: null, PrfContext());
        await session.PrepareAsync(TestContext.Current.CancellationToken);

        var extensions = session.PrepareInputs(selected: null, token: null);

        Assert.NotNull(extensions);
        var reader = new CborReader(extensions.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.Equal(CborReaderState.StartMap, reader.PeekState());
    }

    /// <summary>L3 §10.1.4: enabled is the hmac-secret boolean output; false stays false.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_PrFRegistrationFalseOutputIsNotEnabled()
    {
        var output = PrfCodec.ParseRegistrationOutput(
            new Dictionary<string, ReadOnlyMemory<byte>> { ["hmac-secret"] = new byte[] { 0xF4 } });
        Assert.NotNull(output);
        Assert.False(output.Enabled);
    }

    /// <summary>L3 §10.1.4: evalByCredential keyed on the selected credential is what gets sent.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_PrFOnlyCredentialMapIsSelectedForMatchingAllowCredential()
    {
        byte[] id = [0x01];
        var input = new PrfInput
        {
            EvalByCredential = new Dictionary<string, PrfInputValues>
            {
                ["AQ"] = new() { First = new byte[] { 0xBB } }
            }
        };
        var selected = new PublicKeyCredentialDescriptor(id);
        using var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(Prf: input), [selected], PrfContext());
        await session.PrepareAsync(TestContext.Current.CancellationToken);

        var extensions = session.PrepareInputs(selected, token: null);

        Assert.NotNull(extensions);
        var reader = new CborReader(extensions.Value, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.Equal(CborReaderState.StartMap, reader.PeekState());
    }

    /// <summary>
    ///     The adapter's InvalidState must reach the caller: ExtensionPipeline only swallows CborContentException, so a
    ///     registration whose previewSign output lacks the inner attestation fails instead of returning the WebAuthn
    ///     credential's key as the signing key.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_RegistrationFailsWhenPreviewSignInnerAttestationIsMissing()
    {
        var extensions = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensions.WriteStartMap(1);
        extensions.WriteTextString("previewSign");
        extensions.WriteStartMap(1);
        extensions.WriteInt32(3);
        extensions.WriteInt32(CoseAlgorithm.Es256.Value);
        extensions.WriteEndMap();
        extensions.WriteEndMap();
        var authData = MockFido2Responses.BuildAuthDataWithAttestedCredential(Guid.NewGuid()).ToList();
        authData[32] |= 0x80; // ED: extensions follow the attested credential data
        authData.AddRange(extensions.Encode());

        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(extensions: MockFido2Responses.AllKnownExtensions));
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MakeCredentialResponse.Decode(MockFido2Responses.BuildMakeCredentialResponseCbor([.. authData], "none")));
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(backend, Assert.IsType<WebAuthnOrigin>(origin),
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain is "com" });

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(new RegistrationOptions
        {
            Challenge = new byte[32],
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(new byte[] { 1 }, "user", "User"),
            PubKeyCredParams = [CoseAlgorithm.Es256],
            UserVerification = UserVerificationPreference.Discouraged,
            Extensions = new RegistrationExtensionInputs(PreviewSign: new WebAuthn.Extensions.PreviewSign.PreviewSignRegistrationInput([CoseAlgorithm.Es256]))
        }, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_PreviewSignMissingInnerAttestationDoesNotSubstituteCredentialKey()
    {
        var publicKey = new CborWriter(CborConformanceMode.Ctap2Canonical);
        publicKey.WriteStartMap(5);
        publicKey.WriteInt32(1);
        publicKey.WriteInt32(2);
        publicKey.WriteInt32(3);
        publicKey.WriteInt32(CoseAlgorithm.Es256.Value);
        publicKey.WriteInt32(-1);
        publicKey.WriteInt32(1);
        publicKey.WriteInt32(-2);
        publicKey.WriteByteString(new byte[32]);
        publicKey.WriteInt32(-3);
        publicKey.WriteByteString(new byte[32]);
        publicKey.WriteEndMap();

        var extensions = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensions.WriteStartMap(1);
        extensions.WriteTextString("previewSign");
        extensions.WriteStartMap(1);
        extensions.WriteInt32(3);
        extensions.WriteInt32(CoseAlgorithm.Es256.Value);
        extensions.WriteEndMap();
        extensions.WriteEndMap();

        var authData = new List<byte>();
        authData.AddRange(SHA256.HashData("example.com"u8));
        authData.Add(0xC1); // UP + attested credential + extensions
        authData.AddRange(new byte[4 + 16]);
        authData.AddRange(new byte[] { 0, 1, 0xAA });
        authData.AddRange(publicKey.Encode());
        authData.AddRange(extensions.Encode());

        var parsed = WebAuthnAuthenticatorData.Decode(authData.ToArray());
        Assert.Throws<WebAuthnClientError>(() => PreviewSignCodec.ParseRegistrationOutput(parsed, null));
    }

    /// <summary>
    ///     End to end through the client: GetCredBlob = true sends <c>"credBlob": true</c> (CTAP §12.2.1) and the stored
    ///     blob is reported; without the request nothing is sent and a stray credBlob output is not reported.
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1619")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task YESDK1619_ClientSendsCredBlobTrueOnlyWhenRequested_AndParsesOnlyThen(bool getCredBlob)
    {
        byte[] storedBlob = [0xC0, 0xFF, 0xEE];
        var outputExtensions = new CborWriter(CborConformanceMode.Ctap2Canonical);
        outputExtensions.WriteStartMap(1);
        outputExtensions.WriteTextString("credBlob");
        outputExtensions.WriteByteString(storedBlob);
        outputExtensions.WriteEndMap();

        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(extensions: MockFido2Responses.AllKnownExtensions));
        BackendGetAssertionRequest? sent = null;
        backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockGetAssertionResponse(authDataExtensionsCbor: outputExtensions.Encode()));
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(backend, Assert.IsType<WebAuthnOrigin>(origin),
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain is "com" });

        var matches = await client.GetAssertionAsync(new AuthenticationOptions
        {
            Challenge = new byte[32],
            RpId = "example.com",
            UserVerification = UserVerificationPreference.Discouraged,
            Extensions = new AuthenticationExtensionInputs(GetCredBlob: getCredBlob)
        }, cancellationToken: TestContext.Current.CancellationToken);
        var response = await Assert.Single(matches).SelectAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(sent);
        if (getCredBlob)
        {
            var reader = new CborReader(sent.Extensions!.Value, CborConformanceMode.Ctap2Canonical);
            Assert.Equal(1, reader.ReadStartMap());
            Assert.Equal("credBlob", reader.ReadTextString());
            Assert.True(reader.ReadBoolean());
            Assert.Equal(storedBlob, response.ClientExtensionResults?.CredBlob?.Blob.ToArray());
        }
        else
        {
            Assert.Null(sent.Extensions);
            Assert.Null(response.ClientExtensionResults?.CredBlob);
        }
    }

    [Fact]
    [Trait("Audit", "YESDK-1619")]
    public void YESDK1619_WebAuthnOffersGetCredBlobClientInput()
    {
        // Public contract: WebAuthn must offer the getCredBlob client extension input,
        // not merely parse a credBlob response that it never requested.
        Assert.NotNull(typeof(AuthenticationExtensionInputs).GetProperty("GetCredBlob"));
    }
}