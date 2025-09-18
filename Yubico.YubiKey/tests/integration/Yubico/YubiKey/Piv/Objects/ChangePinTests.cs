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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv.Objects;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class ChangePinTests : PivSessionIntegrationTestBase
{
    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void NoAdminData_ChangePin_NoUpdate(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        SetupTest(testDeviceType);

        // Act
        Session.ChangePin();

        // Assert
        var adminData = Session.ReadObject<AdminData>();
        Assert.True(adminData.IsEmpty);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void AdminData_ChangePin_Updated(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        SetupTest(testDeviceType);
        var adminData = new AdminData
        {
            PinProtected = true
        };

        // Act
        Session.WriteObject(adminData);
        Session.ChangePin();

        // Assert
        adminData = Session.ReadObject<AdminData>();
        Assert.False(adminData.IsEmpty);
        Assert.True(adminData.PinProtected);
        _ = Assert.NotNull(adminData.PinLastUpdated);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void AdminData_ResetPin_NotUpdated(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        SetupTest(testDeviceType);
        var adminData = new AdminData
        {
            PinProtected = true
        };

        // Act
        Session.WriteObject(adminData);
        Session.ResetPin();

        // Assert
        adminData = Session.ReadObject<AdminData>();

        Assert.False(adminData.IsEmpty);
        Assert.True(adminData.PinProtected);
        Assert.Null(adminData.PinLastUpdated);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void AdminData_ResetRetry_Updated(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        SetupTest(testDeviceType);
        var adminData = new AdminData
        {
            PinProtected = true
        };

        // Act
        Session.WriteObject(adminData);
        Session.ChangePinAndPukRetryCounts(5, 5);

        // Assert
        adminData = Session.ReadObject<AdminData>();

        Assert.False(adminData.IsEmpty);
        Assert.True(adminData.PinProtected);
        _ = Assert.NotNull(adminData.PinLastUpdated);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void AdminData_ChangePuk_NoUpdate(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        SetupTest(testDeviceType);
        var adminData = new AdminData
        {
            PinProtected = true
        };

        // Act
        Session.WriteObject(adminData);
        Session.ChangePuk();

        // Assert
        adminData = Session.ReadObject<AdminData>();
        Assert.False(adminData.IsEmpty);
        Assert.True(adminData.PinProtected);
        Assert.Null(adminData.PinLastUpdated);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void AdminData_ChangeMgmtKey_NoUpdate(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        SetupTest(testDeviceType);
        var adminData = new AdminData
        {
            PinLastUpdated = DateTimeOffset.Parse("2000-02-29T00:00:00Z").UtcDateTime
        };

        // Act
        Session.WriteObject(adminData);
        Session.ChangeManagementKey();

        // Assert
        adminData = Session.ReadObject<AdminData>();
        Assert.False(adminData.IsEmpty);
        _ = Assert.NotNull(adminData.PinLastUpdated);
        if (adminData.PinLastUpdated is not null)
        {
            var check = new DateTimeOffset(adminData.PinLastUpdated.Value, TimeSpan.Zero);
            Assert.Equal(29, check.Day);
            Assert.Equal(2, check.Month);
            Assert.Equal(2000, check.Year);
        }
    }

    private void SetupTest(
        StandardTestDevice testDeviceType)
    {
        var collectorObj = new Simple39KeyCollector(true);
        TestDeviceType = testDeviceType;
        Session.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
    }
}
