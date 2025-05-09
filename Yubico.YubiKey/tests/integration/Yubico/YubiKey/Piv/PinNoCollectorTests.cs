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
using System.Text;
using Xunit;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class PinNoCollectorTests : PivSessionIntegrationTestBase
    {

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyPin_Sign_Succeeds(
            StandardTestDevice testDeviceType)
        {
            // Arrange
            TestDeviceType = testDeviceType;

            // Act
            var isValid = Session.TryVerifyPin(DefaultPin, out var retriesRemaining);

            // Assert
            var pinProtect = Session.ReadObject<PinProtectedData>();
            Assert.True(isValid);
            Assert.Null(retriesRemaining);
            Assert.True(Session.PinVerified);
            Assert.True(pinProtect.IsEmpty);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyPin_WrongPin_ReturnsFalse(
            StandardTestDevice testDeviceType)
        {
            // Arrange
            TestDeviceType = testDeviceType;
            var pin = "A23456"u8.ToArray();

            // Act
            var isValid = Session.TryVerifyPin(pin, out var retriesRemaining);
            
            // Assert
            Assert.False(isValid);
            _ = Assert.NotNull(retriesRemaining);
            if (retriesRemaining is not null)
            {
                Assert.Equal(2, retriesRemaining);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePin_Succeeds(
            StandardTestDevice testDeviceType)
        {
            // Arrange
            TestDeviceType = testDeviceType;
            var newPin = "ABCDEF"u8.ToArray();

            // Act
            var isValid = Session.TryChangePin(DefaultPin, newPin, out var retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = Session.TryVerifyPin(newPin, out retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            var pinProtect = Session.ReadObject<PinProtectedData>();
            Assert.True(Session.PinVerified);
            Assert.True(pinProtect.IsEmpty);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePuk_Succeeds(
            StandardTestDevice testDeviceType)
        {
            // Arrange
            TestDeviceType = testDeviceType;
            var newPuk = "gjH@5K!8"u8.ToArray();
            var newPin = "1@$#5s!8"u8.ToArray();

            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.ResetApplication();
                var isValid = pivSession.TryChangePuk(DefaultPuk, newPuk, out var retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var isValid = pivSession.TryResetPin(newPuk, newPin, out var retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryVerifyPin(newPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                var pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.True(pivSession.PinVerified);
                Assert.True(pinProtect.IsEmpty);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangeRetryCounts_Succeeds(
            StandardTestDevice testDeviceType)
        {
            // Arrange
            TestDeviceType = testDeviceType;
            var mgmtKey = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };
            var newPuk = "gjH@5K!8"u8.ToArray();
            var newPin = "1@$#5s!8"u8.ToArray();
            
            using (var pivSession = GetSession(authenticate: false))
            {
                var isValid = pivSession.TryChangePin(DefaultPin, newPin, out var retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryChangePuk(DefaultPuk, newPuk, out retriesRemaining);
                Assert.Null(retriesRemaining);
                Assert.True(isValid);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var isValid = pivSession.TryVerifyPin(newPin, out var retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryResetPin(newPuk, DefaultPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var isValid = pivSession.TryVerifyPin(DefaultPin, out var retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryChangePin(DefaultPin, newPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryVerifyPin(newPin, out retriesRemaining);
                Assert.Null(retriesRemaining);
                Assert.True(isValid);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var isValid = pivSession.TryChangePinAndPukRetryCounts(mgmtKey, newPin, 7, 8, out var retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryVerifyPin(DefaultPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryResetPin(DefaultPuk, newPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryChangePin(newPin, DefaultPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);
            }
        }
    }
}
