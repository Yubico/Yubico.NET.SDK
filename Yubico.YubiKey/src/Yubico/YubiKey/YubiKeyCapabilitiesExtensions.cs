﻿// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey
{
    internal static class YubiKeyCapabilitiesExtensions
    {
        public static YubiKeyCapabilities ToDeviceInfoCapabilities(
            this YubiKeyCapabilities capabilities)
        {
            if (capabilities.HasFlag(YubiKeyCapabilities.All))
            {
                return capabilities;
            }

            var deviceInfoCapabilities = YubiKeyCapabilities.None;

            if (capabilities.HasFlag(YubiKeyCapabilities.Otp))
            {
                deviceInfoCapabilities |= YubiKeyCapabilities.Otp;
            }

            if (capabilities.HasFlag(YubiKeyCapabilities.FidoU2f))
            {
                deviceInfoCapabilities |= YubiKeyCapabilities.FidoU2f | YubiKeyCapabilities.Fido2;
            }

            if (capabilities.HasFlag(YubiKeyCapabilities.Ccid))
            {
                deviceInfoCapabilities |=
                    YubiKeyCapabilities.Piv
                    | YubiKeyCapabilities.Oath
                    | YubiKeyCapabilities.OpenPgp
                    | YubiKeyCapabilities.YubiHsmAuth;
            }

            return deviceInfoCapabilities;
        }
    }
}
