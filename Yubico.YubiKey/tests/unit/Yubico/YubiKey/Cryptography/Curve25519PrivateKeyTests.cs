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

using System;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class Curve25519PrivateKeyTests
{
    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(KeyType.Ed25519);
        var privateKey = Curve25519PrivateKey.CreateFromPkcs8(testKey.EncodedKey);

        // Act
        privateKey.Dispose();

        // Assert all bytes are zero
        Assert.True(privateKey.PrivateKey.ToArray().All(b => b == 0));
    }
    
    [Fact]
    public void CreateFromValue_CreatesInstance()
    {
        foreach (var keyType in (KeyType[])[KeyType.X25519, KeyType.Ed25519])
        {
            // Arrange
            var testKey = TestKeys.GetTestPrivateKey(keyType);
            var privateKey = testKey.GetPrivateKeyValue();

            // Act
            var privateKeyParams = Curve25519PrivateKey.CreateFromValue(privateKey, keyType); 

            // Assert
            Assert.NotNull(privateKeyParams);
            Assert.Equal(privateKey, privateKeyParams.PrivateKey);
            Assert.Equal(testKey.GetKeyDefinition(), privateKeyParams.KeyDefinition);
        }
    }
        
    [Fact]
    public void CreateFromPkcs8_CreatesInstance()
    {
        foreach (var keyType in (KeyType[])[KeyType.X25519, KeyType.Ed25519])
        {
            // Arrange
            var testKey = TestKeys.GetTestPrivateKey(keyType);
            var privateKey = testKey.GetPrivateKeyValue();

            // Act
            var privateKeyParams = Curve25519PrivateKey.CreateFromPkcs8(testKey.EncodedKey); 

            // Assert
            Assert.NotNull(privateKeyParams);
            Assert.Equal(privateKey, privateKeyParams.PrivateKey);
            Assert.Equal(testKey.GetKeyDefinition(), privateKeyParams.KeyDefinition);
        }
    }
    
    [Fact]
    public void VerifyX25519Key_ValidKey_Succeeds()
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(KeyType.X25519);
        var privateKey = testKey.GetPrivateKeyValue();

        // Act
        var privateKeyParams = Curve25519PrivateKey.CreateFromValue(privateKey, KeyType.X25519);

        // Assert
        Assert.NotNull(privateKeyParams);
        Assert.Equal(KeyType.X25519, privateKeyParams.KeyType);
        Assert.Equal(privateKey, privateKeyParams.PrivateKey);
        
        // Verify key follows X25519 requirements per RFC 7748
        var keyBytes = privateKeyParams.PrivateKey.Span;
        Assert.Equal(32, keyBytes.Length);
        Assert.Equal(0, keyBytes[0] & 0b111); // 3 least significant bits should be 0
        Assert.Equal(0, keyBytes[31] & 0b_10000000); // Most significant bit should be 0
        Assert.Equal(0b_1000000, keyBytes[31] & 0b_1000000); // Second-most significant bit should be 1
    }
    
    [Fact]
    public void VerifyX25519Key_InvalidBitClamping_ThrowsException()
    {
        // Arrange - Create a key that violates bit clamping requirements
        byte[] invalidKey = new byte[32];
        Random.Shared.NextBytes(invalidKey);
        
        // Break bit clamping - set lowest 3 bits to non-zero
        invalidKey[0] |= 0b111;
        
        // Act & Assert
        Assert.Throws<CryptographicException>(() => 
            Curve25519PrivateKey.CreateFromValue(invalidKey, KeyType.X25519));
    }
    
    [Fact]
    public void VerifyX25519Key_InvalidSecondMostSignificantBit_ThrowsException()
    {
        // Arrange - Create a key that violates bit clamping requirements
        byte[] invalidKey = new byte[32];
        Random.Shared.NextBytes(invalidKey);
        
        // Make key valid first
        invalidKey[0] &= 0b11111000; // Clear 3 LSB
        invalidKey[31] &= 0b01111111; // Clear MSB
        
        // Break second-most significant bit requirement (should be 1)
        invalidKey[31] &= 0b10111111; // Set second-most significant bit to 0
        
        // Act & Assert
        Assert.Throws<CryptographicException>(() => 
            Curve25519PrivateKey.CreateFromValue(invalidKey, KeyType.X25519));
    }
}
