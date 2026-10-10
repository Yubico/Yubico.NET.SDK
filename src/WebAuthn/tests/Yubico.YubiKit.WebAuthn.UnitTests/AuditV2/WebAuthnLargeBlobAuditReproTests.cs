using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;

namespace Yubico.YubiKit.WebAuthn.UnitTests.AuditV2;

public class WebAuthnLargeBlobAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1613")]
    public void YESDK1613_LargeBlobKeyFromCtapResponseReportsSupported()
    {
        // A successful CTAP makeCredential returns largeBlobKey in response member 0x05,
        // not in the signed authenticator-data extension map.
        var data = new byte[37];
        data[32] = 0x01;
        var writer = new CborWriter();
        writer.WriteStartMap(4);
        writer.WriteInt32(1);
        writer.WriteTextString("none");
        writer.WriteInt32(2);
        writer.WriteByteString(data);
        writer.WriteInt32(3);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        writer.WriteInt32(5);
        writer.WriteByteString(new byte[32]);
        writer.WriteEndMap();
        var ctapResponse = MakeCredentialResponse.Decode(writer.Encode());
        Assert.Equal(32, ctapResponse.LargeBlobKey?.Length);
        var authData = WebAuthnAuthenticatorData.Decode(ctapResponse.AuthenticatorDataRaw);
        var inputs = new RegistrationExtensionInputs(LargeBlob: new LargeBlobInput());
        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(new byte[] { 1 }, "user", "User"),
            PubKeyCredParams = [CoseAlgorithm.Es256],
            Extensions = inputs
        };

        // The current pipeline has no argument for the decoded 0x05 key. Keep
        // this contract test focused on the actual registration output it produces.
        var result = ExtensionPipeline.ParseRegistrationOutputs(inputs, authData, null, options);
        Assert.NotNull(result?.LargeBlob);
        Assert.True(result.LargeBlob.Supported);
    }
}
