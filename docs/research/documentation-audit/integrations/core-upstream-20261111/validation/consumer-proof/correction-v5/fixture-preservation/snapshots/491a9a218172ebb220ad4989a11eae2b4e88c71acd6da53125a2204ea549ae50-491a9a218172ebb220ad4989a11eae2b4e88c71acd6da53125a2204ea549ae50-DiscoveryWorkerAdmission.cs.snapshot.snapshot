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
///     Process-wide, nonqueueing admission for best-effort native discovery workers.
/// </summary>
internal static class DiscoveryWorkerAdmission
{
    internal const int MaximumConcurrentWorkers = 4;

    private static readonly SemaphoreSlim Slots =
        new(MaximumConcurrentWorkers, MaximumConcurrentWorkers);

    public static bool TryAcquire(out IDisposable admission)
    {
        if (!Slots.Wait(0))
        {
            admission = null!;
            return false;
        }

        admission = new Admission();
        return true;
    }

    /// <summary>
    ///     Waits (asynchronously, unbounded) for a worker slot. Used by identity reads, whose callers bound
    ///     their own wait via the read budget; the slot count still bounds concurrent native work.
    /// </summary>
    public static async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await Slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Admission();
    }

    private sealed class Admission : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                Slots.Release();
        }
    }
}