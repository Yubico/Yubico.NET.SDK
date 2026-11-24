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

using NSubstitute;
using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Codec;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;
using WebAuthnPreviewSign = Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.PreviewSign;

/// <summary>
/// Tests previewSign draft v4 §10.2.1 input translation and output extraction through PreviewSignCodec.
/// </summary>
[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
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

    [Theory]
    [InlineData(true, true, 0)]
    [InlineData(true, true, 0, false)]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(true, true, 1)] // trailing inner data
    [InlineData(true, true, 2)] // inner attStmt is not a map
    public void RegistrationExtraction_BothRoutesHandleTheSameWireResponse(bool signed, bool unsigned, int malformation, bool arkg = true)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECPoint point = ecdsa.ExportParameters(false).Q;
        byte[] sec1 = [4, .. Assert.IsType<byte[]>(point.X), .. Assert.IsType<byte[]>(point.Y)];
        var seed = new CoseArkgP256SeedKey(CoseAlgorithm.ArkgP256SeedKey, CoseAlgorithm.Esp256, sec1, sec1);
        byte[] seedKey = arkg ? seed.Encode() : [0xA2, 0x01, 0x18, 0x63, 0x03, 0x28]; // unmodeled kty 99
        var algorithm = arkg ? CoseAlgorithm.ArkgP256 : CoseAlgorithm.Esp256;
        var extensionWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensionWriter.WriteStartMap(1);
        extensionWriter.WriteTextString("previewSign");
        extensionWriter.WriteStartMap(1);
        extensionWriter.WriteInt32(3);
        extensionWriter.WriteInt32(algorithm.Value);
        extensionWriter.WriteEndMap();
        extensionWriter.WriteEndMap();
        byte[] outerAuthData = signed
            ? [.. new byte[32], 0xC5, .. new byte[4 + 16], 0, 1, 0x2A, .. seedKey,
                 .. extensionWriter.Encode()]
            : [.. new byte[32], 0x45, .. new byte[4 + 16], 0, 1, 0x2A, .. seedKey];

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(unsigned ? 4 : 3);
        writer.WriteInt32(1);
        writer.WriteTextString("none");
        writer.WriteInt32(2);
        writer.WriteByteString(outerAuthData);
        writer.WriteInt32(3);
        writer.WriteEncodedValue([0xA0]);
        if (unsigned)
        {
            byte[] inner = BuildAttestationObject(seedKey, WebAuthnPreviewSign.PreviewSignFlags.RequireUserVerification, []);
            if (malformation == 1) inner = [.. inner, 0xF5];
            if (malformation == 2) inner[^1] = 0x80; // Replace {} with [].

            writer.WriteInt32(6);
            writer.WriteStartMap(1);
            writer.WriteTextString("previewSign");
            writer.WriteStartMap(1);
            writer.WriteInt32(7);
            writer.WriteByteString(inner);
            writer.WriteEndMap();
            writer.WriteEndMap();
        }
        writer.WriteEndMap();

        var response = MakeCredentialResponse.Decode(writer.Encode());
        var authData = WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw);
        bool extracted = PreviewSignCbor.TryExtractGeneratedKey(response, out var ctapKey);
        WebAuthnPreviewSign.PreviewSignRegistrationOutput? ExtractWebAuthn() =>
            PreviewSignCodec.ParseRegistrationOutput(authData, response.UnsignedExtensionOutputs);

        if (signed && unsigned && malformation == 0)
        {
            Assert.True(extracted);
            Assert.NotNull(ctapKey);
            var webAuthn = ExtractWebAuthn();
            Assert.NotNull(webAuthn);
            Assert.Equal(algorithm, ctapKey.Algorithm);
            Assert.Equal(ctapKey.Algorithm, webAuthn.GeneratedKey.Algorithm);
            Assert.True(ctapKey.KeyHandle.IsEmpty);
            Assert.True(webAuthn.GeneratedKey.KeyHandle.IsEmpty);
            Assert.True(ctapKey.AttestationObject.Span.SequenceEqual(webAuthn.GeneratedKey.AttestationObject.Encode()));
            if (!arkg)
            {
                Assert.IsType<PreviewSignGeneratedKey>(ctapKey);
                Assert.True(seedKey.AsSpan().SequenceEqual(Assert.IsType<CoseOtherKey>(ctapKey.PublicKey).RawCbor.Span));
                Assert.IsType<CoseOtherKey>(webAuthn.GeneratedKey.PublicKey);
                return;
            }

            var arkgKey = Assert.IsType<ArkgP256GeneratedKey>(ctapKey);
            Assert.Same(arkgKey.PublicKey, arkgKey.SeedKey);
            Assert.Equal(CoseAlgorithm.ArkgP256SeedKey, arkgKey.PublicKey.Algorithm);
            Assert.Equal(CoseAlgorithm.Esp256, arkgKey.DerivedKeyAlgorithm);
            Assert.True(sec1.AsSpan().SequenceEqual(arkgKey.BlindingPublicKey.Span));
            Assert.True(sec1.AsSpan().SequenceEqual(arkgKey.KemPublicKey.Span));
            Assert.IsType<CoseArkgP256SeedKey>(webAuthn.GeneratedKey.PublicKey);
            return;
        }

        Assert.False(extracted);
        Assert.Null(ctapKey);
        if (!signed)
            Assert.Null(ExtractWebAuthn());
        else if (!unsigned)
            Assert.Equal(WebAuthnClientErrorCode.InvalidState, Assert.Throws<WebAuthnClientError>(() => ExtractWebAuthn()).Code);
        else
            Assert.Throws<CborContentException>(() => ExtractWebAuthn());
    }

    [Theory(Timeout = 5000)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreviewSign_GeneratedHandleReachesSelectedCredentialsAssertion(bool includeExtraInnerAlgorithm)
    {
        // Arrange - mock authenticator data with previewSign extension containing algorithm + flags in authData
        // and the inner generated-key attestation object in unsignedExtensionOutputs.
        byte[] innerCredentialId = includeExtraInnerAlgorithm ? [0xA1, 0xA2] : [];
        var publicKeyBytes = BuildCoseEc2PublicKey(CoseAlgorithm.Es256);
        var attestationObject = BuildAttestationObject(publicKeyBytes, WebAuthnPreviewSign.PreviewSignFlags.RequireUserVerification,
            innerCredentialId, includeExtraInnerAlgorithm);

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

        var unsignedExtensionOutputs = new Dictionary<string, Memory<byte>>
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
        var inner = new CborReader(output.GeneratedKey.AttestationObject.AuthenticatorData.ParsedExtensions["previewSign"]);
        Assert.Equal(includeExtraInnerAlgorithm ? 2 : 1, inner.ReadStartMap());
        if (includeExtraInnerAlgorithm)
        {
            Assert.Equal(3, inner.ReadInt32());
            Assert.Equal(-7, inner.ReadInt32());
        }
        Assert.Equal(4, inner.ReadInt32());
        Assert.Equal((byte)WebAuthnPreviewSign.PreviewSignFlags.RequireUserVerification, inner.ReadInt32());
        inner.ReadEndMap();

        var absent = new PublicKeyCredentialDescriptor(new byte[] { 0x10 });
        var selected = new PublicKeyCredentialDescriptor(new byte[] { 0x20 });
        byte[] tbs = [0xB1, 0xB2];
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(extensions: ["previewSign"]));
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            if (request.Options?.GetValueOrDefault("up") == false)
            {
                if (request.AllowList?.Any(d => d.Id.Span.SequenceEqual(selected.Id.Span)) != true)
                    throw new Yubico.YubiKit.Ctap2.CtapException(Yubico.YubiKit.Ctap2.CtapStatus.NoCredentials);
            }
            else
            {
                Assert.Equal(selected.Id.ToArray(), Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<PublicKeyCredentialDescriptor>>(request.AllowList)).Id.ToArray());
                var encoded = Assert.IsType<ReadOnlyMemory<byte>>(request.Extensions);
                // {"previewSign": {2: h'...' (40 if empty), 6: h'B1B2'}}; key 7 is absent.
                string handleEncoding = includeExtraInnerAlgorithm ? "42A1A2" : "40";
                Assert.Equal(Convert.FromHexString($"A16B707265766965775369676EA202{handleEncoding}0642B1B2"), encoded.ToArray());
            }
            return MockCtap2Responses.CreateMockGetAssertionResponse(selected.Id.ToArray());
        });

        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(backend, Assert.IsType<WebAuthnOrigin>(origin),
            new WebAuthnClientOptions { RpIdValidator = (_, rpId) => rpId == "example.com" });
        var options = new PublicKeyCredentialRequestOptions
        {
            Challenge = new byte[32],
            RpId = "example.com",
            AllowCredentials = [absent, selected],
            UserVerification = UserVerificationPreference.Discouraged,
            Extensions = new AuthenticationExtensionInputs(PreviewSign: new WebAuthnPreviewSign.PreviewSignAuthenticationInput(
                new Dictionary<ReadOnlyMemory<byte>, WebAuthnPreviewSign.PreviewSignSigningParams>
                {
                    [absent.Id] = new(new byte[] { 0xF1 }, new byte[] { 0xF2 }),
                    [selected.Id] = new(output.GeneratedKey.KeyHandle, tbs)
                }))
        };
        var ct = TestContext.Current.CancellationToken;
        var match = Assert.Single(await client.GetAssertionAsync(options, cancellationToken: ct));
        Assert.Equal(selected.Id.ToArray(), (await match.SelectAsync(ct)).CredentialId.ToArray());
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

    [Theory]
    [InlineData("published")]
    [InlineData("omitted")]
    [InlineData("error")]
    public async Task Registration_ClearsOwnedUnsignedValuesWithoutClearingPublishedAuthenticatorData(string outcome)
    {
        byte[] handle = [0xD1, 0xD2];
        var signed = Convert.FromHexString("A16B707265766965775369676EA10326"); // {previewSign: {3: -7}}
        byte[] authData = [.. MockCtap2Responses.BuildAuthDataWithAttestedCredential(Guid.Empty), .. signed];
        authData[32] |= 0x80;
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteTextString(outcome == "error" ? "unrecognized" : "previewSign");
        writer.WriteStartMap(1);
        writer.WriteInt32(outcome == "omitted" ? 8 : 7);
        writer.WriteByteString(BuildAttestationObject(BuildCoseEc2PublicKey(CoseAlgorithm.Es256),
            WebAuthnPreviewSign.PreviewSignFlags.RequireUserPresence, handle));
        writer.WriteEndMap();
        writer.WriteEndMap();

        var ctap = MakeCredentialResponse.Decode(MockCtap2Responses.BuildMakeCredentialResponseCbor(authData,
            "none", unsignedExtensions: writer.Encode()));
        var owned = Assert.Single(Assert.IsAssignableFrom<IReadOnlyDictionary<string, Memory<byte>>>(ctap.UnsignedExtensionOutputs)).Value;
        var parsed = WebAuthnAuthenticatorData.FromOwned(ctap.AuthenticatorData);
        var parsedExtension = parsed.ParsedExtensions["previewSign"];
        Assert.True(owned.Span.ContainsAnyExcept((byte)0));
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(
            MockCtap2Responses.CreateMockAuthenticatorInfo(extensions: ["previewSign"]));
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>()).Returns(ctap);
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(backend, origin, new() { RpIdValidator = (_, rpId) => rpId == "example.com" });
        var options = ExtensionSessionTests.CreateMockOptions() with
        {
            UserVerification = UserVerificationPreference.Discouraged,
            Extensions = new RegistrationExtensionInputs(PreviewSign: new([CoseAlgorithm.Es256]))
        };

        var ct = TestContext.Current.CancellationToken;
        if (outcome == "error")
        {
            var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(options, cancellationToken: ct));
            Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
            Assert.False(parsed.Raw.Span.ContainsAnyExcept((byte)0));
            Assert.False(parsedExtension.Span.ContainsAnyExcept((byte)0));
        }
        else
        {
            var result = await client.MakeCredentialAsync(options, cancellationToken: ct);
            Assert.True(parsed.Raw.Equals(result.AuthenticatorData.Raw));
            Assert.True(parsedExtension.Equals(result.AuthenticatorData.ParsedExtensions["previewSign"]));
            Assert.Equal(authData, result.RawAuthenticatorData.ToArray());
            if (outcome == "omitted")
                Assert.Null(result.ClientExtensionResults?.PreviewSign);
            else
            {
                var generated = Assert.IsType<WebAuthnPreviewSign.PreviewSignRegistrationOutput>(result.ClientExtensionResults?.PreviewSign).GeneratedKey;
                Assert.Equal(handle, generated.KeyHandle.ToArray());
                Assert.Equal(handle, WebAuthn.Attestation.WebAuthnAttestationObject.Decode(generated.AttestationObject.Encode())
                    .AuthenticatorData.AttestedCredentialData?.CredentialId.ToArray());
            }
        }
        Assert.False(owned.Span.ContainsAnyExcept((byte)0));
    }

    private static byte[] BuildAttestationObject(byte[] publicKey, WebAuthnPreviewSign.PreviewSignFlags flags, byte[] credId,
        bool includeExtraInnerAlgorithm = true)
    {
        // Build an inner attestation fixture with the previewSign extension in authData, in the
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

        // Draft v4 puts policy flags here and the selected algorithm in the outer registration output.
        var extensionWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensionWriter.WriteStartMap(1);
        extensionWriter.WriteTextString("previewSign");

        extensionWriter.WriteStartMap(includeExtraInnerAlgorithm ? 2 : 1);
        if (includeExtraInnerAlgorithm)
        {
            extensionWriter.WriteInt32(3);
            extensionWriter.WriteInt32(-7);
        }
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