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

public class Curve25519PrivateKeyParametersTests
{
    [Fact]
    public void CreateFromValue_CreatesInstance()
    {
        foreach (var keyType in (KeyDefinitions.KeyType[])[KeyDefinitions.KeyType.X25519, KeyDefinitions.KeyType.Ed25519])
        {
            // Arrange
            var testKey = TestKeys.GetTestPrivateKey(keyType);
            var privateKey = testKey.GetPrivateKey();

            // Act
            var privateKeyParams = Curve25519PrivateKeyParameters.CreateFromValue(privateKey, keyType); 

            // Assert
            Assert.NotNull(privateKeyParams);
            Assert.Equal(privateKey, privateKeyParams.GetPrivateKey());
            Assert.Equal(testKey.GetKeyDefinition(), privateKeyParams.GetKeyDefinition());
        }
    }
        
    [Fact]
    public void CreateFromPkcs8_CreatesInstance()
    {
        foreach (var keyType in (KeyDefinitions.KeyType[])[KeyDefinitions.KeyType.X25519, KeyDefinitions.KeyType.Ed25519])
        {
            // Arrange
            var testKey = TestKeys.GetTestPrivateKey(keyType);
            var privateKey = testKey.GetPrivateKey();

            // Act
            var privateKeyParams = Curve25519PrivateKeyParameters.CreateFromPkcs8(testKey.EncodedKey); 

            // Assert
            Assert.NotNull(privateKeyParams);
            Assert.Equal(privateKey, privateKeyParams.GetPrivateKey());
            Assert.Equal(testKey.GetKeyDefinition(), privateKeyParams.GetKeyDefinition());
        }
    }
}
