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

using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Applies a finite retry policy to integration-test scans rejected by PC/SC worker saturation.
/// </summary>
/// <remarks>
///     Only the exact <see cref="InvalidOperationException" /> emitted when worker admission is saturated
///     is retried. Other exceptions propagate immediately. Five failed attempts exhaust the policy.
/// </remarks>
public static class TransientScanRetry
{
    private const int MaxAttempts = 5;

    private static readonly TimeSpan InitialBackoff = TimeSpan.FromMilliseconds(200);

    /// <summary>
    ///     Runs <paramref name="scan" />, retrying only on discovery-worker saturation.
    /// </summary>
    public static Task<T> ScanAsync<T>(Func<Task<T>> scan) =>
        ScanAsync(scan, static duration => Task.Delay(duration));

    internal static async Task<T> ScanAsync<T>(
        Func<Task<T>> scan,
        Func<TimeSpan, Task> delay)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(delay);

        var backoff = InitialBackoff;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await scan().ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (IsWorkerSaturation(ex))
            {
                if (attempt >= MaxAttempts)
                {
                    throw new RetryExhaustedException(
                        $"PC/SC discovery worker capacity was still saturated after {MaxAttempts} scan attempts.",
                        ex);
                }

                await delay(backoff).ConfigureAwait(false);
                backoff *= 2;
            }
        }
    }

    internal static bool IsExhaustion(Exception exception) => exception is RetryExhaustedException;

    private static bool IsWorkerSaturation(InvalidOperationException exception) =>
        exception.GetType() == typeof(InvalidOperationException)
        && string.Equals(exception.Message, FindPcscDevices.WorkerSaturationMessage, StringComparison.Ordinal);

    private sealed class RetryExhaustedException(string message, Exception innerException)
        : InvalidOperationException(message, innerException);
}