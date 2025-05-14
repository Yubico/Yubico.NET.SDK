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
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class PivSessionTests : PivSessionIntegrationTestBase
    {
        public PivSessionTests()
        {
            Session.ResetApplication();
            SetKeyFlag(0);
        }

        [Fact]
        public void VerifyPin()
        {
            TryVerifyPin();
            Assert.True(Session.PinVerified);
        }

        [Fact]
        public void VerifyPin_WrongPin()
        {
            SetKeyFlag(1);
            TryVerifyPin(false);
            Assert.False(Session.PinVerified);

            SetKeyFlag(0);
            TryVerifyPin();
            Assert.True(Session.PinVerified);
        }

        [Fact]
        public void AuthenticateMgmtKey_Single()
        {
            TryAuthenticate();
            Assert.Equal(AuthenticateManagementKeyResult.SingleAuthenticated,
                Session.ManagementKeyAuthenticationResult);
        }

        [Fact]
        public void AuthenticateMgmtKey_Mutual()
        {
            TryAuthenticate(true);
            Assert.Equal(
                AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                Session.ManagementKeyAuthenticationResult);
        }

        [Fact]
        public void ChangeMgmtKey()
        {
            DoFailedAuth();

            SetKeyFlag(0);
            var isChanged = Session.TryChangeManagementKey();
            Assert.True(isChanged);
            Assert.Equal(
                AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                Session.ManagementKeyAuthenticationResult);

            var genPairCommand =
                new GenerateKeyPairCommand(0x86, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            Session.Connection.SendCommand(genPairCommand);

            SetKeyFlag(1);
            isChanged = Session.TryChangeManagementKey();
            Assert.True(isChanged);
        }

        [Fact]
        public void Auth_ThenWrongKey()
        {
            DoFailedAuth();
            TryAuthenticate(true);

            Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                Session.ManagementKeyAuthenticationResult);

            var genPairCommand =
                new GenerateKeyPairCommand(0x86, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            var genPairResponse = Session.Connection.SendCommand(genPairCommand);
            Assert.Equal(ResponseStatus.Success, genPairResponse.Status);

            SetKeyFlag(1);
            TryAuthenticate(true, false);
            Assert.Equal(AuthenticateManagementKeyResult.SingleAuthenticationFailed,
                Session.ManagementKeyAuthenticationResult);

            genPairCommand =
                new GenerateKeyPairCommand(0x87, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            genPairResponse = Session.Connection.SendCommand(genPairCommand);
            Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);
        }

        [Fact]
        public void ChangePin()
        {
            TryAuthenticate();
            SetKeyFlag(0);
            var isChanged = Session.TryChangePin();
            Assert.True(isChanged);

            SetKeyFlag(1);
            isChanged = Session.TryChangePin();
            Assert.True(isChanged);
        }

        [Fact]
        public void ChangePuk()
        {
            SetKeyFlag(0);
            var isChanged = Session.TryChangePuk();
            Assert.True(isChanged);

            SetKeyFlag(1);
            isChanged = Session.TryChangePuk();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPin()
        {
            SetKeyFlag(0);
            var isChanged = Session.TryResetPin();
            Assert.True(isChanged);
            
            isChanged = Session.TryChangePuk();
            Assert.True(isChanged);

            SetKeyFlag(1);
            isChanged = Session.TryResetPin();
            Assert.True(isChanged);

            isChanged = Session.TryChangePuk();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPin_WrongPuk()
        {
            SetKeyFlag(1);
            var isChanged = Session.TryResetPin();
            Assert.False(isChanged);

            SetKeyFlag(0);
            isChanged = Session.TryResetPin();
            Assert.True(isChanged);

            isChanged = Session.TryChangePuk();
            Assert.True(isChanged);

            SetKeyFlag(1);
            isChanged = Session.TryResetPin();
            Assert.True(isChanged);

            isChanged = Session.TryChangePuk();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPiv()
        {
            TryAuthenticate();
            TryGenerate(0x86, ResponseStatus.Success);

            TryVerifyPin();
            TrySign(0x86, ResponseStatus.Success);

            var isChanged = Session.TryChangePin();
            Assert.True(isChanged);
            Assert.True(Session.PinVerified);

            // Reset PIV
            Session.ResetApplication();
            AssertAuthIsReset();

            TryGenerate(0x87, ResponseStatus.AuthenticationRequired);
            TryAuthenticate();
            TryGenerate(0x87, ResponseStatus.Success);
            TrySign(0x87, ResponseStatus.AuthenticationRequired);

            SetKeyFlag(1);
            TryVerifyPin(false);

            SetKeyFlag(0);
            TryVerifyPin(true);
            TrySign(0x86, ResponseStatus.Failed);
            TrySign(0x87, ResponseStatus.Success);
        }

        [Fact]
        public void FixedBytes_Replace()
        {
            byte[] fixedBytes =
            {
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48
            };

            var replacement = RandomObjectUtility.SetRandomProviderFixedBytes(fixedBytes);

            try
            {
                var random = CryptographyProviders.RngCreator();
                var randomBytes = new byte[32];
                random.GetBytes(randomBytes);
                var compareResult = randomBytes.SequenceEqual(fixedBytes);
                Assert.True(compareResult);
            }
            finally
            {
                replacement.RestoreRandomProvider();
            }
        }

        private void TryGenerate(
            byte slotNumber,
            ResponseStatus expectedStatus,
            PivSession? session = null
        )
        {
            var genPairCommand = new GenerateKeyPairCommand(
                slotNumber, KeyType.ECP256, PivPinPolicy.Always, PivTouchPolicy.Never);

            var sessionToUse = session ?? Session;
            var genPairResponse = sessionToUse.Connection.SendCommand(genPairCommand);
            var success = genPairResponse.Status == expectedStatus;

            Assert.True(success, "Expected status: " + expectedStatus + ", but got: " + genPairResponse.Status);
        }

        private void TrySign(
            byte slotNumber,
            ResponseStatus expectedStatus,
            PivSession? session = null)
        {
            byte[] dataToSign =
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            var signCommand = new AuthenticateSignCommand(dataToSign, slotNumber);
            var sessionToUse = session ?? Session;
            var signResponse = sessionToUse.Connection.SendCommand(signCommand);
            var success = signResponse.Status == expectedStatus;

            Assert.True(success, "Expected status: " + expectedStatus + ", but got: " + signResponse.Status);
        }

        private void DoFailedAuth()
        {
            // This should fail, the mgmt key is not authenticated.
            var genPairCommand = new GenerateKeyPairCommand(
                0x86, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            var genPairResponse = Session.Connection.SendCommand(genPairCommand);
            Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);
        }

        private void TryAuthenticate(
            bool mutualAuthentication = false,
            bool expectedResult = true,
            PivSession? session = null
        )
        {
            var sessionToUse = session ?? Session;
            var isAuthenticated = sessionToUse.TryAuthenticateManagementKey(mutualAuthentication);
            Assert.Equal(expectedResult, isAuthenticated);
        }

        private void AssertAuthIsReset()
        {
            Assert.Equal(
                AuthenticateManagementKeyResult.Unauthenticated,
                Session.ManagementKeyAuthenticationResult);
            Assert.False(Session.ManagementKeyAuthenticated);
            Assert.False(Session.PinVerified);
        }

        private void TryVerifyPin(
            bool expectedResult = true,
            PivSession? session = null)
        {
            var sessionToUse = session ?? Session;
            var isVerified = sessionToUse.TryVerifyPin();
            Assert.Equal(expectedResult, isVerified);
        }

        private void SetKeyFlag(
            int keyFlag,
            PivSession? session = null
        )
        {
            var collectorObj = new Simple39KeyCollector();
            var sessionToUse = session ?? Session;
            sessionToUse.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            collectorObj.KeyFlag = keyFlag;
        }
    }
}
