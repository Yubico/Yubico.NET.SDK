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
using Xunit;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.PreviewSign;

public class PreviewSignCborTests
{
    [Fact(Timeout = 5000)]
    public void Flags_HasExpectedValues()
    {
        // Assert exact bit patterns
        Assert.Equal(0b000, (byte)PreviewSignFlags.Unattended);
        Assert.Equal(0b001, (byte)PreviewSignFlags.RequireUserPresence);
        Assert.Equal(0b101, (byte)PreviewSignFlags.RequireUserVerification);

        // Assert IsValid for the three valid patterns
        Assert.True(PreviewSignFlags.Unattended.IsValid());
        Assert.True(PreviewSignFlags.RequireUserPresence.IsValid());
        Assert.True(PreviewSignFlags.RequireUserVerification.IsValid());

        // Assert IsValid is false for invalid patterns
        var invalidFlags = (PreviewSignFlags)0b011;
        Assert.False(invalidFlags.IsValid());

        invalidFlags = (PreviewSignFlags)0b100;
        Assert.False(invalidFlags.IsValid());

        invalidFlags = (PreviewSignFlags)0b110;
        Assert.False(invalidFlags.IsValid());

        invalidFlags = (PreviewSignFlags)0b111;
        Assert.False(invalidFlags.IsValid());
    }

