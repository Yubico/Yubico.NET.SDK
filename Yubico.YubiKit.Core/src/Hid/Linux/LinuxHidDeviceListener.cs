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

namespace Yubico.YubiKit.Core.Hid.Linux;

/// <summary>
/// Linux implementation of HID device listener using udev_monitor with poll().
/// </summary>
internal sealed class LinuxHidDeviceListener : HidDeviceListener
{
    public LinuxHidDeviceListener()
    {
        // TODO: Implement in Phase 3.3
        Status = DeviceListenerStatus.Started;
    }

    protected override void Dispose(bool disposing)
    {
        // TODO: Implement in Phase 3.3
        base.Dispose(disposing);
    }
}
