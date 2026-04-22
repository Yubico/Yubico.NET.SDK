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
    public void PreviewSign_Registration_BuildsCtapInput_FromAlgorithms_AndUvPreferencePromotion()
    {
        // Arrange - default flags (RequireUserPresence) + UV Required preference
        var input = new PreviewSignRegistrationInput(
            algorithms: [CoseAlgorithm.Es256, CoseAlgorithm.EdDsa],
            flags: PreviewSignFlags.RequireUserPresence);  // Default

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [CoseAlgorithm.Es256],
            UserVerification = UserVerificationPreference.Required  // UV promotion trigger
        };

        // Act - build CBOR with UV promotion
        var cbor = PreviewSignAdapter.BuildRegistrationCbor(input, options);

        // Assert - flags should be promoted to RequireUserVerification (0b101 = 5)
        Assert.NotNull(cbor);

        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();
        Assert.Equal(2, mapSize);

        // Read key 3: algorithms
        Assert.Equal(3, reader.ReadInt32());
        int? algArraySize = reader.ReadStartArray();
        Assert.Equal(2, algArraySize);
        Assert.Equal(-7, reader.ReadInt32());  // ES256
        Assert.Equal(-8, reader.ReadInt32());  // EdDSA
        reader.ReadEndArray();

        // Read key 4: flags (promoted to 0b101)
        Assert.Equal(4, reader.ReadInt32());
        Assert.Equal(5, reader.ReadInt32());  // 0b101 = RequireUserVerification

        reader.ReadEndMap();
    }

    [Fact(Timeout = 5000)]
    public void PreviewSign_Registration_FlagsConflict_ThrowsInvalidRequest()
    {
        // Arrange - explicit Unattended flags + UV Required preference → contradiction
        var input = new PreviewSignRegistrationInput(
            algorithms: [CoseAlgorithm.Es256],
            flags: PreviewSignFlags.Unattended);  // Explicitly requested no UP/UV

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = "example.com", Name = "Example" },
            User = new WebAuthnUser { Id = RandomNumberGenerator.GetBytes(16), Name = "user@example.com", DisplayName = "User" },
            PubKeyCredParams = [CoseAlgorithm.Es256],
            UserVerification = UserVerificationPreference.Required  // Contradicts Unattended
        };

        // Act & Assert - should throw BEFORE any backend call
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            PreviewSignAdapter.BuildRegistrationCbor(input, options));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
        Assert.Contains("Flags conflict", ex.Message);
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
        // Arrange - mock authenticator data with previewSign extension containing unsigned att-obj
        var keyHandle = RandomNumberGenerator.GetBytes(16);
        var publicKeyBytes = BuildCoseEc2PublicKey(CoseAlgorithm.Es256);
        var attestationObject = BuildAttestationObject(publicKeyBytes, PreviewSignFlags.RequireUserVerification);

        // Build previewSign extension output (unsigned form: key 7 = att-obj)
        var extensionWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        extensionWriter.WriteStartMap(1);
        extensionWriter.WriteInt32(7);  // att-obj key
        extensionWriter.WriteByteString(attestationObject);
        extensionWriter.WriteEndMap();

        var authData = BuildAuthDataWithExtensions(new Dictionary<string, byte[]>
        {
            ["previewSign"] = extensionWriter.Encode()
        });

        var parsedAuthData = WebAuthnAuthenticatorData.Decode(authData);

        // Act - parse registration output
        var output = PreviewSignAdapter.ParseRegistrationOutput(parsedAuthData);

        // Assert - GeneratedKey should be populated from att-obj
        Assert.NotNull(output);
        Assert.NotNull(output.GeneratedKey);
        Assert.NotNull(output.GeneratedKey.PublicKey);
        Assert.Equal(CoseAlgorithm.Es256, output.GeneratedKey.Algorithm);
        Assert.Equal(PreviewSignFlags.RequireUserVerification, output.GeneratedKey.Flags);
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
    public void PreviewSign_Authentication_RoutesCorrectSigningParams_ToBackend()
    {
        // Arrange - two credentials with different signing params
        var credA = RandomNumberGenerator.GetBytes(32);
        var credB = RandomNumberGenerator.GetBytes(32);

        var paramsA = new PreviewSignSigningParams(
            keyHandle: RandomNumberGenerator.GetBytes(16),
            tbs: "Data for credential A"u8.ToArray());

        var paramsB = new PreviewSignSigningParams(
            keyHandle: RandomNumberGenerator.GetBytes(16),
            tbs: "Data for credential B"u8.ToArray());

        var input = new PreviewSignAuthenticationInput(
            new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(ByteArrayKeyComparer.Instance)
            {
                [credA] = paramsA,
                [credB] = paramsB
            });

        var allowCredentials = new List<WebAuthnCredentialDescriptor>
        {
            new(credA),
            new(credB)
        };

        // Act - build authentication CBOR
        var cbor = PreviewSignAdapter.BuildAuthenticationCbor(input, allowCredentials);

        // Assert - CBOR should contain BOTH entries (authenticator filters down)
        Assert.NotNull(cbor);

        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();
        Assert.Equal(2, mapSize);  // Both credentials present

        // Read first entry (credA or credB, order determined by canonical sort)
        var firstCredId = reader.ReadByteString();
        int? firstParamsSize = reader.ReadStartMap();
        Assert.Equal(2, firstParamsSize);  // kh + tbs

        var firstKeyHandleKey = reader.ReadInt32();
        Assert.Equal(2, firstKeyHandleKey);
        var firstKeyHandle = reader.ReadByteString();

        var firstTbsKey = reader.ReadInt32();
        Assert.Equal(6, firstTbsKey);
        var firstTbs = reader.ReadByteString();
        reader.ReadEndMap();

        // Read second entry
        var secondCredId = reader.ReadByteString();
        int? secondParamsSize = reader.ReadStartMap();
        Assert.Equal(2, secondParamsSize);

        var secondKeyHandleKey = reader.ReadInt32();
        Assert.Equal(2, secondKeyHandleKey);
        var secondKeyHandle = reader.ReadByteString();

        var secondTbsKey = reader.ReadInt32();
        Assert.Equal(6, secondTbsKey);
        var secondTbs = reader.ReadByteString();
        reader.ReadEndMap();

        reader.ReadEndMap();

        // Verify both parameter sets are present (order may vary due to canonical CBOR)
        var allTbs = new[] { firstTbs, secondTbs };
        Assert.Contains(paramsA.Tbs.ToArray(), allTbs);
        Assert.Contains(paramsB.Tbs.ToArray(), allTbs);
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
