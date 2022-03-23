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
using Yubico.YubiKey.Sample.SharedCode;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    public static class PinOnlyMode
    {
        // Get the current mode.
        public static bool RunGetPivPinOnlyMode(IYubiKeyDevice yubiKey, out PivPinOnlyMode mode)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                mode = pivSession.GetPinOnlyMode();
            }

            return true;
        }

        // Set the YubiKey to the specified PIN-only mode.
        public static bool RunSetPivPinOnlyMode(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            PivPinOnlyMode mode)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                pivSession.SetPinOnlyMode(mode);
            }

            return true;
        }

        // Set the YubiKey to the PinProtected, using the PIN provided.
        // This demonstrates how to set a YubiKey to PIN-only mode without using
        // a KeyCollector. This is possible only if the mgmt key is currently set
        // to the default, and the mode to set is PIN-protected. It is currently
        // not possible to set a YubiKey to PIN-derived without a KeyCollector.
        public static bool RunSetPinOnlyNoKeyCollector(
            IYubiKeyDevice yubiKey,
            ReadOnlyMemory<byte> pin,
            out int? retriesRemaining)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                if (!pivSession.TryVerifyPin(pin, out retriesRemaining))
                {
                    return false;
                }

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);
            }

            return true;
        }

        // Recover the PIN-only mode if the ADMIN DATA and/or PRINTED storage
        // areas are improperly overwritten.
        public static bool RunRecoverPivPinOnlyMode(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            out PivPinOnlyMode mode)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                mode = pivSession.TryRecoverPinOnlyMode();
            }

            return true;
        }
    }
}
