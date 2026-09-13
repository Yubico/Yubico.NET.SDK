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
        var protocolResponse = new ApduResponse(new byte[] { 0x00, 0x11, 0x90, 0x00 });
        protocol.TransmitAndReceiveAsync(Arg.Any<ApduCommand>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(protocolResponse);
        var backend = new SmartCardBackend(protocol);

        // Act
        var response = await backend.SendCborAsync(
            new byte[] { 0x04 },
            UserPresenceNotification.None,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new byte[] { 0x11 }, response.ToArray());
        Assert.Equal(new byte[] { 0x00, 0x11 }, protocolResponse.Data.ToArray());
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
        byte[] protocolResponse = [0x00, 0x11];
        protocol.SendVendorCommandAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(protocolResponse);
        var backend = new HidBackend(protocol);

        // Act
        var response = await backend.SendCborAsync(
            new byte[] { 0x04 },
            UserPresenceNotification.None,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new byte[] { 0x11 }, response.ToArray());
        Assert.Equal(new byte[] { 0x00, 0x11 }, protocolResponse);
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

    [Fact]
    public async Task HidBackend_WhenResolutionFails_ClearsUntransferredResponse()
    {
        byte[] response = [0x00, 0x11, 0x22, 0x33];
        var protocol = Substitute.For<IFidoHidProtocol>();
        protocol.SendVendorCommandAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                TestContext.Current.CancellationToken)
            .Returns(callInfo => RequestAndReturnResponseAsync(
                callInfo.ArgAt<UserPresenceNotification>(2),
                response,
                TestContext.Current.CancellationToken));
        var expected = new InvalidOperationException("resolution failed");
        UserPresenceNotification notification = CreateNotification(new RecordingUserPresencePrompt(expected));
        var backend = new HidBackend(protocol);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            backend.SendCborAsync(new byte[] { 0x04 }, notification, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.All(response, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task SmartCardBackend_WhenResolutionFails_ClearsUntransferredResponse()
    {
        var response = new ApduResponse(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x90, 0x00 });
        var protocol = Substitute.For<ISmartCardProtocol>();
        protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                cancellationToken: TestContext.Current.CancellationToken)
            .Returns(response);
        var expected = new InvalidOperationException("resolution failed");
        UserPresenceNotification notification = CreateNotification(new RecordingUserPresencePrompt(expected));
        var backend = new SmartCardBackend(protocol);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            backend.SendCborAsync(new byte[] { 0x04 }, notification, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.All(response.Data.ToArray(), value => Assert.Equal(0, value));
    }

    private static async Task<ReadOnlyMemory<byte>> RequestAndReturnStatusAsync(
        UserPresenceNotification notification,
        CtapStatus status,
        CancellationToken cancellationToken)
    {
        await notification.RequestAsync(UserPresenceBasis.DeviceWaiting, cancellationToken);
        return new byte[] { (byte)status };
    }

    private static async Task<ReadOnlyMemory<byte>> RequestAndReturnResponseAsync(
        UserPresenceNotification notification,
        ReadOnlyMemory<byte> response,
        CancellationToken cancellationToken)
    {
        await notification.RequestAsync(UserPresenceBasis.DeviceWaiting, cancellationToken);
        return response;
    }

    private static UserPresenceNotification CreateNotification(IUserPresencePrompt prompt) =>
        UserPresenceNotification.Create(
            prompt,
            new UserPresenceContext
            {
                Basis = UserPresenceBasis.PolicyRequires,
                Application = "FIDO2"
            });

    private sealed class RecordingUserPresencePrompt(Exception? resolutionException = null) : IUserPresencePrompt
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
            return resolutionException is null
                ? default
                : ValueTask.FromException(resolutionException);
        }
    }
}