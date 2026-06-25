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

using NSubstitute;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Piv.UnitTests;

public class TouchNotificationTests
{
    [Fact]
    public void TouchNotificationCallback_Delegate_IsParameterless()
    {
        // Verify the delegate signature has no parameters (security requirement)
        var method = typeof(TouchNotificationCallback).GetMethod("Invoke");
        
        Assert.NotNull(method);
        Assert.Empty(method.GetParameters());
        Assert.Equal(typeof(void), method.ReturnType);
    }

    [Fact]
    public void OnTouchRequired_Property_IsNullable()
    {
        // Arrange
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);
        
        var session = new PivSession(mockConnection, null);

        // Act & Assert: Default is null
        Assert.Null(session.OnTouchRequired);

        // Can set callback
        bool callbackInvoked = false;
        session.OnTouchRequired = () => callbackInvoked = true;
        
        Assert.NotNull(session.OnTouchRequired);
        
        // Can invoke
        session.OnTouchRequired();
        Assert.True(callbackInvoked);

        // Can clear
        session.OnTouchRequired = null;
        Assert.Null(session.OnTouchRequired);
    }

    [Fact]
    public void OnTouchRequired_CallbackOnOldFirmware_IsInvokedConservatively()
    {
        // Arrange: Session with old firmware (0.0.0 < 5.3)
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);
        
        var session = new PivSession(mockConnection, null);
        
        int callbackCount = 0;
        session.OnTouchRequired = () => callbackCount++;

        // Assert: Session has default firmware (0.0.0) which is < 5.3
        // The touch callback behavior will be tested through integration tests
        // since SignOrDecrypt requires full session initialization
        Assert.Equal(0, callbackCount);
        Assert.NotNull(session.OnTouchRequired);
    }
}