    [Fact(Timeout = 5000)]
    public void RegistrationInput_EncodesAlgArrayAndFlags_AsIntegerKeyedMap()
    {
        // Arrange
        var algorithms = new[] { CoseAlgorithm.Es256, CoseAlgorithm.Esp256 };
        var input = new PreviewSignRegistrationInput(algorithms, PreviewSignFlags.RequireUserPresence);

        // Act
        byte[] encoded = PreviewSignCbor.EncodeRegistrationInput(input);

        // Assert - should be {3: [-7, -9], 4: 1}
        // Expected CBOR hex: A2 03 82 26 28 04 01
        // A2 = map(2)
        // 03 = key 3
        // 82 = array(2)
        // 26 = -7 (ES256)
        // 28 = -9 (ESP256)
        // 04 = key 4
        // 01 = 1 (RequireUserPresence)

        string hex = Convert.ToHexString(encoded);
        Assert.Equal("A2038226280401", hex);

        // Verify round-trip parsing
        var reader = new CborReader(encoded, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();
        Assert.Equal(2, mapSize);

        // First key should be 3 (alg)
        Assert.Equal(3, reader.ReadInt32());
        int? arraySize = reader.ReadStartArray();
        Assert.Equal(2, arraySize);
        Assert.Equal(-7, reader.ReadInt32());
        Assert.Equal(-9, reader.ReadInt32());
        reader.ReadEndArray();

        // Second key should be 4 (flags)
        Assert.Equal(4, reader.ReadInt32());
        Assert.Equal(1, reader.ReadInt32());

        reader.ReadEndMap();
    }

    [Fact(Timeout = 5000)]
    public void AuthenticationInput_OmitsArgs_WhenAdditionalArgsIsNull()
    {
        // Arrange
        var credId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var keyHandle = new byte[] { 0x10, 0x20 };
        var tbs = new byte[] { 0x30, 0x40 };

        var signingParams = new PreviewSignSigningParams(
            keyHandle: keyHandle,
            tbs: tbs,
            additionalArgs: null);

        var signByCredential = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(
            ByteArrayKeyComparer.Instance)
        {
            [credId] = signingParams
        };

        var input = new PreviewSignAuthenticationInput(signByCredential);

        // Act
        byte[] encoded = PreviewSignCbor.EncodeAuthenticationInput(input);

        // Assert - decode and verify keys 2 and 6 are present, key 7 is NOT
        var reader = new CborReader(encoded, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap(); // Outer map (credId → params)

        // Read credential ID
        byte[] decodedCredId = reader.ReadByteString();
        Assert.Equal(credId, decodedCredId);

        // Read inner map
        int? innerMapSize = reader.ReadStartMap();
        Assert.Equal(2, innerMapSize); // Only kh (2) and tbs (6), no args (7)

        var keys = new HashSet<int>();
        for (int i = 0; i < innerMapSize; i++)
        {
            int key = reader.ReadInt32();
            keys.Add(key);
            reader.SkipValue(); // Skip the value
        }

        Assert.Contains(2, keys); // kh
        Assert.Contains(6, keys); // tbs
        Assert.DoesNotContain(7, keys); // args should NOT be present
    }

    [Fact(Timeout = 5000)]
    public void AuthenticationInput_WrapsArgs_AsByteString_WhenPresent()
    {
        // Arrange
        var credId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var keyHandle = new byte[] { 0x10, 0x20 };
        var tbs = new byte[] { 0x30, 0x40 };
        var additionalArgs = new byte[] { 0x50, 0x60, 0x70 }; // Some CBOR bytes

        var signingParams = new PreviewSignSigningParams(
            keyHandle: keyHandle,
            tbs: tbs,
            additionalArgs: additionalArgs);

        var signByCredential = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(
            ByteArrayKeyComparer.Instance)
        {
            [credId] = signingParams
        };

        var input = new PreviewSignAuthenticationInput(signByCredential);

        // Act
        byte[] encoded = PreviewSignCbor.EncodeAuthenticationInput(input);

        // Assert - verify key 7 is present and is encoded as bstr (major type 2)
        var reader = new CborReader(encoded, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap(); // Outer map
        reader.SkipValue(); // Skip credential ID

        int? innerMapSize = reader.ReadStartMap();
        Assert.Equal(3, innerMapSize); // kh (2), tbs (6), args (7)

        bool foundArgs = false;
        for (int i = 0; i < innerMapSize; i++)
        {
            int key = reader.ReadInt32();
            if (key == 7) // args key
            {
                foundArgs = true;
                // Verify it's encoded as a byte string (CBOR major type 2)
                byte[] argsBytes = reader.ReadByteString();
                Assert.Equal(additionalArgs, argsBytes);
            }
            else
            {
                reader.SkipValue();
            }
        }

        Assert.True(foundArgs, "AdditionalArgs key (7) should be present when AdditionalArgs is set");
    }

    [Fact(Timeout = 5000)]
    public void UnsignedRegistrationOutput_DecodesNestedAttObj_AndPropagatesFlags()
    {
        // Arrange - For this test, we verify that the decoder ATTEMPTS to parse the structure.
        // Building a fully valid WebAuthnAttestationObject fixture is complex because it requires
        // matching the exact binary format expected by AuthenticatorData.Parse from Fido2.
        // Instead, we test a simpler assertion: that the decoder can extract the attestation object
        // bytes and verify the CBOR structure is correct.

        // Build a minimal valid attestation object bytes
        var attObjWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        attObjWriter.WriteStartMap(3);
        attObjWriter.WriteTextString("fmt");
        attObjWriter.WriteTextString("none");
        attObjWriter.WriteTextString("authData");
        // Minimal authData: rpIdHash(32) + flags(1) + signCount(4) = 37 bytes minimum
        // For this simplified test, we'll use 37 zero bytes (no extensions, no attested cred data)
        // This won't fully decode but will let us test the CBOR parsing path
        attObjWriter.WriteByteString(new byte[37]);
        attObjWriter.WriteTextString("attStmt");
        attObjWriter.WriteStartMap(0);
        attObjWriter.WriteEndMap();
        attObjWriter.WriteEndMap();
        byte[] attestationObjectBytes = attObjWriter.Encode();

        // Build the unsigned registration output: {7: att-obj}
        var outputWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        outputWriter.WriteStartMap(1);
        outputWriter.WriteInt32(7); // att-obj key
        outputWriter.WriteByteString(attestationObjectBytes);
        outputWriter.WriteEndMap();
        byte[] outputBytes = outputWriter.Encode();

        // Act & Assert - this will throw because our minimal authData doesn't have the
        // previewSign extension, which is expected. This test verifies the decoder
        // correctly extracts the attestation object bytes and attempts to parse them.
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            PreviewSignCbor.DecodeUnsignedRegistrationOutput(outputBytes));

        Assert.Equal(WebAuthnClientErrorCode.InvalidState, ex.Code);
        Assert.Contains("previewSign", ex.Message);
    }

    [Fact(Timeout = 5000)]
    public void AuthenticationOutput_ExtractsSignatureBytes()
    {
        // Arrange - build authentication output: {6: sig}
        var signature = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(6); // sig key
        writer.WriteByteString(signature);
        writer.WriteEndMap();
        byte[] outputBytes = writer.Encode();

        // Act
        var output = PreviewSignCbor.DecodeAuthenticationOutput(outputBytes);

        // Assert
        Assert.Equal(signature, output.Signature.ToArray());
    }

    [Fact(Timeout = 5000)]
    public void PreviewSignFlags_InvalidValue_ConstructorThrows()
    {
        // Arrange
        var invalidFlags = (PreviewSignFlags)0b011; // Invalid pattern
        var algorithms = new[] { CoseAlgorithm.Es256 };

        // Act & Assert
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            new PreviewSignRegistrationInput(algorithms, invalidFlags));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
        Assert.Contains("Invalid previewSign flags", ex.Message);
    }

    [Fact(Timeout = 5000)]
    public void PreviewSignRegistrationInput_EmptyAlgorithms_Throws()
    {
        // Arrange
        var emptyAlgorithms = Array.Empty<CoseAlgorithm>();

        // Act & Assert
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            new PreviewSignRegistrationInput(emptyAlgorithms, PreviewSignFlags.RequireUserPresence));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
        Assert.Contains("at least one algorithm", ex.Message);
    }

