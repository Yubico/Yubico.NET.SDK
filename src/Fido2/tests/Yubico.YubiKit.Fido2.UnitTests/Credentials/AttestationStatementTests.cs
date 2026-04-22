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
    public void DecodeWithRawData_PopulatesRawData()
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
        var statement = AttestationStatement.DecodeWithRawData(rawCbor);

        // Assert
        Assert.NotNull(statement);
        Assert.Equal(-7, statement.Algorithm);
        Assert.NotNull(statement.Signature);
        Assert.Equal(4, statement.Signature.Value.Length);

        // Critical assertion: RawData should now be populated
        Assert.False(statement.RawData.IsEmpty);
        Assert.Equal(rawCbor.Length, statement.RawData.Length);
        Assert.True(rawCbor.AsSpan().SequenceEqual(statement.RawData.Span));
    }

    [Fact]
    public void Decode_LegacyPath_HasEmptyRawData()
    {
        // Arrange - Same CBOR but via the legacy reader-based Decode
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);

        writer.WriteTextString("alg");
        writer.WriteInt32(-7);

        writer.WriteTextString("sig");
        writer.WriteByteString([0x01, 0x02, 0x03, 0x04]);

        writer.WriteEndMap();

        var rawCbor = writer.Encode();
        var reader = new CborReader(rawCbor, CborConformanceMode.Lax);

        // Act
        var statement = AttestationStatement.Decode(reader);

        // Assert
        Assert.NotNull(statement);
        Assert.Equal(-7, statement.Algorithm);

        // Legacy path doesn't capture RawData
        Assert.True(statement.RawData.IsEmpty);
    }

    [Fact]
    public void DecodeWithRawData_NoneAttestation_PopulatesRawData()
    {
        // Arrange - Empty map for "none" attestation
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(0);
        writer.WriteEndMap();

        var rawCbor = writer.Encode();

        // Act
        var statement = AttestationStatement.DecodeWithRawData(rawCbor);

        // Assert
        Assert.NotNull(statement);
        Assert.True(statement.IsNone);
        Assert.False(statement.RawData.IsEmpty);
        Assert.True(rawCbor.AsSpan().SequenceEqual(statement.RawData.Span));
    }
}
