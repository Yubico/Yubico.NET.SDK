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
using Yubico.YubiKey.Oath;

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    public static class ManagePassword
    {
        // Verify the OATH password.
        public static bool RunVerifyPassword(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;
                oathSession.VerifyPassword();
            }

            return true;
        }

        // Set a new OATH password.
        public static bool RunSetPassword(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;
                oathSession.SetPassword();
            }

            return true;
        }

        // Remove the OATH password.
        public static bool RunUnsetPassword(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;
                oathSession.UnsetPassword();
            }

            return true;
        }
    }
}
