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
using Xunit;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions;

public class ExtensionPipelineTests
{
    private static RegistrationOptions CreateMockOptions() =>
        new()
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user", DisplayName = "User" },
            PubKeyCredParams = [CoseAlgorithm.Es256]
        };

    [Fact]
    public void BuildRegistrationExtensionsCbor_NoExtensions_ReturnsNull()
    {
        // Arrange - No extensions
        RegistrationExtensionInputs? inputs = null;
        var options = CreateMockOptions();

        // Act
        var result = ExtensionPipeline.BuildRegistrationExtensionsCbor(inputs, options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void BuildRegistrationExtensionsCbor_WithCredProtect_ProducesCborWithCredProtectKey()
    {
        // Arrange
        var inputs = new RegistrationExtensionInputs(
            CredProtect: new CredProtectInput(CredProtectPolicy.UserVerificationRequired));
        var options = CreateMockOptions();

        // Act
        var cborBytes = ExtensionPipeline.BuildRegistrationExtensionsCbor(inputs, options);

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
    public void BuildRegistrationExtensionsCbor_WithCredBlob_ProducesCborWithBlob()
    {
        // Arrange
        var blobData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var inputs = new RegistrationExtensionInputs(
            CredBlob: new WebAuthn.Extensions.Inputs.CredBlobInput(blobData));
        var options = CreateMockOptions();

        // Act
        var cborBytes = ExtensionPipeline.BuildRegistrationExtensionsCbor(inputs, options);

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
    public void ParseRegistrationOutputs_NoInputs_ReturnsNull()
    {
        // Arrange
        var authData = CreateMockAuthenticatorData();
        var options = new RegistrationOptions
        {
            Challenge = new byte[32],
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = new byte[16], Name = "user", DisplayName = "User" },
            PubKeyCredParams = [new CoseAlgorithm(-7)]
        };

        // Act
        var result = ExtensionPipeline.ParseRegistrationOutputs(null, authData, options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseRegistrationOutputs_CredPropsRequested_DerivesFromResidentKeyOption()
    {
        // Arrange
        var inputs = new RegistrationExtensionInputs(CredProps: new CredPropsInput());
        var authData = CreateMockAuthenticatorData();
        var options = new RegistrationOptions
        {
            Challenge = new byte[32],
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = new byte[16], Name = "user", DisplayName = "User" },
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            ResidentKey = ResidentKeyPreference.Required
        };

        // Act
        var result = ExtensionPipeline.ParseRegistrationOutputs(inputs, authData, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CredProps);
        Assert.True(result.CredProps.ResidentKey); // Required → true
    }

    [Fact(Timeout = 5000)]
    public void BuildRegistrationExtensionsCbor_PreviewSignWithMultipleStandardExtensions_ProducesCtap2CanonicalOrder()
    {
        // Arrange - combine previewSign with standard extensions that have different lengths
        var inputs = new RegistrationExtensionInputs(
            CredProtect: new CredProtectInput(CredProtectPolicy.UserVerificationRequired), // length 11
            CredBlob: new WebAuthn.Extensions.Inputs.CredBlobInput([0x01, 0x02]), // length 8
            PreviewSign: new PreviewSignRegistrationInput(
                Algorithms: [CoseAlgorithm.Es256SplitArkgPlaceholder],
                Flags: PreviewSignFlags.RequireUserVerification)); // length 11
        var options = CreateMockOptions();
        options.UserVerification = UserVerificationPreference.Required;

        // Act
        var cborBytes = ExtensionPipeline.BuildRegistrationExtensionsCbor(inputs, options);

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
}
