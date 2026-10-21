// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using System.Reflection;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.YubiHsm.Backend;

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class HsmAuthCredentialPromptTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SingleCommandCores_ClearEncodedPayloadAndRawResponseBeforeInputRelease(bool delete, bool reject)
    {
        var backend = new CapturingBackend(reject);
        var owner = new SecretOwner(delete ? new byte[16] : [0x61])
        {
            OnDispose = () =>
            {
                Assert.NotEmpty(backend.Payloads);
                foreach (var memory in backend.Payloads)
                    foreach (byte value in memory.Span) Assert.Equal(0, value);
                foreach (var memory in backend.Responses)
                    foreach (byte value in memory.Span) Assert.Equal(0, value);
            }
        };
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        var connection = new RecordingSmartCardConnection([0x90, 0]);
        await using var session = await Create(connection, prompt, attempts: 1);
        // Recording transport tests pin real framing separately. This one-hop backend seam retains
        // borrowed payload/response views specifically to observe the session's zeroing boundaries.
        (typeof(HsmAuthSession).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing backend field")).SetValue(session, backend);
        async Task Invoke()
        {
            if (delete) await session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken);
            else { using var keys = await session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken); }
        }
        if (reject) await Assert.ThrowsAsync<HsmAuthRetryException>(Invoke); else await Invoke();
        Assert.Single(backend.Payloads);
        Assert.Equal(1, owner.DisposeCount);
    }

    private sealed class CapturingBackend(bool reject) : IHsmAuthBackend
    {
        public List<ReadOnlyMemory<byte>> Payloads { get; } = [];
        public List<ReadOnlyMemory<byte>> Responses { get; } = [];
        public Task<FirmwareVersion> InitializeAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            if (command.Ins == 5) return Task.FromResult(new ApduResponse(List()));
            if (command.Ins == 9) return Task.FromResult(new ApduResponse((byte[])[8, 0x90, 0]));
            Assert.False(throwOnError);
            Assert.True(command.Ins is 2 or 3);
            Payloads.Add(command.Data);
            var response = new ApduResponse(reject ? (byte[])[0x63, 0xC7] : command.Ins == 2 ? (byte[])[0x90, 0] : Keys());
            Responses.Add(response.RawData);
            return Task.FromResult(response);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task PendingPrompt_ExcludesEveryDirectDeviceOperation(int operation)
    {
        var pending = new TaskCompletionSource<IMemoryOwner<byte>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var prompt = new SecretPrompt((_, _) => { entered.SetResult(); return new(pending.Task); });
        var connection = new RecordingSmartCardConnection([0x90, 0], List());
        await using var session = await Create(connection, prompt);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var calculating = session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        var token = TestContext.Current.CancellationToken;
        var bytes = new byte[16];
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => operation switch
            {
                0 => session.ListCredentialsAsync(token),
                1 => session.PutCredentialSymmetricAsync(bytes, "cred", bytes, bytes, bytes, cancellationToken: token),
                2 => session.PutCredentialDerivedAsync(bytes, "cred", bytes, bytes, cancellationToken: token),
                3 => session.DeleteCredentialAsync(bytes, "cred", token),
                4 => session.CalculateSessionKeysSymmetricAsync("cred", bytes, bytes, cancellationToken: token),
                5 => session.GetManagementKeyRetriesAsync(token),
                6 => session.PutManagementKeyAsync(bytes, bytes, token),
                7 => session.ResetAsync(token),
                8 => session.CalculateSessionKeysAsymmetricAsync("cred", bytes, bytes, bytes, bytes, token),
                9 => session.GetChallengeAsync("cred", cancellationToken: token),
                10 => session.PutCredentialAsymmetricAsync(bytes, "cred", bytes, bytes, cancellationToken: token),
                11 => session.GenerateCredentialAsymmetricAsync(bytes, "cred", bytes, cancellationToken: token),
                12 => session.GetPublicKeyAsync("cred", token),
                13 => session.ChangeCredentialPasswordAsync("cred", bytes, bytes, token),
                _ => session.ChangeCredentialPasswordAdminAsync(bytes, "cred", bytes, token)
            });
            Assert.Equal(2, connection.TransmittedCommands.Count);
        }
        finally { cancellation.Cancel(); pending.TrySetResult(null); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => calculating.WaitAsync(TimeSpan.FromSeconds(3), token));
    }

    [Fact]
    public async Task NoProvider_BothPromptedMethodsFailWithoutDeviceAccess()
    {
        var connection = new RecordingSmartCardConnection([0x90, 0]);
        await using var session = await HsmAuthSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        IHsmAuthSession contract = session;
        await Assert.ThrowsAsync<InvalidOperationException>(() => contract.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => contract.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
        Assert.Single(connection.TransmittedCommands);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CallbackReentry_IsRejectedPromptly(int stage)
    {
        HsmAuthSession? session = null;
        async ValueTask Reenter() => await Assert.ThrowsAsync<InvalidOperationException>(() =>
            (session ?? throw new InvalidOperationException()).ResetAsync(TestContext.Current.CancellationToken)).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        var prompt = new SecretPrompt(async (_, _) => { if (stage == 0) await Reenter(); return new SecretOwner([1]); });
        var presence = new PresencePrompt { OnRequest = stage == 1 ? Reenter : null, OnResolve = stage == 2 ? Reenter : null };
        var connection = new RecordingSmartCardConnection([0x90, 0], List(touch: 1), Keys());
        session = await Create(connection, prompt, presence: presence);
        await using (session)
        { using var keys = await session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken); }
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownTouchAndFinalCancellation_HasConservativePolicyAndNoLateTransfer(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var owner = new SecretOwner([1]);
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        var presence = new PresencePrompt { OnResolve = () => { Assert.Equal(1, owner.DisposeCount); if (cancel) cancellation.Cancel(); return default; } };
        var connection = new RecordingSmartCardConnection([0x90, 0], List(touch: 0xFF), Keys());
        await using var session = await Create(connection, prompt, presence: presence);
        if (cancel)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: cancellation.Token));
        else { using var keys = await session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken); }
        Assert.Equal(UserPresenceBasis.PolicyMayRequire, Assert.Single(presence.Requests).Basis);
        Assert.Equal(UserPresenceOutcome.Completed, Assert.Single(presence.Outcomes));
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task DirectOperationAlreadyRunning_RefusesPromptedOperation()
    {
        var recording = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0]);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new InterceptingConnection(recording, async (command, _) =>
        { if (command.Span[1] == 9) { started.SetResult(); await release.Task; } });
        var prompt = new SecretPrompt((_, _) => throw new InvalidOperationException("must not prompt"));
        await using var session = await HsmAuthSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        var direct = session.GetManagementKeyRetriesAsync(TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
            Assert.Empty(prompt.Requests);
        }
        finally { release.TrySetResult(); }
        Assert.Equal(8, await direct.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StaleCallbackContext_DrainsLaterDirectOperation()
    {
        HsmAuthSession? session = null;
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposalStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task disposed = Task.CompletedTask;
        var prompt = new SecretPrompt((_, _) =>
        {
            disposed = Task.Run(async () =>
            {
                await resume.Task;
                var disposing = (session ?? throw new InvalidOperationException()).DisposeAsync();
                disposalStarted.SetResult();
                await disposing;
            });
            return ValueTask.FromResult<IMemoryOwner<byte>?>(new SecretOwner([1]));
        });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recording = new RecordingSmartCardConnection([0x90, 0], List(), Keys(), [8, 0x90, 0]);
        var connection = new InterceptingConnection(recording, async (command, _) =>
        { if (command.Span[1] == 9) { started.SetResult(); await release.Task; } });
        session = await HsmAuthSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        await using (session)
        {
            using var keys = await session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken);
            var direct = session.GetManagementKeyRetriesAsync(TestContext.Current.CancellationToken);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            try
            {
                resume.SetResult();
                await disposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
                await Assert.ThrowsAsync<TimeoutException>(() => disposed.WaitAsync(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken));
            }
            finally { release.TrySetResult(); }
            Assert.Equal(8, await direct.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            await disposed.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportUncertainty_IsNotRetriedAndReleasesSecret(bool timeout)
    {
        var owner = new SecretOwner([1]);
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        int transmissions = 0;
        var recording = new RecordingSmartCardConnection([0x90, 0], List());
        var connection = new InterceptingConnection(recording, (command, _) =>
        {
            if (command.Span[1] == 3) { transmissions++; throw timeout ? new TimeoutException() : new IOException(); }
            return ValueTask.CompletedTask;
        });
        await using var session = await HsmAuthSession.CreateAsync(connection, new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, transmissions);
        Assert.Single(prompt.Requests);
        Assert.Equal(1, owner.DisposeCount);
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
    }

    internal sealed class InterceptingConnection(RecordingSmartCardConnection inner,
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> intercept) : ISmartCardConnection
    {
        public Transport Transport => inner.Transport;
        public ConnectionType Type => inner.Type;
        public bool SupportsExtendedApdu() => inner.SupportsExtendedApdu();
        public IDisposable BeginTransaction(CancellationToken token = default) => inner.BeginTransaction(token);
        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken token = default)
        { await intercept(command, token); return await inner.TransmitAndReceiveAsync(command, token); }
        public void Dispose() => inner.Dispose();
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }

    [Fact]
    public async Task Rejection_ReacquiresAfterCleanupWithSamePeerInputsAndPerAttemptPresence()
    {
        var first = new SecretOwner([1]);
        var second = new SecretOwner([2]);
        var connection = new RecordingSmartCardConnection([0x90, 0], List(touch: 1), [0x63, 0xC7], Keys());
        var events = new List<string>();
        var prompt = new SecretPrompt((_, _) =>
        {
            events.Add("secret");
            if (events.Count > 1) { Assert.Equal(1, first.DisposeCount); Assert.All(first.Bytes, b => Assert.Equal(0, b)); }
            return ValueTask.FromResult<IMemoryOwner<byte>?>(events.Count == 1 ? first : second);
        });
        var presence = new PresencePrompt
        {
            OnRequest = () => { events.Add("request"); return default; },
            OnResolve = () => { events.Add("resolve"); return default; }
        };
        await using var session = await Create(connection, prompt, presence: presence);
        using var intent = UserPresenceIntent.BeginScope("authorize test peer");
        using var keys = await session.CalculateSessionKeysSymmetricWithPromptAsync("cred", Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), new byte[8], TestContext.Current.CancellationToken);
        Assert.Equal((string[])["secret", "request", "resolve", "secret", "request", "resolve"], events);
        Assert.Equal((UserPresenceOutcome[])[UserPresenceOutcome.Failed, UserPresenceOutcome.Completed], presence.Outcomes);
        Assert.NotSame(presence.Requests[0], presence.Requests[1]);
        Assert.All(presence.Requests, c => { Assert.Equal("authorize test peer", c.Intent); Assert.Equal(UserPresenceOperations.YubiHsmAuth.CalculateSessionKeysSymmetric, c.Operation); });
        Assert.Equal(8, prompt.Requests[0].RetriesRemaining);
        Assert.Equal(7, prompt.Requests[1].RetriesRemaining);
        Assert.True(prompt.Requests[1].IsRetry);
        Assert.True(CryptographicOperations.FixedTimeEquals(connection.TransmittedCommands[2].AsSpan()[..^17], connection.TransmittedCommands[3].AsSpan()[..^17]));
        Assert.Equal(1, second.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyOwnerIsPassword_NullOwnerIsDecline(bool decline)
    {
        var owner = new SecretOwner([]);
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(decline ? null : owner));
        var connection = new RecordingSmartCardConnection([0x90, 0], List(), Keys());
        await using var session = await Create(connection, prompt);
        if (decline)
            await Assert.ThrowsAsync<CredentialPromptDeclinedException>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken));
        else
        { using var keys = await session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken); Assert.Equal(1, owner.DisposeCount); }
        Assert.Equal(decline ? 2 : 3, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task UnusableMetadata_StopsBeforeAcquiringSecret(int scenario)
    {
        byte[] metadata = scenario switch
        {
            0 => List(0),
            1 => [0x90, 0],
            2 => List(algorithm: 0x27),
            3 => [0x72, 2, 0x26, 0, 0x90, 0],
            4 => [0x6A, 0x80],
            _ => [0x72, 4, 0x26, 0, 0xFF, 8, 0x90, 0]
        };
        var prompt = new SecretPrompt((_, _) => throw new InvalidOperationException("unexpected prompt"));
        var connection = new RecordingSmartCardConnection([0x90, 0], metadata);
        await using var session = await Create(connection, prompt);
        await Assert.ThrowsAnyAsync<Exception>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(prompt.Requests);
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task LocalOversize_ConsumesOnlyPromptBudgetAndNeverNotifies()
    {
        var owners = new List<SecretOwner>();
        var prompt = new SecretPrompt((_, _) => { var o = new SecretOwner(new byte[17]); owners.Add(o); return ValueTask.FromResult<IMemoryOwner<byte>?>(o); });
        var connection = new RecordingSmartCardConnection([0x90, 0], List(touch: 1));
        var presence = new PresencePrompt();
        await using var session = await Create(connection, prompt, attempts: 2, presence: presence);
        await Assert.ThrowsAsync<ArgumentException>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(2, prompt.Requests.Count);
        Assert.All(prompt.Requests, c => Assert.Equal(8, c.RetriesRemaining));
        Assert.All(owners, o => Assert.Equal(1, o.DisposeCount));
        Assert.Empty(presence.Requests);
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(0xC0, 1)]
    [InlineData(0xC7, 2)]
    public async Task ConfirmedRejection_StopsAtBlockedOrLocalBound(int status, int count)
    {
        var connection = new RecordingSmartCardConnection([0x90, 0], List(), [0x63, (byte)status], [0x63, (byte)status]);
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(new SecretOwner([1])));
        await using var session = await Create(connection, prompt, 2);
        var error = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(status & 15, error.RetriesRemaining);
        Assert.Equal(count, prompt.Requests.Count);
        Assert.Equal(count + 2, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task CallbackRetryException_IsNeverDeviceRejection(int stage)
    {
        var error = new HsmAuthRetryException(7, "callback failure");
        var owner = new SecretOwner([1]) { OnDispose = stage == 3 ? () => throw error : null };
        var prompt = new SecretPrompt((_, _) => stage == 0 ? throw error : ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        var presence = new PresencePrompt
        {
            OnRequest = stage == 1 ? () => throw error : null,
            OnResolve = stage is 2 or 4 ? () => throw error : null
        };
        var connection = new RecordingSmartCardConnection([0x90, 0], List(touch: 1), stage == 4 ? [0x63, 0xC7] : Keys());
        await using var session = await Create(connection, prompt, presence: presence);
        Assert.Same(error, await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken)));
        Assert.Single(prompt.Requests);
        Assert.Equal(stage <= 1 ? 2 : 3, connection.TransmittedCommands.Count);
        if (stage > 0) { Assert.Equal(1, owner.DisposeCount); Assert.All(owner.Bytes, b => Assert.Equal(0, b)); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonRetryResponseOrMalformedKeys_NeverResubmits(bool malformed)
    {
        var connection = new RecordingSmartCardConnection([0x90, 0], List(), malformed ? [1, 0x90, 0] : [0x69, 0x82]);
        var owner = new SecretOwner([1]);
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        await using var session = await Create(connection, prompt);
        await Assert.ThrowsAnyAsync<Exception>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.Single(prompt.Requests);
        Assert.Equal(1, owner.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingProvider_CancellationOrDisposalReleasesLateOwnerOnce(bool dispose)
    {
        var pending = new TaskCompletionSource<IMemoryOwner<byte>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var prompt = new SecretPrompt((_, _) => { entered.SetResult(); return new(pending.Task); });
        var connection = new RecordingSmartCardConnection([0x90, 0], List());
        await using var session = await Create(connection, prompt);
        using var cancellation = new CancellationTokenSource();
        var operation = session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetAsync(TestContext.Current.CancellationToken));
        if (dispose) await session.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken); else cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var late = new SecretOwner([1]) { OnDispose = () => released.SetResult() };
        pending.SetResult(late);
        await released.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        Assert.Equal(1, late.DisposeCount);
        Assert.All(late.Bytes, b => Assert.Equal(0, b));
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public async Task CallbackDisposal_DoesNotWaitOnItselfOrTransferKeys(int stage, bool asynchronous)
    {
        HsmAuthSession? session = null;
        async ValueTask DisposeSession()
        {
            var target = session ?? throw new InvalidOperationException();
            if (asynchronous) { await Task.Yield(); await target.DisposeAsync(); } else target.Dispose();
        }
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new SecretOwner([1]) { OnDispose = () => released.TrySetResult() };
        var prompt = new SecretPrompt(async (_, _) => { if (stage == 0) await DisposeSession(); return owner; });
        var presence = new PresencePrompt { OnRequest = stage == 1 ? DisposeSession : null, OnResolve = stage == 2 ? DisposeSession : null };
        var connection = new RecordingSmartCardConnection([0x90, 0], List(touch: 1), Keys());
        session = await Create(connection, prompt, presence: presence);
        await using (session)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], cancellationToken: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            Assert.Equal(stage == 2 ? 3 : 2, connection.TransmittedCommands.Count);
            await released.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Assert.Equal(1, owner.DisposeCount);
        }
    }

    [Fact]
    public async Task ConfiguredProvider_DirectMethodIsSingleAttemptAndNeverPrompts()
    {
        var prompt = new SecretPrompt((_, _) => throw new InvalidOperationException("must not prompt"));
        var connection = new RecordingSmartCardConnection([0x90, 0], [0x63, 0xC7]);
        await using var session = await Create(connection, prompt);
        await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.CalculateSessionKeysSymmetricAsync("cred", new byte[16], (byte[])[0x61], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(prompt.Requests);
        Assert.Equal(2, connection.TransmittedCommands.Count);
        Assert.Equal(0x61, connection.TransmittedCommands[^1][^17]);
    }

    [Fact]
    public async Task Calculation_ListsPromptsCalculatesAndReleasesOwnedPassword()
    {
        var owner = new SecretOwner([0x61]);
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        var connection = new RecordingSmartCardConnection([0x90, 0], List(), Keys());
        await using var session = await Create(connection, prompt);
        using var keys = await session.CalculateSessionKeysSymmetricWithPromptAsync("cred", new byte[16], new byte[8], TestContext.Current.CancellationToken);
        Assert.True(CryptographicOperations.FixedTimeEquals(new byte[16], keys.SEnc));
        Assert.True(CryptographicOperations.FixedTimeEquals((byte[])[1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1], keys.SMac));
        Assert.True(CryptographicOperations.FixedTimeEquals((byte[])[2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2], keys.SRmac));
        Assert.Equal((byte[])[0xA4, 5, 3], connection.TransmittedCommands.Select(c => c[1]));
        Assert.Equal((byte[])[0, 3, 0, 0, 52, 0x71, 4, 0x63, 0x72, 0x65, 0x64,
            0x77, 16, .. new byte[16], 0x78, 8, .. new byte[8], 0x73, 16, 0x61, .. new byte[15], 0],
            connection.TransmittedCommands[^1]);
        var context = Assert.IsType<HsmAuthCredentialPromptContext>(Assert.Single(prompt.Requests));
        Assert.Equal("cred", context.CredentialLabel);
        Assert.Equal(CredentialKind.Password, context.Kind);
        Assert.Equal(8, context.RetriesRemaining);
        Assert.Equal(0, context.MinLengthBytes);
        Assert.Equal(16, context.MaxLengthBytes);
        Assert.Equal(1, owner.DisposeCount);
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task DirectComposition_DerivedProvisioningAndTouchLookupKeepWireShape()
    {
        var connection = new RecordingSmartCardConnection([0x90, 0], [0x90, 0], List(), Keys());
        var presence = new PresencePrompt();
        await using var session = await HsmAuthSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3), UserPresencePrompt = presence }, TestContext.Current.CancellationToken);
        await session.PutCredentialDerivedAsync(new byte[16], "cred", (byte[])[1], ReadOnlyMemory<byte>.Empty, cancellationToken: TestContext.Current.CancellationToken);
        using var keys = await session.CalculateSessionKeysSymmetricAsync("cred", new byte[16], ReadOnlyMemory<byte>.Empty, new byte[8], TestContext.Current.CancellationToken);
        Assert.Equal((byte[])[0xA4, 1, 5, 3], connection.TransmittedCommands.Select(c => c[1]));
        Assert.Equal(1, connection.TransmittedCommands.Count(c => c[1] == 1));
        Assert.Equal(new byte[16], connection.TransmittedCommands[^1][^16..]);
    }

    internal static byte[] List(byte retries = 8, byte touch = 0, byte algorithm = 0x26) =>
        [0x72, 7, algorithm, touch, 0x63, 0x72, 0x65, 0x64, retries, 0x90, 0];
    internal static byte[] Keys() => [.. new byte[16], .. Enumerable.Repeat((byte)1, 16), .. Enumerable.Repeat((byte)2, 16), 0x90, 0];
    internal static Task<HsmAuthSession> Create(RecordingSmartCardConnection connection, ICredentialPrompt prompt, int attempts = 3,
        IUserPresencePrompt? presence = null) => HsmAuthSession.CreateAsync(connection,
        new SessionCreationOptions
        {
            FirmwareVersionOverride = new FirmwareVersion(5, 4, 3),
            CredentialPrompt = prompt,
            MaxCredentialPromptAttempts = attempts,
            UserPresencePrompt = presence
        }, TestContext.Current.CancellationToken);

    internal sealed class SecretOwner(byte[] bytes) : IMemoryOwner<byte>
    {
        public byte[] Bytes { get; } = bytes;
        public Memory<byte> Memory => Bytes;
        public int DisposeCount { get; private set; }
        public Action? OnDispose { get; init; }
        public void Dispose() { DisposeCount++; OnDispose?.Invoke(); }
    }

    internal sealed class SecretPrompt(Func<CredentialPromptContext, CancellationToken, ValueTask<IMemoryOwner<byte>?>> acquire) : ICredentialPrompt
    {
        public List<CredentialPromptContext> Requests { get; } = [];
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken token)
        { Requests.Add(context); return acquire(context, token); }
    }

    internal sealed class PresencePrompt : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requests { get; } = [];
        public List<UserPresenceOutcome> Outcomes { get; } = [];
        public Func<ValueTask>? OnRequest { get; init; }
        public Func<ValueTask>? OnResolve { get; init; }
        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken token)
        { Requests.Add(context); return OnRequest?.Invoke() ?? default; }
        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken token)
        { Assert.Equal(CancellationToken.None, token); Outcomes.Add(outcome); return OnResolve?.Invoke() ?? default; }
    }
}