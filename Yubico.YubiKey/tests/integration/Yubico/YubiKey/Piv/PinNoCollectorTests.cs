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

using Xunit;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

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

        using (var pivSession = GetSession())
        {
            var isValid = pivSession.TryChangePuk(DefaultPuk, ComplexPuk, out var retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);
        }

        using (var pivSession = GetSession())
        {
            var isValid = pivSession.TryResetPin(ComplexPuk, ComplexPin, out var retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = pivSession.TryVerifyPin(ComplexPin, out retriesRemaining);
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

        using (var pivSession = GetSession())
        {
            var isValid = pivSession.TryChangePin(DefaultPin, ComplexPin, out var retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = pivSession.TryChangePuk(DefaultPuk, ComplexPuk, out retriesRemaining);
            Assert.Null(retriesRemaining);
            Assert.True(isValid);
        }

        using (var pivSession = GetSession())
        {
            var isValid = pivSession.TryVerifyPin(ComplexPin, out var retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = pivSession.TryResetPin(ComplexPuk, DefaultPin, out retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);
        }

        using (var pivSession = GetSession())
        {
            var isValid = pivSession.TryVerifyPin(DefaultPin, out var retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = pivSession.TryChangePin(DefaultPin, ComplexPin, out retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = pivSession.TryVerifyPin(ComplexPin, out retriesRemaining);
            Assert.Null(retriesRemaining);
            Assert.True(isValid);
        }

        using (var pivSession = GetSession())
        {
            var isValid =
                pivSession.TryChangePinAndPukRetryCounts(mgmtKey, ComplexPin, 7, 8, out var retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = pivSession.TryVerifyPin(DefaultPin, out retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = pivSession.TryResetPin(DefaultPuk, ComplexPin, out retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);

            isValid = pivSession.TryChangePin(ComplexPin, DefaultPin, out retriesRemaining);
            Assert.True(isValid);
            Assert.Null(retriesRemaining);
        }
    }
}
