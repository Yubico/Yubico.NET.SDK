// Copyright 2026 Yubico AB
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

using Xunit;

namespace Yubico.YubiKit.Piv.UnitTests;

public class PivAlgorithmExtensionsTests
{
    [Theory]
    [InlineData(PivAlgorithm.Rsa1024, true)]
    [InlineData(PivAlgorithm.Rsa2048, true)]
    [InlineData(PivAlgorithm.Rsa3072, true)]
    [InlineData(PivAlgorithm.Rsa4096, true)]
    [InlineData(PivAlgorithm.EccP256, false)]
    [InlineData(PivAlgorithm.EccP384, false)]
    [InlineData(PivAlgorithm.Ed25519, false)]
    [InlineData(PivAlgorithm.X25519, false)]
    public void IsRsa_ReturnsCorrectValue(PivAlgorithm algorithm, bool expected)
    {
        Assert.Equal(expected, algorithm.IsRsa());
    }

    [Theory]
    [InlineData(PivAlgorithm.Rsa1024, false)]
    [InlineData(PivAlgorithm.Rsa2048, false)]
    [InlineData(PivAlgorithm.Rsa3072, false)]
    [InlineData(PivAlgorithm.Rsa4096, false)]
    [InlineData(PivAlgorithm.EccP256, true)]
    [InlineData(PivAlgorithm.EccP384, true)]
    [InlineData(PivAlgorithm.Ed25519, true)]
    [InlineData(PivAlgorithm.X25519, true)]
    public void IsEcc_ReturnsCorrectValue(PivAlgorithm algorithm, bool expected)
    {
        Assert.Equal(expected, algorithm.IsEcc());
    }
}

public class PivSlotMetadataExtensionsTests
{
    [Fact]
    public void GetRsaPublicKey_WithEccAlgorithm_ThrowsInvalidOperationException()
    {
        // Arrange: Create metadata with ECC algorithm
        var metadata = new PivSlotMetadata(
            Algorithm: PivAlgorithm.EccP256,
            PinPolicy: PivPinPolicy.Default,
            TouchPolicy: PivTouchPolicy.Default,
            IsGenerated: true,
            PublicKey: ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => metadata.GetRsaPublicKey());
        Assert.Contains("RSA", exception.Message);
        Assert.Contains("EccP256", exception.Message);
    }

    [Fact]
    public void GetECDsaPublicKey_WithRsaAlgorithm_ThrowsInvalidOperationException()
    {
        // Arrange: Create metadata with RSA algorithm
        var metadata = new PivSlotMetadata(
            Algorithm: PivAlgorithm.Rsa2048,
            PinPolicy: PivPinPolicy.Default,
            TouchPolicy: PivTouchPolicy.Default,
            IsGenerated: true,
            PublicKey: ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => metadata.GetECDsaPublicKey());
        Assert.Contains("ECC", exception.Message);
        Assert.Contains("Rsa2048", exception.Message);
    }

    [Theory]
    [InlineData(PivAlgorithm.Ed25519)]
    [InlineData(PivAlgorithm.X25519)]
    public void GetECDsaPublicKey_WithCurve25519Algorithm_ThrowsInvalidOperationException(PivAlgorithm algorithm)
    {
        // Arrange: Create metadata with Curve25519 algorithm
        var metadata = new PivSlotMetadata(
            Algorithm: algorithm,
            PinPolicy: PivPinPolicy.Default,
            TouchPolicy: PivTouchPolicy.Default,
            IsGenerated: true,
            PublicKey: ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => metadata.GetECDsaPublicKey());
        Assert.Contains("Curve25519", exception.Message);
        Assert.Contains(algorithm.ToString(), exception.Message);
    }
}
