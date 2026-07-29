// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     One-shot disposal with shared completion. The first caller wins and runs teardown — inner disposal,
///     then <paramref name="lease" /> release in a <c>finally</c> — while every concurrent or later caller
///     observes that same completion: async callers await it, sync callers block on it. Any disposal call
///     returning therefore implies teardown actually finished, and all callers see the same outcome.
/// </summary>
/// <remarks>
///     The lease is never released before inner teardown completes, even when inner teardown fails, so a
///     caller cannot reopen an interface whose physical handle is still being torn down.
/// </remarks>
internal sealed class DisposalGate(IDisposable lease)
{
    private Task? _completion;

    /// <summary>Synchronous disposal. The winner tears down inline; a loser blocks on the winner's teardown.</summary>
    public void Dispose(Action disposeInner) =>
        Run(() =>
        {
            disposeInner();
            return ValueTask.CompletedTask;
        }).GetAwaiter().GetResult();

    /// <summary>Asynchronous disposal. The winner tears down asynchronously; a loser awaits that teardown.</summary>
    public ValueTask DisposeAsync(Func<ValueTask> disposeInnerAsync) => new(Run(disposeInnerAsync));

    private Task Run(Func<ValueTask> teardown)
    {
        var claim = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _completion, claim.Task, null) is { } inFlight)
            return inFlight; // a loser: observe the winner's completion, never touch the inner connection

        // Started inline on this thread, so a synchronous winner never needs a scheduler to finish.
        _ = TearDownAsync(claim, teardown);
        return claim.Task;
    }

    private async Task TearDownAsync(TaskCompletionSource claim, Func<ValueTask> teardown)
    {
        try
        {
            try
            {
                await teardown().ConfigureAwait(false);
            }
            finally
            {
                lease.Dispose();
            }

            claim.SetResult();
        }
        catch (Exception ex)
        {
            claim.SetException(ex);
        }
    }
}