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
        var publicKey = Curve25519PublicKey.CreateFromValue(testPublicPoint, keyType); 

        // Assert
        Assert.NotNull(publicKey);
        Assert.Equal(testPublicPoint, publicKey.PublicPoint);
        Assert.Equal(testKey.GetKeyDefinition(), publicKey.KeyDefinition);
    }

    [Theory]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void CreateCurve25519FromPkcs8EncodedKey_WithValidKey_CreatesInstance(KeyType keyType)
    {
        // Arrange
        var testPublicKey = TestKeys.GetTestPublicKey(keyType);
            
        // Act
        var publicKey = Curve25519PublicKey.CreateFromPkcs8(testPublicKey.EncodedKey);
        Assert.NotNull(publicKey);

        // Assert
        Assert.Equal(testPublicKey.GetPublicPoint(), publicKey.PublicPoint);
    }
    
    [Theory]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void CreateFromPkcs8_CreatesInstance(KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var testPublicPoint = testKey.GetPublicPoint();

        // Act
        var publicKey = Curve25519PublicKey.CreateFromPkcs8(testKey.EncodedKey); 

        // Assert
        Assert.NotNull(publicKey);
        Assert.Equal(testPublicPoint, publicKey.PublicPoint);
        Assert.Equal(testKey.GetKeyDefinition(), publicKey.KeyDefinition);
    }
}
