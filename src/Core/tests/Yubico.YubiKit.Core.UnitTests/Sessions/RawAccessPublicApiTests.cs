// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public class RawAccessPublicApiTests
{
    [Fact]
    public void RawSessionsArePublicWhileProtocolMachineryIsInternal()
    {
        Assert.True(typeof(RawSmartCardSession).IsPublic);
        Assert.True(typeof(RawFidoHidSession).IsPublic);
        Assert.True(typeof(RawOtpHidSession).IsPublic);

        Assert.False(typeof(ProtocolFactory).IsPublic);
        Assert.False(typeof(IProtocol).IsPublic);
        Assert.False(typeof(ISmartCardProtocol).IsPublic);
        Assert.False(typeof(IFidoHidProtocol).IsPublic);
        Assert.False(typeof(IOtpHidProtocol).IsPublic);
    }
}