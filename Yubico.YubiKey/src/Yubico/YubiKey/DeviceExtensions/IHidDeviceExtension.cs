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

using Yubico.Core.Devices.Hid;

namespace Yubico.YubiKey.DeviceExtensions;

internal static class IHidDeviceExtension
{
    public static bool IsFido(this IHidDevice device) => device.UsagePage == HidUsagePage.Fido;

    public static bool IsKeyboard(this IHidDevice device) => device.UsagePage == HidUsagePage.Keyboard;

    public static bool IsYubicoDevice(this IHidDevice device) =>
        device.VendorId == VendorIdentifiers.Yubico
        && ProductIdentifiers.AllYubiKeys.Contains(device.ProductId);
}
