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
using Yubico.YubiKit.Fido2.Cose;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Cose;

public class CoseAlgorithmTests
{
    [Fact]
    public void Esp256SplitArkgPlaceholder_Value_IsNegative65539()
    {
        // Assert
        CoseAlgorithm.Esp256SplitArkgPlaceholder.Value.Should().Be(-65539);
    }

    [Fact]
    public void Other_RoundTripsArbitraryValue()
    {
        // Arrange
        int arbitraryValue = 12345;

        // Act
        CoseAlgorithm algorithm = CoseAlgorithm.Other(arbitraryValue);

        // Assert
        algorithm.Value.Should().Be(arbitraryValue);
    }

    [Fact]
    public void IsKnown_True_ForNamedConstants()
    {
        // Assert
        CoseAlgorithm.Es256.IsKnown.Should().BeTrue();
        CoseAlgorithm.EdDsa.IsKnown.Should().BeTrue();
        CoseAlgorithm.Esp256.IsKnown.Should().BeTrue();
        CoseAlgorithm.Es384.IsKnown.Should().BeTrue();
        CoseAlgorithm.Rs256.IsKnown.Should().BeTrue();
        CoseAlgorithm.Esp256SplitArkgPlaceholder.IsKnown.Should().BeTrue();
    }

    [Fact]
    public void IsKnown_False_ForUnknownValues()
    {
        // Arrange
        CoseAlgorithm unknown = CoseAlgorithm.Other(999);

        // Assert
        unknown.IsKnown.Should().BeFalse();
    }

    [Fact]
    public void ToString_NamesKnownAlgorithms()
    {
        // Assert
        CoseAlgorithm.Es256.ToString().Should().Be("ES256");
        CoseAlgorithm.EdDsa.ToString().Should().Be("EdDSA");
        CoseAlgorithm.Esp256.ToString().Should().Be("ESP256");
        CoseAlgorithm.Es384.ToString().Should().Be("ES384");
        CoseAlgorithm.Rs256.ToString().Should().Be("RS256");
        CoseAlgorithm.Esp256SplitArkgPlaceholder.ToString().Should().Be("ESP256_SPLIT_ARKG_PLACEHOLDER");
    }

    [Fact]
    public void ToString_FormatsUnknownAsCodeWithValue()
    {
        // Arrange
        CoseAlgorithm unknown = CoseAlgorithm.Other(999);

        // Assert
        unknown.ToString().Should().Be("COSE(999)");
    }
}