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
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public sealed class SelectApplicationTests
{
    [Theory]
    [InlineData(StandardTestDevice.Fw5)]
    public void ConnectOathHasData(
        StandardTestDevice testDeviceType)
    {
        var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
        Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

        using var connection = testDevice.Connect(YubiKeyApplication.Piv);
        Assert.NotNull(connection);

        // Connect does not actually select the app.  We need a command for this.  It can be anything.
        _ = connection!.SendCommand(new GetSerialNumberCommand());

        Assert.NotNull(connection!.SelectApplicationData);
        var data = Assert.IsType<GenericSelectApplicationData>(connection.SelectApplicationData);

        Assert.False(data!.RawData.IsEmpty);
    }
}
