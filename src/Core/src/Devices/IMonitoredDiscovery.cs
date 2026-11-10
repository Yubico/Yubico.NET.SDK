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

using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>Discovery and evidence lifetime owned by a monitor.</summary>
internal interface IMonitoredDiscovery
{
    /// <summary>Discovers all YubiKeys in this finder's scope.</summary>
    /// <param name="cancellationToken">Cancels the scan's wait.</param>
    /// <returns>The physical keys observed by the scan.</returns>
    /// <exception cref="OperationCanceledException">The scan was canceled.</exception>
    /// <exception cref="TimeoutException">
    /// A transport's interface enumeration exceeded its budget. The result would be partial, so nothing is returned
    /// for the monitor to publish; the next trigger scans again.
    /// </exception>
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken);

    /// <summary>Discards cached evidence and supersedes pending reads after transport activity.</summary>
    /// <param name="transport">The transport reporting activity, not an eviction scope.</param>
    /// <remarks>Called by the monitor's scan loop before the scan a hotplug trigger requests. Does not reactivate retired discovery.</remarks>
    void InvalidateEvidence(ConnectionType transport);

    /// <summary>Permanently retires discovery reads without releasing already-open native work.</summary>
    /// <remarks>The monitor must stop its listeners and end its scan loop before calling this method.</remarks>
    void Retire();
}