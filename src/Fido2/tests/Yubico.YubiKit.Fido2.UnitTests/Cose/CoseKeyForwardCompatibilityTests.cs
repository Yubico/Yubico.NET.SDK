// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Fido2.Cose;

namespace Yubico.YubiKit.Fido2.UnitTests.Cose;

public class CoseKeyForwardCompatibilityTests
{
    [Fact]
    public void Decode_UnknownKeyTypeWithArrayParameter_ReturnsOtherKeyWithRawCbor()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(-1);
        writer.WriteStartArray(2);
        writer.WriteInt32(1);
        writer.WriteTextString("vendor");
        writer.WriteEndArray();
        writer.WriteInt32(1);
        writer.WriteInt32(99);
        writer.WriteInt32(3);
        writer.WriteInt32(-70000);
        writer.WriteEndMap();
        byte[] encoded = writer.Encode();

        var key = Assert.IsType<CoseOtherKey>(CoseKey.Decode(encoded));

        Assert.Equal(99, key.KeyType);
        Assert.Equal(-70000, key.Algorithm.Value);
        Assert.Equal(encoded, key.RawCbor.ToArray());
    }

    [Fact]
    public void Decode_UnknownAlgorithmOnEc2Key_ReturnsEc2Key()
    {
        byte[] encoded = EncodeEc2Key(algorithm: -70000, curve: 1);

        var key = Assert.IsType<CoseEc2Key>(CoseKey.Decode(encoded));

        Assert.Equal(-70000, key.Algorithm.Value);
    }

    [Fact]
    public void Decode_UnknownCurveOnEc2Key_ReturnsEc2Key()
    {
        byte[] encoded = EncodeEc2Key(algorithm: -7, curve: 99);

        var key = Assert.IsType<CoseEc2Key>(CoseKey.Decode(encoded));

        Assert.Equal(99, key.Curve);
    }

    [Fact]
    public void Decode_Ec2KeyWithExtraArrayParameter_ReturnsEc2Key()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(6);
        writer.WriteInt32(-4);
        writer.WriteStartArray(2);
        writer.WriteInt32(1);
        writer.WriteInt32(2);
        writer.WriteEndArray();
        WriteEc2Parameters(writer, algorithm: -7, curve: 1);
        writer.WriteEndMap();

        Assert.IsType<CoseEc2Key>(CoseKey.Decode(writer.Encode()));
    }

    [Fact]
    public void Decode_Ec2KeyWithTextStringLabel_ReturnsEc2Key()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(6);
        WriteEc2Parameters(writer, algorithm: -7, curve: 1);
        writer.WriteTextString("vendor");
        writer.WriteStartMap(1);
        writer.WriteTextString("version");
        writer.WriteInt32(1);
        writer.WriteEndMap();
        writer.WriteEndMap();

        Assert.IsType<CoseEc2Key>(CoseKey.Decode(writer.Encode()));
    }

    [Fact]
    public void Decode_Ec2KeyWithWideIntegerLabels_ReturnsEc2Key()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(8);
        writer.WriteCborNegativeIntegerRepresentation(ulong.MaxValue);
        writer.WriteBoolean(true);
        writer.WriteInt64(long.MinValue);
        writer.WriteTextString("future-negative");
        WriteEc2Parameters(writer, algorithm: -7, curve: 1);
        writer.WriteUInt64(ulong.MaxValue);
        writer.WriteStartArray(1);
        writer.WriteTextString("future");
        writer.WriteEndArray();
        writer.WriteEndMap();

        Assert.IsType<CoseEc2Key>(CoseKey.Decode(writer.Encode()));
    }

    [Fact]
    public void Decode_MalformedCbor_Throws()
    {
        byte[] malformed = [0xA2, 0x01, 0x02, 0x03];

        Assert.Throws<CborContentException>(() => CoseKey.Decode(malformed));
    }

    [Fact]
    public void Decode_MissingKeyTypeOrAlgorithm_Throws()
    {
        var missingKeyType = new CborWriter(CborConformanceMode.Ctap2Canonical);
        missingKeyType.WriteStartMap(1);
        missingKeyType.WriteInt32(3);
        missingKeyType.WriteInt32(-7);
        missingKeyType.WriteEndMap();

        var missingAlgorithm = new CborWriter(CborConformanceMode.Ctap2Canonical);
        missingAlgorithm.WriteStartMap(1);
        missingAlgorithm.WriteInt32(1);
        missingAlgorithm.WriteInt32(2);
        missingAlgorithm.WriteEndMap();

        Assert.Throws<InvalidOperationException>(() => CoseKey.Decode(missingKeyType.Encode()));
        Assert.Throws<InvalidOperationException>(() => CoseKey.Decode(missingAlgorithm.Encode()));
    }

    [Fact]
    public void Decode_Ec2KeyMissingRequiredCoordinate_Throws()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        writer.WriteInt32(-2);
        writer.WriteByteString(new byte[32]);
        writer.WriteInt32(-1);
        writer.WriteInt32(1);
        writer.WriteInt32(1);
        writer.WriteInt32(2);
        writer.WriteInt32(3);
        writer.WriteInt32(-7);
        writer.WriteEndMap();

        Assert.Throws<InvalidOperationException>(() => CoseKey.Decode(writer.Encode()));
    }

    private static byte[] EncodeEc2Key(int algorithm, int curve)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(5);
        WriteEc2Parameters(writer, algorithm, curve);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static void WriteEc2Parameters(CborWriter writer, int algorithm, int curve)
    {
        writer.WriteInt32(-3);
        writer.WriteByteString(new byte[32]);
        writer.WriteInt32(-2);
        writer.WriteByteString(new byte[32]);
        writer.WriteInt32(-1);
        writer.WriteInt32(curve);
        writer.WriteInt32(1);
        writer.WriteInt32(2);
        writer.WriteInt32(3);
        writer.WriteInt32(algorithm);
    }
}