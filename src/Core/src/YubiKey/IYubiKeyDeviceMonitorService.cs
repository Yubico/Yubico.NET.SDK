// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Service responsible for device discovery and monitoring lifecycle.
/// </summary>
/// <remarks>
/// This service owns the device listeners (HID and SmartCard) and coordinates
/// with <see cref="IYubiKeyDeviceRepository"/> to update the device cache.
/// It has no cache of its own - all state is maintained in the repository.
/// </remarks>
internal interface IYubiKeyDeviceMonitorService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether device monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Performs a single device scan and updates the repository.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RescanAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts continuous monitoring for device changes.
    /// </summary>
    /// <param name="interval">The polling interval between scans when no events occur.</param>
    /// <remarks>
    /// Monitoring is event-driven with interval-based fallback. When device events
    /// occur (via HID or SmartCard listeners), a rescan is triggered immediately
    /// with coalescing to avoid redundant scans.
    /// </remarks>
    void StartMonitoring(TimeSpan interval);

    /// <summary>
    /// Stops monitoring for device changes.
    /// </summary>
    void StopMonitoring();
}
