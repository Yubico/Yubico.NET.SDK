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
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class PivObjectTests : PivSessionIntegrationTestBase
{
    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void ReadChuid_IsEmpty_Correct(
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;
        var chuid = Session.ReadObject<CardholderUniqueId>();
        Assert.True(chuid.IsEmpty);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void WriteThenReadChuid_Data_Correct(
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;

        // Arrange
        var expected = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01
        };

        var chuid = Session.ReadObject<CardholderUniqueId>();
        Assert.True(chuid.IsEmpty);

        chuid.SetGuid(expected);

        // Act
        Session.WriteObject(chuid);

        // Assert
        chuid = Session.ReadObject<CardholderUniqueId>();
        Assert.False(chuid.IsEmpty);

        var isValid = expected.AsSpan().SequenceEqual(chuid.GuidValue.Span);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void AltTag_WriteThenReadChuid_Data_Correct(
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;
        var expected = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01
        };

        var chuid = Session.ReadObject<CardholderUniqueId>();
        Assert.True(chuid.IsEmpty);

        chuid.SetGuid(expected);
        chuid.DataTag = 0x5F0010;

        Session.WriteObject(chuid);

        chuid = Session.ReadObject<CardholderUniqueId>(0x5F0010);
        Assert.False(chuid.IsEmpty);

        var isValid = expected.AsSpan().SequenceEqual(chuid.GuidValue.Span);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void WriteEmpty_Correct(
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;

        var expected = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01
        };

        var emptyChuid = Session.ReadObject<CardholderUniqueId>();
        Assert.True(emptyChuid.IsEmpty);

        // Write an empty object.
        Session.WriteObject(emptyChuid);

        // Make sure the contents are still empty.
        var chuid = Session.ReadObject<CardholderUniqueId>();
        Assert.True(chuid.IsEmpty);

        // Now write a CHUID with data.
        chuid.SetGuid(expected);
        Session.WriteObject(chuid);

        // Make sure that worked.
        chuid = Session.ReadObject<CardholderUniqueId>();
        Assert.False(chuid.IsEmpty);
        var isValid = expected.AsSpan().SequenceEqual(chuid.GuidValue.Span);
        Assert.True(isValid);

        // Now write an empty object.
        Session.WriteObject(emptyChuid);

        // Make sure the contents are empty.
        chuid = Session.ReadObject<CardholderUniqueId>();
        Assert.True(chuid.IsEmpty);
    }

    [Theory]
    [InlineData(0x015FFF10, StandardTestDevice.Fw5)]
    [InlineData(0x0000007E, StandardTestDevice.Fw5)]
    [InlineData(0x00007F61, StandardTestDevice.Fw5)]
    [InlineData(0x005FC101, StandardTestDevice.Fw5)]
    [InlineData(0x005FC104, StandardTestDevice.Fw5)]
    [InlineData(0x005FC105, StandardTestDevice.Fw5)]
    [InlineData(0x005FC10A, StandardTestDevice.Fw5)]
    [InlineData(0x005FC10B, StandardTestDevice.Fw5)]
    [InlineData(0x005FC10D, StandardTestDevice.Fw5)]
    [InlineData(0x005FC120, StandardTestDevice.Fw5)]
    [InlineData(0x005FFF01, StandardTestDevice.Fw5)]
    public void Read_InvalidTag_Throws(
        int newTag,
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;
        _ = Assert.Throws<ArgumentException>(() => Session.ReadObject<CardholderUniqueId>(newTag));
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void WriteEmptyObject_DataEmpty(
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;
        var keyData = new byte[]
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58
        };

        using (var pivSession = GetSession())
        {
            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(keyData);
            pivSession.WriteObject(pinProtect);
        }

        using (var pivSession = GetSession())
        {
            pivSession.VerifyPin();
            pivSession.AuthenticateManagementKey();

            var getDataCommand = new GetDataCommand((int)PivDataTag.Printed);
            var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);

            Assert.Equal(ResponseStatus.Success, getDataResponse.Status);
            var getData = getDataResponse.GetData();
            Assert.Equal(0x53, getData.Span[0]);
        }

        using (var pivSession = GetSession())
        {
            using var pinProtect = new PinProtectedData();
            pinProtect.SetManagementKey(ReadOnlyMemory<byte>.Empty);
            pivSession.WriteObject(pinProtect);
        }

        using (var pivSession = GetSession())
        {
            pivSession.VerifyPin();
            pivSession.AuthenticateManagementKey();

            var getDataCommand = new GetDataCommand((int)PivDataTag.Printed);
            var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
            var getData = getDataResponse.GetData();

            Assert.Equal(4, getData.Length);
        }
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void Write_NullArg_Throws(
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;
        _ = Assert.Throws<ArgumentNullException>(() => Session.WriteObject(null!));
    }
}
