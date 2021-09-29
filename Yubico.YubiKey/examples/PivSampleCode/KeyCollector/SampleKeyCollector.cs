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

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This KeyCollector class can be used to provide the KeyCollector delegate
    // to the PivSession class (and others in the future).
    // This KeyCollector is not secure. It simply asks for the user to enter
    // PINs, PUKs, and management keys at the keyboard, with no hiding.
    public class SampleKeyCollector
    {
        private const string DefaultPinString = "123456";
        private const string DefaultPukString = "12345678";
        private const string DefaultMgmtKeyString = "01 02 ... 08 three times";

        private readonly SampleMenu _menuObject;

        public SampleKeyCollector(SampleMenu menuObject)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            _menuObject = menuObject;
        }

        // This is the callback. When the SDK needs a PIN, PUK, or management
        // key, this is the method that will be called.
        public bool SampleKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                if (!(keyEntryData.RetriesRemaining is null))
                {
                    if (GetUserInputOnRetries(keyEntryData) == false)
                    {
                        return false;
                    }
                }
            }

            byte[] currentValue;
            byte[] newValue = null;

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.VerifyPivPin:
                    currentValue = CollectValue(DefaultPinString, "PIN");
                    break;

                case KeyEntryRequest.ChangePivPin:
                    currentValue = CollectValue(DefaultPinString, "Current PIN");
                    newValue = CollectValue(DefaultPinString, "New PIN");
                    break;

                case KeyEntryRequest.ChangePivPuk:
                    currentValue = CollectValue(DefaultPukString, "Current PUK");
                    newValue = CollectValue(DefaultPukString, "New PUK");
                    break;

                case KeyEntryRequest.ResetPivPinWithPuk:
                    currentValue = CollectValue(DefaultPukString, "PUK");
                    newValue = CollectValue(DefaultPinString, "PIN");
                    break;

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    if (keyEntryData.IsRetry == true)
                    {
                        return false;
                    }
                    currentValue = CollectValue(DefaultMgmtKeyString, "Management Key (24 bytes in hex, e.g. A1 29 07... or A12907...)");
                    break;

                case KeyEntryRequest.ChangePivManagementKey:
                    if (keyEntryData.IsRetry == true)
                    {
                        return false;
                    }
                    currentValue = CollectValue(DefaultMgmtKeyString, "Current Management Key (24 bytes in hex, e.g. A1 29 07... or A12907...)");
                    newValue = CollectValue(DefaultMgmtKeyString, "New Management Key (24 bytes in hex, e.g. A1 29 07... or A12907...)");
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

        private bool GetUserInputOnRetries(KeyEntryData keyEntryData)
        {
            if (keyEntryData.RetriesRemaining == 0)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "Out of key entry retries, cancelling operation.");
                return false;
            }

            string title = keyEntryData.RetriesRemaining + " tries remaining, continue?";
            string[] menuItems = new string[] {
                "Yes, try again",
                "No, cancel operation"
            };
            int response = _menuObject.RunMenu(title, menuItems);
            return response == 0;
        }

        // Collect a value.
        // The name describes what to collect.
        // The defaultValueString is a string describing the default value and
        // must be one of the private conts string values "Default...Sting".
        private static byte[] CollectValue(string defaultValueString, string name)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter " + name);
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter D for default value (" + defaultValueString + ")");
            char[] collectedValue = SampleMenu.ReadResponse(out int _);

            if ((collectedValue.Length == 1) && (collectedValue[0] == 'D'))
            {
                return defaultValueString switch
                {
                    DefaultPinString => new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 },
                    DefaultPukString => new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 },
                    DefaultMgmtKeyString => new byte[] {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    },
                    _ => Array.Empty<byte>(),
                };
            }

            byte[] returnValue = ConvertCharArrayToByteArray(defaultValueString, collectedValue);
            Array.Fill<char>(collectedValue, '0');

            return returnValue;
        }

        // If the valueType is MgmtKeyString, then convert a hex string to a byte
        // array.
        // Otherwise, convert each character to its associated ASCII byte.
        private static byte[] ConvertCharArrayToByteArray(string valueType, char[] valueChars)
        {
            if (string.Compare(valueType, DefaultMgmtKeyString, StringComparison.Ordinal) == 0)
            {
                byte[] keyArray = new byte[24];
                int indexV = 0;
                int indexR = 0;
                while (indexV < valueChars.Length)
                {
                    if (valueChars[indexV] != ' ')
                    {
                        keyArray[indexR] = (byte)valueChars[indexV];
                        indexR++;
                        if (indexR >= 24)
                        {
                            break;
                        }
                    }
                    indexV++;
                }

                return keyArray;
            }

            byte[] pinArray = new byte[valueChars.Length];
            for (int index = 0; index < valueChars.Length; index++)
            {
                pinArray[index] = (byte)valueChars[index];
            }

            return pinArray;
        }
    }
}
