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
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.Ed25519)]
        [InlineData(KeyType.X25519)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        [InlineData(KeyType.RSA1024, true)]
        [InlineData(KeyType.RSA2048, true)]
        [InlineData(KeyType.RSA3072, true)]
        [InlineData(KeyType.RSA4096, true)]
        [InlineData(KeyType.X25519, true)]
        [InlineData(KeyType.Ed25519, true)]
        [InlineData(KeyType.ECP256, true)]
        [InlineData(KeyType.ECP384, true)]
        public void SimpleGenerate(
            KeyType expectedAlgorithm,
            bool useScp03 = false)
        {
            using var pivSession = useScp03
                ? GetSessionScp()
                : GetSession();

            var result = pivSession.GenerateKeyPair(PivSlot.Retired12, expectedAlgorithm);
            Assert.Equal(expectedAlgorithm, result.KeyType);
        }
    }
}
