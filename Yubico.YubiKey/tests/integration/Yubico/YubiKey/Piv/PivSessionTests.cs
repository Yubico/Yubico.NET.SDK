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

using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyPin(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                bool isVerified = pivSession.TryVerifyPin();

                Assert.True(isVerified);
                Assert.True(pivSession.PinVerified);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyPin_WrongPin(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                collectorObj.KeyFlag = 1;
                bool isVerified = pivSession.TryVerifyPin();

                Assert.False(isVerified);
                Assert.False(pivSession.PinVerified);

                collectorObj.KeyFlag = 0;
                isVerified = pivSession.TryVerifyPin();

                Assert.True(isVerified);
                Assert.True(pivSession.PinVerified);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void AuthenticateMgmtKey_Single(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                bool isAuthenticated = pivSession.TryAuthenticateManagementKey(false);

                Assert.True(isAuthenticated);
                Assert.Equal(
                    AuthenticateManagementKeyResult.SingleAuthenticated, pivSession.ManagementKeyAuthenticationResult);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void AuthenticateMgmtKey_Mutual(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                bool isAuthenticated = pivSession.TryAuthenticateManagementKey();

                Assert.True(isAuthenticated);
                Assert.Equal(
                    AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangeMgmtKey(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                // This should fail, the mgmt key is not authenticated.
                var genPairCommand = new GenerateKeyPairCommand(
                    0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
                GenerateKeyPairResponse genPairResponse =
                    pivSession.Connection.SendCommand(genPairCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);

                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
                bool isChanged = pivSession.TryChangeManagementKey();

                Assert.True(isChanged);
                Assert.Equal(
                    AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);

                genPairCommand = new GenerateKeyPairCommand(
                    0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
                genPairResponse = pivSession.Connection.SendCommand(genPairCommand);

                collectorObj.KeyFlag = 1;
                isChanged = pivSession.TryChangeManagementKey();

                Assert.True(isChanged);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void Auth_ThenWrongKey(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                // This should fail, the mgmt key is not authenticated.
                var genPairCommand = new GenerateKeyPairCommand(
                    0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
                GenerateKeyPairResponse genPairResponse =
                    pivSession.Connection.SendCommand(genPairCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);

                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
                bool isAuthenticated = pivSession.TryAuthenticateManagementKey();
                Assert.True(isAuthenticated);
                Assert.Equal(
                    AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);

                genPairCommand = new GenerateKeyPairCommand(
                    0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
                genPairResponse = pivSession.Connection.SendCommand(genPairCommand);
                Assert.Equal(ResponseStatus.Success, genPairResponse.Status);

                collectorObj.KeyFlag = 1;
                isAuthenticated = pivSession.TryAuthenticateManagementKey(false);

                Assert.False(isAuthenticated);
                Assert.Equal(
                    AuthenticateManagementKeyResult.SingleAuthenticationFailed,
                    pivSession.ManagementKeyAuthenticationResult);

                genPairCommand = new GenerateKeyPairCommand(
                    0x87, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
                genPairResponse = pivSession.Connection.SendCommand(genPairCommand);

                Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePin(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                bool isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                isValid = TryGenerate(pivSession, 0x86, ResponseStatus.Success);
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                bool isValid = TrySign(pivSession, 0x86, ResponseStatus.AuthenticationRequired);
                Assert.True(isValid);

                bool isChanged = pivSession.TryChangePin();
                Assert.True(isChanged);
                Assert.False(pivSession.PinVerified);

                isValid = TrySign(pivSession, 0x86, ResponseStatus.AuthenticationRequired);
                Assert.True(isValid);

                collectorObj.KeyFlag = 1;
                bool isVerified = pivSession.TryVerifyPin();
                Assert.True(isVerified);

                isValid = TrySign(pivSession, 0x86, ResponseStatus.Success);
                Assert.True(isValid);

                collectorObj.KeyFlag = 1;
                isChanged = pivSession.TryChangePin();
                Assert.True(isChanged);
                Assert.True(pivSession.PinVerified);
                isVerified = pivSession.TryVerifyPin();
                Assert.True(isVerified);

                isValid = TrySign(pivSession, 0x86, ResponseStatus.Success);
                Assert.True(isValid);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePuk(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                bool isChanged = pivSession.TryChangePuk();
                Assert.True(isChanged);

                collectorObj.KeyFlag = 1;
                isChanged = pivSession.TryChangePuk();
                Assert.True(isChanged);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ResetPin(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                bool isChanged = pivSession.TryResetPin();
                Assert.True(isChanged);

                isChanged = pivSession.TryChangePuk();
                Assert.True(isChanged);

                collectorObj.KeyFlag = 1;
                isChanged = pivSession.TryResetPin();
                Assert.True(isChanged);

                collectorObj.KeyFlag = 1;
                isChanged = pivSession.TryChangePuk();
                Assert.True(isChanged);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ResetPin_WrongPuk(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                collectorObj.KeyFlag = 1;
                bool isChanged = pivSession.TryResetPin();
                Assert.False(isChanged);

                collectorObj.KeyFlag = 0;
                isChanged = pivSession.TryResetPin();
                Assert.True(isChanged);

                collectorObj.KeyFlag = 0;
                isChanged = pivSession.TryChangePuk();
                Assert.True(isChanged);

                collectorObj.KeyFlag = 1;
                isChanged = pivSession.TryResetPin();
                Assert.True(isChanged);

                collectorObj.KeyFlag = 1;
                isChanged = pivSession.TryChangePuk();
                Assert.True(isChanged);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ResetPiv(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

                bool isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                isValid = TryGenerate(pivSession, 0x86, ResponseStatus.Success);
                Assert.True(isValid);

                bool isVerified = pivSession.TryVerifyPin();
                Assert.True(isVerified);

                isValid = TrySign(pivSession, 0x86, ResponseStatus.Success);
                Assert.True(isValid);

                bool isChanged = pivSession.TryChangePin();
                Assert.True(isChanged);
                Assert.True(pivSession.PinVerified);

                pivSession.ResetApplication();

                Assert.Equal(
                    AuthenticateManagementKeyResult.Unauthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.False(pivSession.PinVerified);

                isValid = TryGenerate(pivSession, 0x87, ResponseStatus.AuthenticationRequired);
                Assert.True(isValid);

                isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                isValid = TryGenerate(pivSession, 0x87, ResponseStatus.Success);
                Assert.True(isValid);

                isValid = TrySign(pivSession, 0x87, ResponseStatus.AuthenticationRequired);
                Assert.True(isValid);

                collectorObj.KeyFlag = 1;
                isVerified = pivSession.TryVerifyPin();
                Assert.False(isVerified);

                collectorObj.KeyFlag = 0;
                isVerified = pivSession.TryVerifyPin();
                Assert.True(isVerified);

                isValid = TrySign(pivSession, 0x86, ResponseStatus.Failed);
                Assert.True(isValid);

                isValid = TrySign(pivSession, 0x87, ResponseStatus.Success);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void FixedBytes_Replace()
        {
            byte[] fixedBytes = new byte[] {
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48
            };

            var replacement = RandomObjectUtility.SetRandomProviderFixedBytes(fixedBytes);

            try
            {
                RandomNumberGenerator random = CryptographyProviders.RngCreator();

                byte[] randomBytes = new byte[32];
                random.GetBytes(randomBytes);
                bool compareResult = randomBytes.SequenceEqual(fixedBytes);
                Assert.True(compareResult);
            }
            finally
            {
                replacement.RestoreRandomProvider();
            }
        }

        private static bool TryGenerate(PivSession pivSession, byte slotNumber, ResponseStatus expectedStatus)
        {
            var genPairCommand = new GenerateKeyPairCommand(
                slotNumber, PivAlgorithm.EccP256, PivPinPolicy.Always, PivTouchPolicy.Never);
            GenerateKeyPairResponse genPairResponse = pivSession.Connection.SendCommand(genPairCommand);

            return genPairResponse.Status == expectedStatus;
        }

        private static bool TrySign(PivSession pivSession, byte slotNumber, ResponseStatus expectedStatus)
        {
            byte[] dataToSign = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            var signCommand = new AuthenticateSignCommand(dataToSign, slotNumber);
            AuthenticateSignResponse signResponse = pivSession.Connection.SendCommand(signCommand);

            return signResponse.Status == expectedStatus;
        }
    }
}
