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

namespace Yubico.YubiKit.Core.Hid.Windows;

/// <summary>
/// Windows implementation of HID device listener using CM_Register_Notification.
/// </summary>
internal sealed class WindowsHidDeviceListener : HidDeviceListener
{
    public WindowsHidDeviceListener()
    {
        // TODO: Implement in Phase 3.1
        Status = DeviceListenerStatus.Started;
    }

    protected override void Dispose(bool disposing)
    {
        // TODO: Implement in Phase 3.1
        base.Dispose(disposing);
    }
}
