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
    private static readonly Dictionary<ushort, SemaphoreSlim[]> PidGates = ReaderNamePidParser.KnownProductIds
        .ToDictionary(pid => pid, _ => (SemaphoreSlim[])[new(1, 1)]);
    private static readonly SemaphoreSlim[] AllUsbGates = [.. PidGates.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value)];

    private readonly IReadOnlyList<SemaphoreSlim> _gates;
    private int _disposed;
    private bool _transferredToLease;

    private DiscoveryReadAdmission(IReadOnlyList<SemaphoreSlim> gates) => _gates = gates;

    // Unknown USB identity must exclude every known PID too: an unparsed CCID reader can be a sibling
    // of any HID descriptor. Ordered multi-gate acquisition avoids deadlock without globally serializing
    // ordinary reads of distinct known PIDs. No topology/serial aliases survive a discovery operation.
    internal static IReadOnlyList<SemaphoreSlim> GatesFor(ushort? pid) =>
        pid is { } known && PidGates.TryGetValue(known, out var gates) ? gates : AllUsbGates;

    internal static async Task<DiscoveryReadAdmission> AcquireAsync(
        IReadOnlyList<SemaphoreSlim> gates,
        CancellationToken cancellationToken)
    {
        var acquired = new List<SemaphoreSlim>(gates.Count);
        try
        {
            foreach (var gate in gates)
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                acquired.Add(gate);
            }

            return new DiscoveryReadAdmission(acquired);
        }
        catch
        {
            for (var i = acquired.Count - 1; i >= 0; i--)
                acquired[i].Release();
            throw;
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

        for (var i = _gates.Count - 1; i >= 0; i--)
            _gates[i].Release();
    }
}