using System.Buffers;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

public sealed class PivCredentialPromptTests
{
    [Theory]
    [InlineData(PivPinPolicy.Never)]
    [InlineData(PivPinPolicy.MatchAlways)]
    [InlineData(PivPinPolicy.MatchOnce)]
    public async Task PolicyWithoutTypedPin_DoesNotQueryOrPrompt(PivPinPolicy policy)
    {
        var connection = CreateConnection(Metadata(policy), [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        var prompt = new RecordingPrompt();
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Signature, PivAlgorithm.EccP256, new byte[32], TestContext.Current.CancellationToken);
        Assert.Empty(prompt.Contexts);
        Assert.Equal(new byte[] { 0xF7, 0x87 }, connection.TransmittedCommands.Skip(3).Select(c => c[1]));
    }

    [Fact]
    public async Task AlreadyVerified_QueriesButDoesNotPrompt()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [0x90, 0x00],
            [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        var prompt = new RecordingPrompt();
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        Assert.Empty(prompt.Contexts);
        Assert.Equal(new byte[] { 0xF7, 0x20, 0x87 }, connection.TransmittedCommands.Skip(3).Select(c => c[1]));
    }

    [Fact]
    public async Task Always_RequiresFreshVerificationEvenIfPinWasPreviouslyVerified()
    {
        // An empty VERIFY can return 9000 from an earlier Once verification, while
        // the Always-protected signing command still requires a new VERIFY.
        var connection = CreateConnection(Metadata(PivPinPolicy.Always), [0x06, 0x02, 3, 3, 0x90, 0x00],
            [0x90, 0x00],
            [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        var prompt = new RecordingPrompt([1, 2, 3, 4, 5, 6]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        Assert.Single(prompt.Contexts);
        Assert.Equal(3, prompt.Contexts[0].RetriesRemaining); // Status does not authorize Always.
        Assert.Equal(new byte[] { 0xF7, 0xF7, 0x20, 0x87 },
            connection.TransmittedCommands.Skip(3).Select(command => command[1]));
        Assert.Equal(14, connection.TransmittedCommands[^2].Length); // VERIFY carries PIN, not an empty probe.
    }

    [Fact]
    public async Task Always_BlockedPinMetadata_StopsBeforePromptOrVerify()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Always), [0x06, 0x02, 3, 0, 0x90, 0x00]);
        var prompt = new RecordingPrompt();
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<InvalidPinException>(() => session.SignOrDecryptAsync(
            PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken));
        Assert.Equal(0, error.RetriesRemaining);
        Assert.Empty(prompt.Contexts);
        Assert.Equal(new byte[] { 0xF7, 0xF7 }, connection.TransmittedCommands.Skip(3).Select(command => command[1]));
    }

    [Fact]
    public async Task Always_OneRetryRemaining_PromptsWithAuthoritativeCountAndVerifiesFresh()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Always), [0x06, 0x02, 3, 1, 0x90, 0x00],
            [0x90, 0x00], [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        var prompt = new RecordingPrompt([1, 2, 3, 4, 5, 6]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        Assert.Equal(1, Assert.Single(prompt.Contexts).RetriesRemaining);
        Assert.Equal(new byte[] { 0xF7, 0xF7, 0x20, 0x87 },
            connection.TransmittedCommands.Skip(3).Select(command => command[1]));
    }

    [Theory]
    [InlineData(0)] // Missing retry tag.
    [InlineData(1)] // Retry tag too short.
    [InlineData(2)] // Invalid total of zero.
    [InlineData(3)] // Remaining exceeds total.
    public async Task Always_UnusableRetryMetadata_DoesNotInventBlockedStatusOrCount(int variant)
    {
        byte[] pinMetadata = variant switch
        {
            0 => [0x05, 0x01, 0x01, 0x90, 0x00],
            1 => [0x06, 0x01, 0x00, 0x90, 0x00],
            2 => [0x06, 0x02, 0x00, 0x00, 0x90, 0x00],
            _ => [0x06, 0x02, 0x01, 0x02, 0x90, 0x00]
        };
        var connection = CreateConnection(Metadata(PivPinPolicy.Always), pinMetadata,
            [0x90, 0x00], [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        var prompt = new RecordingPrompt([1, 2, 3, 4, 5, 6]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        Assert.Null(Assert.Single(prompt.Contexts).RetriesRemaining);
        Assert.Equal(2, connection.TransmittedCommands.Skip(3).Count(command => command[1] == 0xF7));
    }

    [Theory]
    [InlineData(0x69, 0x83, typeof(InvalidPinException))]
    [InlineData(0x6A, 0x80, typeof(ApduException))]
    public async Task PinStateFailure_DoesNotPromptOrSign(byte sw1, byte sw2, Type expected)
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [sw1, sw2]);
        var prompt = new RecordingPrompt();
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        Exception error = await Record.ExceptionAsync(() => session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken))
            ?? throw new InvalidOperationException("Expected state failure");
        Assert.IsType(expected, error);
        Assert.Empty(prompt.Contexts);
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x87);
    }

