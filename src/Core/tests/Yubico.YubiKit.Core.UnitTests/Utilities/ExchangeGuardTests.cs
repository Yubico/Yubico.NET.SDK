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

using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Utilities;

public class ExchangeGuardTests
{
    [Fact]
    public async Task RunAsync_OverlappingCall_ThrowsWithoutRunningDelegate()
    {
        var guard = new ExchangeGuard();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = guard.RunAsync(async _ =>
        {
            entered.SetResult();
            await release.Task;
        }, TestContext.Current.CancellationToken);
        await entered.Task;

        var secondRan = false;
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            guard.RunAsync(_ =>
            {
                secondRan = true;
                return Task.CompletedTask;
            }, TestContext.Current.CancellationToken));

        Assert.Contains("one operation at a time", refusal.Message, StringComparison.Ordinal);
        Assert.False(secondRan);
        release.SetResult();
        await first;
    }

    [Fact]
    public async Task RunAsync_DelegateThrows_GuardResetsForNextCall()
    {
        var guard = new ExchangeGuard();

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            guard.RunAsync<int>(_ => throw new InvalidOperationException("exchange failed"), TestContext.Current.CancellationToken));
        var result = await guard.RunAsync(_ => Task.FromResult(42), TestContext.Current.CancellationToken);

        Assert.Equal("exchange failed", failure.Message);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RunAsync_PreCanceledToken_ThrowsBeforeClaimingGuard()
    {
        var guard = new ExchangeGuard();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            guard.RunAsync(_ => Task.CompletedTask, cancellation.Token));

        await guard.RunAsync(_ => Task.CompletedTask, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RunAsync_PassesNonCancelableTokenToClaimedExchange()
    {
        var guard = new ExchangeGuard();
        using var cancellation = new CancellationTokenSource();
        CancellationToken observed = default;

        await guard.RunAsync(
            token =>
            {
                observed = token;
                return Task.CompletedTask;
            },
            cancellation.Token);

        Assert.False(observed.CanBeCanceled);
    }
}