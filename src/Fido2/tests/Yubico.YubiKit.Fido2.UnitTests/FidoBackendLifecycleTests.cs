using NSubstitute;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Fido2.Backend;

namespace Yubico.YubiKit.Fido2.UnitTests;

public class FidoBackendLifecycleTests
{
    [Fact]
    public void IFidoBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(IFidoBackend)));
    }

    [Fact]
    public void SmartCardBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(SmartCardBackend)));
    }

    [Fact]
    public void HidBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(HidBackend)));
    }

    [Fact]
    public async Task SmartCardBackend_SendCborAsync_UsesBorrowedProtocol()
    {
        // Arrange
        var protocol = Substitute.For<ISmartCardProtocol>();
        protocol.TransmitAndReceiveAsync(Arg.Any<ApduCommand>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(new ApduResponse(new byte[] { 0x00, 0x90, 0x00 }));
        var backend = new SmartCardBackend(protocol);

        // Act
        var response = await backend.SendCborAsync(new byte[] { 0x04 }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(response.ToArray());
        await protocol.Received(1).TransmitAndReceiveAsync(
            Arg.Any<ApduCommand>(),
            true,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HidBackend_SendCborAsync_UsesBorrowedProtocol()
    {
        // Arrange
        var protocol = Substitute.For<IFidoHidProtocol>();
        protocol.SendVendorCommandAsync(Arg.Any<byte>(), Arg.Any<ReadOnlyMemory<byte>>(), TestContext.Current.CancellationToken)
            .Returns(new byte[] { 0x00 });
        var backend = new HidBackend(protocol);

        // Act
        var response = await backend.SendCborAsync(new byte[] { 0x04 }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(response.ToArray());
        await protocol.Received(1).SendVendorCommandAsync(
            0x10,
            Arg.Any<ReadOnlyMemory<byte>>(),
            TestContext.Current.CancellationToken);
    }
}