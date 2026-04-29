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
using Yubico.YubiKit.Fido2.Cose;

namespace Yubico.YubiKit.Fido2.UnitTests.Cose;

/// <summary>
/// Tests for <see cref="CoseArkgP256SeedKey"/> CBOR decoding and encoding.
/// </summary>
public class CoseArkgP256SeedKeyTests
{
    // Test P-256 coordinates for nested COSE_Key maps
    private static readonly byte[] TestKemX = new byte[32];
    private static readonly byte[] TestKemY = new byte[32];
    private static readonly byte[] TestBlX = new byte[32];
    private static readonly byte[] TestBlY = new byte[32];

    static CoseArkgP256SeedKeyTests()
    {
        // Fill with distinct non-zero data
        for (int i = 0; i < 32; i++)
        {
            TestKemX[i] = (byte)(0x10 + i);
            TestKemY[i] = (byte)(0x30 + i);
            TestBlX[i] = (byte)(0x20 + i);
            TestBlY[i] = (byte)(0x40 + i);
        }
    }

    /// <summary>
    /// Builds a nested COSE_Key EC2 map: {1:2, 3:-7, -1:1, -2:x, -3:y}
    /// </summary>
    private static byte[] BuildNestedEc2Key(byte[] x, byte[] y)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);
        // Canonical order: -3, -2, -1, 1, 3
        writer.WriteInt32(-3);
        writer.WriteByteString(y);
        writer.WriteInt32(-2);
        writer.WriteByteString(x);
        writer.WriteInt32(-1);
        writer.WriteInt32(1); // P-256 curve ID
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
        // Arrange: construct CBOR map with NESTED COSE_Key maps
        // {1:2, 3:-65700, -3:-9, -2:<BL nested map>, -1:<KEM nested map>}
        var kemNested = BuildNestedEc2Key(TestKemX, TestKemY);
        var blNested = BuildNestedEc2Key(TestBlX, TestBlY);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);
        // Canonical order: -3, -2, -1, 1, 3
        writer.WriteInt32(-3);
        writer.WriteInt32(-9); // Derived key alg (Esp256)
        writer.WriteInt32(-2);
        writer.WriteByteString(blNested);
        writer.WriteInt32(-1);
        writer.WriteByteString(kemNested);
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
}
