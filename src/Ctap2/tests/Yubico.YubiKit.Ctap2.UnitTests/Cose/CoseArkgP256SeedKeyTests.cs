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
using Yubico.YubiKit.Ctap2.Cose;

namespace Yubico.YubiKit.Ctap2.UnitTests.Cose;

/// <summary>
/// Tests for <see cref="CoseArkgP256SeedKey"/> CBOR decoding and encoding.
/// </summary>
/// <remarks>
/// Fixed public points test the experimental SDK's wrapped-map profile and label routing, not
/// draft-wide conformance, production cryptography or device interoperability.
/// </remarks>
[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public class CoseArkgP256SeedKeyTests
{
    // Same public seed points as the ARKG derivation vector in ArkgP256GeneratedKeyTests.
    private static readonly byte[] TestKemX = Convert.FromHexString("c38bbdd7286196733fa177e43b73cfd3d6d72cd11cc0bb2c9236cf85a42dcff5");
    private static readonly byte[] TestKemY = Convert.FromHexString("dfa339c1e07dfcdfda8d7be2a5a3c7382991f387dfe332b1dd8da6e0622cfb35");
    private static readonly byte[] TestBlX = Convert.FromHexString("6d3bdf31d0db48988f16d47048fdd24123cd286e42d0512daa9f726b4ecf18df");
    private static readonly byte[] TestBlY = Convert.FromHexString("65ed42169c69675f936ff7de5f9bd93adbc8ea73036b16e8d90adbfabdaddba7");

    /// <summary>
    /// Builds a nested COSE_Key EC2 map: {1:2, 3:-7, -1:1, -2:x, -3:y}
    /// </summary>
    private static byte[] BuildNestedEc2Key(byte[] x, byte[] y, int curve = 1)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);
        // Numeric insertion order; the canonical writer sorts the map to 1, 3, -1, -2, -3.
        writer.WriteInt32(-3);
        writer.WriteByteString(y);
        writer.WriteInt32(-2);
        writer.WriteByteString(x);
        writer.WriteInt32(-1);
        writer.WriteInt32(curve);
        writer.WriteInt32(1);
        writer.WriteInt32(2); // kty = 2 (EC2)
        writer.WriteInt32(3);
        writer.WriteInt32(-7); // ES256 alg
        writer.WriteEndMap();
        return writer.Encode();
    }

    [Fact]
    public void Decode_WithValidArkgSeedKey_ReturnsCorrectVariant()
    {
        // SDK-profile fixture: kty2/alg-65700, -3 derived alg, -2 bstr(KEM map), -1 bstr(BL map).
        // ARKG-10 agrees on seed alg=-65700, BL/KEM labels and example derived alg=-9, but uses
        // kty=-65537/direct maps and permits optional int/tstr dkalg instead of requiring an integer.
        var kemNested = BuildNestedEc2Key(TestKemX, TestKemY);
        var blNested = BuildNestedEc2Key(TestBlX, TestBlY);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);
        // Numeric insertion order; the canonical writer sorts the map to 1, 3, -1, -2, -3.
        writer.WriteInt32(-3);
        writer.WriteInt32(-9); // Derived key alg (Esp256)
        writer.WriteInt32(-2);
        writer.WriteByteString(kemNested); // -2 = KEM; SDK wrapper contains the encoded map.
        writer.WriteInt32(-1);
        writer.WriteByteString(blNested);  // -1 = BL; SDK wrapper contains the encoded map.
        writer.WriteInt32(1);  // kty = 2
        writer.WriteInt32(2);
        writer.WriteInt32(3);  // alg = -65700
        writer.WriteInt32(-65700);
        writer.WriteEndMap();
        var cborData = writer.Encode();

        // Act
        var decoded = CoseKey.Decode(cborData);

        // Assert
        Assert.IsType<CoseArkgP256SeedKey>(decoded);
        var arkgKey = (CoseArkgP256SeedKey)decoded;
        Assert.Equal(2, arkgKey.KeyType);
        Assert.Equal(new CoseAlgorithm(-65700), arkgKey.Algorithm);
        Assert.Equal(new CoseAlgorithm(-9), arkgKey.DerivedKeyAlgorithm);

        // Verify reconstructed SEC1 points (0x04 || x || y)
        Assert.Equal(65, arkgKey.KemPublicKey.Length);
        Assert.Equal(65, arkgKey.BlPublicKey.Length);
        Assert.Equal(0x04, arkgKey.KemPublicKey.Span[0]);
        Assert.Equal(0x04, arkgKey.BlPublicKey.Span[0]);

        // Verify coordinates match (accounting for possible zero-padding)
        Assert.Equal(TestKemX, arkgKey.KemPublicKey.Slice(1, 32).ToArray());
        Assert.Equal(TestKemY, arkgKey.KemPublicKey.Slice(33, 32).ToArray());
        Assert.Equal(TestBlX, arkgKey.BlPublicKey.Slice(1, 32).ToArray());
        Assert.Equal(TestBlY, arkgKey.BlPublicKey.Slice(33, 32).ToArray());
    }

    [Fact]
    public void Encode_RoundTrip_ProducesCanonicalCbor()
    {
        // Arrange: build 65-byte SEC1 points from test coordinates
        byte[] kemSec1 = new byte[65];
        byte[] blSec1 = new byte[65];
        kemSec1[0] = 0x04;
        blSec1[0] = 0x04;
        TestKemX.CopyTo(kemSec1, 1);
        TestKemY.CopyTo(kemSec1, 33);
        TestBlX.CopyTo(blSec1, 1);
        TestBlY.CopyTo(blSec1, 33);

        var original = new CoseArkgP256SeedKey(
            Algorithm: new CoseAlgorithm(-65700),
            DerivedKeyAlgorithm: new CoseAlgorithm(-9),
            KemPublicKey: kemSec1,
            BlPublicKey: blSec1);

        // Act
        var encoded = original.Encode();
        var decoded = CoseKey.Decode(encoded);

        // Assert
        Assert.IsType<CoseArkgP256SeedKey>(decoded);
        var roundtripped = (CoseArkgP256SeedKey)decoded;
        Assert.Equal(original.KeyType, roundtripped.KeyType);
        Assert.Equal(original.Algorithm, roundtripped.Algorithm);
        Assert.Equal(original.DerivedKeyAlgorithm, roundtripped.DerivedKeyAlgorithm);
        Assert.Equal(original.KemPublicKey.ToArray(), roundtripped.KemPublicKey.ToArray());
        Assert.Equal(original.BlPublicKey.ToArray(), roundtripped.BlPublicKey.ToArray());
    }

    /// <summary>
    /// Keeps the distinct ARKG-10 §5.1 public-seed derivation roles mapped correctly:
    ///   -1 = pkBl  (blinding public key)
    ///   -2 = pkKem (KEM public key)
    /// This test builds the SDK wrapped-map profile with two distinguishable P-256 keys
    /// and asserts that the decoder routes them to <c>BlPublicKey</c> and <c>KemPublicKey</c>
    /// respectively. Swapping these roles changes downstream derivation inputs. The test does not
    /// execute previewSign or establish firmware behavior or full Internet-Draft conformance.
    /// ARKG-10 is work in progress with stated expiry 31 August 2026, not a production standard.
    /// </summary>
    [Fact]
    public void Decode_pkBlAtMinus1_pkKemAtMinus2_PerSpec()
    {
        // Arrange: nested EC2 keys with distinguishable coordinates
        var blNested = BuildNestedEc2Key(TestBlX, TestBlY);
        var kemNested = BuildNestedEc2Key(TestKemX, TestKemY);

        // Label routing: -1 = BL, -2 = KEM; the byte-string wrappers are the SDK profile.
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);
        writer.WriteInt32(-3);
        writer.WriteInt32(-9); // derived alg
        writer.WriteInt32(-2);
        writer.WriteByteString(kemNested); // -2 = KEM, wrapped encoded map
        writer.WriteInt32(-1);
        writer.WriteByteString(blNested);  // -1 = BL, wrapped encoded map
        writer.WriteInt32(1);
        writer.WriteInt32(2);   // kty
        writer.WriteInt32(3);
        writer.WriteInt32(-65700); // alg = ARKG-P256 seed-key
        writer.WriteEndMap();

        // Act
        var decoded = (CoseArkgP256SeedKey)CoseKey.Decode(writer.Encode());

        // Assert — modern routes -1 to BlPublicKey and -2 to KemPublicKey
        Assert.Equal(TestBlX, decoded.BlPublicKey.Slice(1, 32).ToArray());
        Assert.Equal(TestBlY, decoded.BlPublicKey.Slice(33, 32).ToArray());
        Assert.Equal(TestKemX, decoded.KemPublicKey.Slice(1, 32).ToArray());
        Assert.Equal(TestKemY, decoded.KemPublicKey.Slice(33, 32).ToArray());
    }

    [Theory]
    [InlineData(2, 32)] // wrong curve with otherwise valid coordinates
    [InlineData(1, 31)] // short coordinate
    [InlineData(1, 0)]  // use the full-width off-curve (0,0) point
    public void Decode_RejectsInvalidNestedPoint(int curve, int xLength)
    {
        byte[] x = xLength == 0 ? new byte[32] : TestKemX[..xLength];
        byte[] y = xLength == 0 ? new byte[32] : TestKemY;
        var fields = new Dictionary<int, object?>
        {
            [-1] = BuildNestedEc2Key(TestBlX, TestBlY),
            [-2] = BuildNestedEc2Key(x, y, curve),
            [-3] = -9,
        };

        Assert.Throws<InvalidOperationException>(() => CoseArkgP256SeedKey.Decode(fields, new CoseAlgorithm(-65700)));
    }

    [Fact]
    public void Decode_WithStandardEc2Key_ReturnsEc2Variant()
    {
        // Arrange: standard EC2 key {1:2, 3:-7, -1:1, -2:X, -3:Y}
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);
        writer.WriteInt32(-3);
        writer.WriteByteString(new byte[32]); // Y
        writer.WriteInt32(-2);
        writer.WriteByteString(new byte[32]); // X
        writer.WriteInt32(-1);
        writer.WriteInt32(1); // curve identifier (NOT 65-byte point)
        writer.WriteInt32(1);
        writer.WriteInt32(2);
        writer.WriteInt32(3);
        writer.WriteInt32(-7); // ES256 alg
        writer.WriteEndMap();
        var cborData = writer.Encode();

        // Act
        var decoded = CoseKey.Decode(cborData);

        // Assert - should dispatch to CoseEc2Key, not CoseArkgP256SeedKey
        Assert.IsType<CoseEc2Key>(decoded);
    }

    [Fact]
    public void Decode_WithArkgAlgorithmAndNonEc2KeyType_UsesArkgVariant()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);
        writer.WriteInt32(-3);
        writer.WriteInt32(-9);
        writer.WriteInt32(-2);
        writer.WriteByteString(BuildNestedEc2Key(TestKemX, TestKemY));
        writer.WriteInt32(-1);
        writer.WriteByteString(BuildNestedEc2Key(TestBlX, TestBlY));
        writer.WriteInt32(1);
        writer.WriteInt32(99);
        writer.WriteInt32(3);
        writer.WriteInt32(-65700);
        writer.WriteEndMap();

        Assert.IsType<CoseArkgP256SeedKey>(CoseKey.Decode(writer.Encode()));
    }
}