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
///     Serializes potentially sibling USB discovery reads before worker admission. These fixed PID gates
///     contain no identity or metadata; distinct finders and evidence epochs share only native admission.
/// </summary>
internal sealed class DiscoveryReadAdmission : IDisposable
{
    private static readonly Lock AdmissionLock = new();
    private static readonly Dictionary<ushort, SemaphoreSlim[]> PidGates = ReaderNamePidParser.KnownProductIds
        .ToDictionary(pid => pid, _ => (SemaphoreSlim[])[new(1, 1)]);
    private static readonly SemaphoreSlim[] AllUsbGates = [.. PidGates.Values.SelectMany(gates => gates)];
    private static TaskCompletionSource _availabilityChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly IReadOnlyList<SemaphoreSlim> _gates;
    private int _disposed;
    private bool _transferredToLease;

    private DiscoveryReadAdmission(IReadOnlyList<SemaphoreSlim> gates) => _gates = gates;

    // Unknown USB identity must exclude every known PID too: an unparsed CCID reader can be a sibling
    // of any HID descriptor. Grant the entire set atomically: a blocked unknown request must not reserve
    // available PIDs before it can run. No topology/serial aliases survive a discovery operation.
    internal static IReadOnlyList<SemaphoreSlim> GatesFor(ushort? pid) =>
        pid is { } known && PidGates.TryGetValue(known, out var gates) ? gates : AllUsbGates;

    internal static async Task<DiscoveryReadAdmission> AcquireAsync(
        IReadOnlyList<SemaphoreSlim> gates,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            Task changed;
            lock (AdmissionLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (gates.All(gate => gate.CurrentCount != 0))
                {
                    // All acquisition and release use AdmissionLock; these waits cannot block.
                    foreach (var gate in gates)
                        _ = gate.Wait(0);
                    return new DiscoveryReadAdmission(gates);
                }

                // Capture under the same lock as the eligibility check so no release is missed.
                changed = _availabilityChanged.Task;
            }

            // Event-driven retry only: no reservations, native workers, timers, or polling while queued.
            await changed.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal void TransferToLease() => _transferredToLease = true;

    internal void DisposeUnlessTransferred()
    {
        if (!_transferredToLease)
            Dispose();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        TaskCompletionSource changed;
        lock (AdmissionLock)
        {
            foreach (var gate in _gates)
                gate.Release();
            changed = _availabilityChanged;
            _availabilityChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        changed.SetResult();
    }
}