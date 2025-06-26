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
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class RecoverPinOnlyTests : PivSessionIntegrationTestBase
    {
        [Fact]
        public void NotPinOnly_Recover_ReturnsNone()
        {
            using var pivSession = GetSession(authenticate: false);
            var mode = pivSession.TryRecoverPinOnlyMode();

            Assert.Equal(PivPinOnlyMode.None, mode);
        }

        [Fact]
        public void PinProtected_OverwriteAdmin_CanRecover()
        {
            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);
                using var adminData = pivSession.ReadObject<AdminData>();

                Assert.True(adminData.PinProtected);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var nonAdminData = new ReadOnlyMemory<byte>(new byte[]
                {
                    0x53, 0x0A, 0x04, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
                });

                pivSession.AuthenticateManagementKey();
                var putDataCmd = new PutDataCommand(0x005FFF00, nonAdminData);
                var putDataRsp = pivSession.Connection.SendCommand(putDataCmd);

                Assert.Equal(ResponseStatus.Success, putDataRsp.Status);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var mode = pivSession.GetPinOnlyMode();
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable));
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable));
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinProtected));
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinDerived));
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.GenerateKeyPair(
                    PivSlot.Authentication, KeyType.ECP256));
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var mode = pivSession.TryRecoverPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected, mode);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
                mode = pivSession.GetPinOnlyMode();
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinProtected));
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinDerived));
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var publicKey = pivSession.GenerateKeyPair(0x86, KeyType.ECP256);
                Assert.Equal(KeyType.ECP256, publicKey.KeyType);
            }
        }


        [Fact]
        public void PinProtectedAndDerived_OverwritePrinted_CanRecover()
        {
            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived);
                using var adminData = pivSession.ReadObject<AdminData>();
                Assert.True(adminData.PinProtected);
                _ = Assert.NotNull(adminData.Salt);
            }

            using (var pivSession = GetSession(authenticate: true))
            {
                var nonPrintedData = new ReadOnlyMemory<byte>(new byte[]
                {
                    0x53, 0x0A, 0x04, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
                });

                var putDataCmd = new PutDataCommand((int)PivDataTag.Printed, nonPrintedData);
                var putDataRsp = pivSession.Connection.SendCommand(putDataCmd);

                Assert.Equal(ResponseStatus.Success, putDataRsp.Status);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var mode = pivSession.GetPinOnlyMode();
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable));
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable));
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinProtected));
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinDerived));
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var publicKey = pivSession.GenerateKeyPair(0x87, KeyType.ECP256);
                Assert.Equal(KeyType.ECP256, publicKey.KeyType);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var mode = pivSession.TryRecoverPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, mode);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
                mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, mode);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                using var pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
            }
        }
    }
}
