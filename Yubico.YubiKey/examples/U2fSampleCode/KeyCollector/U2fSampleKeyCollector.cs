// Copyright 2022 Yubico AB
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
using System.Security.Cryptography;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.U2fSampleCode
{
    // This KeyCollector class can be used to provide the KeyCollector delegate
    // to the PivSession class (and others in the future).
    // This sample key collector is specifically built for U2F only. It will also
    // not allow for retries.
    public class U2fSampleKeyCollector
    {
        // This allows the caller to specify what the operation is.
        // Before calling an SDK method that will call the KeyCollector. Set this
        // property so the KeyCollector knows what message to report.
        public U2fKeyCollectorOperation Operation { get; set; }

        public U2fSampleKeyCollector()
        {
            Operation = U2fKeyCollectorOperation.None;
        }

        public bool U2fSampleKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                return false;
            }

            char[] collectedValue;
            byte[] pinValue;

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.TouchRequest:
                    ReportOperation();
                    SampleMenu.WriteMessage(MessageType.Title, 0, "touch the YubiKey's contact to complete the operation.\n");
                    return true;

                case KeyEntryRequest.SetU2fPin:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Setting the U2F application on the YubiKey to have a PIN, enter the PIN.");
                    collectedValue = SampleMenu.ReadResponse(out int _);
                    pinValue = ConvertCharArrayToByteArray(collectedValue);
                    keyEntryData.SubmitValue(pinValue);
                    Array.Fill<char>(collectedValue, '0');
                    CryptographicOperations.ZeroMemory(pinValue);
                    break;

                case KeyEntryRequest.VerifyU2fPin:
                    if (Operation == U2fKeyCollectorOperation.Register)
                    {
                        SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the PIN in order to complete registration.");
                    }
                    else
                    {
                        SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the PIN in order to verify.");
                    }
                    collectedValue = SampleMenu.ReadResponse(out int _);
                    pinValue = ConvertCharArrayToByteArray(collectedValue);
                    keyEntryData.SubmitValue(pinValue);
                    Array.Fill<char>(collectedValue, '0');
                    CryptographicOperations.ZeroMemory(pinValue);
                    break;

                case KeyEntryRequest.ChangeU2fPin:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Changing the U2F PIN, enter the current PIN.");
                    collectedValue = SampleMenu.ReadResponse(out int _);
                    byte[] currentPin = ConvertCharArrayToByteArray(collectedValue);
                    Array.Fill<char>(collectedValue, '0');
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the new PIN.");
                    collectedValue = SampleMenu.ReadResponse(out int _);
                    byte[] newPin = ConvertCharArrayToByteArray(collectedValue);
                    keyEntryData.SubmitValues(currentPin, newPin);
                    Array.Fill<char>(collectedValue, '0');
                    CryptographicOperations.ZeroMemory(currentPin);
                    CryptographicOperations.ZeroMemory(newPin);
                    break;
            }

            return true;
        }

        private void ReportOperation()
        {
            switch (Operation)
            {
                default:
                    break;

                case U2fKeyCollectorOperation.Register:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "The YubiKey is trying to register a U2F credential,");
                    break;

                case U2fKeyCollectorOperation.Authenticate:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "The YubiKey is trying to authenticate a U2F credential,");
                    break;

                case U2fKeyCollectorOperation.Reset:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "The YubiKey is trying to reset the U2F application,");
                    break;
            }
        }

        private static byte[] ConvertCharArrayToByteArray(char[] valueChars)
        {
            byte[] pinArray = new byte[valueChars.Length];
            for (int index = 0; index < valueChars.Length; index++)
            {
                pinArray[index] = (byte)valueChars[index];
            }

            return pinArray;
        }
    }
}
