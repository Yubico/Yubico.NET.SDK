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

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions;

public class ExtensionSessionTests
{
    private static RegistrationOptions CreateMockOptions() =>
        new()
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user", "User"),
            PubKeyCredParams = [CoseAlgorithm.Es256]
        };

    [Fact]
    public void PrepareInputs_NoExtensions_ReturnsNull()
    {
        // Arrange - No extensions
        RegistrationExtensionInputs? inputs = null;
        var options = CreateMockOptions();

        // Act
        var result = RegistrationExtensionSession.Create(inputs, options, requireResidentKey: false, TestExtensionContext.SupportingAll()).PrepareInputs(token: null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PrepareInputs_WithCredProtect_ProducesCborWithCredProtectKey()
    {
        // Arrange
        var inputs = new RegistrationExtensionInputs(
            CredProtect: new CredProtectInput(CredProtectPolicy.UserVerificationRequired));
        var options = CreateMockOptions();

        // Act
        var cborBytes = RegistrationExtensionSession.Create(inputs, options, requireResidentKey: false, TestExtensionContext.SupportingAll()).PrepareInputs(token: null);

        // Assert
        Assert.NotNull(cborBytes);

        // Decode and verify the CBOR contains the credProtect extension
        var reader = new CborReader(cborBytes.Value, CborConformanceMode.Lax);
        var mapLength = reader.ReadStartMap();

        Assert.True(mapLength > 0);

        var extensionKey = reader.ReadTextString();
        Assert.Equal("credProtect", extensionKey);

        var policyValue = reader.ReadInt32();
        Assert.Equal(3, policyValue); // UserVerificationRequired = 3
    }

    [Fact]
    public void PrepareInputs_WithCredBlob_ProducesCborWithBlob()
    {
        // Arrange
        var blobData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var inputs = new RegistrationExtensionInputs(
            CredBlob: new Fido2.Extensions.CredBlobInput { Blob = blobData });
        var options = CreateMockOptions();

        // Act
        var cborBytes = RegistrationExtensionSession.Create(inputs, options, requireResidentKey: false, TestExtensionContext.SupportingAll()).PrepareInputs(token: null);

        // Assert
        Assert.NotNull(cborBytes);

        // Decode and verify
        var reader = new CborReader(cborBytes.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();

        var key = reader.ReadTextString();
        Assert.Equal("credBlob", key);

        var value = reader.ReadByteString();
        Assert.Equal(blobData, value);
    }

    [Fact]
    public async Task PrepareOutputs_NoInputs_ReturnsNull()
    {
        // Arrange
        var authData = CreateMockAuthenticatorData();
        var options = new RegistrationOptions
        {
            Challenge = new byte[32],
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(new byte[16], "user", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        // Act
        var result = await RegistrationExtensionSession.Create(null, options, requireResidentKey: options.ResidentKey == ResidentKeyPreference.Required, TestExtensionContext.SupportingAll())
            .PrepareOutputsAsync(MockFido2Responses.CreateMockMakeCredentialResponse(), authData, token: null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PrepareOutputs_CredPropsRequested_ReportsEffectiveRequireResidentKey()
    {
        // Arrange
        var inputs = new RegistrationExtensionInputs(CredProps: new CredPropsInput());
        var authData = CreateMockAuthenticatorData();
        var options = new RegistrationOptions
        {
            Challenge = new byte[32],
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(new byte[16], "user", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            ResidentKey = ResidentKeyPreference.Required
        };

        // Act
        var result = await RegistrationExtensionSession.Create(inputs, options, requireResidentKey: options.ResidentKey == ResidentKeyPreference.Required, TestExtensionContext.SupportingAll())
            .PrepareOutputsAsync(MockFido2Responses.CreateMockMakeCredentialResponse(), authData, token: null, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CredProps);
        Assert.True(result.CredProps.ResidentKey); // Required → true
    }

    [Fact(Timeout = 5000)]
    public void PrepareInputs_PreviewSignWithMultipleStandardExtensions_ProducesCtap2CanonicalOrder()
    {
        // Arrange - combine previewSign with standard extensions that have different lengths
        var inputs = new RegistrationExtensionInputs(
            CredProtect: new CredProtectInput(CredProtectPolicy.UserVerificationRequired), // length 11
            CredBlob: new Fido2.Extensions.CredBlobInput { Blob = new byte[] { 0x01, 0x02 } }, // length 8
            PreviewSign: new WebAuthn.Extensions.PreviewSign.PreviewSignRegistrationInput(
                algorithms: new[] { CoseAlgorithm.Es256 })); // length 11
        var options = CreateMockOptions();


        // Act
        var cborBytes = RegistrationExtensionSession.Create(inputs, options, requireResidentKey: false, TestExtensionContext.SupportingAll()).PrepareInputs(token: null);

        // Assert - Verify CBOR doesn't throw when encoding (canonical mode validates order)
        Assert.NotNull(cborBytes);

        // Decode and verify CTAP2 canonical order: length-ascending, then lex within same-length
        var reader = new CborReader(cborBytes.Value, CborConformanceMode.Ctap2Canonical);
        int? mapLength = reader.ReadStartMap();
        Assert.NotNull(mapLength);
        Assert.Equal(3, mapLength.Value); // credBlob + credProtect + previewSign

        // Expected order: credBlob(8), credProtect(11), previewSign(11)
        var key1 = reader.ReadTextString();
        Assert.Equal("credBlob", key1); // length 8 comes first

        reader.SkipValue(); // skip credBlob value

        var key2 = reader.ReadTextString();
        Assert.Equal("credProtect", key2); // length 11, lex before previewSign

        reader.SkipValue(); // skip credProtect value

        var key3 = reader.ReadTextString();
        Assert.Equal("previewSign", key3); // length 11, lex after credProtect
    }

    private static WebAuthnAuthenticatorData CreateMockAuthenticatorData()
    {
        // Build minimal authenticator data with no extensions
        var data = new List<byte>();

        // rpIdHash (32 bytes)
        data.AddRange(new byte[32]);

        // flags (1 byte)
        data.Add(0x01); // UP bit only

        // signCount (4 bytes)
        data.AddRange(new byte[4]);

        return WebAuthnAuthenticatorData.Decode(data.ToArray());
    }

    /// <summary>WebAuthn L3 §9.4: output the client cannot process is ignored; only that extension is dropped.</summary>
    [Fact]
    public async Task PrepareOutputs_WrongTypedExtensionOutput_DropsOnlyThatExtension()
    {
        var extensions = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensions.WriteStartMap(2);
        extensions.WriteTextString("credBlob");
        extensions.WriteBoolean(true); // valid makeCredential output
        extensions.WriteTextString("credProtect");
        extensions.WriteTextString("junk"); // must be an integer
        extensions.WriteEndMap();
        var authData = WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData(extensions.Encode()));
        var inputs = new RegistrationExtensionInputs(
            CredProtect: new CredProtectInput(CredProtectPolicy.UserVerificationOptional),
            CredBlob: new CredBlobInput { Blob = new byte[] { 0x01 } });

        var result = await RegistrationExtensionSession.Create(inputs, CreateMockOptions(), requireResidentKey: false, TestExtensionContext.SupportingAll())
            .PrepareOutputsAsync(MockFido2Responses.CreateMockMakeCredentialResponse(), authData, token: null, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Null(result.CredProtect);
        Assert.NotNull(result.CredBlob);
        Assert.True(result.CredBlob.Stored);
    }

    /// <summary>Requested but client-only extensions (credProps) produce no CTAP input.</summary>
    [Fact]
    public void PrepareInputs_OnlyClientSideExtensions_ReturnsNull()
    {
        var inputs = new RegistrationExtensionInputs(CredProps: new CredPropsInput());

        var cbor = RegistrationExtensionSession.Create(inputs, CreateMockOptions(), requireResidentKey: true, TestExtensionContext.SupportingAll()).PrepareInputs(token: null);

        Assert.Null(cbor);
    }

    /// <summary>
    ///     WebAuthn L3 §9.4 / python-fido2 is_supported: an extension GetInfo does not advertise produces no CTAP
    ///     input and no output; the ones it does advertise are unaffected.
    /// </summary>
    [Fact]
    public async Task Create_UnsupportedExtension_ProducesNoInputAndNoOutput()
    {
        var onlyCredBlob = TestExtensionContext.For(MockFido2Responses.CreateMockAuthenticatorInfo(extensions: ["credBlob"]));
        var inputs = new RegistrationExtensionInputs(
            CredProtect: new CredProtectInput(CredProtectPolicy.UserVerificationRequired),
            CredBlob: new CredBlobInput { Blob = new byte[] { 0x01 } });
        var session = RegistrationExtensionSession.Create(inputs, CreateMockOptions(), requireResidentKey: false, onlyCredBlob);

        var cbor = session.PrepareInputs(token: null);
        var reader = new CborReader(cbor!.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("credBlob", reader.ReadTextString());

        var outputs = new CborWriter(CborConformanceMode.Ctap2Canonical);
        outputs.WriteStartMap(2);
        outputs.WriteTextString("credBlob"); outputs.WriteBoolean(true);
        outputs.WriteTextString("credProtect"); outputs.WriteInt32(3); // the key answered anyway; the client did not ask
        outputs.WriteEndMap();
        var authData = WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData(outputs.Encode()));
        var result = await session.PrepareOutputsAsync(MockFido2Responses.CreateMockMakeCredentialResponse(), authData, token: null, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.CredBlob);
        Assert.Null(result.CredProtect);
    }

    /// <summary>largeBlob needs both the largeBlobKey extension and the largeBlobs option (python-fido2 LargeBlobExtension.is_supported).</summary>
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    public void Create_LargeBlob_RequiresExtensionAndOption(bool extension, bool option, bool expectInput)
    {
        var context = TestExtensionContext.For(MockFido2Responses.CreateMockAuthenticatorInfo(
            extensions: extension ? ["largeBlobKey"] : [], largeBlobsSupported: option));

        var cbor = RegistrationExtensionSession.Create(new RegistrationExtensionInputs(LargeBlob: new LargeBlobInput()),
            CreateMockOptions(), requireResidentKey: false, context).PrepareInputs(token: null);

        Assert.Equal(expectInput, cbor is not null);
    }

    /// <summary>Permissions are the union over processors; today no processor needs any (R3 wires them).</summary>
    [Fact]
    public void Permissions_NoProcessorNeedsAny_IsNone()
    {
        var session = AuthenticationExtensionSession.Create(new AuthenticationExtensionInputs(GetCredBlob: true), allowCredentials: null, TestExtensionContext.SupportingAll());

        Assert.Equal(Yubico.YubiKit.Fido2.Pin.PinUvAuthTokenPermissions.None, session.Permissions);
    }
}