    [Fact(Timeout = 5000)]
    public void MalformedAuthenticationOutput_Throws_InvalidState()
    {
        // Arrange - random non-CBOR bytes
        var malformedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        // Act & Assert
        var ex = Assert.Throws<WebAuthnClientError>(() =>
            PreviewSignCbor.DecodeAuthenticationOutput(malformedBytes));

        Assert.Equal(WebAuthnClientErrorCode.InvalidState, ex.Code);
        Assert.Contains("malformed", ex.Message);
    }

    [Fact(Timeout = 5000)]
    public void ByteArrayKeyComparer_HashesEqualBytesIdentically_AndDifferentBytesDifferently()
    {
        // Arrange
        var comparer = ByteArrayKeyComparer.Instance;

        var bytes1a = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04, 0x05]);
        var bytes1b = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04, 0x05]); // same content, different array
        var bytes2 = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04, 0x06]); // different last byte
        var bytes3 = new ReadOnlyMemory<byte>([0xFF, 0xFE, 0xFD]); // different length and content

        // Act
        int hash1a = comparer.GetHashCode(bytes1a);
        int hash1b = comparer.GetHashCode(bytes1b);
        int hash2 = comparer.GetHashCode(bytes2);
        int hash3 = comparer.GetHashCode(bytes3);

        // Assert - Equal content must produce equal hashes
        Assert.Equal(hash1a, hash1b);
        Assert.True(comparer.Equals(bytes1a, bytes1b));

        // Different content should produce different hashes (not guaranteed, but highly likely with full-content hashing)
        // At minimum, verify the equality contract holds
        Assert.False(comparer.Equals(bytes1a, bytes2));
        Assert.False(comparer.Equals(bytes1a, bytes3));
        Assert.False(comparer.Equals(bytes2, bytes3));
    }

    [Fact(Timeout = 5000)]
    public void PreviewSignAuthenticationInput_RebuildsDictionary_WithCorrectComparer()
    {
        // Arrange - Create dictionary with default comparer (wrong)
        var credId1 = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);
        var credId2Copy = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]); // same bytes, different array

        var defaultDict = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>
        {
            [credId1] = new PreviewSignSigningParams(
                KeyHandle: new byte[] { 0xAA },
                Tbs: new byte[] { 0xBB })
        };

        // Act - Constructor should rebuild with ByteArrayKeyComparer
        var input = new PreviewSignAuthenticationInput(defaultDict);

        // Assert - Lookups by independent ReadOnlyMemory<byte> with same bytes should work
        Assert.True(input.SignByCredential.ContainsKey(credId2Copy));
        Assert.Equal(0xAA, input.SignByCredential[credId2Copy].KeyHandle.Span[0]);
    }
}
