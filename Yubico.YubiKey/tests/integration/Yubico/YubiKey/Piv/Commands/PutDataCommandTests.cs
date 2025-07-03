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
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv.Commands;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class PutDataCommandTests : PivSessionIntegrationTestBase
{
    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void SendCommand_With_TooLargeApdu_ReturnsResultFailed(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        TestDeviceType = testDeviceType;
        
        var tooLargeTlv = new TlvObject(0x53, new byte[10000]);
        var tlvBytes = tooLargeTlv.GetBytes();
        var command = new PutDataCommand(0x5F0000, tlvBytes);

        // Act
        var response = Session.Connection.SendCommand(command);

        // Assert
        Assert.Equal(ResponseStatus.Failed, response.Status);
        Assert.Equal(SWConstants.WrongLength, response.StatusWord);
    }

    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void SendCommand_with_ValidSizeApdu_ReturnsResultSuccess(
        StandardTestDevice testDeviceType)
    {
        // Arrange
        TestDeviceType = testDeviceType;

        var validSizeTlv = new TlvObject(0x53, new byte[SmartCardMaxApduSizes.YK4_3]);
        var tlvBytes = validSizeTlv.GetBytes();
        var command = new PutDataCommand(0x5F0000, tlvBytes);

        // Act
        var response = Session.Connection.SendCommand(command);
        var actualSize = command.CreateCommandApdu().AsByteArray().Length;
        
        // Assert
        Assert.Equal(3078, actualSize); // This is the current max APDU size of the YubiKey 5 series.
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.Equal(SWConstants.Success, response.StatusWord);
    }
}
