using FluentAssertions;
using NSubstitute;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2.UnitTests;

public class FidoSessionTests
{
    [Fact]
    public async Task CreateAsync_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => FidoSession.CreateAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_UnsupportedConnectionType_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedConnection = Substitute.For<IConnection>();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => FidoSession.CreateAsync(unsupportedConnection, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_UsbBefore58_ThrowsNotSupportedException()
    {
        var exception = Assert.Throws<NotSupportedException>(() =>
            FidoSession.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(5, 7, 2)));

        exception.Message.Should().Contain("firmware 5.8.0");
        exception.Message.Should().Contain("IFidoHidConnection");
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_Usb58_Succeeds()
    {
        FidoSession.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(5, 8, 0));
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_UsbSentinelFirmware_Succeeds()
    {
        FidoSession.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(0, 0, 1));
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_NfcBefore58_Succeeds()
    {
        FidoSession.EnsureSmartCardTransportSupported(Transport.Nfc, new FirmwareVersion(5, 0, 0));
    }
}
