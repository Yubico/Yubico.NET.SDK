using NSubstitute;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Fido2.Backend;
using Yubico.YubiKit.Fido2.Ctap;

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
        var response = await backend.SendCborAsync(
            new byte[] { 0x04 },
            UserPresenceNotification.None,
            cancellationToken: TestContext.Current.CancellationToken);

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
        protocol.SendVendorCommandAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(new byte[] { 0x00 });
        var backend = new HidBackend(protocol);

        // Act
        var response = await backend.SendCborAsync(
            new byte[] { 0x04 },
            UserPresenceNotification.None,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(response.ToArray());
        await protocol.Received(1).SendVendorCommandAsync(
            0x10,
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<UserPresenceNotification>(),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HidBackend_InitializeAsync_InitializesBorrowedProtocol()
    {
        // Arrange
        var protocol = Substitute.For<IFidoHidProtocol>();
        var backend = new HidBackend(protocol);

        // Act
        await backend.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await protocol.Received(1).InitializeAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(CtapStatus.KeepAliveCancel, UserPresenceOutcome.Cancelled)]
    [InlineData(CtapStatus.UserActionTimeout, UserPresenceOutcome.TimedOut)]
    [InlineData(CtapStatus.Other, UserPresenceOutcome.Failed)]
    public async Task HidBackend_CtapFailure_ResolvesAtBackendWithMappedOutcome(
        CtapStatus status,
        UserPresenceOutcome expectedOutcome)
    {
        var protocol = Substitute.For<IFidoHidProtocol>();
        protocol.SendVendorCommandAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                TestContext.Current.CancellationToken)
            .Returns(callInfo => RequestAndReturnStatusAsync(
                callInfo.ArgAt<UserPresenceNotification>(2),
                status,
                TestContext.Current.CancellationToken));
        var prompt = new RecordingUserPresencePrompt();
        UserPresenceNotification notification = UserPresenceNotification.Create(
            prompt,
            new UserPresenceContext
            {
                Basis = UserPresenceBasis.PolicyRequires,
                Application = "FIDO2"
            });
        var backend = new HidBackend(protocol);

        CtapException exception = await Assert.ThrowsAsync<CtapException>(() => backend.SendCborAsync(
            new byte[] { 0x04 },
            notification,
            TestContext.Current.CancellationToken));

        Assert.Equal(status, exception.Status);
        Assert.Equal(expectedOutcome, Assert.Single(prompt.Outcomes));
    }

    private static async Task<ReadOnlyMemory<byte>> RequestAndReturnStatusAsync(
        UserPresenceNotification notification,
        CtapStatus status,
        CancellationToken cancellationToken)
    {
        await notification.RequestAsync(UserPresenceBasis.DeviceWaiting, cancellationToken);
        return new byte[] { (byte)status };
    }

    private sealed class RecordingUserPresencePrompt : IUserPresencePrompt
    {
        public List<UserPresenceOutcome> Outcomes { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken) => default;

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Outcomes.Add(outcome);
            return default;
        }
    }
}