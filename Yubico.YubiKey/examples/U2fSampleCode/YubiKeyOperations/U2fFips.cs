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
using Yubico.YubiKey.U2f;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.Sample.U2fSampleCode
{
    public static class U2fFips
    {
        // If the YubiKey is not FIPS series, this method will return false.
        // If the YubiKey is FIPS, it will determine if it is in FIPS mode or
        // not. If it is, set isFipsMode to true.
        public static bool GetFipsMode(IYubiKeyDevice yubiKey, out bool isFipsMode)
        {
            isFipsMode = false;
            if ((yubiKey is null) || (!yubiKey.IsFipsSeries))
            {
                return false;
            }

            using (var u2fSession = new U2fSession(yubiKey))
            {
                var fipsModeCommand = new VerifyFipsModeCommand();
                VerifyFipsModeResponse fipsModeResponse = u2fSession.Connection.SendCommand(fipsModeCommand);
                isFipsMode = fipsModeResponse.GetData();

                return true;
            }
        }

        public static bool SetPin(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var u2fSession = new U2fSession(yubiKey))
            {
                u2fSession.KeyCollector = KeyCollectorDelegate;
                return u2fSession.TrySetPin();
            }
        }

        public static bool ChangePin(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var u2fSession = new U2fSession(yubiKey))
            {
                u2fSession.KeyCollector = KeyCollectorDelegate;
                return u2fSession.TryChangePin();
            }
        }

        public static bool VerifyPin(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var u2fSession = new U2fSession(yubiKey))
            {
                u2fSession.KeyCollector = KeyCollectorDelegate;
                return u2fSession.TryVerifyPin();
            }
        }
    }
}
