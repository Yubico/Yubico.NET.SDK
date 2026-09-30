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

using System.Collections.Concurrent;
using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.Core.UnitTests.Credentials;

// Every test is async so that AsyncLocal changes made in a test body cannot leak into other tests.
public sealed class UserPresenceIntentTests
{
    [Fact]
    public async Task WithoutScope_ContextIsUnchanged()
    {
        var prompt = new RecordingPrompt();
        UserPresenceContext context = CreateContext();

        await RequestAndResolveAsync(prompt, context);

        Assert.Same(context, prompt.Requested.Single());
        Assert.Null(prompt.Requested.Single().Intent);
    }

    [Fact]
    public async Task ActiveScope_IsAttachedToRequestAndResolution()
    {
        var prompt = new RecordingPrompt();

        using (UserPresenceIntent.BeginScope("approve the transfer"))
        {
            await RequestAndResolveAsync(prompt, CreateContext());
        }

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal("approve the transfer", requested.Intent);
        Assert.Equal(UserPresenceOperations.Fido2.GetAssertion, requested.Operation);
        Assert.Equal("example.com", requested.Scope);
        Assert.Same(requested, Assert.Single(prompt.Resolved));
    }

    [Fact]
    public async Task BasisOverride_KeepsIntent()
    {
        var prompt = new RecordingPrompt();
        UserPresenceNotification notification;
        using (UserPresenceIntent.BeginScope("sign in to the bank"))
        {
            notification = UserPresenceNotification.Create(prompt, CreateContext(UserPresenceBasis.PolicyMayRequire));
        }

        await notification.RequestAsync(UserPresenceBasis.DeviceWaiting, TestContext.Current.CancellationToken);
        await notification.ResolveAsync(UserPresenceOutcome.Completed);

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
        Assert.Equal("sign in to the bank", requested.Intent);
        Assert.Same(requested, Assert.Single(prompt.Resolved));
    }

