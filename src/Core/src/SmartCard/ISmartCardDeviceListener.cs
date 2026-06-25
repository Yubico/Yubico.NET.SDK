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

namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
/// Interface for monitoring SmartCard device arrival and removal events.
/// </summary>
/// <remarks>
/// Listeners do not auto-start. Call <see cref="Start"/> after setting up <see cref="DeviceEvent"/>
/// callback. The listener establishes baseline state during <see cref="Start"/> to avoid
/// duplicate events for devices already present.
/// </remarks>
public interface ISmartCardDeviceListener : IDisposable
{
    /// <summary>
    /// Callback invoked when any SmartCard device event (arrival or removal) occurs.
    /// </summary>
    Action? DeviceEvent { get; set; }

    /// <summary>
    /// Gets the current status of the listener.
    /// </summary>
    DeviceListenerStatus Status { get; }

    /// <summary>
    /// Starts the listener. Establishes baseline of currently connected devices,
    /// then begins monitoring for changes. Only fires events for subsequent changes.
    /// </summary>
    /// <remarks>
    /// This method should be called after setting <see cref="DeviceEvent"/> callback.
    /// Calling Start() on an already started listener has no effect.
    /// </remarks>
    void Start();

    /// <summary>
    /// Stops the listener and releases monitoring resources.
    /// </summary>
    /// <remarks>
    /// After calling Stop(), the listener can be restarted by calling <see cref="Start"/> again.
    /// Calling Stop() on an already stopped listener has no effect.
    /// </remarks>
    void Stop();
}
