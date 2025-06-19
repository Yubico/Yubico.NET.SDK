// Copyright 2021 Yubico AB
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
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class GenerateTests : PivSessionIntegrationTestBase
    {
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
        [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5)]
        [InlineData(KeyType.X25519, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP256, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, StandardTestDevice.Fw5)]
        
        [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5Fips)]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5Fips)]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5Fips)]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5Fips)]
        [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5Fips)]
        [InlineData(KeyType.X25519, StandardTestDevice.Fw5Fips)]
        [InlineData(KeyType.ECP256, StandardTestDevice.Fw5Fips)]
        [InlineData(KeyType.ECP384, StandardTestDevice.Fw5Fips)]
        
        [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5Fips, true),]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5Fips, true),]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5Fips, true),]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5Fips, true),]
        [InlineData(KeyType.X25519, StandardTestDevice.Fw5Fips, true),]
        [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5Fips, true),]
        [InlineData(KeyType.ECP256, StandardTestDevice.Fw5Fips, true),]
        [InlineData(KeyType.ECP384, StandardTestDevice.Fw5Fips, true),]
        public void SimpleGenerate(
            KeyType keyType,
            StandardTestDevice deviceType,
            bool useScp03 = false)
        {
            using var pivSession = useScp03
                ? GetSessionScp()
                : GetSession();

            var pinPolicy = PivPinPolicy.Never;
            var touchPolicy = PivTouchPolicy.Never;
            if (deviceType == StandardTestDevice.Fw5Fips)
            {
                FipsTestUtilities.SetFipsApprovedCredentials(Session);
                pinPolicy = PivPinPolicy.Always;
                touchPolicy = PivTouchPolicy.Always;
            }

            var result = Session.GenerateKeyPair(PivSlot.Retired12, keyType, pinPolicy, touchPolicy);

            Assert.Equal(keyType, result.KeyType);
        }
    }
}
