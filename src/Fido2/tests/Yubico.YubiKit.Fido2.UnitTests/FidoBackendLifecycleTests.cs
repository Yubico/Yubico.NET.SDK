using NSubstitute;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.SmartCard;
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
    public void SmartCardFidoBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(SmartCardFidoBackend)));
    }

    [Fact]
    public void FidoHidBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(FidoHidBackend)));
    }

    [Fact]
    public async Task SmartCardFidoBackend_SendCborAsync_UsesBorrowedProtocol()
    {
        // Arrange
        var protocol = Substitute.For<ISmartCardProtocol>();
        protocol.TransmitAndReceiveAsync(Arg.Any<ApduCommand>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(new ApduResponse(new byte[] { 0x00, 0x90, 0x00 }));
        var backend = new SmartCardFidoBackend(protocol);

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
    public async Task FidoHidBackend_SendCborAsync_UsesBorrowedProtocol()
    {
        // Arrange
        var protocol = Substitute.For<IFidoHidProtocol>();
        protocol.SendVendorCommandAsync(Arg.Any<byte>(), Arg.Any<ReadOnlyMemory<byte>>(), TestContext.Current.CancellationToken)
            .Returns(new byte[] { 0x00 });
        var backend = new FidoHidBackend(protocol);

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