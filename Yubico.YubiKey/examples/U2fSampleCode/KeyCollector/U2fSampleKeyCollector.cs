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
        // If this is true, we have called the Register method and any contact
        // from the SDK will be during a registration operation.
        // If false, we have called the Authenticate method.
        // The sample code will set IsRegistering to true before calling Register
        // and set it to false before calling Authenticate.
        public bool IsRegistering { get; set; }

        public U2fSampleKeyCollector()
        {
            IsRegistering = true;
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
                    if (IsRegistering)
                    {
                        SampleMenu.WriteMessage(MessageType.Title, 0, "The YubiKey is trying to register a U2F credential,");
                    }
                    else
                    {
                        SampleMenu.WriteMessage(MessageType.Title, 0, "The YubiKey is trying to authenticate a U2F credential,");
                    }

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
                    if (IsRegistering)
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
