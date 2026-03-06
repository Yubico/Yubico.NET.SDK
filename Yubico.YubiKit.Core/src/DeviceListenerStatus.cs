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

namespace Yubico.YubiKit.Core;

/// <summary>
/// Represents the current status of a device listener.
/// </summary>
public enum DeviceListenerStatus
{
    /// <summary>
    /// The listener is not running.
    /// </summary>
    Stopped,

    /// <summary>
    /// The listener is actively monitoring for device events.
    /// </summary>
    Started,

    /// <summary>
    /// The listener encountered an error and stopped.
    /// </summary>
    Error
}
