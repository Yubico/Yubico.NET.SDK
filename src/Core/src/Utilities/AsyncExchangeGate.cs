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
///     Serializes concurrent logical exchanges against a stateful sequential peer (a smart card or HID
///     device). Callers wrap a <em>complete</em> multi-step exchange — e.g. a command-chained APDU sequence
///     followed by its chained response reads — so no foreign traffic can interleave it on the shared
///     underlying connection.
/// </summary>
/// <remarks>
///     <para>
///         This is a fairness-agnostic mutual-exclusion gate, not a throughput primitive: concurrent
///         callers are safe but execute sequentially. Waiting is asynchronous (no thread blocked) and
///         cancellable — cancellation applies only while <em>waiting to enter</em>, before the exchange has
///         started. Once entered, the exchange runs to completion: the delegate receives
///         <see cref="CancellationToken.None" />, so a caller's token can never abort a logical exchange
///         between its constituent transmits (which would strand chained command/response or SCP MAC state
///         on the card and poison the next caller's exchange).
///     </para>
///     <para>
///         The gate must NOT be re-entered from within a running exchange (that would deadlock). Keep the
///         wrapped delegate confined to transport/processor calls that never call back into gated methods.
///     </para>
/// </remarks>
internal sealed class AsyncExchangeGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    ///     Runs <paramref name="exchange" /> exclusively; concurrent calls queue asynchronously.
    ///     <paramref name="cancellationToken" /> cancels only the wait to enter; the delegate is invoked
    ///     with <see cref="CancellationToken.None" /> so the in-flight exchange always completes.
    /// </summary>
    public async Task<T> RunExclusiveAsync<T>(
        Func<CancellationToken, Task<T>> exchange,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await exchange(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    ///     Result-free overload of <see cref="RunExclusiveAsync{T}" />, for exchanges whose only purpose is
    ///     their side effect (lazy initialization, for example). Same entry-only cancellation semantics.
    /// </summary>
    public Task RunExclusiveAsync(
        Func<CancellationToken, Task> exchange,
        CancellationToken cancellationToken = default) =>
        RunExclusiveAsync<object?>(
            async exchangeToken =>
            {
                await exchange(exchangeToken).ConfigureAwait(false);
                return null;
            },
            cancellationToken);
}