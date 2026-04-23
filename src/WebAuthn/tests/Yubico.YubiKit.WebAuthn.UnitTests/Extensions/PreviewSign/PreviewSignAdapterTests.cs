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
using Xunit;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Extensions.Adapters;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.PreviewSign;

/// <summary>
/// Tests for PreviewSignAdapter - validates extension input/output wire-up per Phase 8 spec.
/// </summary>
public class PreviewSignAdapterTests
{
    [Fact(Timeout = 5000)]
    public void PreviewSign_Registration_DerivesFlagsFromUserVerificationPreference()
    {
        // Arrange - flags are derived from UserVerification, not user-controllable
        var input = new PreviewSignRegistrationInput(
            algorithms: [CoseAlgorithm.Es256, CoseAlgorithm.EdDsa]);

        var optionsUvRequired = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [CoseAlgorithm.Es256],
            UserVerification = UserVerificationPreference.Required
        };

        var optionsUvNotRequired = optionsUvRequired with
        {
            UserVerification = UserVerificationPreference.Preferred
        };

        // Act
        var cborUvRequired = PreviewSignAdapter.BuildRegistrationCbor(input, optionsUvRequired);
        var cborUvNotRequired = PreviewSignAdapter.BuildRegistrationCbor(input, optionsUvNotRequired);

        // Assert - UV=Required → flags=0b101, otherwise → flags=0b001
        Assert.NotNull(cborUvRequired);
        Assert.NotNull(cborUvNotRequired);

