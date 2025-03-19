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

public class Curve25519PublicKeyParametersTests
{
    [Fact]
    public void CreateFromValue_CreatesInstance()
    {
        foreach (var keyType in (KeyType[])[KeyType.X25519, KeyType.Ed25519])
        {
            // Arrange
            var testKey = TestKeys.GetTestPublicKey(keyType);
            var publicKey = testKey.GetPublicPoint();

            // Act
            var publicKeyParams = Curve25519PublicKeyParameters.CreateFromValue(publicKey, keyType); 

            // Assert
            Assert.NotNull(publicKeyParams);
            Assert.Equal(publicKey, publicKeyParams.PublicPoint);
            Assert.Equal(testKey.GetKeyDefinition(), publicKeyParams.KeyDefinition);
        }
    }
        
    [Fact]
    public void CreateFromPkcs8_CreatesInstance()
    {
        foreach (var keyType in (KeyType[])[KeyType.X25519, KeyType.Ed25519])
        {
            // Arrange
            var testKey = TestKeys.GetTestPublicKey(keyType);
            var publicKey = testKey.GetPublicPoint();

            // Act
            var publicKeyParams = Curve25519PublicKeyParameters.CreateFromPkcs8(testKey.EncodedKey); 

            // Assert
            Assert.NotNull(publicKeyParams);
            Assert.Equal(publicKey, publicKeyParams.PublicPoint);
            Assert.Equal(testKey.GetKeyDefinition(), publicKeyParams.KeyDefinition);
        }
    }
}
