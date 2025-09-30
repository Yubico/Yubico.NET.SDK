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

using Yubico.YubiKit.Core;

namespace Yubico.YubiKit.Device;

public enum YubiKeyDeviceAction
{
    Added,
    Removed,
    Updated
}

public class YubiKeyDeviceEvent
{
    public YubiKeyDeviceEvent(YubiKeyDeviceAction action, IYubiKey? device)
    {
        Action = action;
        Device = device;
        Timestamp = DateTime.UtcNow;
    }

    public IYubiKey? Device { get; }
    public YubiKeyDeviceAction Action { get; }
    public string? DeviceId { get; set; }
    public DateTime Timestamp { get; }
}