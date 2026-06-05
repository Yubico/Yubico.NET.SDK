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

public class AaguidTests
{
    [Fact]
    public void RoundTrip_FromGuid_ToBytes_ToGuid()
    {
        // Arrange
        Guid originalGuid = Guid.NewGuid();

        // Act
        Aaguid aaguid = new(originalGuid);
        Guid roundTripGuid = aaguid.Value;

        // Assert
        roundTripGuid.Should().Be(originalGuid);
    }

    [Fact]
    public void Equality_TreatsSameBytesAsEqual()
    {
        // Arrange
        byte[] bytes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        Aaguid aaguid1 = new(bytes);
        Aaguid aaguid2 = new(bytes);

        // Assert
        (aaguid1 == aaguid2).Should().BeTrue();
        aaguid1.Equals(aaguid2).Should().BeTrue();
        aaguid1.GetHashCode().Should().Be(aaguid2.GetHashCode());
    }

    [Fact]
    public void ToString_FormatsAsHyphenatedHex()
    {
        // Arrange - Create a known AAGUID from a known GUID
        Guid guid = new("01020304-0506-0708-090a-0b0c0d0e0f10");
        Aaguid aaguid = new(guid);

        // Assert
        string formatted = aaguid.ToString();
        formatted.Should().Be("01020304-0506-0708-090a-0b0c0d0e0f10");
    }

    [Fact]
    public void Constructor_WithBytes_StoresExactCopy()
    {
        // Arrange
        byte[] originalBytes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

        // Act
        Aaguid aaguid = new(originalBytes);
        byte[] retrievedBytes = aaguid.AsSpan().ToArray();

        // Assert
        retrievedBytes.Should().BeEquivalentTo(originalBytes);
    }

    [Fact]
    public void Constructor_WithInvalidLength_ThrowsArgumentException()
    {
        // Arrange
        byte[] tooShort = [1, 2, 3];
        byte[] tooLong = new byte[20];

        // Act & Assert
        Action actShort = () => new Aaguid(tooShort);
        Action actLong = () => new Aaguid(tooLong);

        actShort.Should().Throw<ArgumentException>().WithMessage("AAGUID must be exactly 16 bytes.*");
        actLong.Should().Throw<ArgumentException>().WithMessage("AAGUID must be exactly 16 bytes.*");
    }
}
