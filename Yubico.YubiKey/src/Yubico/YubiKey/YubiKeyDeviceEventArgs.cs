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

using System;

namespace Yubico.YubiKey;

/// <summary>
///     This class contains properties that are specific to the event being raised.
/// </summary>
public class YubiKeyDeviceEventArgs : EventArgs
{
    /// <summary>
    ///     Constructs an event arguments.
    /// </summary>
    /// <param name="device">A YubiKey device</param>
    public YubiKeyDeviceEventArgs(IYubiKeyDevice device)
    {
        Device = device;
    }

    /// <summary>
    ///     A YubiKey arrived or removed.
    /// </summary>
    public IYubiKeyDevice Device { get; }
}
