// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.Cli.Shared.UnitTests.Output;

public sealed class ConsoleUserPresencePromptTests
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    [Theory]
    [InlineData(UserPresenceBasis.DeviceWaiting)]
    [InlineData(UserPresenceBasis.PolicyRequires)]
    public void Requested_WithCertainBasis_WritesOnceImmediately(UserPresenceBasis basis)
    {
        var output = new ConcurrentQueue<string>();
        var prompt = new ConsoleUserPresencePrompt(output.Enqueue, Task.Delay, Debounce);

        ValueTask request = prompt.OnUserPresenceRequestedAsync(CreateContext(basis), CancellationToken.None);

        Assert.True(request.IsCompletedSuccessfully);
        Assert.Equal(["Touch your YubiKey."], output);
    }

    [Fact]
    public async Task Requested_WithPolicyMayRequireResolvedBeforeDelay_WritesNothing()
    {
        var output = new ConcurrentQueue<string>();
        var delay = new ControlledDelay();
        var prompt = new ConsoleUserPresencePrompt(output.Enqueue, delay.WaitAsync, Debounce);
        UserPresenceContext context = CreateContext(UserPresenceBasis.PolicyMayRequire);

        ValueTask request = prompt.OnUserPresenceRequestedAsync(context, CancellationToken.None);
        ValueTask resolved = prompt.OnUserPresenceResolvedAsync(
            context,
            UserPresenceOutcome.Completed,
            CancellationToken.None);
        delay.Complete(0);

        Assert.True(request.IsCompletedSuccessfully);
        await resolved;
        Assert.Empty(output);
    }

    [Fact]
    public async Task Requested_WithPolicyMayRequireAfterDelay_WritesOnce()
    {
        var output = new ConcurrentQueue<string>();
        var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delay = new ControlledDelay();
        var prompt = new ConsoleUserPresencePrompt(
            message =>
            {
                output.Enqueue(message);
                written.TrySetResult();
            },
            delay.WaitAsync,
            Debounce);

        ValueTask request = prompt.OnUserPresenceRequestedAsync(
            CreateContext(UserPresenceBasis.PolicyMayRequire),
            CancellationToken.None);

        Assert.True(request.IsCompletedSuccessfully);
        Assert.Empty(output);
        delay.Complete(0);
        await written.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        Assert.Equal(["Touch your YubiKey."], output);
    }

    [Fact]
    public async Task Requested_WithCancellation_WritesNothingAfterDelayCompletes()
    {
        var output = new ConcurrentQueue<string>();
        var delay = new ControlledDelay();
        var prompt = new ConsoleUserPresencePrompt(output.Enqueue, delay.WaitAsync, Debounce);
        UserPresenceContext context = CreateContext(UserPresenceBasis.PolicyMayRequire);
        using var cancellation = new CancellationTokenSource();

        ValueTask request = prompt.OnUserPresenceRequestedAsync(context, cancellation.Token);
        cancellation.Cancel();
        delay.Complete(0);
        await prompt.OnUserPresenceResolvedAsync(
            context,
            UserPresenceOutcome.Cancelled,
            CancellationToken.None);

        Assert.True(request.IsCompletedSuccessfully);
        Assert.Empty(output);
    }

    [Fact]
    public async Task Resolved_CorrelatesEqualContextsByInstance()
    {
        var output = new ConcurrentQueue<string>();
        var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delay = new ControlledDelay();
        var prompt = new ConsoleUserPresencePrompt(
            message =>
            {
                output.Enqueue(message);
                written.TrySetResult();
            },
            delay.WaitAsync,
            Debounce);
        UserPresenceContext first = CreateContext(UserPresenceBasis.PolicyMayRequire);
        UserPresenceContext second = CreateContext(UserPresenceBasis.PolicyMayRequire);

        await prompt.OnUserPresenceRequestedAsync(first, CancellationToken.None);
        await prompt.OnUserPresenceRequestedAsync(second, CancellationToken.None);
        ValueTask resolved = prompt.OnUserPresenceResolvedAsync(
            first,
            UserPresenceOutcome.Completed,
            CancellationToken.None);
        delay.Complete(0);
        delay.Complete(1);

        await resolved;
        await written.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        Assert.Equal(["Touch your YubiKey."], output);
    }

    private static UserPresenceContext CreateContext(UserPresenceBasis basis) =>
        new()
        {
            Basis = basis,
            Application = "Test",
            Scope = "private context"
        };

    private sealed class ControlledDelay
    {
        private readonly List<TaskCompletionSource> _delays = [];

        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Assert.Equal(Debounce, delay);
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _delays.Add(completion);
            return completion.Task;
        }

        public void Complete(int index) => _delays[index].SetResult();
    }
}