        // Read UV=Required case
        var reader = new CborReader(cborUvRequired, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        reader.ReadInt32(); // key 3
        reader.SkipValue(); // algorithms array
        Assert.Equal(4, reader.ReadInt32()); // key 4
        Assert.Equal(5, reader.ReadInt32()); // 0b101 = RequireUserVerification
        reader.ReadEndMap();

        // Read UV!=Required case
        reader = new CborReader(cborUvNotRequired, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        reader.ReadInt32(); // key 3
        reader.SkipValue(); // algorithms array
        Assert.Equal(4, reader.ReadInt32()); // key 4
        Assert.Equal(1, reader.ReadInt32()); // 0b001 = RequireUserPresence
        reader.ReadEndMap();
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Authentication_EmptyAllowCredentials_Throws_BeforeBackendCall()
    {
        // Arrange - authentication input with empty allowCredentials
        var input = new PreviewSignAuthenticationInput(
            new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(ByteArrayKeyComparer.Instance)
            {
                [RandomNumberGenerator.GetBytes(32)] = new PreviewSignSigningParams(
                    keyHandle: RandomNumberGenerator.GetBytes(16),
                    tbs: RandomNumberGenerator.GetBytes(64))
            });

        // Act & Assert - empty allowCredentials should throw InvalidRequest
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            PreviewSignAdapter.BuildAuthenticationCbor(input, allowCredentials: null));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
        Assert.Contains("non-empty allowCredentials", ex.Message);

        // Also test empty list (not just null)
        ex = Assert.Throws<WebAuthnClientError>(() =>
            PreviewSignAdapter.BuildAuthenticationCbor(input, allowCredentials: []));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
        Assert.Contains("non-empty allowCredentials", ex.Message);
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Authentication_MissingSignByCredentialEntry_Throws_BeforeBackendCall()
    {
        // Arrange - allowCredentials has 2 entries, but signByCredential only has 1
        var credA = RandomNumberGenerator.GetBytes(32);
        var credB = RandomNumberGenerator.GetBytes(32);

        var input = new PreviewSignAuthenticationInput(
            new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(ByteArrayKeyComparer.Instance)
            {
                [credA] = new PreviewSignSigningParams(
                    keyHandle: RandomNumberGenerator.GetBytes(16),
                    tbs: RandomNumberGenerator.GetBytes(64))
                // Missing entry for credB
            });

        var allowCredentials = new List<WebAuthnCredentialDescriptor>
        {
            new(credA),
            new(credB)
        };

        // Act & Assert - missing signByCredential entry should throw InvalidRequest
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            PreviewSignAdapter.BuildAuthenticationCbor(input, allowCredentials));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
        Assert.Contains("missing entries", ex.Message);
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Registration_OutputPopulatedFromUnsignedAttObj()
    {
        // Arrange - mock authenticator data with previewSign extension containing algorithm + flags in authData
        // and attestation object in unsignedExtensionOutputs (per Fix #6b)
        var keyHandle = RandomNumberGenerator.GetBytes(16);
        var publicKeyBytes = BuildCoseEc2PublicKey(CoseAlgorithm.Es256);
        var attestationObject = BuildAttestationObject(publicKeyBytes, PreviewSignFlags.RequireUserVerification);

        // Build authData.extensions["previewSign"] = {3: alg, 4: flags}
        var authDataExtension = new CborWriter(CborConformanceMode.Ctap2Canonical);
        authDataExtension.WriteStartMap(2);
        authDataExtension.WriteInt32(3);  // alg key
        authDataExtension.WriteInt32(CoseAlgorithm.Es256.Value);
        authDataExtension.WriteInt32(4);  // flags key
        authDataExtension.WriteInt32((byte)PreviewSignFlags.RequireUserVerification);
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
        var output = PreviewSignAdapter.ParseRegistrationOutput(parsedAuthData, unsignedExtensionOutputs);

        // Assert - GeneratedKey should be populated from both sources
        Assert.NotNull(output);
        Assert.NotNull(output.GeneratedKey);
        Assert.NotNull(output.GeneratedKey.PublicKey);
        Assert.Equal(CoseAlgorithm.Es256, output.GeneratedKey.Algorithm);
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Authentication_OutputSignaturePopulated()
    {
        // Arrange - mock authenticator data with previewSign extension containing signature
        var signatureBytes = RandomNumberGenerator.GetBytes(64);  // Mock ES256 signature (r||s)

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
        var output = PreviewSignAdapter.ParseAuthenticationOutput(parsedAuthData);

        // Assert - Signature should match fixture bytes
        Assert.NotNull(output);
        Assert.Equal(signatureBytes, output.Signature.ToArray());
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Authentication_EncodesAsFlatSingleCredentialMap()
    {
        // Arrange - single allowed credential with single signByCredential entry
        var credA = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);

        var paramsA = new PreviewSignSigningParams(
            keyHandle: new byte[] { 0xAA, 0xBB },
            tbs: new byte[] { 0xCC, 0xDD, 0xEE });

        var input = new PreviewSignAuthenticationInput(
            new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(ByteArrayKeyComparer.Instance)
            {
                [credA] = paramsA
            });

        var allowCredentials = new List<WebAuthnCredentialDescriptor>
        {
            new(credA)
        };

        // Act - build authentication CBOR
        var cbor = PreviewSignAdapter.BuildAuthenticationCbor(input, allowCredentials);

        // Assert - CBOR should be a FLAT map {2: kh, 6: tbs} (no outer credential-keyed wrapper)
        Assert.NotNull(cbor);

        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();
        Assert.Equal(2, mapSize);  // Just kh + tbs

        // Key 2: keyHandle
        Assert.Equal(2, reader.ReadInt32());
        var keyHandle = reader.ReadByteString();
        Assert.Equal(paramsA.KeyHandle.ToArray(), keyHandle);

        // Key 6: tbs
        Assert.Equal(6, reader.ReadInt32());
        var tbs = reader.ReadByteString();
        Assert.Equal(paramsA.Tbs.ToArray(), tbs);

        reader.ReadEndMap();

        // Byte-exact hex check
        string hex = Convert.ToHexString(cbor);
        Assert.Equal("A20242AABB0643CCDDEE", hex);
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Authentication_MultipleSignByCredentialEntries_Throws_NotSupported()
    {
        // Arrange - two signByCredential entries (multi-credential probe not yet implemented)
        var credA = new ReadOnlyMemory<byte>([0x01, 0x02]);
        var credB = new ReadOnlyMemory<byte>([0x03, 0x04]);

        var input = new PreviewSignAuthenticationInput(
            new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(ByteArrayKeyComparer.Instance)
            {
                [credA] = new PreviewSignSigningParams(new byte[] { 0xAA }, new byte[] { 0xBB }),
                [credB] = new PreviewSignSigningParams(new byte[] { 0xCC }, new byte[] { 0xDD })
            });

        var allowCredentials = new List<WebAuthnCredentialDescriptor>
        {
            new(credA),
            new(credB)
        };

        // Act & Assert
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            PreviewSignAdapter.BuildAuthenticationCbor(input, allowCredentials));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, ex.Code);
        Assert.Contains("single-credential scope", ex.Message);
        Assert.Contains("Phase 10", ex.Message);
        Assert.Contains("phase-10-previewsign-auth.md", ex.Message);
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Authentication_SignByCredentialMismatchesAllowList_Throws_InvalidRequest()
    {
        // Arrange - signByCredential has credB, but allowCredentials has credA
        var credA = new ReadOnlyMemory<byte>([0x01, 0x02]);
        var credB = new ReadOnlyMemory<byte>([0x03, 0x04]);

        var input = new PreviewSignAuthenticationInput(
            new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(ByteArrayKeyComparer.Instance)
            {
                [credB] = new PreviewSignSigningParams(new byte[] { 0xAA }, new byte[] { 0xBB })
            });

        var allowCredentials = new List<WebAuthnCredentialDescriptor>
        {
            new(credA)
        };

        // Act & Assert - validation catches missing credA in signByCredential first
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            PreviewSignAdapter.BuildAuthenticationCbor(input, allowCredentials));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
        Assert.Contains("missing entries", ex.Message);
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

    private static byte[] BuildAttestationObject(byte[] publicKey, PreviewSignFlags flags)
    {
        // Build minimal attestation object with previewSign extension in authData
        var rpIdHash = SHA256.HashData("example.com"u8);
        var credId = RandomNumberGenerator.GetBytes(32);

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

        // Build attestation object
        var attObjWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        attObjWriter.WriteStartMap(3);

        // fmt
        attObjWriter.WriteTextString("fmt");
        attObjWriter.WriteTextString("none");

        // authData
        attObjWriter.WriteTextString("authData");
        attObjWriter.WriteByteString(authDataList.ToArray());

        // attStmt (empty)
        attObjWriter.WriteTextString("attStmt");
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
