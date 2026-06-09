// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.Hid;

public class ConnectionTypeMapperTests
{
    [Theory]
    [InlineData(HidInterfaceType.Fido, ConnectionType.HidFido)]
    [InlineData(HidInterfaceType.Otp, ConnectionType.HidOtp)]
    [InlineData(HidInterfaceType.Unknown, ConnectionType.Unknown)]
    public void ToConnectionType_MapsHidInterfaces(
        HidInterfaceType interfaceType,
        ConnectionType expectedConnectionType)
    {
        Assert.Equal(expectedConnectionType, ConnectionTypeMapper.ToConnectionType(interfaceType));
    }

    [Theory]
    [InlineData(HidInterfaceType.Fido, ConnectionType.Hid, true)]
    [InlineData(HidInterfaceType.Fido, ConnectionType.HidFido, true)]
    [InlineData(HidInterfaceType.Fido, ConnectionType.HidOtp, false)]
    [InlineData(HidInterfaceType.Fido, ConnectionType.All, true)]
    [InlineData(HidInterfaceType.Otp, ConnectionType.Hid, true)]
    [InlineData(HidInterfaceType.Otp, ConnectionType.HidFido, false)]
    [InlineData(HidInterfaceType.Otp, ConnectionType.HidOtp, true)]
    [InlineData(HidInterfaceType.Otp, ConnectionType.All, true)]
    [InlineData(HidInterfaceType.Unknown, ConnectionType.Hid, false)]
    [InlineData(HidInterfaceType.Unknown, ConnectionType.All, false)]
    public void SupportsConnectionType_AppliesSpecificAndGroupFilters(
        HidInterfaceType interfaceType,
        ConnectionType connectionType,
        bool expected)
    {
        Assert.Equal(expected, ConnectionTypeMapper.SupportsConnectionType(interfaceType, connectionType));
    }
}