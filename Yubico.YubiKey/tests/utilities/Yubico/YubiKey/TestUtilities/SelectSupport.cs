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
using System.Globalization;
using System.Linq;

namespace Yubico.YubiKey.TestUtilities
{
    public static class SelectSupport
    {
        // This gets the first YubiKey it finds that supports the given
        // Transport.
        // Pass in Transport.SmartCard to get a YubiKey that supports the smart
        // card protocol (PIV, OATH, and OpenPGP). Or pass in Transport.HidFido
        // to get a YubiKey that supports Fido. And so on.
        // If this method cannot find a YubiKey it will throw an exception (the
        // calling test will fail).
        // Remember, if there is more than one YubiKey connected, and you want to
        // choose among them, don't call this method.
        public static IYubiKeyDevice GetFirstYubiKey(Transport transport)
        {
            var yubiKeyList = YubiKeyDevice.FindByTransport(transport).ToList();
            if (yubiKeyList.Count != 0)
            {
                return yubiKeyList[0];
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.NoInterfaceAvailable));
        }

        // Set the out param yubiKey to the first Transport.UsbSmartCard YubiKey
        // it can find.
        // Most tests will likely use the newer GetFirstYubiKey(Transport)
        // method, but this one is provided for backwards compatibility.
        public static bool TrySelectYubiKey(out IYubiKeyDevice yubiKey)
        {
            var yubiKeyList = YubiKeyDevice.FindByTransport(Transport.UsbSmartCard).ToList();
            if (yubiKeyList.Count != 0)
            {
                yubiKey = yubiKeyList[0];
                return true;
            }

            yubiKey = new HollowYubiKeyDevice();
            return false;
        }
    }
}
