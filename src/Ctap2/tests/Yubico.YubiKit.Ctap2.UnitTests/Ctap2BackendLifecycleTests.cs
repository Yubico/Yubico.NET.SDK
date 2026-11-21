using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Collections;
using System.Collections.ObjectModel;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Ctap2.Backend;

namespace Yubico.YubiKit.Ctap2.UnitTests;

[CollectionDefinition(nameof(Ctap2BackendLifecycleTests), DisableParallelization = true)]
public sealed class Ctap2BackendLoggingCollection;

[Collection(nameof(Ctap2BackendLifecycleTests))]
public class Ctap2BackendLifecycleTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedExchange_UnwritableExceptionDataPreservesExactFailureAndClearsResponse(bool getterThrows)
    {
        var ct = TestContext.Current.CancellationToken;
        byte[] response = [0x00, 0x11, 0x22];
        var protocol = Substitute.For<IFidoHidProtocol>();
        protocol.SendCommandAsync(Arg.Any<byte>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<UserPresenceNotification>(), ct)
            .Returns(call => RequestAndReturnResponseAsync(call.ArgAt<UserPresenceNotification>(2), response, ct));
        var expected = new UnwritableDiagnosticException(getterThrows);
        var notification = CreateNotification(new RecordingUserPresencePrompt(new InvalidOperationException("resolution failed")));
        var backend = new HidBackend(protocol);
        using var factory = new FailingResolutionLoggerFactory(new InvalidOperationException("logging failed"));
        using var logging = YubiKitLogging.UseTemporary(factory);

        var actual = await Record.ExceptionAsync(() =>
            backend.SendCborAsync(new byte[] { CtapCommand.Reset }, notification, ct, onSuccess: () => throw expected));

        Assert.All(response, value => Assert.Equal(0, value));
        Assert.Same(expected, actual);
    }

    private sealed class UnwritableDiagnosticException(bool getterThrows) : Exception
    {
        public override IDictionary Data => getterThrows
            ? throw new InvalidOperationException("Diagnostics unavailable")
            : new ReadOnlyDictionary<object, object>(new Dictionary<object, object>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedExchange_PreservesStatusAndClearsResponseWhenResolutionAndLoggingFail(bool useHid)
    {
        var ct = TestContext.Current.CancellationToken;
        const CtapStatus status = CtapStatus.PinInvalid;
        byte[] response = [(byte)status, 0x11, 0x22, 0x33];
        byte[] apduBytes = [.. response, 0x90, 0x00];
        var apdu = new ApduResponse(apduBytes);
        var hid = Substitute.For<IFidoHidProtocol>();
        hid.SendCommandAsync(Arg.Any<byte>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<UserPresenceNotification>(), ct)
            .Returns(call => RequestAndReturnResponseAsync(call.ArgAt<UserPresenceNotification>(2), response, ct));
        var smartCard = Substitute.For<ISmartCardProtocol>();
        smartCard.TransmitAndReceiveAsync(Arg.Any<ApduCommand>(), cancellationToken: ct).Returns(apdu);
        ICtap2Backend backend = useHid ? new HidBackend(hid) : new SmartCardBackend(smartCard);
        ReadOnlyMemory<byte> retained = useHid ? response : apdu.Data;
        var callbackFailure = new InvalidOperationException("resolution failed");
        var loggerFailure = new InvalidOperationException("logging failed");
        var notification = CreateNotification(new RecordingUserPresencePrompt(callbackFailure));
        using var factory = new FailingResolutionLoggerFactory(loggerFailure);
        using var logging = YubiKitLogging.UseTemporary(factory);

        var error = await Assert.ThrowsAsync<CtapException>(() => backend.SendCborAsync(new byte[] { 4 }, notification, ct));

        Assert.Equal(status, error.Status);
        Assert.Same(callbackFailure, error.Data["UserPresenceResolutionException"]);
        Assert.Same(loggerFailure, error.Data["UserPresenceLoggingException"]);
        Assert.All(retained.ToArray(), value => Assert.Equal(0, value));
    }

    private sealed class FailingResolutionLoggerFactory(Exception failure) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => categoryName.EndsWith(nameof(UserPresenceNotification), StringComparison.Ordinal)
            ? new FailingResolutionLogger(failure) : NullLogger.Instance;
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }

    private sealed class FailingResolutionLogger(Exception failure) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => throw failure;
    }
    [Theory]
    [InlineData(0x6A82, true)]
    [InlineData(0x6D00, true)]
    [InlineData(0x6E00, true)]
    [InlineData(0x6F00, false)]
    public async Task SmartCardSelect_MapsOnlyUnavailableStatuses(short sw, bool unavailable)
    {
        var ct = TestContext.Current.CancellationToken;
        var expected = ApduException.FromStatusWord(sw);
        var protocol = Substitute.For<ISmartCardProtocol>();
        protocol.SelectAsync(Arg.Any<ReadOnlyMemory<byte>>(), ct).ThrowsAsync(expected);
        var backend = new SmartCardBackend(protocol);

        var actual = await Record.ExceptionAsync(() => backend.SelectApplicationAsync(ct));

        if (unavailable)
            Assert.Same(expected, Assert.IsType<NotSupportedException>(actual).InnerException);
        else
            Assert.Same(expected, actual);
    }

    [Fact]
    public async Task CborBackends_RejectMissingStatusByte()
    {
        var ct = TestContext.Current.CancellationToken;
        var smartCard = Substitute.For<ISmartCardProtocol>();
        smartCard.TransmitAndReceiveAsync(Arg.Any<ApduCommand>(), cancellationToken: ct)
            .Returns(new ApduResponse(new byte[] { 0x90, 0x00 }));
        var hid = Substitute.For<IFidoHidProtocol>();
        hid.SendCommandAsync(Arg.Any<byte>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<UserPresenceNotification>(), ct)
            .Returns(ReadOnlyMemory<byte>.Empty);
        byte[] request = [CtapCommand.GetInfo];

        await Assert.ThrowsAsync<BadResponseException>(() => new SmartCardBackend(smartCard).SendCborAsync(request, UserPresenceNotification.None, ct));
        await Assert.ThrowsAsync<BadResponseException>(() => new HidBackend(hid).SendCborAsync(request, UserPresenceNotification.None, ct));
    }

    [Fact]
    public void ICtap2Backend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(ICtap2Backend)));
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
        protocol.SendCommandAsync(
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
        await protocol.Received(1).SendCommandAsync(
            0x10,
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<UserPresenceNotification>(),
            cancellationToken: TestContext.Current.CancellationToken);
        await protocol.DidNotReceive().InitializeAsync(Arg.Any<CancellationToken>());
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
        protocol.SendCommandAsync(
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
        protocol.SendCommandAsync(
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