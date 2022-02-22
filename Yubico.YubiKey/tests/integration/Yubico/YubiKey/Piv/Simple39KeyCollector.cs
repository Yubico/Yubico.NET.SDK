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
using System.Collections.Generic;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv
{
    // This KeyCollector class can be used to provide the KeyCollector delegate
    // to the PivSession class (and others in the future).
    // It is called Simple because it returns fixed, default values. The "39" is
    // there because it is possible to return the fixed values with one byte
    // changed to 0x39.
    public class Simple39KeyCollector
    {
        private static bool _setKeyFlagOnChange;

        // If KeyFlag is set to 0, the current PIN, PUK, or key returned will be
        // the default and the new PIN, PUK, or key will be the alternate.
        // The alternate is the same except the first byte is different: 0x39.
        // If KeyFlag is set to 1, the current will be the alternate and the new
        // will be the default.
        public int KeyFlag { get; set; }

        // If RetryFlag is set to 0, the collector will return false if the
        // RetriesRemaining is 1. This way the PIN or PUK will not be blocked.
        // But if it is set to 1, go ahead and keep returning the wrong PIN or
        // PUK. This way we can block the PIN or PUK for testing purposes.
        public int RetryFlag { get; set; }

        // If the caller sets the input arg to true, then when the call asks for
        // Change, return the old and new, then set KeyFlag to the opposite of
        // what it currently is.
        // For false, then this returns old and new, but does nothing to KeyFlag.
        // If there is no arg, that's false.
        public Simple39KeyCollector(bool setKeyFlagOnChange = false)
        {
            KeyFlag = 0;
            RetryFlag = 0;
            _setKeyFlagOnChange = setKeyFlagOnChange;
        }

        public bool Simple39KeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if ((keyEntryData.IsRetry == true) && (RetryFlag == 0))
            {
                if (!(keyEntryData.RetriesRemaining is null))
                {
                    if (keyEntryData.RetriesRemaining == 1)
                    {
                        return false;
                    }
                }
            }

            bool isChange = false;
            byte[] currentValue;
            byte[]? newValue = null;

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    if (!_setKeyFlagOnChange)
                    {
                        KeyFlag = 0;
                    }
                    return true;

                case KeyEntryRequest.VerifyPivPin:
                    currentValue = CollectPin();
                    break;

                case KeyEntryRequest.ChangePivPin:
                    currentValue = CollectPin();
                    newValue = CollectPin();
                    isChange = true;
                    break;

                case KeyEntryRequest.ChangePivPuk:
                    currentValue = CollectPuk();
                    newValue = CollectPuk();
                    isChange = true;
                    break;

                case KeyEntryRequest.ResetPivPinWithPuk:
                    currentValue = CollectPuk();
                    newValue = CollectPin();
                    isChange = true;
                    break;

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    if (keyEntryData.IsRetry == true)
                    {
                        return false;
                    }
                    currentValue = CollectMgmtKey();
                    break;

                case KeyEntryRequest.ChangePivManagementKey:
                    if (keyEntryData.IsRetry == true)
                    {
                        return false;
                    }
                    currentValue = CollectMgmtKey();
                    newValue = CollectMgmtKey();
                    isChange = true;
                    break;
            }

            if (newValue is null)
            {
                if (KeyFlag != 0)
                {
                    currentValue[0] = 0x39;
                }
                keyEntryData.SubmitValue(currentValue);
            }
            else
            {
                if (KeyFlag != 0)
                {
                    currentValue[0] = 0x39;
                }
                else
                {
                    newValue[0] = 0x39;
                }
                keyEntryData.SubmitValues(currentValue, newValue);
            }

            if (_setKeyFlagOnChange && isChange)
            {
                if (KeyFlag == 0)
                {
                    KeyFlag = 1;
                }
                else
                {
                    KeyFlag = 1;
                }
            }

            return true;
        }

        public static byte[] CollectPin() =>
            new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

        public static byte[] CollectPuk() =>
            new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 };

        public static byte[] CollectMgmtKey() =>
            new byte[] {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        };
    }
}
