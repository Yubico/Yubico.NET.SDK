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
using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.Fido2.UnitTests.Credentials;

public class AttestationStatementTests
{
    [Fact]
    public void Decode_PackedAttestation_PopulatesRawData()
    {
        // Arrange - Construct a minimal packed attestation statement CBOR map
        // Map with keys: "alg" => -7 (ES256), "sig" => dummy signature bytes
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);

        writer.WriteTextString("alg");
        writer.WriteInt32(-7);

        writer.WriteTextString("sig");
        writer.WriteByteString([0x01, 0x02, 0x03, 0x04]);

        writer.WriteEndMap();

        var rawCbor = writer.Encode();

        // Act
        var statement = AttestationStatement.Decode(AttestationFormat.Packed, rawCbor);

        // Assert
        Assert.NotNull(statement);
        Assert.IsType<PackedAttestationStatement>(statement);

        var packed = (PackedAttestationStatement)statement;
        Assert.Equal(-7, packed.Algorithm);
        Assert.False(packed.Signature.IsEmpty);
        Assert.Equal(4, packed.Signature.Length);

        // Critical assertion: RawCbor should be populated
        Assert.False(packed.RawCbor.IsEmpty);
        Assert.Equal(rawCbor.Length, packed.RawCbor.Length);
        Assert.True(rawCbor.AsSpan().SequenceEqual(packed.RawCbor.Span));
    }

    [Fact]
    public void Decode_FidoU2F_ParsesCorrectly()
    {
        // Arrange - FIDO U2F attestation statement with sig and x5c
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);

        writer.WriteTextString("sig");
        writer.WriteByteString([0x01, 0x02, 0x03, 0x04]);

        writer.WriteTextString("x5c");
        writer.WriteStartArray(1);
        writer.WriteByteString([0xAA, 0xBB, 0xCC]);
        writer.WriteEndArray();

        writer.WriteEndMap();

        var rawCbor = writer.Encode();

        // Act
        var statement = AttestationStatement.Decode(AttestationFormat.FidoU2F, rawCbor);

        // Assert
        Assert.NotNull(statement);
        Assert.IsType<FidoU2FAttestationStatement>(statement);

        var fidoU2F = (FidoU2FAttestationStatement)statement;
        Assert.Equal(4, fidoU2F.Signature.Length);
        Assert.Single(fidoU2F.X5c);
        Assert.Equal(3, fidoU2F.X5c[0].Length);
    }

    [Fact]
    public void Decode_NoneAttestation_PopulatesRawData()
    {
        // Arrange - Empty map for "none" attestation
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(0);
        writer.WriteEndMap();

        var rawCbor = writer.Encode();

        // Act
        var statement = AttestationStatement.Decode(AttestationFormat.None, rawCbor);

        // Assert
        Assert.NotNull(statement);
        Assert.IsType<NoneAttestationStatement>(statement);

        var none = (NoneAttestationStatement)statement;
        Assert.False(none.RawCbor.IsEmpty);
        Assert.True(rawCbor.AsSpan().SequenceEqual(none.RawCbor.Span));
    }
}
