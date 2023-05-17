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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    // This KeyCollector class can be used to provide the KeyCollector delegate
    // to the Fido2Session class.
    // This sample key collector is specifically built for FIDO2 only.
    public class Fido2SampleKeyCollector
    {
        // This allows the caller to specify what the operation is.
        // Some messages (such as Touch) can contain more information if this is
        // known.
        public Fido2KeyCollectorOperation Operation { get; set; }

        public Fido2SampleKeyCollector()
        {
            Operation = Fido2KeyCollectorOperation.None;
        }

        public bool Fido2SampleKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "A previous entry was incorrect, do you want to retry?");
                if (!(keyEntryData.RetriesRemaining is null))
                {
                    string retryString = ((int)keyEntryData.RetriesRemaining).ToString("D", CultureInfo.InvariantCulture);
                    SampleMenu.WriteMessage(MessageType.Title, 0, "(retries remainin until blocked: " + retryString + ")");
                }
                SampleMenu.WriteMessage(MessageType.Title, 0, "y/n");
                char[] answer = SampleMenu.ReadResponse(out int _);
                if ((answer.Length == 0) || ((answer[0] != 'y') && (answer[0] != 'Y')))
                {
                    return false;
                }
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

                case KeyEntryRequest.EnrollFingerprint:
                    if (!(keyEntryData.LastBioEnrollSampleResult is null))
                    {
                        string lastResult = keyEntryData.LastBioEnrollSampleResult.LastEnrollSampleStatus.ToString();
                        SampleMenu.WriteMessage(MessageType.Title, 0, "                      Sample result: " + lastResult);
                        SampleMenu.WriteMessage(
                            MessageType.Title, 0,
                            "Number of good samples still needed: " + keyEntryData.LastBioEnrollSampleResult.RemainingSampleCount);
                    }
                    SampleMenu.WriteMessage(MessageType.Title, 0, "\nPlease provide a fingerprint sample.\n");
                    return true;

                case KeyEntryRequest.VerifyFido2Uv:
                    ReportOperation();
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Use the fingerprint reader to authenticate.\n");
                    return true;

                case KeyEntryRequest.SetFido2Pin:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Setting the FIDO2 application's PIN, enter the PIN.");
                    collectedValue = SampleMenu.ReadResponse(out int _);
                    pinValue = ConvertCharArrayToByteArray(collectedValue);
                    keyEntryData.SubmitValue(pinValue);
                    Array.Fill<char>(collectedValue, '0');
                    CryptographicOperations.ZeroMemory(pinValue);
                    break;

                case KeyEntryRequest.VerifyFido2Pin:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the PIN in order to verify.");
                    collectedValue = SampleMenu.ReadResponse(out int _);
                    pinValue = ConvertCharArrayToByteArray(collectedValue);
                    keyEntryData.SubmitValue(pinValue);
                    Array.Fill<char>(collectedValue, '0');
                    CryptographicOperations.ZeroMemory(pinValue);
                    break;

                case KeyEntryRequest.ChangeFido2Pin:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Changing the FIDO2 PIN, enter the current PIN.");
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

                case Fido2KeyCollectorOperation.MakeCredential:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "The YubiKey is trying to make a FIDO2 credential,");
                    break;

                case Fido2KeyCollectorOperation.GetAssertion:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "The YubiKey is trying to get a FIDO2 assertion,");
                    break;

                case Fido2KeyCollectorOperation.Reset:
                    SampleMenu.WriteMessage(MessageType.Title, 0, "\nThe YubiKey is trying to reset the FIDO2 application,");
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
