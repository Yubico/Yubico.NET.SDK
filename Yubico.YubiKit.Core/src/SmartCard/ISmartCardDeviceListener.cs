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
public interface ISmartCardDeviceListener : IDisposable
{
    /// <summary>
    /// Raised when a SmartCard device is connected to the system.
    /// </summary>
    event EventHandler<SmartCardDeviceEventArgs>? Arrived;

    /// <summary>
    /// Raised when a SmartCard device is disconnected from the system.
    /// </summary>
    event EventHandler<SmartCardDeviceEventArgs>? Removed;

    /// <summary>
    /// Gets the current status of the listener.
    /// </summary>
    DeviceListenerStatus Status { get; }
}
