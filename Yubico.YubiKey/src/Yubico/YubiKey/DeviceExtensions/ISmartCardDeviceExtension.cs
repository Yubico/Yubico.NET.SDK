// Copyright 2021 Yubico AB
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
using System.Diagnostics;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Logging;

namespace Yubico.YubiKey.DeviceExtensions
{
    internal static class ISmartCardDeviceExtension
    {
        public static bool IsYubicoDevice(this ISmartCardDevice device)
        {
            try
            {
                return ProductAtrs.AllYubiKeys.Contains(device.Atr!);
            }
            catch (PlatformInterop.SCardException e)
            {
                Log.GetLogger().LogWarning(e, "Exception encountered when attempting to read device ATR.");
            }

            return false;
        }

        // Assumes that YubiKeys connected over USB will have a reader name that contains "YubiKey".
        // When connected over NFC, the reader is a third-party device and will not contain "YubiKey".
        public static bool IsUsbTransport(this ISmartCardDevice scDevice) =>
            !string.IsNullOrEmpty(scDevice.Path)
            && scDevice.Path.IndexOf("yubikey", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool IsNfcTransport(this ISmartCardDevice scDevice) => !scDevice.IsUsbTransport();
    }
}
