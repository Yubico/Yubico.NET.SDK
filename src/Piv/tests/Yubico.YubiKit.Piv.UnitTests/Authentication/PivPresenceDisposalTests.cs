using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

public sealed class PivPresenceDisposalTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PresenceCallback_DisposesWithoutSelfWait(bool resolve, bool synchronous)
    {
        var connection = new RecordingSmartCardConnection(
            [0x90, 0x00], [0, 0, 1, 0x90, 0x00],
            [0x01, 0x01, (byte)PivManagementKeyType.TripleDes, 0x90, 0x00],
            [0x01, 0x01, (byte)PivAlgorithm.EccP256, 0x02, 0x02, (byte)PivPinPolicy.Never,
                (byte)PivTouchPolicy.Always, 0x90, 0x00],
            [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        PivSession? session = null;
        var prompt = new DisposingPrompt(() => session ?? throw new InvalidOperationException(), resolve, synchronous);
        session = await PivSession.CreateAsync(connection, new SessionCreationOptions { UserPresencePrompt = prompt }, TestContext.Current.CancellationToken);

        Exception? error = await Record.ExceptionAsync(async () =>
            await Task.Run(() => session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        Assert.IsNotType<TimeoutException>(error);
        Assert.Equal(resolve ? 5 : 4, connection.TransmittedCommands.Count);
        Assert.Equal(1, prompt.Resolutions); // A successfully returned request is still resolved.
    }

    private sealed class DisposingPrompt(Func<PivSession> getSession, bool resolve, bool synchronous) : IUserPresencePrompt
    {
        public int Resolutions { get; private set; }

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
            resolve ? default : DisposeSessionAsync();

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolutions++;
            return resolve ? DisposeSessionAsync() : default;
        }

        private ValueTask DisposeSessionAsync()
        {
            if (synchronous)
            {
                getSession().Dispose();
                return default;
            }
            return getSession().DisposeAsync();
        }
    }
}
