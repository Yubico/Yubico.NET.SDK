using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.AuditV2;

public class Fido2ExtensionAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1619")]
    public void YESDK1619_GetAssertionCredBlobUsesBooleanTrue()
    {
        var extensions = new ExtensionBuilder().WithGetCredBlob().Build();
        var request = FidoSessionRequestEncoding.BuildGetAssertionRequest(
            "example.com", new byte[32], new GetAssertionOptions { Extensions = extensions });

        var reader = new CborReader(request.AsMemory(1), CborConformanceMode.Ctap2Canonical);
        Assert.Equal(3, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        reader.SkipValue();
        Assert.Equal(2, reader.ReadInt32());
        reader.SkipValue();
        Assert.Equal(4, reader.ReadInt32());
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("credBlob", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
        reader.ReadEndMap();
        reader.ReadEndMap();
    }

    [Fact]
    [Trait("Audit", "YESDK-1609")]
    public void YESDK1609_RegistrationPrfRequestsHmacSecret()
    {
        // The CTAP layer has no "prf": WebAuthn's PRF processor asks for hmac-secret at registration.
        var extensions = new ExtensionBuilder().WithHmacSecret().Build();
        Assert.NotNull(extensions);
        var reader = new CborReader(extensions.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
        reader.ReadEndMap();
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_MultiplePreviewSignCredentialsCannotSilentlySelectFirst()
    {
        var input = new PreviewSignAuthenticationInput(
            new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>
            {
                [new byte[] { 0x01 }] = new(new byte[] { 0xA1 }, new byte[] { 0xB1 }),
                [new byte[] { 0x02 }] = new(new byte[] { 0xA2 }, new byte[] { 0xB2 })
            });

        // Without an allow-list/selected credential the low-level builder cannot pick safely.
        Assert.Throws<NotSupportedException>(() => new ExtensionBuilder().WithPreviewSign(input).Build());
    }
}