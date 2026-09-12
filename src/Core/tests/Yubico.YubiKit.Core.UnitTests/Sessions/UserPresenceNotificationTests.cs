// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public sealed class UserPresenceNotificationTests
{
    [Fact]
    public async Task None_IsSharedAndInert()
    {
        var prompt = new RecordingUserPresencePrompt();
        UserPresenceContext context = CreateContext();

        Assert.Same(UserPresenceNotification.None, UserPresenceNotification.Create(null, context));
        Assert.Same(UserPresenceNotification.None, UserPresenceNotification.Create(prompt, null));
        Assert.False(UserPresenceNotification.None.IsEnabled);

        await UserPresenceNotification.None.RequestAsync(TestContext.Current.CancellationToken);
        await UserPresenceNotification.None.RequestAsync(
            UserPresenceBasis.DeviceWaiting,
            TestContext.Current.CancellationToken);
        await UserPresenceNotification.None.ResolveAsync(UserPresenceOutcome.Completed);

        Assert.Equal(0, prompt.RequestCount);
        Assert.Equal(0, prompt.ResolutionCount);
    }

    [Fact]
    public async Task EnabledNotification_RequestsAndResolvesAtMostOnce()
    {
        var prompt = new RecordingUserPresencePrompt();
        using var cancellationSource = new CancellationTokenSource();
        UserPresenceContext context = CreateContext();
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, context);

        Assert.True(notification.IsEnabled);
        await notification.RequestAsync(cancellationSource.Token);
        await notification.RequestAsync(cancellationSource.Token);
        cancellationSource.Cancel();
        await notification.ResolveAsync(UserPresenceOutcome.Cancelled);
        await notification.ResolveAsync(UserPresenceOutcome.Completed);

        Assert.Equal(1, prompt.RequestCount);
        Assert.Same(context, prompt.RequestContext);
        Assert.Equal(cancellationSource.Token, prompt.RequestCancellationToken);
        Assert.Equal(1, prompt.ResolutionCount);
        Assert.Same(context, prompt.ResolutionContext);
        Assert.Equal(UserPresenceOutcome.Cancelled, prompt.Outcome);
        Assert.Equal(CancellationToken.None, prompt.ResolutionCancellationToken);
        Assert.False(prompt.ResolutionCancellationToken.CanBeCanceled);
    }

    [Fact]
    public async Task BasisOverride_UsesOneUpgradedContextForRequestAndResolution()
    {
        var prompt = new RecordingUserPresencePrompt();
        UserPresenceContext initialContext = CreateContext(UserPresenceBasis.PolicyMayRequire);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, initialContext);

        await notification.RequestAsync(
            UserPresenceBasis.DeviceWaiting,
            TestContext.Current.CancellationToken);
        await notification.ResolveAsync(UserPresenceOutcome.Completed);

        Assert.NotSame(initialContext, prompt.RequestContext);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, prompt.RequestContext?.Basis);
        Assert.Same(prompt.RequestContext, prompt.ResolutionContext);
    }

    [Fact]
    public async Task RequestFailure_PropagatesWithoutResolutionOrRetry()
    {
        var expected = new IOException("request failed");
        var prompt = new RecordingUserPresencePrompt(requestException: expected);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, CreateContext());

        IOException actual = await Assert.ThrowsAsync<IOException>(() =>
            notification.RequestAsync(TestContext.Current.CancellationToken).AsTask());
        await notification.ResolveAsync(UserPresenceOutcome.Failed, actual);
        await notification.RequestAsync(TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Equal(1, prompt.RequestCount);
        Assert.Equal(0, prompt.ResolutionCount);
    }

    [Fact]
    public async Task ResolutionFailure_WithoutPrimaryException_PropagatesOnceAndRemainsTerminal()
    {
        var expected = new InvalidOperationException("resolution failed");
        var prompt = new RecordingUserPresencePrompt(resolutionException: expected);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, CreateContext());
        await notification.RequestAsync(TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            notification.ResolveAsync(UserPresenceOutcome.Completed).AsTask());
        await notification.ResolveAsync(UserPresenceOutcome.Failed);
        await notification.RequestAsync(TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Equal(1, prompt.RequestCount);
        Assert.Equal(1, prompt.ResolutionCount);
    }

    [Fact]
    public async Task ResolutionFailure_WithPrimaryException_DoesNotMaskPrimary()
    {
        var prompt = new RecordingUserPresencePrompt(
            resolutionException: new InvalidOperationException("resolution failed"));
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, CreateContext());
        var primary = new IOException("device failed");

        await notification.RequestAsync(TestContext.Current.CancellationToken);
        await notification.ResolveAsync(UserPresenceOutcome.Failed, primary);

        Assert.Equal(1, prompt.ResolutionCount);
    }

    [Fact]
    public async Task ResolveBeforeRequest_IsTerminalAndDoesNotEmitCallbacks()
    {
        var prompt = new RecordingUserPresencePrompt();
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, CreateContext());

        await notification.ResolveAsync(UserPresenceOutcome.Failed);
        await notification.ResolveAsync(UserPresenceOutcome.Completed);
        await notification.RequestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, prompt.RequestCount);
        Assert.Equal(0, prompt.ResolutionCount);
    }

    [Fact]
    public async Task ResolveWhileRequestCallbackIsPending_DoesNotReactivateNotification()
    {
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var prompt = new PendingUserPresencePrompt(requestStarted, releaseRequest);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, CreateContext());

        ValueTask request = notification.RequestAsync(TestContext.Current.CancellationToken);
        await requestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        await notification.ResolveAsync(UserPresenceOutcome.Failed);
        releaseRequest.SetResult();
        await request;
        await notification.ResolveAsync(UserPresenceOutcome.Completed);
        await notification.RequestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, prompt.RequestCount);
        Assert.Equal(0, prompt.ResolutionCount);
    }

    [Fact]
    public void UserPresenceContext_UsesReferenceIdentityAndRedactsScopeFromFormatting()
    {
        var first = CreateContext();
        var equalValues = CreateContext();
        var contexts = new Dictionary<UserPresenceContext, string> { [first] = "first" };

        Assert.NotEqual(first, equalValues);
        Assert.False(contexts.ContainsKey(equalValues));
        Assert.Contains("FIDO2", first.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("example.com", first.ToString(), StringComparison.Ordinal);
    }

    private static UserPresenceContext CreateContext(
        UserPresenceBasis basis = UserPresenceBasis.DeviceWaiting) => new()
        {
            Basis = basis,
            Application = "FIDO2",
            Scope = "example.com"
        };

    private sealed class RecordingUserPresencePrompt(
        Exception? requestException = null,
        Exception? resolutionException = null) : IUserPresencePrompt
    {
        public int RequestCount { get; private set; }
        public UserPresenceContext? RequestContext { get; private set; }
        public CancellationToken RequestCancellationToken { get; private set; }
        public int ResolutionCount { get; private set; }
        public UserPresenceContext? ResolutionContext { get; private set; }
        public UserPresenceOutcome Outcome { get; private set; }
        public CancellationToken ResolutionCancellationToken { get; private set; }

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestContext = context;
            RequestCancellationToken = cancellationToken;
            return requestException is null
                ? default
                : ValueTask.FromException(requestException);
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            ResolutionCount++;
            ResolutionContext = context;
            Outcome = outcome;
            ResolutionCancellationToken = cancellationToken;
            return resolutionException is null
                ? default
                : ValueTask.FromException(resolutionException);
        }
    }

    private sealed class PendingUserPresencePrompt(
        TaskCompletionSource requestStarted,
        TaskCompletionSource releaseRequest) : IUserPresencePrompt
    {
        public int RequestCount { get; private set; }

        public int ResolutionCount { get; private set; }

        public async ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            requestStarted.SetResult();
            await releaseRequest.Task.WaitAsync(cancellationToken);
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            ResolutionCount++;
            return default;
        }
    }

}