// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
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
using Yubico.YubiKit.WebAuthn.Extensions.Codec;
using Yubico.YubiKit.WebAuthn.Preferences;
using WebAuthnPreviewSign = Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.PreviewSign;

/// <summary>
/// Tests for PreviewSignCodec - validates extension input/output wire-up per Phase 8 spec.
/// </summary>
public class PreviewSignCodecTests
{
    [Fact(Timeout = 5000)]
    public void PreviewSign_Registration_DerivesFlagsFromUserVerificationPreference()
    {
        // Arrange - flags are derived from UserVerification, not user-controllable
        var input = new WebAuthnPreviewSign.PreviewSignRegistrationInput(
            algorithms: [CoseAlgorithm.Es256, CoseAlgorithm.EdDsa]);

        var optionsUvRequired = new PublicKeyCredentialCreationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [CoseAlgorithm.Es256],
            UserVerification = UserVerificationPreference.Required
        };

        var optionsUvNotRequired = optionsUvRequired with
        {
            UserVerification = UserVerificationPreference.Preferred
        };

        // Act - apply to builder and build
        var builderUvRequired = new ExtensionBuilder();
        PreviewSignCodec.ApplyToBuilderForRegistration(builderUvRequired, input, optionsUvRequired.UserVerification);
        var cborUvRequired = builderUvRequired.Build();

        var builderUvNotRequired = new ExtensionBuilder();
        PreviewSignCodec.ApplyToBuilderForRegistration(builderUvNotRequired, input, optionsUvNotRequired.UserVerification);
        var cborUvNotRequired = builderUvNotRequired.Build();

        // Assert - UV=Required → flags=0b101, otherwise → flags=0b001
        Assert.NotNull(cborUvRequired);
        Assert.NotNull(cborUvNotRequired);

        // Read UV=Required case - extensions map contains previewSign
        var reader = new CborReader(cborUvRequired.Value, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap(); // extensions map
        Assert.Equal("previewSign", reader.ReadTextString());
        reader.ReadStartMap(); // previewSign value
        reader.ReadInt32(); // key 3
        reader.SkipValue(); // algorithms array
        Assert.Equal(4, reader.ReadInt32()); // key 4
        Assert.Equal(5, reader.ReadInt32()); // 0b101 = RequireUserVerification
        reader.ReadEndMap();

        // Read UV!=Required case
        reader = new CborReader(cborUvNotRequired.Value, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap(); // extensions map
        Assert.Equal("previewSign", reader.ReadTextString());
        reader.ReadStartMap(); // previewSign value
        reader.ReadInt32(); // key 3
        reader.SkipValue(); // algorithms array
        Assert.Equal(4, reader.ReadInt32()); // key 4
        Assert.Equal(1, reader.ReadInt32()); // 0b001 = RequireUserPresence
        reader.ReadEndMap();
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Registration_OutputPopulatedFromUnsignedAttObj()
    {
        // Arrange - mock authenticator data with previewSign extension containing algorithm + flags in authData
        // and attestation object in unsignedExtensionOutputs (per Fix #6b)
        var innerCredentialId = RandomNumberGenerator.GetBytes(32);
        var publicKeyBytes = BuildCoseEc2PublicKey(CoseAlgorithm.Es256);
        var attestationObject = BuildAttestationObject(publicKeyBytes, WebAuthnPreviewSign.PreviewSignFlags.RequireUserVerification, innerCredentialId);

        // Build authData.extensions["previewSign"] = {3: alg, 4: flags}
        var authDataExtension = new CborWriter(CborConformanceMode.Ctap2Canonical);
        authDataExtension.WriteStartMap(2);
        authDataExtension.WriteInt32(3);  // alg key
        authDataExtension.WriteInt32(CoseAlgorithm.Es256.Value);
        authDataExtension.WriteInt32(4);  // flags key
        authDataExtension.WriteInt32((byte)WebAuthnPreviewSign.PreviewSignFlags.RequireUserVerification);
        authDataExtension.WriteEndMap();

        // Build unsignedExtensionOutputs["previewSign"] = {7: att-obj}
        var unsignedExtension = new CborWriter(CborConformanceMode.Ctap2Canonical);
        unsignedExtension.WriteStartMap(1);
        unsignedExtension.WriteInt32(7);  // att-obj key
        unsignedExtension.WriteByteString(attestationObject);
        unsignedExtension.WriteEndMap();

        var authData = BuildAuthDataWithExtensions(new Dictionary<string, byte[]>
        {
            ["previewSign"] = authDataExtension.Encode()
        });

        var parsedAuthData = WebAuthnAuthenticatorData.Decode(authData);

        var unsignedExtensionOutputs = new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["previewSign"] = unsignedExtension.Encode()
        };

        // Act - parse registration output
        var output = PreviewSignCodec.ParseRegistrationOutput(parsedAuthData, unsignedExtensionOutputs);

        // Assert - GeneratedKey comes from the inner attestation object (previewSign draft v4 §10.2.1)
        Assert.NotNull(output);
        Assert.NotNull(output.GeneratedKey);
        Assert.NotNull(output.GeneratedKey.PublicKey);
        Assert.Equal(CoseAlgorithm.Es256, output.GeneratedKey.Algorithm);
        Assert.Equal(innerCredentialId, output.GeneratedKey.KeyHandle.ToArray());
        Assert.NotNull(output.GeneratedKey.AttestationObject);
        Assert.Equal(innerCredentialId, output.GeneratedKey.AttestationObject.AuthenticatorData.AttestedCredentialData!.CredentialId.ToArray());
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Authentication_OutputSignaturePopulated()
    {
        // Arrange - mock authenticator data with previewSign extension containing signature
        var signatureBytes = RandomNumberGenerator.GetBytes(64);  // Mock algorithm-specific signature bytes

        // Build previewSign extension output (authentication form: key 6 = signature)
        var extensionWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensionWriter.WriteStartMap(1);
        extensionWriter.WriteInt32(6);  // sig key
        extensionWriter.WriteByteString(signatureBytes);
        extensionWriter.WriteEndMap();

        var authData = BuildAuthDataWithExtensions(new Dictionary<string, byte[]>
        {
            ["previewSign"] = extensionWriter.Encode()
        });

        var parsedAuthData = WebAuthnAuthenticatorData.Decode(authData);

        // Act - parse authentication output
        var output = PreviewSignCodec.ParseAuthenticationOutput(parsedAuthData);

        // Assert - Signature should match fixture bytes
        Assert.NotNull(output);
        Assert.Equal(signatureBytes, output.Signature.ToArray());
    }

    // Helper methods for building mock CBOR structures

    private static byte[] BuildCoseEc2PublicKey(CoseAlgorithm algorithm)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);

        // kty = 2 (EC2)
        writer.WriteInt32(1);
        writer.WriteInt32(2);

        // alg
        writer.WriteInt32(3);
        writer.WriteInt32(algorithm.Value);

        // crv = 1 (P-256)
        writer.WriteInt32(-1);
        writer.WriteInt32(1);

        // x coordinate (32 bytes)
        writer.WriteInt32(-2);
        writer.WriteByteString(new byte[32]);

        // y coordinate (32 bytes)
        writer.WriteInt32(-3);
        writer.WriteByteString(new byte[32]);

        writer.WriteEndMap();
        return writer.Encode();
    }

