// Copyright 2024 Yubico AB
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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class Curve25519PublicKeyTests
{
    [Theory]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void CreateFromValue_CreatesInstance(KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var testPublicPoint = testKey.GetPublicPoint();

        // Act
        var publicKeyParams = Curve25519PublicKey.CreateFromValue(testPublicPoint, keyType); 

        // Assert
        Assert.NotNull(publicKeyParams);
        Assert.Equal(testPublicPoint, publicKeyParams.PublicPoint);
        Assert.Equal(testKey.GetKeyDefinition(), publicKeyParams.KeyDefinition);
    }

    [Theory]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void CreateCurve25519FromPkcs8EncodedKey_WithValidKey_CreatesInstance(KeyType keyType)
    {
        // Arrange
        var testPublicKey = TestKeys.GetTestPublicKey(keyType);
            
        // Act
        var publicKeyParams = ECPublicKey.CreateFromPkcs8(testPublicKey.EncodedKey);
        var ecPublicKeyParams = publicKeyParams as Curve25519PublicKey;
        Assert.NotNull(ecPublicKeyParams);

        // Assert
        Assert.Equal(testPublicKey.GetPublicPoint(), ecPublicKeyParams.PublicPoint);
    }
    
    [Theory]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void CreateFromPkcs8_CreatesInstance(KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var publicKey = testKey.GetPublicPoint();

        // Act
        var publicKeyParams = Curve25519PublicKey.CreateFromPkcs8(testKey.EncodedKey); 

        // Assert
        Assert.NotNull(publicKeyParams);
        Assert.Equal(publicKey, publicKeyParams.PublicPoint);
        Assert.Equal(testKey.GetKeyDefinition(), publicKeyParams.KeyDefinition);
    }
}
