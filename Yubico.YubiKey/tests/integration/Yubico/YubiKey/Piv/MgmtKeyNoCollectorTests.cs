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

using System;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class MgmtKeyNoCollectorTests : PivSessionIntegrationTestBase
{
    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void Authenticate_Succeeds(
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;
        var mgmtKey = new ReadOnlyMemory<byte>(new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        });

        var isValid = Session.TryAuthenticateManagementKey(mgmtKey);
        Assert.True(isValid);

        Assert.True(Session.ManagementKeyAuthenticated);
        Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
            Session.ManagementKeyAuthenticationResult);

        var publicKey = Session.GenerateKeyPair(0x86, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.None);
        Assert.Equal(KeyType.ECP256, publicKey.KeyType);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void ChangeKey_Succeeds(
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;
        var mgmtKey = new ReadOnlyMemory<byte>(new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        });
        var newKey = new ReadOnlyMemory<byte>(new byte[]
        {
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        });

        using (var pivSession = GetSession())
        {
            var isValid = pivSession.TryAuthenticateManagementKey(mgmtKey);
            Assert.True(isValid);
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                pivSession.ManagementKeyAuthenticationResult);
        }

        using (var pivSession = GetSession())
        {
            var isValid = pivSession.TryChangeManagementKey(mgmtKey, newKey);
            Assert.True(isValid);

            isValid = pivSession.TryAuthenticateManagementKey(newKey);
            Assert.True(isValid);
        }

        using (var pivSession = GetSession())
        {
            var isValid = pivSession.TryAuthenticateManagementKey(newKey);
            Assert.True(isValid);

            var publicKey = pivSession.GenerateKeyPair(0x87, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.None);
            Assert.Equal(KeyType.ECP256, publicKey.KeyType);
        }
    }
}
