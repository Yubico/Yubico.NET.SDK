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

using System.Diagnostics;

namespace Yubico.YubiKit.Core.UnitTests.Infrastructure;

/// <summary>
/// Polls a condition until it holds, for concurrency tests that observe work happening on another
/// thread (monitor loops, discovery workers, subscription bookkeeping).
/// </summary>
/// <remarks>
/// <para>
/// The condition is evaluated once before the first delay and once more before the timeout is
/// reported, so a condition that becomes true inside the final poll window is never missed.
/// </para>
/// <para>
/// Two shapes, because the callers genuinely differ: <see cref="WaitUntilAsync"/> treats a timeout as
/// a test failure, while <see cref="TryWaitUntilAsync"/> returns the outcome for tests where "did not
/// happen within the bound" is the thing being asserted rather than a broken precondition.
/// </para>
/// </remarks>
internal static class AsyncWait
{
    /// <summary>
    /// Default bound for "this should already have happened". Generous on purpose: these waits gate
    /// on real scheduler progress, and CI machines are slower than laptops.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// Waits for <paramref name="condition"/>, returning whether it held within the bound.
    /// </summary>
    /// <param name="condition">Polled predicate. Must be cheap and side-effect free.</param>
    /// <param name="timeout">Bound to wait for; defaults to <see cref="DefaultTimeout"/>.</param>
    /// <param name="cancellationToken">
    /// Defaults to the ambient <see cref="TestContext.Current"/> token. Pass an explicit token only
    /// when the test drives its own <see cref="CancellationTokenSource"/>.
    /// </param>
    public static async Task<bool> TryWaitUntilAsync(
        Func<bool> condition,
        TimeSpan? timeout = null,
        CancellationToken? cancellationToken = null)
    {
        var bound = timeout ?? DefaultTimeout;
        var token = cancellationToken ?? TestContext.Current.CancellationToken;
        var elapsed = Stopwatch.StartNew();

        while (true)
        {
            if (condition())
            {
                return true;
            }

            if (elapsed.Elapsed >= bound)
            {
                return false;
            }

            await Task.Delay(PollInterval, token);
        }
    }

    /// <summary>
    /// Waits for <paramref name="condition"/>, throwing <see cref="TimeoutException"/> with
    /// <paramref name="failureMessage"/> if it does not hold within the bound.
    /// </summary>
    /// <inheritdoc cref="TryWaitUntilAsync" path="/param"/>
    public static async Task WaitUntilAsync(
        Func<bool> condition,
        string failureMessage,
        TimeSpan? timeout = null,
        CancellationToken? cancellationToken = null)
    {
        if (!await TryWaitUntilAsync(condition, timeout, cancellationToken))
        {
            throw new TimeoutException(failureMessage);
        }
    }
}