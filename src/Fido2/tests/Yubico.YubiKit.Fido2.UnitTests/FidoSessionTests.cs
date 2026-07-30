using FluentAssertions;
using NSubstitute;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;

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
    public async Task CreateAsync_AppletProbeFailure_DisposesProtocolExactlyOnce()
    {
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Transport.Returns(Transport.Usb);
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReadOnlyMemory<byte>>(
                new InvalidOperationException("session-init probe failure")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FidoSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        connection.Received(1).Dispose();
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

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 255, 255)]
    public void EnsureSmartCardTransportSupported_UsbSentinelFirmware_Succeeds(int major, int minor, int patch)
    {
        FidoSession.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(major, minor, patch));
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_ReportedNfcBefore58_Succeeds()
    {
        FidoSession.EnsureSmartCardTransportSupported(Transport.Nfc, new FirmwareVersion(5, 0, 0));
    }
}