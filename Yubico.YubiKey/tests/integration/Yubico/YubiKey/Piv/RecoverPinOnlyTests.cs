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
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    // All these tests will reset the PIV application, run, then reset the PIV
    // application again.
    // All these tests will also use a random number generator with a specified
    // set of bytes, followed by 2048 random bytes. If you want to get only
    // random bytes, skip the first SpecifiedStart bytes (get a random object and
    // generate that many bytes).
    public class RecoverPinOnlyTests : IDisposable
    {
        private readonly IYubiKeyDevice yubiKey;

        public RecoverPinOnlyTests()
        {
            yubiKey = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            ResetPiv(yubiKey);
        }

        public void Dispose()
        {
            ResetPiv(yubiKey);
        }

        [Fact]
        public void NotPinOnly_Recover_ReturnsNone()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                PivPinOnlyMode mode = pivSession.TryRecoverPinOnlyMode();

                Assert.Equal(PivPinOnlyMode.None, mode);
            }
        }

        [Fact]
        public void PinProtected_OverwriteAdmin_CanRecover()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);

                using AdminData adminData = pivSession.ReadObject<AdminData>();

                Assert.True(adminData.PinProtected);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var nonAdminData = new ReadOnlyMemory<byte>(new byte[]
                {
                    0x53, 0x0A, 0x04, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
                });

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.AuthenticateManagementKey();
                var putDataCmd = new PutDataCommand(0x005FFF00, nonAdminData);
                PutDataResponse putDataRsp = pivSession.Connection.SendCommand(putDataCmd);

                Assert.Equal(ResponseStatus.Success, putDataRsp.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable));
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable));
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinProtected));
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinDerived));
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                _ = Assert.Throws<OperationCanceledException>(() => pivSession.GenerateKeyPair(
                    PivSlot.Authentication, PivAlgorithm.EccP256));
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                PivPinOnlyMode mode = pivSession.TryRecoverPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected, mode);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
                mode = pivSession.GetPinOnlyMode();
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinProtected));
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinDerived));
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                PivPublicKey publicKey = pivSession.GenerateKeyPair(0x86, PivAlgorithm.EccP256);
                Assert.Equal(PivAlgorithm.EccP256, publicKey.Algorithm);
            }
        }


        [Fact]
        public void PinProtectedAndDerived_OverwritePrinted_CanRecover()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived);

                using AdminData adminData = pivSession.ReadObject<AdminData>();

                Assert.True(adminData.PinProtected);
                _ = Assert.NotNull(adminData.Salt);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var nonPrintedData = new ReadOnlyMemory<byte>(new byte[]
                {
                    0x53, 0x0A, 0x04, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
                });

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.AuthenticateManagementKey();
                var putDataCmd = new PutDataCommand((int)PivDataTag.Printed, nonPrintedData);
                PutDataResponse putDataRsp = pivSession.Connection.SendCommand(putDataCmd);

                Assert.Equal(ResponseStatus.Success, putDataRsp.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinProtectedUnavailable));
                Assert.False(mode.HasFlag(PivPinOnlyMode.PinDerivedUnavailable));
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinProtected));
                Assert.True(mode.HasFlag(PivPinOnlyMode.PinDerived));
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                PivPublicKey publicKey = pivSession.GenerateKeyPair(0x87, PivAlgorithm.EccP256);
                Assert.Equal(PivAlgorithm.EccP256, publicKey.Algorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                PivPinOnlyMode mode = pivSession.TryRecoverPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, mode);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
                mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, mode);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                using PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
            }
        }

        private static void ResetPiv(IYubiKeyDevice yubiKey)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();
            }
        }
    }
}
