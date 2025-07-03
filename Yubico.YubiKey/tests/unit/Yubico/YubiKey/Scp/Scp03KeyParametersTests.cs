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

using System.Linq;
using Xunit;

namespace Yubico.YubiKey.Scp
{
    public class Scp03KeyParametersTests
    {
        [Fact]
        public void DefaultKey_ReturnsValidInstance()
        {
            // Act
            var keyParams = Scp03KeyParameters.DefaultKey;

            // Assert
            Assert.NotNull(keyParams);
            Assert.NotNull(keyParams.StaticKeys);
            Assert.Equal(ScpKeyIds.Scp03, keyParams.KeyReference.Id);
            Assert.Equal(0xFF, keyParams.KeyReference.VersionNumber);
        }

        [Fact]
        public void FromStaticKeys_ValidKeys_ReturnsValidInstance()
        {
            // Arrange
            var staticKeys = new StaticKeys(
                new byte[16], // enc
                new byte[16], // mac
                new byte[16]  // dek
            );

            // Act
            var keyParams = Scp03KeyParameters.FromStaticKeys(staticKeys);

            // Assert
            Assert.NotNull(keyParams);
            Assert.True(staticKeys.AreKeysSame(keyParams.StaticKeys));
            Assert.Equal(ScpKeyIds.Scp03, keyParams.KeyReference.Id);
            Assert.Equal(0x01, keyParams.KeyReference.VersionNumber);
        }

        [Fact]
        public void Constructor_ValidParameters_SetsProperties()
        {
            // Arrange
            var staticKeys = new StaticKeys(
                new byte[16], // enc
                new byte[16], // mac
                new byte[16]  // dek
            );
            const int keyId = ScpKeyIds.Scp03;
            const int kvn = 0x02;

            // Act
            var keyParams = new Scp03KeyParameters(keyId, kvn, staticKeys);

            // Assert
            Assert.NotNull(keyParams);
            Assert.True(staticKeys.AreKeysSame(keyParams.StaticKeys));
            Assert.Equal(keyId, keyParams.KeyReference.Id);
            Assert.Equal(kvn, keyParams.KeyReference.VersionNumber);
        }

        [Fact]
        public void Dispose_DisposesStaticKeys()
        {
            // Arrange
            var staticKeys = new StaticKeys(
                new byte[16], // enc
                new byte[16], // mac
                new byte[16]  // dek
            );

            var keyParams = new Scp03KeyParameters(ScpKeyIds.Scp03, 0x01, staticKeys);

            // Act
            keyParams.Dispose();
            Assert.True(keyParams.StaticKeys.DataEncryptionKey.ToArray().All(b => b == 0));
        }

        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            // Arrange
            var keyParams = Scp03KeyParameters.DefaultKey;

            // Act & Assert - Should not throw
            keyParams.Dispose();
            keyParams.Dispose();
        }
    }
}
