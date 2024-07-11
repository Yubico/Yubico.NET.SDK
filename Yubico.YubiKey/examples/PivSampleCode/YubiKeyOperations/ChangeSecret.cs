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
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    public static class ChangeSecret
    {
        // Change the PIV PIN and PUK retry counts.
        public static bool RunChangeRetryCount(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            byte newRetryCountPin,
            byte newRetryCountPuk)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;

                pivSession.ChangePinAndPukRetryCounts(newRetryCountPin, newRetryCountPuk);
            }

            return true;
        }

        // Change the PIV PIN.
        // This will return false if the PIN is not successfully changed.
        public static bool RunChangePivPin(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                bool changeResult = pivSession.TryChangePin();

                ReportChangeResult(changeResult, "PIN");

                return changeResult;
            }
        }

        // Change the PIV PUK.
        // This will return false if the PUK is not successfully changed.
        public static bool RunChangePivPuk(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                bool changeResult = pivSession.TryChangePuk();

                ReportChangeResult(changeResult, "PUK");

                return changeResult;
            }
        }

        // Reset the PIV PIN using the PUK.
        // This changes the PIN, but it is generally used to recover the PIN.
        // This will return false if the PIN is not successfully changed.
        public static bool RunResetPivPinWithPuk(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                bool changeResult = pivSession.TryResetPin();

                ReportChangeResult(changeResult, "PIN");

                return changeResult;
            }
        }

        // This changes the management key. It is possible to set the touch
        // policy for the management key as well, but this sample only leaves the
        // touch policy as the default (no touch needed).
        public static bool RunChangePivManagementKey(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                bool changeResult = pivSession.TryChangeManagementKey();

                ReportChangeResult(changeResult, "Management Key");

                return changeResult;
            }
        }

        // Reset the entire PIV application. This deletes any keys and certs
        // (other than the attesting key and cert) and resets the PIN, PUK, and
        // management key to their defaults.
        public static bool RunResetPiv(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;
                pivSession.ResetApplication();
            }

            return true;
        }

        private static void ReportChangeResult(bool changeResult, string valueToChange)
        {
            string resultMessage = changeResult ?
                "Successfully changed " :
                "Was not able to change ";

            SampleMenu.WriteMessage(MessageType.Special, 0, resultMessage + valueToChange);
        }
    }
}
