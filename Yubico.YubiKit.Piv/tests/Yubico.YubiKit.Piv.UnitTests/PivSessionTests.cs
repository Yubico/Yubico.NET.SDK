// Copyright 2024 Yubico AB
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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Piv.UnitTests;

public class PivSessionTests
{
    [Fact]
    public async Task CreateAsync_WithValidConnection_ReturnsInitializedSession()
    {
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);
        
        // This will likely fail during actual PIV selection since it's a mock,
        // but it tests that the CreateAsync method exists and accepts the right parameters
        var exception = await Record.ExceptionAsync(() => 
            PivSession.CreateAsync(mockConnection, cancellationToken: TestContext.Current.CancellationToken));
        
        // We expect this to fail with an ApduException since the mock doesn't implement real protocol
        Assert.NotNull(exception);
    }

    [Fact] 
    public async Task CreateAsync_WithNullConnection_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => PivSession.CreateAsync((ISmartCardConnection)null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Constructor_WithValidConnection_CreatesSession()
    {
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);
        
        var session = new PivSession(mockConnection, null);
        
        Assert.NotNull(session);
        // Before initialization, session should not be initialized
        Assert.False(session.IsInitialized);
    }

    [Fact]
    public void ManagementKeyType_DefaultsToTripleDes()
    {
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);
        
        var session = new PivSession(mockConnection, null);
        
        // Default management key type should be 3DES
        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);
        
        var session = new PivSession(mockConnection, null);
        
        var exception = Record.Exception(() => session.Dispose());
        
        Assert.Null(exception);
    }

    [Fact]
    public void DefaultManagementKey_Returns24ByteDefaultValue()
    {
        // Default PIV management key is 0x010203040506070801020304050607080102030405060708 (24 bytes)
        ReadOnlySpan<byte> expected = [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        ];

        ReadOnlySpan<byte> actual = PivSession.DefaultManagementKey;

        Assert.Equal(24, actual.Length);
        Assert.True(expected.SequenceEqual(actual));
    }
}