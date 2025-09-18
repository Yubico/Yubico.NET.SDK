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

namespace Yubico.YubiKey.Piv;

// This KeyCollector class can be used to provide the KeyCollector delegate
// to the PivSession class (and others in the future).
// It is called Simple because it returns fixed, default values.
public class SimpleKeyCollector
{
    private static readonly byte[] _pin =
        { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

    private static readonly byte[] _puk =
        { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 };

    private static readonly byte[] _mgmtKey =
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    private readonly bool _allowRetry;

    public SimpleKeyCollector(
        bool allowRetry)
    {
        _allowRetry = allowRetry;
    }

    public bool SimpleKeyCollectorDelegate(
        KeyEntryData keyEntryData)
    {
        if (keyEntryData is null)
        {
            return false;
        }

        if (keyEntryData.IsRetry)
        {
            if (!_allowRetry)
            {
                return false;
            }

            if (keyEntryData.RetriesRemaining is not null)
            {
                if (keyEntryData.RetriesRemaining == 1)
                {
                    return false;
                }
            }
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
                currentValue = CollectPin();
                break;

            case KeyEntryRequest.ChangePivPin:
                currentValue = CollectPin();
                newValue = CollectPin();
                break;

            case KeyEntryRequest.ChangePivPuk:
                currentValue = CollectPuk();
                newValue = CollectPuk();
                break;

            case KeyEntryRequest.ResetPivPinWithPuk:
                currentValue = CollectPuk();
                newValue = CollectPin();
                break;

            case KeyEntryRequest.AuthenticatePivManagementKey:
                if (keyEntryData.IsRetry)
                {
                    return false;
                }

                currentValue = CollectMgmtKey();
                break;

            case KeyEntryRequest.ChangePivManagementKey:
                if (keyEntryData.IsRetry)
                {
                    return false;
                }

                currentValue = CollectMgmtKey();
                newValue = CollectMgmtKey();
                break;
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

    public static SimpleKeyCollector Create(
        bool allowRetry)
    {
        return new SimpleKeyCollector(allowRetry);
    }

    public static byte[] CollectPin()
    {
        return _pin;
    }

    public static byte[] CollectPuk()
    {
        return _puk;
    }

    public static byte[] CollectMgmtKey()
    {
        return _mgmtKey;
    }
}
