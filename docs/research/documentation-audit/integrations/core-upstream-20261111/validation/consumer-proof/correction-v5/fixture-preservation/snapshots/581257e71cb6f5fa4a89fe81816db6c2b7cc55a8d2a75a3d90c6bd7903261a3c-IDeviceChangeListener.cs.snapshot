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

namespace Yubico.YubiKit.Core.Transports;

/// <summary>Monitors transport changes that may require device discovery to run again.</summary>
/// <remarks>
/// Set <see cref="Changed"/> before calling <see cref="Start"/>. Notifications are rescan hints,
/// never authoritative device arrival or removal state. Do not call <see cref="Stop"/> or
/// <see cref="IDisposable.Dispose"/> inside the callback: native listener shutdown may wait for it to return.
/// </remarks>
public interface IDeviceChangeListener : IDisposable
{
    /// <summary>Gets or sets the callback invoked when a transport change requires a rescan.</summary>
    Action? Changed { get; set; }

    /// <summary>Gets the listener's current diagnostic status.</summary>
    DeviceListenerStatus Status { get; }

    /// <summary>Establishes baseline state and begins monitoring subsequent changes.</summary>
    /// <remarks>Set <see cref="Changed"/> first. Check <see cref="Status"/> after starting.</remarks>
    /// <exception cref="ObjectDisposedException">The listener has been disposed.</exception>
    void Start();

    /// <summary>Stops monitoring and releases monitoring resources.</summary>
    /// <remarks>Call outside the <see cref="Changed"/> callback to avoid waiting on that callback.</remarks>
    void Stop();
}