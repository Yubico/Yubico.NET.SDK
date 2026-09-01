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

using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.IntegrationTests;

/// <summary>
///     Retries a discovery scan that failed because the PC/SC discovery worker pool was momentarily
///     saturated — and nothing else.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="FindPcscDevices.FindAllAsync" /> is fail-fast: if all
///         <c>DiscoveryWorkerAdmission.MaximumConcurrentWorkers</c> slots are taken it throws immediately
///         rather than queueing. That is a deliberate product contract, and the monitor honours it by
///         retrying on its next interval (<c>RescanSafelyAsync</c>). Integration tests call
///         <c>FindAllAsync</c> once, so a scan that collides with the monitor's own background rescan or
///         with an in-flight identity read fails the test even though the SDK behaved exactly as designed.
///         This helper gives those direct call sites the same bounded retry the monitor already has.
///     </para>
///     <para>
///         <b>The match is deliberately as narrow as it can be made.</b> Only an
///         <see cref="InvalidOperationException" /> whose message is ordinal-equal to
///         <see cref="FindPcscDevices.WorkerSaturationMessage" /> is retried. That constant is the product's
///         own — referenced, not copied — and the single <c>throw</c> site that uses it is the only code in
///         the repository that can produce that string. Any other failure, including any other
///         <see cref="InvalidOperationException" />, propagates untouched on the first attempt. Widening
///         this to <c>catch (Exception)</c>, to all <see cref="InvalidOperationException" />s, or to a
///         substring match would turn genuine product regressions into slow green runs, which is strictly
///         worse than the flake this fixes.
///     </para>
///     <para>
///         Exhausting the bound is a real failure and is reported as one: persistent saturation means a
///         worker slot is held indefinitely, which no retry should paper over.
///     </para>
/// </remarks>
internal static class TransientScanRetry
{
    /// <summary>
    ///     Total attempts, including the first. Four retries at <see cref="InitialBackoff" /> doubling each
    ///     time wait 200 + 400 + 800 + 1600 ms = 3 s in total.
    /// </summary>
    /// <remarks>
    ///     3 s is a pragmatic diagnostic cutoff, not a proof of transience. There is no bound on how long a
    ///     worker slot can be held: <c>ProtocolDeviceInfo</c>'s 3 s budget bounds the <em>caller's wait</em>,
    ///     and on exhaustion the read is abandoned rather than aborted, so the abandoned task keeps its slot
    ///     until the native call returns. The bound therefore exists to separate "collided with a scan in
    ///     flight" from "something is wedged", and deliberately errs on the side of reporting the latter.
    /// </remarks>
    private const int MaxAttempts = 5;

    private const string TotalBackoffDescription = "3 s";

    private static readonly TimeSpan InitialBackoff = TimeSpan.FromMilliseconds(200);

    /// <summary>
    ///     Runs <paramref name="scan" />, retrying only on transient discovery-worker saturation.
    /// </summary>
    public static async Task<T> ScanAsync<T>(Func<Task<T>> scan)
    {
        var backoff = InitialBackoff;
        var attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                return await scan();
            }
            catch (InvalidOperationException ex) when (IsWorkerSaturation(ex))
            {
                if (attempt >= MaxAttempts)
                {
                    throw new InvalidOperationException(
                        $"PC/SC discovery worker capacity was still saturated after {MaxAttempts} scan " +
                        $"attempts spanning ~{TotalBackoffDescription} of backoff. This is no longer the " +
                        "transient collision the retry exists for — a worker slot is being held " +
                        "indefinitely. See the inner exception.",
                        ex);
                }

                await Task.Delay(backoff);
                backoff *= 2;
            }
        }
    }

    /// <summary>
    ///     Ordinal equality against the product constant, not a substring and not a copy, and pinned to
    ///     exactly <see cref="InvalidOperationException" />.
    /// </summary>
    /// <remarks>
    ///     The exact-type check matters because <see cref="ObjectDisposedException" /> derives from
    ///     <see cref="InvalidOperationException" />, and its message is caller-supplied — so a disposal
    ///     defect carrying this exact text would otherwise be retried away, turning a real bug into a slow
    ///     green run. Pinning the type also has the safer failure mode of the two options: if the product
    ///     ever narrows the throw site to a derived type, this stops matching and the flake returns
    ///     loudly, which is strictly better than silently masking a regression.
    /// </remarks>
    private static bool IsWorkerSaturation(InvalidOperationException exception) =>
        exception.GetType() == typeof(InvalidOperationException)
        && string.Equals(exception.Message, FindPcscDevices.WorkerSaturationMessage, StringComparison.Ordinal);
}