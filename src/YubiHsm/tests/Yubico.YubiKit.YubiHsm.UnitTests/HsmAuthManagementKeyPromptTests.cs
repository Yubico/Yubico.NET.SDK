// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Core.Sessions;
using static Yubico.YubiKit.YubiHsm.UnitTests.HsmAuthCredentialPromptTests;

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class HsmAuthManagementKeyPromptTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportUncertainty_NeverResubmits(bool timeout)
    {
        int sent = 0;
        var recording = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0]);
        var connection = new InterceptingConnection(recording, (command, _) =>
        {
            if (command.Span[1] == 2) { sent++; throw timeout ? new TimeoutException() : new IOException(); }
            return ValueTask.CompletedTask;
        });
        var owner = new SecretOwner(new byte[16]);
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        await using var session = await HsmAuthSession.CreateAsync(connection,
            new SessionCreationOptions { CredentialPrompt = prompt }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
        Assert.Equal(1, sent);
        Assert.Single(prompt.Requests);
        Assert.Equal(1, owner.DisposeCount);
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task ProviderRetryException_NeverDeletesOrReacquires()
    {
        var error = new HsmAuthRetryException(7, "provider failed");
        var prompt = new SecretPrompt((_, _) => throw error);
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0]);
        await using var session = await Create(connection, prompt);
        Assert.Same(error, await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken)));
        Assert.Single(prompt.Requests);
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task MissingCredential_CommandFailureDoesNotAcquireAnotherKey()
    {
        var owner = new SecretOwner(new byte[16]);
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0], [0x6A, 0x82]);
        await using var session = await Create(connection, prompt);
        await Assert.ThrowsAsync<Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.ApduException>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
        Assert.Single(prompt.Requests);
        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.Equal(1, owner.DisposeCount);
    }

    [Fact]
    public async Task DirectDeletion_WithProviderStillUsesSuppliedKeyOnce()
    {
        var prompt = new SecretPrompt((_, _) => throw new InvalidOperationException("must not prompt"));
        var connection = new RecordingSmartCardConnection([0x90, 0], [0x63, 0xC7]);
        await using var session = await Create(connection, prompt);
        await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.DeleteCredentialAsync(new byte[16], "cred", TestContext.Current.CancellationToken));
        Assert.Empty(prompt.Requests);
        Assert.Equal((byte[])[0xA4, 2], connection.TransmittedCommands.Select(c => c[1]));
    }

    [Fact]
    public async Task Delete_QueriesRetriesPromptsAndSendsExactKeyThenReleases()
    {
        var owner = new SecretOwner(Enumerable.Repeat((byte)1, 16).ToArray());
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0], [0x90, 0]);
        await using var session = await Create(connection, prompt);
        await session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken);
        Assert.Equal((byte[])[0xA4, 9, 2], connection.TransmittedCommands.Select(c => c[1]));
        Assert.Equal((byte[])[0, 2, 0, 0, 24, 0x7B, 16, .. Enumerable.Repeat((byte)1, 16), 0x71, 4, 0x63, 0x72, 0x65, 0x64, 0], connection.TransmittedCommands[^1]);
        var context = Assert.IsType<HsmAuthCredentialPromptContext>(Assert.Single(prompt.Requests));
        Assert.Equal("cred", context.CredentialLabel);
        Assert.Equal(CredentialKind.ManagementKey, context.Kind);
        Assert.Equal(16, context.MinLengthBytes);
        Assert.Equal(16, context.MaxLengthBytes);
        Assert.Equal(8, context.RetriesRemaining);
        Assert.Equal(1, owner.DisposeCount);
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAfterResponse_PreservesSuccessAndStopsRejectedAttempt(bool rejected)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var owner = new SecretOwner(new byte[16]) { OnDispose = cancellation.Cancel };
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0],
            rejected ? [0x63, 0xC7] : [0x90, 0]);
        await using var session = await Create(connection, prompt);

        if (rejected)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.DeleteCredentialWithPromptAsync("cred", cancellation.Token));
        else
            await session.DeleteCredentialWithPromptAsync("cred", cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Single(prompt.Requests);
        Assert.Equal((byte[])[0xA4, 9, 2], connection.TransmittedCommands.Select(c => c[1]));
        Assert.Equal(1, owner.DisposeCount);
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task WrongThenCorrect_ReacquiresAfterCleanupAndUsesReturnedCounter()
    {
        var first = new SecretOwner(new byte[16]);
        var second = new SecretOwner(Enumerable.Repeat((byte)1, 16).ToArray());
        var prompt = new SecretPrompt((c, _) =>
        {
            if (c.IsRetry) { Assert.Equal(1, first.DisposeCount); Assert.All(first.Bytes, b => Assert.Equal(0, b)); }
            return ValueTask.FromResult<IMemoryOwner<byte>?>(c.IsRetry ? second : first);
        });
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0], [0x63, 0xC7], [0x90, 0]);
        await using var session = await Create(connection, prompt);
        await session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken);
        Assert.Equal((byte[])[0xA4, 9, 2, 2], connection.TransmittedCommands.Select(c => c[1]));
        Assert.Equal(7, prompt.Requests[1].RetriesRemaining);
        Assert.Equal(1, second.DisposeCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task UnusableRetryQuery_NeverPromptsOrDeletes(int scenario)
    {
        var connection = new RecordingSmartCardConnection([0x90, 0], scenario switch { 0 => [0, 0x90, 0], 1 => [0x90, 0], _ => [0x69, 0x82] });
        var prompt = new SecretPrompt((_, _) => throw new InvalidOperationException());
        await using var session = await Create(connection, prompt);
        await Assert.ThrowsAnyAsync<Exception>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
        Assert.Empty(prompt.Requests);
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public async Task WrongLength_ConsumesBudgetWithoutDeleting(int length)
    {
        var owners = new List<SecretOwner>();
        var prompt = new SecretPrompt((_, _) => { var o = new SecretOwner(new byte[length]); owners.Add(o); return ValueTask.FromResult<IMemoryOwner<byte>?>(o); });
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0]);
        await using var session = await Create(connection, prompt, 2);
        await Assert.ThrowsAsync<ArgumentException>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
        Assert.Equal(2, prompt.Requests.Count);
        Assert.All(owners, o => Assert.Equal(1, o.DisposeCount));
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessOrRejectionWithCleanupFault_NeverResubmits(bool rejected)
    {
        var error = new HsmAuthRetryException(7, "cleanup failed");
        var owner = new SecretOwner(new byte[16]) { OnDispose = () => throw error };
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(owner));
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0], rejected ? [0x63, 0xC7] : [0x90, 0]);
        await using var session = await Create(connection, prompt);
        Assert.Same(error, await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken)));
        Assert.Single(prompt.Requests);
        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.All(owner.Bytes, b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData(0xC0, 1)]
    [InlineData(0xC7, 2)]
    public async Task Rejection_StopsAtZeroOrPromptBound(int status, int count)
    {
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(new SecretOwner(new byte[16])));
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0], [0x63, (byte)status], [0x63, (byte)status]);
        await using var session = await Create(connection, prompt, 2);
        await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
        Assert.Equal(count, prompt.Requests.Count);
        Assert.Equal(count + 2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task Decline_NeverDeletes()
    {
        var prompt = new SecretPrompt((_, _) => ValueTask.FromResult<IMemoryOwner<byte>?>(null));
        var connection = new RecordingSmartCardConnection([0x90, 0], [8, 0x90, 0]);
        await using var session = await Create(connection, prompt);
        await Assert.ThrowsAsync<CredentialPromptDeclinedException>(() => session.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }
}