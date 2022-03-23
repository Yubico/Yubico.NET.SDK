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

namespace Yubico.YubiKey.Oath
{
    // This KeyCollector class can be used to provide the KeyCollector delegate
    // to the OathSession class.
    public class SimpleOathKeyCollector
    {
        // If KeyFlag is set to 0, the current password and 
        // the new password returned will be the alternate.
        // The alternate is the same except the first byte is different: 0x39.
        // If KeyFlag is set to 1, the current will be the alternate and the new
        // will be the default.
        public int KeyFlag { get; set; }

        public SimpleOathKeyCollector()
        {
            KeyFlag = 0;
        }

        public bool SimpleKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
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
                    KeyFlag = 0;
                    return true;

                case KeyEntryRequest.VerifyOathPassword:
                    currentValue = CollectPassword();
                    break;

                case KeyEntryRequest.SetOathPassword:
                    currentValue = CollectPassword();
                    newValue = CollectPassword();
                    break;
            }

            if (newValue is null)
            {
                if (KeyFlag == 0)
                {
                    currentValue[0] = 0x39;
                }

                keyEntryData.SubmitValue(currentValue);
            }
            else
            {
                if (KeyFlag == 0)
                {
                    newValue[0] = 0x39;
                }
                else
                {
                    currentValue[0] = 0x39;
                }

                keyEntryData.SubmitValues(currentValue, newValue);
            }

            return true;
        }

        public static byte[] CollectPassword() => new byte[] { 0x74, 0x65, 0x73, 0x74 };
    }
}