    [Fact]
    public async Task AlgorithmMismatchAndEmptySlot_RejectBeforePrompt()
    {
        var prompt = new RecordingPrompt();
        var mismatch = CreateConnection(Metadata(PivPinPolicy.Once));
        await using (var session = await PivSession.CreateAsync(mismatch, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken))
            await Assert.ThrowsAsync<ArgumentException>(() => session.SignOrDecryptAsync(PivSlot.Signature, PivAlgorithm.Rsa2048, new byte[32], TestContext.Current.CancellationToken));
        var empty = CreateConnection([0x6A, 0x88]);
        await using (var session = await PivSession.CreateAsync(empty, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken))
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken));
        Assert.Empty(prompt.Contexts);
    }

    [Fact]
    public async Task ManagementWitnessFailure_IsNotARejectedHostKey()
    {
        var connection = CreateConnection([0x69, 0x82]);
        var prompt = new RecordingPrompt(Enumerable.Range(1, 24).Select(i => (byte)i).ToArray());
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        ApduException error = await Assert.ThrowsAsync<ApduException>(() => session.GenerateKeyAsync(PivSlot.Signature, PivAlgorithm.EccP256, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal((short?)0x6982, error.SW);
        Assert.Single(prompt.Contexts);
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x47);
    }

    [Fact]
    public async Task WrongPinReachesZero_DoesNotRequestAgain()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [0x63, 0xC1], [0x63, 0xC0]);
        var prompt = new RecordingPrompt([1, 2, 3, 4, 5, 6]);
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        InvalidPinException error = await Assert.ThrowsAsync<InvalidPinException>(() => session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken));
        Assert.Equal(0, error.RetriesRemaining);
        Assert.Single(prompt.Contexts);
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x87);
    }

    [Fact]
    public async Task Always_TwoOperationsAcquireFreshPinAndSignOnceEach()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Always), [0x06, 0x02, 3, 3, 0x90, 0x00], [0x90, 0x00],
            [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00],
            Metadata(PivPinPolicy.Always), [0x06, 0x02, 3, 3, 0x90, 0x00], [0x90, 0x00],
            [0x7C, 0x03, 0x82, 0x01, 0xBB, 0x90, 0x00]);
        var prompt = new RecordingPrompt([1, 2, 3, 4, 5, 6], [1, 2, 3, 4, 5, 6]);
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        Assert.Equal(2, prompt.Contexts.Count);
        Assert.Equal(2, connection.TransmittedCommands.Count(c => c[1] == 0x87));
        Assert.Equal(2, connection.TransmittedCommands.Count(c => c[1] == 0x20 && c.Length == 14));
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x20 && c.Length == 5);
        Assert.Equal(2, connection.TransmittedCommands.Count(c => c[1] == 0xF7 && c[3] == (byte)PivSlot.Signature));
    }

    [Fact]
    public async Task BadLengthThenDecline_NeverSubmitsPinOrSigns()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [0x63, 0xC3]);
        var prompt = new RecordingPrompt([1]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = new DecliningSecondPrompt(prompt) }, TestContext.Current.CancellationToken);
        CredentialPromptDeclinedException error = await Assert.ThrowsAsync<CredentialPromptDeclinedException>(
            () => session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken));
        Assert.Equal(CredentialKind.Pin, error.Kind);
        Assert.True(Assert.Single(prompt.Owners).Disposed);
        Assert.Equal(5, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(PivPinPolicy.Default)]
    [InlineData((PivPinPolicy)0x7F)]
    public async Task UncertainPinPolicy_QueriesVerificationAndPrompts(PivPinPolicy policy)
    {
        await AssertUncertainPolicyAsync(Metadata(policy));
    }

    [Fact]
    public async Task MissingPinPolicyTag_QueriesVerificationAndPrompts() =>
        await AssertUncertainPolicyAsync([0x01, 0x01, (byte)PivAlgorithm.EccP256, 0x90, 0x00]);

    private static async Task AssertUncertainPolicyAsync(byte[] metadata)
    {
        var connection = CreateConnection(metadata, [0x63, 0xC3], [0x90, 0x00],
            [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        var prompt = new RecordingPrompt([1, 2, 3, 4, 5, 6]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);
        Assert.Single(prompt.Contexts);
        Assert.Equal(new byte[] { 0xF7, 0x20, 0x20, 0x87 },
            connection.TransmittedCommands.Skip(3).Select(command => command[1]));
    }

    [Fact]
    public async Task PromptOnOldFirmware_ExplicitSigningKeepsExistingCommandSequence()
    {
        var connection = CreateConnection([0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        var prompt = new RecordingPrompt();
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt, FirmwareVersionOverride = new(5, 2, 0) },
            TestContext.Current.CancellationToken);
        _ = await session.SignOrDecryptAsync(PivSlot.Signature, PivAlgorithm.EccP256, new byte[32],
            TestContext.Current.CancellationToken);
        Assert.Empty(prompt.Contexts);
        Assert.Equal(new byte[] { 0x87 }, connection.TransmittedCommands.Skip(3).Select(c => c[1]));
    }

    [Fact]
    public async Task PinPromptBoundReached_ThrowsLastDeviceRejectionWithoutSigning()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [0x63, 0xC3], [0x63, 0xC2], [0x63, 0xC1]);
        var prompt = new RecordingPrompt([1, 2, 3, 4, 5, 6], [1, 2, 3, 4, 5, 6]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt, MaxCredentialPromptAttempts = 2 },
            TestContext.Current.CancellationToken);
        InvalidPinException error = await Assert.ThrowsAsync<InvalidPinException>(
            () => session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken));
        Assert.Equal(1, error.RetriesRemaining);
        Assert.Equal(2, prompt.Contexts.Count);
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x87);
    }

    [Fact]
    public async Task DeviceAuthMismatch_PropagatesWithoutRetryingOrGenerating()
    {
        var connection = CreateConnection([0x7C, 0x0A, 0x80, 0x08, .. new byte[8], 0x90, 0x00],
            [0x7C, 0x0A, 0x82, 0x08, .. new byte[8], 0x90, 0x00]);
        var prompt = new RecordingPrompt(Enumerable.Range(1, 24).Select(i => (byte)i).ToArray());
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        ApduException error = await Assert.ThrowsAsync<ApduException>(() => session.GenerateKeyAsync(
            PivSlot.Authentication, PivAlgorithm.EccP256, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("device response mismatch", error.Message);
        Assert.Single(prompt.Contexts);
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x47);
    }

    [Fact]
    public async Task ManagementKeyInvalidLengthThenDecline_DoesNotSubmitOrGenerate()
    {
        var connection = CreateConnection();
        var prompt = new RecordingPrompt([1]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = new DecliningSecondPrompt(prompt) },
            TestContext.Current.CancellationToken);
        CredentialPromptDeclinedException error = await Assert.ThrowsAsync<CredentialPromptDeclinedException>(
            () => session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256,
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(CredentialKind.ManagementKey, error.Kind);
        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.All(Assert.Single(prompt.Owners).Bytes, b => Assert.Equal(0, b));
    }

    private sealed class DecliningSecondPrompt(RecordingPrompt first) : ICredentialPrompt
    {
        private int _requests;
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken) =>
            ++_requests == 1 ? first.RequestSecretAsync(context, cancellationToken) : ValueTask.FromResult<IMemoryOwner<byte>?>(null);
    }

    [Fact]
    public async Task Once_WrongThenCorrectPin_UsesFreshOwnersAndSignsOnlyOnce()
    {
        var connection = CreateConnection(
            Metadata(PivPinPolicy.Once), [0x63, 0xC3], [0x63, 0xC2], [0x90, 0x00],
            [0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        var prompt = new RecordingPrompt([1, 2, 3, 4, 5, 6], [6, 5, 4, 3, 2, 1]);
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        _ = await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken);

        Assert.Equal(2, prompt.Contexts.Count);
        Assert.Equal((CredentialKind.Pin, "PIV", 6, 8, (int?)2, true),
            (prompt.Contexts[1].Kind, prompt.Contexts[1].Application, prompt.Contexts[1].MinLengthBytes,
             prompt.Contexts[1].MaxLengthBytes, prompt.Contexts[1].RetriesRemaining, prompt.Contexts[1].IsRetry));
        Assert.All(prompt.Owners, owner => { Assert.True(owner.Disposed); Assert.All(owner.Bytes, b => Assert.Equal(0, b)); });
        Assert.Equal(new byte[] { 0xF7, 0x20, 0x20, 0x20, 0x87 },
            connection.TransmittedCommands.Skip(3).Select(command => command[1]));
    }

    [Fact]
    public async Task PromptPending_RejectsReentrantAndOverlappingCalls_AndCancellationReleasesLateOwner()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [0x63, 0xC3]);
        var pending = new PendingPrompt();
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = pending }, TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();
        Task<ReadOnlyMemory<byte>> sign = session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], cts.Token);
        await pending.Started.Task;
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetPinMetadataAsync(TestContext.Current.CancellationToken));
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sign);
        var owner = new TrackingOwner([1, 2, 3, 4, 5, 6]);
        pending.Complete(owner);
        await owner.Disposal.Task;
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
        Assert.Equal(5, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task CallbackDisposesSession_DoesNotDeadlockOrSendVerify()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [0x63, 0xC3]);
        PivSession? session = null;
        var owner = new TrackingOwner([1, 2, 3, 4, 5, 6]);
        var prompt = new CallbackPrompt(async () => await (session ?? throw new InvalidOperationException()).DisposeAsync(), owner);
        session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);

        Exception error = await Record.ExceptionAsync(async () =>
            await session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken))
            ?? throw new InvalidOperationException("Disposed session unexpectedly signed");
        Assert.IsNotType<TimeoutException>(error);
        Assert.Equal(5, connection.TransmittedCommands.Count);
        await owner.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task ExternalDisposalDuringPrompt_CancelsOperationAndReleasesLateOwner()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [0x63, 0xC3]);
        var pending = new PendingPrompt();
        var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = pending },
            TestContext.Current.CancellationToken);
        Task<ReadOnlyMemory<byte>> sign = session.SignOrDecryptAsync(PivSlot.Signature, new byte[32],
            TestContext.Current.CancellationToken);
        await pending.Started.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        Task disposal = session.DisposeAsync().AsTask();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sign.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        await disposal.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        var owner = new TrackingOwner([1, 2, 3, 4, 5, 6]);
        pending.Complete(owner);
        await owner.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
        Assert.Equal(5, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task CallbackReentersSession_IsRejectedWithoutConsumingPinAttempt()
    {
        var connection = CreateConnection(Metadata(PivPinPolicy.Once), [0x63, 0xC3]);
        PivSession? session = null;
        var prompt = new CallbackPrompt(async () =>
            await (session ?? throw new InvalidOperationException()).GetPinMetadataAsync(TestContext.Current.CancellationToken));
        session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        await using (session)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.SignOrDecryptAsync(PivSlot.Signature, new byte[32], TestContext.Current.CancellationToken));
            Assert.Equal(5, connection.TransmittedCommands.Count);
        }
    }

    [Fact]
    public async Task GenerateKey_WrongKeyResponse_RetriesOnlyChallengeAndGeneratesOnce()
    {
        var connection = CreateConnection(
            [0x7C, 0x0A, 0x80, 0x08, .. new byte[8], 0x90, 0x00], [0x69, 0x82],
            [0x7C, 0x0A, 0x80, 0x08, .. new byte[8], 0x90, 0x00], [0x69, 0x82]);
        var prompt = new RecordingPrompt(Enumerable.Range(1, 24).Select(i => (byte)i).ToArray(),
            Enumerable.Range(1, 24).Select(i => (byte)i).ToArray());
        await using var session = await PivSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt, MaxCredentialPromptAttempts = 2 }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<ApduException>(() => session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(2, prompt.Contexts.Count);
        Assert.All(prompt.Contexts, context => { Assert.Equal(CredentialKind.ManagementKey, context.Kind); Assert.Null(context.RetriesRemaining); Assert.Equal(24, context.MinLengthBytes); });
        Assert.All(prompt.Owners, owner => { Assert.True(owner.Disposed); Assert.All(owner.Bytes, b => Assert.Equal(0, b)); });
        Assert.DoesNotContain(connection.TransmittedCommands, command => command[1] == 0x47);
    }

    private static RecordingSmartCardConnection CreateConnection(params byte[][] responses) =>
        new([[0x90, 0x00], [0, 0, 1, 0x90, 0x00],
            [0x01, 0x01, (byte)PivManagementKeyType.TripleDes, 0x90, 0x00], .. responses]);

    private static byte[] Metadata(PivPinPolicy policy) =>
        [0x01, 0x01, (byte)PivAlgorithm.EccP256, 0x02, 0x02, (byte)policy, (byte)PivTouchPolicy.Never, 0x90, 0x00];

    private sealed class TrackingOwner(byte[] bytes) : IMemoryOwner<byte>
    {
        public byte[] Bytes { get; } = bytes;
        public TaskCompletionSource Disposal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Disposed { get; private set; }
        public Memory<byte> Memory => Bytes;
        public void Dispose() { Disposed = true; Disposal.TrySetResult(); }
    }

    private sealed class RecordingPrompt(params byte[][] secrets) : ICredentialPrompt
    {
        private readonly Queue<byte[]> _secrets = new(secrets);
        public List<CredentialPromptContext> Contexts { get; } = [];
        public List<TrackingOwner> Owners { get; } = [];
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken)
        {
            Contexts.Add(context);
            var owner = new TrackingOwner(_secrets.Dequeue());
            Owners.Add(owner);
            return ValueTask.FromResult<IMemoryOwner<byte>?>(owner);
        }
    }

    private sealed class PendingPrompt : ICredentialPrompt
    {
        private readonly TaskCompletionSource<IMemoryOwner<byte>?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken)
        { Started.TrySetResult(); return new(_result.Task); }
        public void Complete(IMemoryOwner<byte> owner) => _result.SetResult(owner);
    }

    private sealed class CallbackPrompt(Func<Task> callback, TrackingOwner? owner = null) : ICredentialPrompt
    {
        public async ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken)
        {
            await callback();
            return owner ?? new TrackingOwner([1, 2, 3, 4, 5, 6]);
        }
    }
}
