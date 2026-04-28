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
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

/// <summary>
/// Byte-level verification that the C# CBOR encoder produces wire format matching the Rust reference.
/// </summary>
/// <remarks>
/// Reference: cnh-authenticator-rs-extension @ get_assertion.rs:290-323 (serde_cbor encoder)
/// The Rust upstream uses BTreeMap with Value::Integer keys and Value::Bytes values, which serializes
/// keys in ascending order: 2 → 6 → 7.
/// </remarks>
public class PreviewSignCborTests
{
    [Fact]
    public void EncodeAuthenticationInput_SingleCredential_NoArgs_MatchesRustByteStructure()
    {
        // Arrange: Mimic hid-test inputs (32-byte SHA-256 TBS, fixed key handle)
        // Rust hid-test uses: tbs = Sha256("Hello, previewSign v4!"), kh from registration
        byte[] keyHandle = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        byte[] tbs = new byte[32]; // SHA-256 hash (all zeros for deterministic test)
        Array.Fill(tbs, (byte)0xAA);

        var signingParams = new PreviewSignSigningParams(
            keyHandle: keyHandle,
            tbs: tbs,
            additionalArgs: null);

        // Act
        byte[] cborBytes = PreviewSignCbor.EncodeAuthenticationInput(signingParams);

        // Assert: Decode and verify structure matches Rust {2: bytes, 6: bytes}
        var reader = new CborReader(cborBytes, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();
        Assert.Equal(2, mapSize); // Two keys: 2 (kh) and 6 (tbs)

        // First key should be 2 (keyHandle)
        int key1 = reader.ReadInt32();
        Assert.Equal(2, key1);
        byte[] decodedKh = reader.ReadByteString();
        Assert.Equal(keyHandle, decodedKh);

        // Second key should be 6 (tbs)
        int key2 = reader.ReadInt32();
        Assert.Equal(6, key2);
        byte[] decodedTbs = reader.ReadByteString();
        Assert.Equal(tbs, decodedTbs);

        reader.ReadEndMap();
    }

    [Fact]
    public void EncodeAuthenticationInput_WithAdditionalArgs_MatchesRustThreeKeyStructure()
    {
        // Arrange: ARKG case with additional_args (opaque CBOR bytes for COSE_Sign_Args)
        byte[] keyHandle = [0x01, 0x02, 0x03, 0x04];
        byte[] tbs = new byte[32];
        Array.Fill(tbs, (byte)0xBB);

        // Build minimal ARKG COSE_Sign_Args: {3: -65539, -1: kh, -2: ctx}
        // This mimics Rust hid-test's encode_arkg_sign_args output (opaque bytes from caller's perspective).
        // alg = -65539 (Esp256SplitArkgPlaceholder / "ARKG-P256-ESP256") — NOT -9 (Esp256, the
        // output signature alg). YK 5.8.0-beta firmware rejects everything but -65539 here.
        var argWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        argWriter.WriteStartMap(3);
        argWriter.WriteInt32(3);       // alg
        argWriter.WriteInt32(-65539);  // ARKG-P256-ESP256 placeholder (request alg on wire)
        argWriter.WriteInt32(-1); // arkg_kh
        argWriter.WriteByteString([0xAA, 0xBB, 0xCC, 0xDD]);
        argWriter.WriteInt32(-2); // ctx
        argWriter.WriteByteString([0x11, 0x22]);
        argWriter.WriteEndMap();
        byte[] additionalArgs = argWriter.Encode();

        var signingParams = new PreviewSignSigningParams(
            keyHandle: keyHandle,
            tbs: tbs,
            additionalArgs: additionalArgs);

        // Act
        byte[] cborBytes = PreviewSignCbor.EncodeAuthenticationInput(signingParams);

        // Assert: Verify structure {2: bytes, 6: bytes, 7: bytes} with BTreeMap ascending order
        var reader = new CborReader(cborBytes, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();
        Assert.Equal(3, mapSize); // Three keys: 2, 6, 7

        // Key 2: keyHandle
        Assert.Equal(2, reader.ReadInt32());
        Assert.Equal(keyHandle, reader.ReadByteString());

        // Key 6: tbs
        Assert.Equal(6, reader.ReadInt32());
        Assert.Equal(tbs, reader.ReadByteString());

        // Key 7: additional_args (byte-wrapped CBOR)
        Assert.Equal(7, reader.ReadInt32());
        byte[] decodedArgs = reader.ReadByteString();
        Assert.Equal(additionalArgs, decodedArgs);

        reader.ReadEndMap();
    }

    [Fact]
    public void EncodeAuthenticationInput_ProducesCanonicalCborByteString()
    {
        // Arrange: Verify that byte strings use correct CBOR major type 2 with proper length encoding
        byte[] keyHandle = new byte[64]; // Length > 23, triggers 1-byte length header (major type 2, info 24)
        Array.Fill(keyHandle, (byte)0xFF);
        byte[] tbs = new byte[32];
        Array.Fill(tbs, (byte)0xEE);

        var signingParams = new PreviewSignSigningParams(
            keyHandle: keyHandle,
            tbs: tbs,
            additionalArgs: null);

        // Act
        byte[] cborBytes = PreviewSignCbor.EncodeAuthenticationInput(signingParams);

        // Assert: First byte should be CBOR map header
        // CBOR definite-length map with 2 entries: major type 5 (0b101_00000) | info 2 = 0xA2
        Assert.Equal(0xA2, cborBytes[0]);

        // Next should be integer key 2 (0x02)
        Assert.Equal(0x02, cborBytes[1]);

        // Next should be byte string with length 64
        // Major type 2 (0b010_00000) | info 24 (1-byte length follows) = 0x58
        Assert.Equal(0x58, cborBytes[2]);
        Assert.Equal(64, cborBytes[3]); // Length byte
    }

    [Fact]
    public void EncodeRegistrationInput_MatchesCborStructure()
    {
        // Arrange
        var input = new PreviewSignRegistrationInput(
            algorithms: [-7, -257], // Es256, Rs256
            flags: 0x01);           // RequireUserPresence

        // Act
        byte[] cborBytes = PreviewSignCbor.EncodeRegistrationInput(input);

        // Assert: Decode and verify structure {3: [-7, -257], 4: 1}
        var reader = new CborReader(cborBytes, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();
        Assert.Equal(2, mapSize);

        // Key 3: algorithms array
        int key1 = reader.ReadInt32();
        Assert.Equal(3, key1);
        int? arraySize = reader.ReadStartArray();
        Assert.Equal(2, arraySize);
        Assert.Equal(-7, reader.ReadInt32());
        Assert.Equal(-257, reader.ReadInt32());
        reader.ReadEndArray();

        // Key 4: flags byte
        int key2 = reader.ReadInt32();
        Assert.Equal(4, key2);
        Assert.Equal(1, reader.ReadInt32());

        reader.ReadEndMap();
    }
}