    [Fact]
    public async Task Intent_IsCapturedWhenTheOperationStarts()
    {
        var prompt = new RecordingPrompt();
        UserPresenceNotification startedInside;
        using (UserPresenceIntent.BeginScope("inside"))
        {
            startedInside = UserPresenceNotification.Create(prompt, CreateContext());
        }

        UserPresenceNotification startedOutside = UserPresenceNotification.Create(prompt, CreateContext());
        using (UserPresenceIntent.BeginScope("too late"))
        {
            await startedInside.RequestAsync(TestContext.Current.CancellationToken);
            await startedOutside.RequestAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(["inside", null], prompt.Requested.Select(context => context.Intent));
    }

    [Fact]
    public async Task NestedScopes_InnermostWinsAndOuterIsRestored()
    {
        using (UserPresenceIntent.BeginScope("outer"))
        {
            Assert.Equal("outer", UserPresenceIntent.Current);
            using (UserPresenceIntent.BeginScope("inner"))
            {
                Assert.Equal("inner", UserPresenceIntent.Current);
            }

            Assert.Equal("outer", UserPresenceIntent.Current);
        }

        Assert.Null(UserPresenceIntent.Current);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task OutOfOrderDisposal_NeverRestoresADisposedScope()
    {
        IDisposable outer = UserPresenceIntent.BeginScope("outer");
        IDisposable inner = UserPresenceIntent.BeginScope("inner");

        outer.Dispose();
        Assert.Equal("inner", UserPresenceIntent.Current);

        inner.Dispose();
        Assert.Null(UserPresenceIntent.Current);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RepeatedDisposal_DoesNotAffectLaterScopes()
    {
        IDisposable first = UserPresenceIntent.BeginScope("first");
        first.Dispose();

        using (UserPresenceIntent.BeginScope("second"))
        {
            first.Dispose();
            Assert.Equal("second", UserPresenceIntent.Current);
        }

        Assert.Null(UserPresenceIntent.Current);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ScopeDisposedFromAnotherFlow_IsNoLongerObserved()
    {
        IDisposable scope = UserPresenceIntent.BeginScope("disposed elsewhere");

        await Task.Run(scope.Dispose, TestContext.Current.CancellationToken);

        Assert.Null(UserPresenceIntent.Current);
    }

    [Fact]
    public async Task Scope_FlowsIntoWorkStartedInsideIt()
    {
        var prompt = new RecordingPrompt();

        using (UserPresenceIntent.BeginScope("background"))
        {
            await Task.Run(() => RequestAndResolveAsync(prompt, CreateContext()), TestContext.Current.CancellationToken);
        }

        Assert.Equal("background", Assert.Single(prompt.Requested).Intent);
    }

    [Fact]
    public async Task ConcurrentOperations_EachSeeTheirOwnIntent()
    {
        var prompt = new RecordingPrompt();
        using var bothInScope = new Barrier(2);

        async Task RunAsync(string intent)
        {
            await Task.Yield();
            using (UserPresenceIntent.BeginScope(intent))
            {
                // Both scopes are open at once before either operation starts.
                bothInScope.SignalAndWait(TestContext.Current.CancellationToken);
                await RequestAndResolveAsync(prompt, CreateContext());
            }
        }

        await Task.WhenAll(
            Task.Run(() => RunAsync("first"), TestContext.Current.CancellationToken),
            Task.Run(() => RunAsync("second"), TestContext.Current.CancellationToken));

        Assert.Equal(["first", "second"], prompt.Requested.Select(context => context.Intent).Order());
        Assert.Null(UserPresenceIntent.Current);
    }

    [Fact]
    public async Task CallerSuppliedIntent_IsKept()
    {
        var prompt = new RecordingPrompt();
        UserPresenceContext context = CreateContext() with { Intent = "explicit" };

        using (UserPresenceIntent.BeginScope("ambient"))
        {
            await RequestAndResolveAsync(prompt, context);
        }

        Assert.Same(context, Assert.Single(prompt.Requested));
    }

    [Fact]
    public async Task WithoutPrompt_ScopeIsHarmless()
    {
        using (UserPresenceIntent.BeginScope("no prompt"))
        {
            Assert.Same(UserPresenceNotification.None, UserPresenceNotification.Create(null, CreateContext()));
        }

        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BeginScope_RejectsBlankIntent(string intent)
    {
        Assert.Throws<ArgumentException>(() => UserPresenceIntent.BeginScope(intent));
        Assert.Throws<ArgumentNullException>(() => UserPresenceIntent.BeginScope(null!));
        Assert.Null(UserPresenceIntent.Current);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ToString_ExcludesIntent()
    {
        UserPresenceContext context = CreateContext() with { Intent = "pay 500 EUR to Alice" };

        Assert.DoesNotContain("500 EUR", context.ToString(), StringComparison.Ordinal);
        await Task.CompletedTask;
    }

    private static async Task RequestAndResolveAsync(IUserPresencePrompt prompt, UserPresenceContext context)
    {
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, context);
        await notification.RequestAsync(TestContext.Current.CancellationToken);
        await notification.ResolveAsync(UserPresenceOutcome.Completed);
    }

    private static UserPresenceContext CreateContext(
        UserPresenceBasis basis = UserPresenceBasis.PolicyRequires) => new()
        {
            Basis = basis,
            Application = UserPresenceApplications.Fido2,
            Operation = UserPresenceOperations.Fido2.GetAssertion,
            Scope = "example.com"
        };

    private sealed class RecordingPrompt : IUserPresencePrompt
    {
        private readonly ConcurrentQueue<UserPresenceContext> _requested = new();
        private readonly ConcurrentQueue<UserPresenceContext> _resolved = new();

        public IReadOnlyList<UserPresenceContext> Requested => [.. _requested];

        public IReadOnlyList<UserPresenceContext> Resolved => [.. _resolved];

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
        {
            _requested.Enqueue(context);
            return default;
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            _resolved.Enqueue(context);
            return default;
        }
    }
}