    private static byte[] BuildAttestationObject(byte[] publicKey, WebAuthnPreviewSign.PreviewSignFlags flags) =>
        BuildAttestationObject(publicKey, flags, RandomNumberGenerator.GetBytes(32));

    private static byte[] BuildAttestationObject(byte[] publicKey, WebAuthnPreviewSign.PreviewSignFlags flags, byte[] credId)
    {
        // Build a minimal inner attestation object with the previewSign extension in authData, in the
        // on-the-wire shape: the previewSign unsigned-extension-output wraps an attestation object whose
        // keys are CTAP-style integers ({1: fmt, 2: authData, 3: attStmt}), not WebAuthn-style text
        // strings ({"fmt", "authData", "attStmt"}). A text-keyed fixture would not exercise the decoder.
        var rpIdHash = SHA256.HashData("example.com"u8);

        // Build authData with AT flag + previewSign extension
        var authDataList = new List<byte>();

        // rpIdHash (32)
        authDataList.AddRange(rpIdHash);

        // flags = UP | UV | AT | ED (0x45 + 0x80 for extensions)
        authDataList.Add(0xC5);

        // signCount (4)
        authDataList.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });

        // AAGUID (16)
        authDataList.AddRange(new byte[16]);

        // credIdLen (2, big-endian)
        authDataList.AddRange(new byte[] { 0x00, (byte)credId.Length });

        // credId
        authDataList.AddRange(credId);

        // publicKey (COSE_Key)
        authDataList.AddRange(publicKey);

        // Extensions: previewSign with flags AND algorithm (both required per decoder)
        var extensionWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensionWriter.WriteStartMap(1);
        extensionWriter.WriteTextString("previewSign");

        // Write algorithm (key 3) + flags (key 4)
        extensionWriter.WriteStartMap(2);
        extensionWriter.WriteInt32(3);  // alg
        extensionWriter.WriteInt32(-7);  // ES256
        extensionWriter.WriteInt32(4);   // flags
        extensionWriter.WriteInt32((byte)flags);
        extensionWriter.WriteEndMap();

        extensionWriter.WriteEndMap();
        authDataList.AddRange(extensionWriter.Encode());

        // Build CTAP-shaped inner attestation object: {1: fmt, 2: authData, 3: attStmt}
        // (canonical CBOR sorts unsigned-int keys ascending: 1, 2, 3)
        var attObjWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        attObjWriter.WriteStartMap(3);

        // 1 -> fmt
        attObjWriter.WriteInt32(1);
        attObjWriter.WriteTextString("none");

        // 2 -> authData
        attObjWriter.WriteInt32(2);
        attObjWriter.WriteByteString(authDataList.ToArray());

        // 3 -> attStmt (empty map)
        attObjWriter.WriteInt32(3);
        attObjWriter.WriteStartMap(0);
        attObjWriter.WriteEndMap();

        attObjWriter.WriteEndMap();
        return attObjWriter.Encode();
    }

    private static byte[] BuildAuthDataWithExtensions(Dictionary<string, byte[]> extensions)
    {
        var rpIdHash = SHA256.HashData("example.com"u8);

        var data = new List<byte>();

        // rpIdHash (32)
        data.AddRange(rpIdHash);

        // flags = UP | UV | ED (0x05 + 0x80 for extensions, NO AT flag)
        // AT (0x40) would signal attested credential data, which we don't have
        data.Add(0x85);

        // signCount (4)
        data.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x01 });

        // Extensions CBOR map
        var extensionWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensionWriter.WriteStartMap(extensions.Count);

        foreach (var (key, value) in extensions.OrderBy(kvp => kvp.Key))
        {
            extensionWriter.WriteTextString(key);
            extensionWriter.WriteEncodedValue(value);
        }

        extensionWriter.WriteEndMap();
        data.AddRange(extensionWriter.Encode());

        return data.ToArray();
    }
}