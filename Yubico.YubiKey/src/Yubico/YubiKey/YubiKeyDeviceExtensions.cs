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
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey;

[Obsolete("This class is deprecated")]
public static class YubiKeyDeviceExtensions
{
    public static IYubiKeyDevice WithScp03(this YubiKeyDevice device, StaticKeys scp03Keys) =>
        GetScp03Device(device, scp03Keys);

    internal static Scp03YubiKeyDevice GetScp03Device(this IYubiKeyDevice device, StaticKeys scp03Keys)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        if (scp03Keys is null)
        {
            throw new ArgumentNullException(nameof(scp03Keys));
        }

        if (device is Scp03YubiKeyDevice scp03Device)
        {
            if (scp03Device.StaticKeys.AreKeysSame(scp03Keys))
            {
                return scp03Device;
            }

            throw new ArgumentException(ExceptionMessages.Scp03KeyMismatch);
        }

        if (device is YubiKeyDevice yubiKeyDevice)
        {
            if (!yubiKeyDevice.HasSmartCard)
            {
                throw new NotSupportedException(ExceptionMessages.CcidNotSupported);
            }

            return new Scp03YubiKeyDevice(yubiKeyDevice, scp03Keys);
        }

        throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
    }
}
