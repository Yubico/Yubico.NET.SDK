using System.Buffers;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

public sealed class PivAdmissionTests
{
    [Fact]
    public async Task StatefulAlways_ConsumesVerificationAfterEverySignature()
    {
        var card = new StatefulAlwaysCard();
        var prompt = new PinPrompt();
        await using var session = await PivSession.CreateAsync(card,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        Assert.Equal(2, card.Signatures);
        Assert.Equal(0, card.StateQueries);
        Assert.Equal(2, prompt.Requests);
        Assert.False(card.Verified);
    }

    private sealed class PinPrompt : ICredentialPrompt
    {
        public int Requests { get; private set; }
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context,
            CancellationToken cancellationToken)
        {
            Requests++;
            Assert.Equal(CredentialKind.Pin, context.Kind);
            return ValueTask.FromResult<IMemoryOwner<byte>?>(DisposableArrayPoolBuffer.CreateFromSpan("123456"u8));
        }
    }

    private sealed class StatefulAlwaysCard : ISmartCardConnection
    {
        private int _initialization;
        public int Signatures { get; private set; }
        public int StateQueries { get; private set; }
        public bool Verified { get; private set; }
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;
        public bool SupportsExtendedApdu() => false;
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => new EmptyTransaction();
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadOnlySpan<byte> apdu = command.Span;
            byte[] response;
            if (_initialization++ == 0)
                response = [0x90, 0x00];
            else if (apdu[1] == 0xFD)
                response = [0, 0, 1, 0x90, 0x00];
            else if (apdu[1] == 0xF7 && apdu[3] == 0x9B)
                response = [0x01, 0x01, (byte)PivManagementKeyType.TripleDes, 0x90, 0x00];
            else if (apdu[1] == 0xF7 && apdu[3] == 0x80)
                response = [0x06, 0x02, 3, 3, 0x90, 0x00];
            else if (apdu[1] == 0xF7)
                response = [0x01, 0x01, (byte)PivAlgorithm.EccP256, 0x02, 0x02,
                    (byte)PivPinPolicy.Always, (byte)PivTouchPolicy.Never, 0x90, 0x00];
            else if (apdu[1] == 0x20 && apdu.Length == 5)
            {
                StateQueries++;
                response = Verified ? [0x90, 0x00] : [0x63, 0xC3];
            }
            else if (apdu[1] == 0x20)
            {
                Verified = true;
                response = [0x90, 0x00];
            }
            else if (apdu[1] == 0x87 && Verified)
            {
                Signatures++;
                Verified = false;
                response = [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00];
            }
            else
                throw new InvalidOperationException("Unexpected or unverified SmartCard command");
            return Task.FromResult((ReadOnlyMemory<byte>)response);
        }
        public void Dispose() { }
        public ValueTask DisposeAsync() => default;
        private sealed class EmptyTransaction : IDisposable { public void Dispose() { } }
    }

    [Fact]
    public async Task DirectOperationInFlight_RejectsPromptedSignBeforeFirstExchange()
    {
        var connection = new BlockingPinMetadataConnection();
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = new DecliningPrompt() }, TestContext.Current.CancellationToken);
        Task<PivPinMetadata> direct = session.GetPinMetadataAsync(TestContext.Current.CancellationToken);
        await connection.Started.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SignOrDecryptAsync(
            PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken));
        Assert.Equal(3, connection.Commands.Count); // The held command has not been transmitted yet.

        connection.Release.TrySetResult();
        Assert.Equal(3, (await direct).RetriesRemaining);
        Assert.Equal(4, connection.Commands.Count);
    }

    [Fact]
    public async Task StaleCallbackContext_CannotSkipLaterOperationDrain()
    {
        var connection = new BlockingPinMetadataConnection(signFirst: true);
        PivSession? session = null;
        var resumeChild = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposalStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var prompt = new CapturingPrompt(() => session ?? throw new InvalidOperationException(), resumeChild.Task,
            disposalStarted);
        session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt },
            TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);

        Task<PivPinMetadata> direct = session.GetPinMetadataAsync(TestContext.Current.CancellationToken);
        await connection.Started.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            resumeChild.TrySetResult();
            await disposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<TimeoutException>(() => prompt.DisposalFinished.WaitAsync(
                TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken));
        }
        finally
        {
            connection.Release.TrySetResult();
        }
        Assert.Equal(3, (await direct).RetriesRemaining);
        await prompt.DisposalFinished.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
    }

    private sealed class CapturingPrompt(Func<PivSession> getSession, Task resumeChild,
        TaskCompletionSource disposalStarted) : ICredentialPrompt
    {
        public Task DisposalFinished { get; private set; } = Task.CompletedTask;
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context,
            CancellationToken cancellationToken)
        {
            DisposalFinished = Task.Run(async () =>
            {
                await resumeChild;
                ValueTask disposing = getSession().DisposeAsync();
                disposalStarted.TrySetResult();
                await disposing;
            }, cancellationToken);
            return ValueTask.FromResult<IMemoryOwner<byte>?>(DisposableArrayPoolBuffer.CreateFromSpan("123456"u8));
        }
    }

    private sealed class DecliningPrompt : ICredentialPrompt
    {
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context,
            CancellationToken cancellationToken) => ValueTask.FromResult<IMemoryOwner<byte>?>(null);
    }

    private sealed class BlockingPinMetadataConnection : ISmartCardConnection
    {
        private readonly RecordingSmartCardConnection _connection;
        public BlockingPinMetadataConnection(bool signFirst = false)
        {
            _connection = signFirst
                ? new RecordingSmartCardConnection(
                    [0x90, 0x00], [0, 0, 1, 0x90, 0x00],
                    [0x01, 0x01, (byte)PivManagementKeyType.TripleDes, 0x90, 0x00],
                    [0x01, 0x01, (byte)PivAlgorithm.EccP256, 0x02, 0x02,
                        (byte)PivPinPolicy.Once, (byte)PivTouchPolicy.Never, 0x90, 0x00],
                    [0x63, 0xC3], [0x90, 0x00], [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00],
                    [0x06, 0x02, 3, 3, 0x90, 0x00])
                : new RecordingSmartCardConnection(
                    [0x90, 0x00], [0, 0, 1, 0x90, 0x00],
                    [0x01, 0x01, (byte)PivManagementKeyType.TripleDes, 0x90, 0x00],
                    [0x06, 0x02, 3, 3, 0x90, 0x00]);
        }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<byte[]> Commands => _connection.TransmittedCommands;
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;
        public bool SupportsExtendedApdu() => false;
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            _connection.BeginTransaction(cancellationToken);
        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            if (command.Span[1] == 0xF7 && command.Span[3] == 0x80)
            {
                Started.TrySetResult();
                await Release.Task.ConfigureAwait(false);
            }
            return await _connection.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        }
        public void Dispose() => _connection.Dispose();
        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}
