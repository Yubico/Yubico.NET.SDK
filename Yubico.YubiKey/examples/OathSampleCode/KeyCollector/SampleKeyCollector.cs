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

using System.Text;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    // This KeyCollector class can be used to provide the KeyCollector delegate to the OathSession class.
    // This KeyCollector is not secure. It simply asks for the user to enter password at the keyboard,
    // with no hiding.
    public static class SampleKeyCollector
    {
        // This is the callback. When the SDK needs a password,
        // this is the method that will be called.
        public static bool SampleKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            byte[] currentValue;
            byte[] newValue = null;

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.VerifyOathPassword:
                    currentValue = CollectPassword("Password");
                    break;

                case KeyEntryRequest.SetOathPassword:
                    currentValue = CollectPassword("Current Password. If there is no current password just press Enter.");
                    newValue = CollectPassword("New Password");
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

        // Collect a value.
        private static byte[] CollectPassword(string name)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter " + name);
            _ = SampleMenu.ReadResponse(out string collectedValue);
            byte[] bytes = Encoding.UTF8.GetBytes(collectedValue);

            return bytes;
        }
    }
}
