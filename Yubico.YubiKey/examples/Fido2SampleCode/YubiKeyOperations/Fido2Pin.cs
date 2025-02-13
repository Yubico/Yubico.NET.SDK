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
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;


namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    // This file contains the methods that demonstrate how to perform PIN
    // operations in the FIDO2 application.
    public static class Fido2Pin
    {
        public static bool SetPin(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;
                return fido2Session.TrySetPin();
            }
        }

        public static bool ChangePin(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;
                return fido2Session.TryChangePin();
            }
        }

        public static bool VerifyPin(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            PinUvAuthTokenPermissions? permissions,
            string relyingPartyId)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;
                return fido2Session.TryVerifyPin(permissions, relyingPartyId);
            }
        }

        public static bool VerifyUv(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            PinUvAuthTokenPermissions permissions,
            string relyingPartyId)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;
                return fido2Session.TryVerifyUv(permissions, relyingPartyId);
            }
        }
    }
}
