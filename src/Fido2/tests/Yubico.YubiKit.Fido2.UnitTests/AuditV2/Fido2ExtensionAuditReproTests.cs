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
        var extensions = new ExtensionBuilder().WithCredBlob(ReadOnlyMemory<byte>.Empty).Build();
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
        var extensions = new ExtensionBuilder().WithPrf().Build();
        Assert.NotNull(extensions);
        var reader = new CborReader(extensions.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
        reader.ReadEndMap();
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_CredentialSpecificPrfOverridesGlobalInput()
    {
        byte[] credentialId = [0x01, 0x02];
        string encodedId = Convert.ToBase64String(credentialId).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var input = new PrfInput
        {
            First = new byte[] { 0xAA },
            EvalByCredential = new Dictionary<string, PrfInputValues>
            {
                [encodedId] = new() { First = new byte[] { 0xBB } }
            }
        };

        // No credential selection is exposed at this boundary. A sole mapped
        // credential must at least affect the request rather than being silently dropped.
        var extensions = new ExtensionBuilder().WithPrf(input).Build();
        var globalOnly = new ExtensionBuilder().WithPrf(new PrfInput { First = input.First }).Build();
        Assert.NotNull(extensions);
        Assert.NotNull(globalOnly);
        Assert.NotEqual(globalOnly.Value.ToArray(), extensions.Value.ToArray());
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_OnlyCredentialSpecificPrfEncodesWithoutFailure()
    {
        var input = new PrfInput
        {
            EvalByCredential = new Dictionary<string, PrfInputValues>
            {
                ["AQ"] = new() { First = new byte[] { 0xBB } }
            }
        };
        var extensions = new ExtensionBuilder().WithPrf(input).Build();
        Assert.NotNull(extensions);
        var reader = new CborReader(extensions.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.Equal(CborReaderState.StartMap, reader.PeekState());
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
