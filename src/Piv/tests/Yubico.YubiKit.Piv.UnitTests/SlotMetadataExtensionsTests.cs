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

using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Piv.UnitTests;

public class KeyClassificationTests
{
    [Theory]
    [InlineData(KeyType.Rsa1024, true)]
    [InlineData(KeyType.Rsa2048, true)]
    [InlineData(KeyType.Rsa3072, true)]
    [InlineData(KeyType.Rsa4096, true)]
    [InlineData(KeyType.P256, false)]
    [InlineData(KeyType.P384, false)]
    [InlineData(KeyType.Ed25519, false)]
    [InlineData(KeyType.X25519, false)]
    public void IsRsa_ReturnsCorrectValue(KeyType keyType, bool expected)
    {
        Assert.Equal(expected, keyType.IsRsa());
    }

    [Theory]
    [InlineData(KeyType.Rsa1024, false)]
    [InlineData(KeyType.Rsa2048, false)]
    [InlineData(KeyType.Rsa3072, false)]
    [InlineData(KeyType.Rsa4096, false)]
    [InlineData(KeyType.P256, true)]
    [InlineData(KeyType.P384, true)]
    [InlineData(KeyType.Ed25519, true)]
    [InlineData(KeyType.X25519, true)]
    public void IsEcc_ReturnsCorrectValue(KeyType keyType, bool expected)
    {
        Assert.Equal(expected, keyType.IsEllipticCurve());
    }
}

public class SlotMetadataExtensionsTests
{
    [Fact]
    public void GetRsaPublicKey_WithEccAlgorithm_ThrowsInvalidOperationException()
    {
        // Arrange: Create metadata with ECC algorithm
        var metadata = new SlotMetadata(
            KeyType: KeyType.P256,
            PinPolicy: PinPolicy.Default,
            TouchPolicy: TouchPolicy.Default,
            IsGenerated: true,
            PublicKey: ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => metadata.GetRsaPublicKey());
        Assert.Contains("RSA", exception.Message);
        Assert.Contains("0x11", exception.Message);
    }

    [Fact]
    public void GetECDsaPublicKey_WithRsaAlgorithm_ThrowsInvalidOperationException()
    {
        // Arrange: Create metadata with RSA algorithm
        var metadata = new SlotMetadata(
            KeyType: KeyType.Rsa2048,
            PinPolicy: PinPolicy.Default,
            TouchPolicy: TouchPolicy.Default,
            IsGenerated: true,
            PublicKey: ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => metadata.GetECDsaPublicKey());
        Assert.Contains("ECC", exception.Message);
        Assert.Contains("0x07", exception.Message);
    }

    [Theory]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void GetECDsaPublicKey_WithCurve25519Algorithm_ThrowsInvalidOperationException(KeyType keyType)
    {
        // Arrange: Create metadata with Curve25519 algorithm
        var metadata = new SlotMetadata(
            KeyType: keyType,
            PinPolicy: PinPolicy.Default,
            TouchPolicy: TouchPolicy.Default,
            IsGenerated: true,
            PublicKey: ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => metadata.GetECDsaPublicKey());
        Assert.Contains("Curve25519", exception.Message);
        Assert.Contains(keyType.ToString(), exception.Message);
    }
}