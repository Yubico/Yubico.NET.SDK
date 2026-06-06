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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests;

public class ConnectionTypeTests
{
    [Fact]
    public void Values_AreExplicitFlags()
    {
        Assert.Equal(0, (int)ConnectionType.Unknown);
        Assert.Equal(1, (int)ConnectionType.Hid);
        Assert.Equal(2, (int)ConnectionType.HidFido);
        Assert.Equal(4, (int)ConnectionType.HidOtp);
        Assert.Equal(8, (int)ConnectionType.SmartCard);
        Assert.Equal(15, (int)ConnectionType.All);
    }

    [Fact]
    public void HidOtp_IsNotCompositeOfHidAndHidFido()
    {
        Assert.NotEqual(ConnectionType.Hid | ConnectionType.HidFido, ConnectionType.HidOtp);
    }

    [Fact]
    public void Transport_ValuesRemainValidFlags()
    {
        Assert.Equal(0, (int)Transport.None);
        Assert.Equal(1, (int)Transport.Usb);
        Assert.Equal(2, (int)Transport.Nfc);
        Assert.Equal(3, (int)Transport.All);
    }
}