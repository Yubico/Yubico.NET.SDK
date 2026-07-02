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
/// Byte-level verification that the C# CBOR encoder produces wire format matching the Rust reference
/// and the python-fido2 ARKG test vectors.
/// </summary>
/// <remarks>
/// <b>WARNING -- EXPERIMENTAL -- test only:</b> ARKG previewSign vectors and helper shapes are not ready for
/// production use and must not be treated as production cryptographic guidance.
///
/// References:
/// - cnh-authenticator-rs-extension @ get_assertion.rs:290-323 (serde_cbor encoder)
/// - python-fido2/tests/test_arkg.py:36-73 (deterministic ARKG vectors — used for KH/CTX shapes)
/// - Yubico.NET.SDK-Legacy commit fe82b007 — EncodeArkgSignArgs in GetAssertionParameters.cs:402-499
///
/// The Rust upstream uses BTreeMap with Value::Integer keys and Value::Bytes values, which
/// serializes positive integer keys before negative ones under canonical encoding: 2 → 6 → 7
/// at the outer level, and 3 → -1 → -2 inside the COSE_Sign_Args map.
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
    public void EncodeAuthenticationInput_WithAdditionalArgs_MatchesLegacyThreeKeyStructure()
    {
        // Arrange: WARNING -- EXPERIMENTAL -- ARKG case, not production cryptographic guidance.
        // Exercise the typed builder end-to-end through EncodeAuthenticationInput.
        // alg = -65539 (Esp256SplitArkgPlaceholder / "ARKG-P256-ESP256") — NOT -9 (Esp256, the
        // output signature alg). YK 5.8.0-beta firmware rejects everything but -65539 here.
        byte[] keyHandle = [0x01, 0x02, 0x03, 0x04];
        byte[] tbs = new byte[32];
        Array.Fill(tbs, (byte)0xBB);

        // Real ARKG-P256 KH shape: 16-byte HMAC tag || 65-byte SEC1 uncompressed P-256 point.
        byte[] arkgKeyHandle = BuildArkgKeyHandleFixture(tagPattern: 0xCD, pubKeyPattern: 0xEF);
        // Real ARKG context: ASCII label per python-fido2 vectors.
        byte[] context = "ARKG-P256.test vectors"u8.ToArray();

        byte[] additionalArgs = PreviewSignCbor.EncodeAdditionalArgs(
            new ArkgP256SignArgs(arkgKeyHandle, context));

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

        // Key 7: algorithm-specific additionalArgs (byte-wrapped CBOR)
        Assert.Equal(7, reader.ReadInt32());
        byte[] decodedArgs = reader.ReadByteString();

        // The decoded inner bytes must equal the caller-provided raw additionalArgs exactly.
        Assert.Equal(additionalArgs, decodedArgs);

        reader.ReadEndMap();
    }

    [Fact]
    public void EncodeCoseSignArgs_ArkgP256_MatchesForensicsByteMap()
    {
        // Arrange: deterministic 81-byte KH (16-byte tag pattern + 65-byte SEC1 uncompressed point).
        byte[] kh = BuildArkgKeyHandleFixture(tagPattern: 0x00, pubKeyPattern: 0x55);
        // 32-byte zero context — small enough to exercise single-byte length prefix.
        byte[] ctx = new byte[32];

        // Act
        byte[] actual = PreviewSignCbor.EncodeCoseSignArgs(new ArkgP256SignArgs(kh, ctx));

        // Build expected per LEGACY_PREVIEWSIGN_FORENSICS.md §3.4:
        //   A3                                # map(3)
        //     03 3A 0001 0002                 #   3 : -65539
        //     20 58 51 <81 KH bytes>          #  -1 : bstr(81)
        //     21 58 20 <32 CTX bytes>         #  -2 : bstr(32)
        var expected = new List<byte>(126);
        expected.Add(0xA3);                                  // map(3)                 — 1 byte
        expected.AddRange([0x03, 0x3A, 0x00, 0x01, 0x00, 0x02]); // 3 : -65539           — 6 bytes
        expected.AddRange([0x20, 0x58, 0x51]);               // -1 : bstr len 81 hdr   — 3 bytes
        expected.AddRange(kh);                               //                          — 81 bytes
        expected.AddRange([0x21, 0x58, 0x20]);               // -2 : bstr len 32 hdr   — 3 bytes
        expected.AddRange(ctx);                              //                          — 32 bytes

        Assert.Equal(expected.ToArray(), actual);
        // Sanity: 1 + 6 + 3 + 81 + 3 + 32 = 126 bytes.
        // (PRD §4 said 125 — arithmetic error in PRD; corrected here. The byte-for-byte
        // structural assertion above is the binding contract.)
        Assert.Equal(126, actual.Length);
    }

    [Fact]
    public void EncodeAdditionalArgs_ArkgP256_MatchesCoseSignArgsBridge()
    {
        byte[] kh = BuildArkgKeyHandleFixture(tagPattern: 0x33, pubKeyPattern: 0x44);
        byte[] ctx = "ARKG-P256.test vectors"u8.ToArray();
        var args = new ArkgP256SignArgs(kh, ctx);

        byte[] additionalArgs = PreviewSignCbor.EncodeAdditionalArgs(args);

        Assert.Equal(PreviewSignCbor.EncodeCoseSignArgs(args), additionalArgs);
    }

    [Fact]
    public void EncodeCoseSignArgs_ArkgP256_WithRealisticPythonFido2Context_RoundTrips()
    {
        // Arrange: realistic context string from python-fido2/tests/test_arkg.py:38
        // ("ARKG-P256.test vectors") — 22 bytes, mirrors the shape used in the upstream test
        // vectors. KH is the deterministic placeholder fixture (no crypto required for encoder
        // correctness — see PRD §7-7).
        byte[] kh = BuildArkgKeyHandleFixture(tagPattern: 0xA5, pubKeyPattern: 0x5A);
        byte[] ctx = "ARKG-P256.test vectors"u8.ToArray();
        Assert.Equal(22, ctx.Length);

        // Act
        byte[] cbor = PreviewSignCbor.EncodeCoseSignArgs(new ArkgP256SignArgs(kh, ctx));

        // Assert: structural round-trip via CborReader
        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(3, reader.ReadStartMap());

        Assert.Equal(3, reader.ReadInt32());
        Assert.Equal(-65539, reader.ReadInt32()); // Esp256SplitArkgPlaceholder / ArkgP256

        Assert.Equal(-1, reader.ReadInt32());
        Assert.Equal(kh, reader.ReadByteString());

        Assert.Equal(-2, reader.ReadInt32());
        Assert.Equal(ctx, reader.ReadByteString());

        reader.ReadEndMap();
        Assert.Equal(CborReaderState.Finished, reader.PeekState());
    }

    [Fact]
    public void EncodeCoseSignArgs_NullArgs_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(
            () => PreviewSignCbor.EncodeCoseSignArgs(null!));

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

    /// <summary>
    /// Constructs a deterministic 81-byte ARKG-P256 key handle fixture:
    /// a 16-byte tag (filled with <paramref name="tagPattern"/>) followed by a 65-byte
    /// SEC1 uncompressed P-256 point (leading 0x04, then 64 bytes of <paramref name="pubKeyPattern"/>).
    /// </summary>
    /// <remarks>
    /// Bytes are NOT cryptographically valid — that's intentional. Encoder correctness does not
    /// depend on the bytes representing a real ARKG ciphertext + EC point; the on-the-wire shape
    /// is what we're asserting. See PRD §5.1 / §7-7.
    /// </remarks>
    private static byte[] BuildArkgKeyHandleFixture(byte tagPattern, byte pubKeyPattern)
    {
        byte[] kh = new byte[81];
        for (int i = 0; i < 16; i++)
        {
            kh[i] = tagPattern;
        }
        kh[16] = 0x04; // SEC1 uncompressed leading byte
        for (int i = 17; i < 81; i++)
        {
            kh[i] = pubKeyPattern;
        }
        return kh;
    }

}