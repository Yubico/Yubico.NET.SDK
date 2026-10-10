using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Adapters;

namespace Yubico.YubiKit.WebAuthn.UnitTests.AuditV2;

public class WebAuthnExtensionAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1609")]
    public void YESDK1609_AuthenticationPrfUsesEncryptedHmacSecretNotLiteralPrf()
    {
        var inputs = new AuthenticationExtensionInputs(Prf: new PrfInput { First = new byte[] { 0xAA } });
        var extensions = ExtensionPipeline.BuildAuthenticationExtensionsCbor(inputs, null);
        Assert.NotNull(extensions);
        var reader = new CborReader(extensions.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.Equal(CborReaderState.StartMap, reader.PeekState());
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_PrFRegistrationFalseOutputIsNotEnabled()
    {
        var output = PrfAdapter.ParseRegistrationOutput(
            new Dictionary<string, ReadOnlyMemory<byte>> { ["prf"] = new byte[] { 0xF4 } });
        Assert.NotNull(output);
        Assert.False(output.Enabled);
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_PrFOnlyCredentialMapIsSelectedForMatchingAllowCredential()
    {
        byte[] id = [0x01];
        var input = new PrfInput
        {
            EvalByCredential = new Dictionary<string, PrfInputValues>
            {
                ["AQ"] = new() { First = new byte[] { 0xBB } }
            }
        };
        var extensions = ExtensionPipeline.BuildAuthenticationExtensionsCbor(
            new AuthenticationExtensionInputs(Prf: input), [new PublicKeyCredentialDescriptor(id)]);
        Assert.NotNull(extensions);
        var reader = new CborReader(extensions.Value, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.Equal(CborReaderState.StartMap, reader.PeekState());
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
        Assert.Throws<WebAuthnClientError>(() => PreviewSignAdapter.ParseRegistrationOutput(parsed, null));
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
