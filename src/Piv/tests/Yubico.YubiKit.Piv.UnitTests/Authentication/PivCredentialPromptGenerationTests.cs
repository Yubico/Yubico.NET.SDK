using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

public sealed class PivCredentialPromptGenerationTests
{
    [Fact]
    public async Task GenerateRejectedAfterAuthentication_ClearsStateAndReacquiresOnNextCall()
    {
        using var directKey = new TestOwner(Enumerable.Range(1, 24).Select(i => (byte)i).ToArray());
        using var promptKey = new TestOwner(Enumerable.Range(1, 24).Select(i => (byte)i).ToArray());
        using var connection = new AuthCard(directKey.Memory, failGeneration: true);
        var prompt = new KeyPrompt(promptKey);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        await session.AuthenticateAsync(directKey.Memory, TestContext.Current.CancellationToken);
        Assert.True(session.IsManagementKeyAuthenticated);
        await Assert.ThrowsAsync<ApduException>(() => session.GenerateKeyAsync(PivSlot.Authentication,
            PivAlgorithm.EccP256, cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(session.IsManagementKeyAuthenticated);
        Assert.NotNull(await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256,
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, prompt.Requests);
        Assert.Equal(2, connection.WitnessRequests);
        Assert.Equal(2, connection.Generations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GenerateKey_PromptsMutualAuthThenGeneratesOnce_EvenIfGenerationFails(bool failGeneration)
    {
        using var key = new TestOwner(Enumerable.Range(1, 24).Select(i => (byte)i).ToArray());
        using var connection = new AuthCard(key.Memory, failGeneration);
        var prompt = new KeyPrompt(key);
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        if (failGeneration)
            await Assert.ThrowsAsync<ApduException>(() => session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, cancellationToken: TestContext.Current.CancellationToken));
        else
            Assert.NotNull(await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(1, prompt.Requests);
        Assert.Equal(1, connection.WitnessRequests);
        Assert.Equal(1, connection.ChallengeResponses);
        Assert.Equal(1, connection.Generations);
        Assert.Equal(!failGeneration, session.IsManagementKeyAuthenticated);
        Assert.True(key.Disposed);
        Assert.All(key.Bytes, b => Assert.Equal(0, b));
    }

    private sealed class KeyPrompt(TestOwner owner) : ICredentialPrompt
    {
        public int Requests { get; private set; }
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.Equal(CredentialKind.ManagementKey, context.Kind);
            Assert.Equal(24, context.MinLengthBytes);
            return ValueTask.FromResult<IMemoryOwner<byte>?>(owner);
        }
    }

    private sealed class TestOwner(byte[] bytes) : IMemoryOwner<byte>
    {
        public byte[] Bytes { get; } = bytes;
        public bool Disposed { get; private set; }
        public Memory<byte> Memory => Bytes;
        public void Dispose() => Disposed = true;
    }

    private sealed class AuthCard(ReadOnlyMemory<byte> key, bool failGeneration) : ISmartCardConnection
    {
        private readonly byte[] _key = key.ToArray();
        private int _initialization;
        public int WitnessRequests { get; private set; }
        public int ChallengeResponses { get; private set; }
        public int Generations { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public bool SupportsExtendedApdu() => false;
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => new EmptyTransaction();

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadOnlySpan<byte> apdu = command.Span;
            byte[] response;
            if (_initialization++ == 0)
                response = [0x90, 0x00]; // SELECT
            else if (apdu[1] == 0xFD)
                response = [0, 0, 1, 0x90, 0x00];
            else if (apdu[1] == 0xF7)
                response = [0x01, 0x01, (byte)PivManagementKeyType.Aes192, 0x90, 0x00];
            else if (apdu[1] == 0x87 && apdu[3] == 0x9B && apdu[8] == 0)
            {
                WitnessRequests++;
                response = [0x7C, 0x12, 0x80, 0x10, .. new byte[16], 0x90, 0x00];
            }
            else if (apdu[1] == 0x87 && apdu[3] == 0x9B)
            {
                ChallengeResponses++;
                using var aes = Aes.Create();
                aes.Key = _key;
                byte[] encrypted = aes.EncryptEcb(apdu[^17..^1], PaddingMode.None);
                response = [0x7C, 0x12, 0x82, 0x10, .. encrypted, 0x90, 0x00];
                CryptographicOperations.ZeroMemory(encrypted);
            }
            else if (apdu[1] == 0x47)
            {
                Generations++;
                if (failGeneration && Generations == 1)
                    response = [0x69, 0x82];
                else
                {
                    using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                    ECParameters parameters = ec.ExportParameters(false);
                    response = [0x7F, 0x49, 0x43, 0x86, 0x41, 0x04, .. parameters.Q.X ?? [], .. parameters.Q.Y ?? [], 0x90, 0x00];
                }
            }
            else
                throw new InvalidOperationException($"Unexpected instruction {apdu[1]:X2}");
            return Task.FromResult((ReadOnlyMemory<byte>)response);
        }

        public void Dispose() => CryptographicOperations.ZeroMemory(_key);
        public ValueTask DisposeAsync() { Dispose(); return default; }
        private sealed class EmptyTransaction : IDisposable { public void Dispose() { } }
    }
}
