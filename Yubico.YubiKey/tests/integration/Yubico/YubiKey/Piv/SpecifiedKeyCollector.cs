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
    // It is called Specified because the caller specifies the PIN, PUK, and mgmt
    // key at construction, and those are the only values it returns.
    public class SpecifiedKeyCollector
    {
        private readonly byte[] _pin;
        private readonly byte[] _puk;
        private readonly byte[] _mgmtKey;

        public SpecifiedKeyCollector(byte[] pin, byte[] puk, byte[] mgmtKey)
        {
            _pin = new byte[pin.Length];
            Array.Copy(pin, 0, _pin, 0, pin.Length);

            _puk = new byte[puk.Length];
            Array.Copy(puk, 0, _puk, 0, puk.Length);

            _mgmtKey = new byte[mgmtKey.Length];
            Array.Copy(mgmtKey, 0, _mgmtKey, 0, mgmtKey.Length);
        }

        public bool SpecifiedKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                return false;
            }

            byte[] currentValue;
            byte[]? newValue = null;

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.VerifyPivPin:
                    currentValue = _pin;
                    break;

//                case KeyEntryRequest.ChangePivPin:
//                    currentValue = CollectPin();
//                    newValue = CollectPin();
//                    break;
//
//                case KeyEntryRequest.ChangePivPuk:
//                    currentValue = CollectPuk();
//                    newValue = CollectPuk();
//                    break;
//
                case KeyEntryRequest.ResetPivPinWithPuk:
                    currentValue = _puk;
                    newValue = _pin;
                    break;

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    currentValue = _mgmtKey;
                    break;

//                case KeyEntryRequest.ChangePivManagementKey:
//                    if (keyEntryData.IsRetry == true)
//                    {
//                        return false;
//                    }
//                    currentValue = CollectMgmtKey();
//                    newValue = CollectMgmtKey();
//                    break;
            }

            if (newValue is null)
            {
                keyEntryData.SubmitValue(currentValue);
            }
            else
            {
                keyEntryData.SubmitValues(currentValue, newValue);
            }

            return true;
        }
    }
}
