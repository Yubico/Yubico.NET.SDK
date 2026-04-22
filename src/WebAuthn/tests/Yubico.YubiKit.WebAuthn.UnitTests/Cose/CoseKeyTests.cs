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

using FluentAssertions;
using Yubico.YubiKit.WebAuthn.Cose;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Cose;

public class CoseKeyTests
{
    [Fact]
    public void Decode_Es256_RoundTripPreservesBytes()
    {
        // Arrange
        ReadOnlyMemory<byte> originalBytes = Fixtures.Es256Key;

        // Act
        CoseKey decoded = CoseKey.Decode(originalBytes);
        byte[] reEncoded = decoded.Encode();

        // Assert
        reEncoded.Should().BeEquivalentTo(originalBytes.ToArray());
    }

    [Fact]
    public void Decode_EdDsa_RoundTripPreservesBytes()
    {
        // Arrange
        ReadOnlyMemory<byte> originalBytes = Fixtures.EdDsaKey;

        // Act
        CoseKey decoded = CoseKey.Decode(originalBytes);
        byte[] reEncoded = decoded.Encode();

        // Assert
        reEncoded.Should().BeEquivalentTo(originalBytes.ToArray());
    }

    [Fact]
    public void Decode_Rsa_RoundTripPreservesBytes()
    {
        // Arrange
        ReadOnlyMemory<byte> originalBytes = Fixtures.RsaKey;

        // Act
        CoseKey decoded = CoseKey.Decode(originalBytes);
        byte[] reEncoded = decoded.Encode();

        // Assert
        reEncoded.Should().BeEquivalentTo(originalBytes.ToArray());
    }

    [Fact]
    public void Decode_UnknownKty_ReturnsCoseOtherKey_PreservingRawBytes()
    {
        // Arrange - Create a COSE key with unknown kty=99
        byte[] unknownKey = [
            0xA3, // map(3)
            0x01, 0x18, 0x63, // 1: kty=99
            0x03, 0x26,       // 3: alg=-7
            0x20, 0x01        // -1: some parameter=1
        ];

        // Act
        CoseKey decoded = CoseKey.Decode(unknownKey);

        // Assert
        decoded.Should().BeOfType<CoseOtherKey>();
        var other = (CoseOtherKey)decoded;
        other.KeyType.Should().Be(99);
        other.Algorithm.Value.Should().Be(-7);
        other.RawCbor.ToArray().Should().BeEquivalentTo(unknownKey);
    }

    [Fact]
    public void Decode_Es256_PopulatesAlgorithmAndCurve()
    {
        // Arrange
        ReadOnlyMemory<byte> es256Bytes = Fixtures.Es256Key;

        // Act
        CoseKey decoded = CoseKey.Decode(es256Bytes);

        // Assert
        decoded.Should().BeOfType<CoseEc2Key>();
        var ec2 = (CoseEc2Key)decoded;
        ec2.KeyType.Should().Be(2);
        ec2.Algorithm.Value.Should().Be(-7);
        ec2.Curve.Should().Be(1); // P-256
        ec2.X.Length.Should().Be(32);
        ec2.Y.Length.Should().Be(32);
    }
}
