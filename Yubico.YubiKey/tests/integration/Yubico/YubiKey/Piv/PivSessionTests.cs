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
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionTests : IDisposable
    {
        private bool _isValid;
        private readonly IYubiKeyDevice _yubiKeyDevice;
        private readonly PivSession _pivSession;
        private readonly Simple39KeyCollector _collectorObj;
        private bool disposedValue;

        // Shared setup for every test that is run.
        public PivSessionTests()
        {
           _isValid = SelectSupport.TrySelectYubiKey(out _yubiKeyDevice);
           _pivSession = new PivSession(_yubiKeyDevice);
           _collectorObj = new Simple39KeyCollector();
        }

        // Shared cleanup for every test that is run.
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _pivSession.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Constructor_Success()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);
        }

        [Fact]
        public void VerifyPin()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;
            bool isVerified = _pivSession.TryVerifyPin();

            Assert.True(isVerified);
            Assert.True(_pivSession.PinVerified);
        }

        [Fact]
        public void VerifyPin_WrongPin()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;
            _collectorObj.KeyFlag = 1;
            bool isVerified = _pivSession.TryVerifyPin();

            Assert.False(isVerified);
            Assert.False(_pivSession.PinVerified);

            _collectorObj.KeyFlag = 0;
            isVerified = _pivSession.TryVerifyPin();

            Assert.True(isVerified);
            Assert.True(_pivSession.PinVerified);
        }

        [Fact]
        public void AuthenticateMgmtKey_Single()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;
            bool isAuthenticated = _pivSession.TryAuthenticateManagementKey(false);

            Assert.True(isAuthenticated);
            Assert.Equal(
                AuthenticateManagementKeyResult.SingleAuthenticated, _pivSession.ManagementKeyAuthenticationResult);
        }

        [Fact]
        public void AuthenticateMgmtKey_Mutual()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;
            bool isAuthenticated = _pivSession.TryAuthenticateManagementKey();

            Assert.True(isAuthenticated);
            Assert.Equal(
                AuthenticateManagementKeyResult.MutualFullyAuthenticated, _pivSession.ManagementKeyAuthenticationResult);
        }

        [Fact]
        public void ChangeMgmtKey()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            // This should fail, the mgmt key is not authenticated.
            var genPairCommand = new GenerateKeyPairCommand(
                0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            GenerateKeyPairResponse genPairResponse =
                _pivSession.Connection.SendCommand(genPairCommand);
            Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;
            bool isChanged = _pivSession.TryChangeManagementKey();

            Assert.True(isChanged);
            Assert.Equal(
                AuthenticateManagementKeyResult.MutualFullyAuthenticated, _pivSession.ManagementKeyAuthenticationResult);

            genPairCommand = new GenerateKeyPairCommand(
                0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            genPairResponse = _pivSession.Connection.SendCommand(genPairCommand);

            _collectorObj.KeyFlag = 1;
            isChanged = _pivSession.TryChangeManagementKey();

            Assert.True(isChanged);
        }

        [Fact]
        public void Auth_ThenWrongKey()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            // This should fail, the mgmt key is not authenticated.
            var genPairCommand = new GenerateKeyPairCommand(
                0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            GenerateKeyPairResponse genPairResponse =
                _pivSession.Connection.SendCommand(genPairCommand);
            Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;
            bool isAuthenticated = _pivSession.TryAuthenticateManagementKey();
            Assert.True(isAuthenticated);
            Assert.Equal(
                AuthenticateManagementKeyResult.MutualFullyAuthenticated, _pivSession.ManagementKeyAuthenticationResult);

            genPairCommand = new GenerateKeyPairCommand(
                0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            genPairResponse = _pivSession.Connection.SendCommand(genPairCommand);
            Assert.Equal(ResponseStatus.Success, genPairResponse.Status);

            _collectorObj.KeyFlag = 1;
            isAuthenticated = _pivSession.TryAuthenticateManagementKey(false);

            Assert.False(isAuthenticated);
            Assert.Equal(
                AuthenticateManagementKeyResult.SingleAuthenticationFailed, _pivSession.ManagementKeyAuthenticationResult);

            genPairCommand = new GenerateKeyPairCommand(
                0x87, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            genPairResponse = _pivSession.Connection.SendCommand(genPairCommand);

            Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);
        }

        [Fact]
        public void ChangePin()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;
            _isValid = _pivSession.TryAuthenticateManagementKey();
            Assert.True(_isValid);

            _isValid = TryGenerate(_pivSession, 0x86, ResponseStatus.Success);
            Assert.True(_isValid);

            using (var pivSession = new PivSession(_yubiKeyDevice))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                _isValid = TrySign(pivSession, 0x86, ResponseStatus.AuthenticationRequired);
                Assert.True(_isValid);

                bool isChanged = pivSession.TryChangePin();
                Assert.True(isChanged);
                Assert.False(pivSession.PinVerified);

                _isValid = TrySign(pivSession, 0x86, ResponseStatus.AuthenticationRequired);
                Assert.True(_isValid);

                collectorObj.KeyFlag = 1;
                bool isVerified = pivSession.TryVerifyPin();
                Assert.True(isVerified);

                _isValid = TrySign(pivSession, 0x86, ResponseStatus.Success);
                Assert.True(_isValid);

                collectorObj.KeyFlag = 1;
                isChanged = pivSession.TryChangePin();
                Assert.True(isChanged);
                Assert.True(pivSession.PinVerified);
                isVerified = pivSession.TryVerifyPin();
                Assert.True(isVerified);

                _isValid = TrySign(pivSession, 0x86, ResponseStatus.Success);
                Assert.True(_isValid);
            }
        }

        [Fact]
        public void ChangePuk()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;

            bool isChanged = _pivSession.TryChangePuk();
            Assert.True(isChanged);

            _collectorObj.KeyFlag = 1;
            isChanged = _pivSession.TryChangePuk();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPin()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;

            bool isChanged = _pivSession.TryResetPin();
            Assert.True(isChanged);

            isChanged = _pivSession.TryChangePuk();
            Assert.True(isChanged);

            _collectorObj.KeyFlag = 1;
            isChanged = _pivSession.TryResetPin();
            Assert.True(isChanged);

            _collectorObj.KeyFlag = 1;
            isChanged = _pivSession.TryChangePuk();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPin_WrongPuk()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;

            _collectorObj.KeyFlag = 1;
            bool isChanged = _pivSession.TryResetPin();
            Assert.False(isChanged);

            _collectorObj.KeyFlag = 0;
            isChanged = _pivSession.TryResetPin();
            Assert.True(isChanged);

            _collectorObj.KeyFlag = 0;
            isChanged = _pivSession.TryChangePuk();
            Assert.True(isChanged);

            _collectorObj.KeyFlag = 1;
            isChanged = _pivSession.TryResetPin();
            Assert.True(isChanged);

            _collectorObj.KeyFlag = 1;
            isChanged = _pivSession.TryChangePuk();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPiv()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
            Assert.NotNull(_pivSession.Connection);

            _pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;

            _isValid = _pivSession.TryAuthenticateManagementKey();
            Assert.True(_isValid);

            _isValid = TryGenerate(_pivSession, 0x86, ResponseStatus.Success);
            Assert.True(_isValid);

            bool isVerified = _pivSession.TryVerifyPin();
            Assert.True(isVerified);

            _isValid = TrySign(_pivSession, 0x86, ResponseStatus.Success);
            Assert.True(_isValid);

            bool isChanged = _pivSession.TryChangePin();
            Assert.True(isChanged);
            Assert.True(_pivSession.PinVerified);

            _pivSession.ResetApplication();

            Assert.Equal(
                AuthenticateManagementKeyResult.Unauthenticated,
                _pivSession.ManagementKeyAuthenticationResult);
            Assert.False(_pivSession.ManagementKeyAuthenticated);
            Assert.False(_pivSession.PinVerified);

            _isValid = TryGenerate(_pivSession, 0x87, ResponseStatus.AuthenticationRequired);
            Assert.True(_isValid);

            _isValid = _pivSession.TryAuthenticateManagementKey();
            Assert.True(_isValid);

            _isValid = TryGenerate(_pivSession, 0x87, ResponseStatus.Success);
            Assert.True(_isValid);

            _isValid = TrySign(_pivSession, 0x87, ResponseStatus.AuthenticationRequired);
            Assert.True(_isValid);

            _collectorObj.KeyFlag = 1;
            isVerified = _pivSession.TryVerifyPin();
            Assert.False(isVerified);

            _collectorObj.KeyFlag = 0;
            isVerified = _pivSession.TryVerifyPin();
            Assert.True(isVerified);

            _isValid = TrySign(_pivSession, 0x86, ResponseStatus.Failed);
            Assert.True(_isValid);

            _isValid = TrySign(_pivSession, 0x87, ResponseStatus.Success);
            Assert.True(_isValid);
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
