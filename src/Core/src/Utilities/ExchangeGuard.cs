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

namespace Yubico.YubiKit.Core.Utilities;

/// <summary>
///     Refuses overlapping logical exchanges against a stateful sequential peer (a smart card or HID device).
///     Callers wrap a <em>complete</em> multi-step exchange so no foreign traffic can interleave it on the
///     shared underlying connection.
/// </summary>
/// <remarks>
///     <para>
///         Sessions support one operation at a time. An overlapping caller receives an
///         <see cref="InvalidOperationException" /> immediately rather than waiting. Once entered, the exchange
///         runs to completion: the delegate receives
///         <see cref="CancellationToken.None" />, so a caller's token can never abort a logical exchange
///         between its constituent transmits (which would strand chained command/response or SCP MAC state
///         on the card and poison the next caller's exchange).
///     </para>
///     <para>
///         The guard must not be re-entered from within a running exchange. Keep the wrapped delegate confined
///         to transport/processor calls that never call back into guarded methods.
///     </para>
/// </remarks>
internal sealed class ExchangeGuard
{
    private int _active;

    /// <summary>
    ///     Runs <paramref name="exchange" /> exclusively; overlapping calls are refused immediately.
    ///     A pre-canceled <paramref name="cancellationToken" /> throws before the guard is claimed.
    ///     The delegate is invoked with <see cref="CancellationToken.None" /> so the in-flight exchange always
    ///     completes.
    /// </summary>
    public async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> exchange,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref _active, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "This session already has an exchange in flight. Sessions support one operation at a time; " +
                "await each call before issuing the next.");
        }

        try
        {
            return await exchange(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _active, 0);
        }
    }

    /// <summary>
    ///     Result-free overload of <see cref="RunAsync{T}" /> for exchanges whose only purpose is their side
    ///     effect (lazy initialization, for example).
    /// </summary>
    public Task RunAsync(
        Func<CancellationToken, Task> exchange,
        CancellationToken cancellationToken = default) =>
        RunAsync<object?>(
            async exchangeToken =>
            {
                await exchange(exchangeToken).ConfigureAwait(false);
                return null;
            },
            cancellationToken);
}