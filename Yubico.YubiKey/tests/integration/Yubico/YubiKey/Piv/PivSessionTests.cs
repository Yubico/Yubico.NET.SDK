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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class PivSessionTests : PivSessionIntegrationTestBase
    {
        private readonly Simple39KeyCollector _keyCollector;

        public PivSessionTests()
        {
            _keyCollector = new Simple39KeyCollector();
        }

        [Fact]
        public void VerifyPin()
        {
            TryVerifyPin();
        }

        [Fact]
        public void VerifyPin_WrongPin()
        {
            UpdatePinPuk_KeyCollector();
            TryVerifyPin(expectedResult: false);

            UpdatePinPuk_KeyCollector();
            TryVerifyPin();
        }

        [Fact]
        public void AuthenticateMgmtKey_Single()
        {
            using var session = GetSession();
            
            TryAuthenticate(session);
            Assert.Equal(AuthenticateManagementKeyResult.SingleAuthenticated,
                session.ManagementKeyAuthenticationResult);
        }

        [Fact]
        public void AuthenticateMgmtKey_Mutual()
        {
            using var session = GetSession();
            
            TryAuthenticate(session, mutualAuthentication: true);
            Assert.Equal(
                AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                session.ManagementKeyAuthenticationResult);
        }

        [Fact]
        public void ChangeMgmtKey()
        {
            using var session = GetSession();

            FailAnonymousOperation(session);

            var isChanged = session.TryChangeManagementKey();
            Assert.True(isChanged);
            Assert.Equal(
                AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                session.ManagementKeyAuthenticationResult);

            DoAuthenticatedOperation(session);

            UpdatePinPuk_KeyCollector(session);
            isChanged = session.TryChangeManagementKey();

            Assert.True(isChanged);
        }

        [Fact]
        public void Auth_ThenWrongKey()
        {
            using var session = GetSession();
            
            TryGenerate(PivSlot.Retired5, session, expectedStatus: ResponseStatus.AuthenticationRequired);
            TryAuthenticate(session, expectedResult: true, mutualAuthentication: true);
            TryGenerate(PivSlot.Retired5, expectedStatus: ResponseStatus.Success);
            
            UpdatePinPuk_KeyCollector(session);

            TryAuthenticate(session, expectedResult: false, mutualAuthentication: true);
            TryGenerate(PivSlot.Retired5, expectedStatus: ResponseStatus.AuthenticationRequired);
        }

        [Fact]
        public void ChangePin()
        {
            using var session = GetSession();

            // Change pin to next in keycollector
            var isChanged = session.TryChangePin();
            Assert.True(isChanged);

            // Change back to previous
            UpdatePinPuk_KeyCollector(session);
            isChanged = session.TryChangePin();
            Assert.True(isChanged);
        }

        [Fact]
        public void ChangePuk()
        {
            using var session = GetSession();

            // Change puk to next in keycollector
            var isChanged = session.TryChangePuk();
            Assert.True(isChanged);

            // Change back to previous
            UpdatePinPuk_KeyCollector(session);
            isChanged = session.TryChangePuk();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPin()
        {
            var isChanged = Session.TryResetPin();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPin_WrongPuk()
        {
            // Try wrong PUK
            UpdatePinPuk_KeyCollector();

            var isChanged = Session.TryResetPin();
            Assert.False(isChanged);

            // Try correct PUK
            UpdatePinPuk_KeyCollector();
            isChanged = Session.TryResetPin();
            Assert.True(isChanged);

            isChanged = Session.TryChangePuk();
            Assert.True(isChanged);

            UpdatePinPuk_KeyCollector();
            isChanged = Session.TryResetPin();
            Assert.True(isChanged);

            isChanged = Session.TryChangePuk();
            Assert.True(isChanged);
        }

        [Fact]
        public void ResetPiv()
        {
            TryAuthenticate();
            TryGenerate(PivSlot.Retired5, expectedStatus: ResponseStatus.Success);

            TryVerifyPin();
            TrySign(PivSlot.Retired5, expectedStatus: ResponseStatus.Success);

            // Change pin to next pin in keycollector
            var isChanged = Session.TryChangePin();
            Assert.True(isChanged);

            // Old pin should not work
            isChanged = Session.TryChangePin();
            Assert.False(isChanged);

            // Reset PIV, old pin should work again 
            Session.ResetApplication();
            Assert_AuthIsReset();

            // Try operations
            TryGenerate(PivSlot.Retired6, expectedStatus: ResponseStatus.AuthenticationRequired);

            TryVerifyPin(Session);
            TryAuthenticate(Session);
            TryGenerate(PivSlot.Retired6, expectedStatus: ResponseStatus.Success);
        }

        private void TryGenerate(
            byte slotNumber,
            PivSession? session = null,
            ResponseStatus expectedStatus = ResponseStatus.Success
        )
        {
            session ??= Session;

            var genPairCommand = new GenerateKeyPairCommand(
                slotNumber, KeyType.ECP256, PivPinPolicy.Always, PivTouchPolicy.Never);

            var sessionToUse = session ?? Session;
            var genPairResponse = sessionToUse.Connection.SendCommand(genPairCommand);
            var success = genPairResponse.Status == expectedStatus;

            Assert.True(success, "Expected status: " + expectedStatus + ", but got: " + genPairResponse.Status);
        }

        private void TrySign(
            byte slotNumber,
            PivSession? session = null,
            ResponseStatus expectedStatus = ResponseStatus.Success)
        {
            session ??= Session;

            byte[] dataToSign =
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            var signCommand = new AuthenticateSignCommand(dataToSign, slotNumber);
            var signResponse = session.Connection.SendCommand(signCommand);
            var success = signResponse.Status == expectedStatus;

            Assert.True(success, "Expected status: " + expectedStatus + ", but got: " + signResponse.Status);
        }

        private void FailAnonymousOperation(
            PivSession? session = null)
        {
            session ??= Session;

            // This should fail, the mgmt key is not authenticated.
            var genPairCommand = new GenerateKeyPairCommand(
                PivSlot.Retired5, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            var genPairResponse = session.Connection.SendCommand(genPairCommand);
            Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);
        }

        private void TryAuthenticate(
            PivSession? session = null,
            bool expectedResult = true,
            bool mutualAuthentication = false)
        {
            session ??= Session;

            var isAuthenticated = session.TryAuthenticateManagementKey(mutualAuthentication);
            Assert.True(expectedResult == isAuthenticated,
                "TryAuthenticate failed. ExpectedResult: " + expectedResult + ", ActualResult: " + isAuthenticated);

            var expectedAuthResult = (expectedResult, mutualAuthentication) switch
            {
                (true, true) => AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                (true, false) => AuthenticateManagementKeyResult.SingleAuthenticated,
                (false, true) => AuthenticateManagementKeyResult.MutualOffCardAuthenticationFailed, // This line may need adjusting in the future 
                (false, false) => AuthenticateManagementKeyResult.SingleAuthenticationFailed
            };

            Assert.Equal(expectedAuthResult, session.ManagementKeyAuthenticationResult);
        }

        private void Assert_AuthIsReset(
            PivSession? session = null)
        {
            session ??= Session;

            Assert.Equal(AuthenticateManagementKeyResult.Unauthenticated,
                session.ManagementKeyAuthenticationResult);
            Assert.False(session.ManagementKeyAuthenticated);
            Assert.False(session.PinVerified);
        }

        private void TryVerifyPin(
            PivSession? session = null,
            bool expectedResult = true,
            ReadOnlyMemory<byte>? pin = null)
        {
            session ??= Session;

            var isVerified = pin.HasValue
#pragma warning disable IDE0059
                ? session.TryVerifyPin(pin.Value, out var retriesRemaining)
#pragma warning restore IDE0059
                : session.TryVerifyPin();

            Assert.True(expectedResult == isVerified,
                "TryVerifyPin failed. ExpectedResult: " + expectedResult + ", ActualResult: " + isVerified);
            Assert.Equal(expectedResult, Session.PinVerified);
        }

        private void UpdatePinPuk_KeyCollector(
            PivSession? session = null)
        {
            var currentValue = _keyCollector.KeyFlag;
            _keyCollector.KeyFlag = currentValue == 0 ? 1 : 0;

            session ??= Session;
            session.KeyCollector = _keyCollector.Simple39KeyCollectorDelegate;
        }

        private void DoAuthenticatedOperation(
            PivSession? session = null)
        {
            session ??= Session;

            var genPairCommand = new GenerateKeyPairCommand(
                PivSlot.Retired5, KeyType.ECP256, PivPinPolicy.Default, PivTouchPolicy.Never);
            session.Connection.SendCommand(genPairCommand);
        }
    }
}
