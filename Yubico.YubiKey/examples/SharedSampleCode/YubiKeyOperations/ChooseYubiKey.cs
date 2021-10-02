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
using System.Linq;
using System.Globalization;

namespace Yubico.YubiKey.Sample.SharedCode
{
    public static class ChooseYubiKey
    {
        // Choose the YubiKey to use.
        // If alwaysAsk is true, then always list the YubiKeys and ask the user
        // to select, even if yubiKeyChosen is not null (one has been previously
        // chosen), or there is only one YubiKey connected.
        // If alwaysAsk is false and yubiKeyChosen is null, choose.
        // If alwaysAsk is false and yubuKeyChosen is not null, verify that the
        // YubiKey listed is still attached. If so, just return true. If not,
        // choose.
        // If alwaysAsk is false, to choose means don't ask the user to choose if
        // there is only one connected, just use it. If there are more than one
        // connected, ask to user to choose.
        // Return true if the method succeeds, false otherwise. Note that if there
        // are no YubiKeys connected, this will report that fact and return
        // false.
        // A false result generally is converted to "Exit", but in this case, you
        // might not want to Exit. You want to give the user a chance to insert a
        // YubiKey and run the main menu again without restarting the program.
        // So make sure your logic works appropriately when receiving a false
        // from this method.
        // Set the transport arg to what sort of YubiKey you need. For example,
        // using Transport.SmartCard finds all YubiKeys connected via USB and NFC.
        // To get only YubiKeys connected via USB, call using
        // Transport.UsbSmartCard.
        // To get only YubiKeys connected via NFC, call Transport.NfcSmartCard.
        public static bool RunChooseYubiKey(
            bool alwaysAsk,
            SampleMenu menuObject,
            Transport transport,
            ref IYubiKeyDevice? yubiKeyChosen)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            // Find all currently connected YubiKeys.
            IEnumerable<IYubiKeyDevice> yubiKeyEnumerable = YubiKeyDevice.FindByTransport(transport);
            IYubiKeyDevice[] yubiKeyArray = yubiKeyEnumerable.ToArray();

            // Are there any?
            if (yubiKeyArray.Length == 0)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "No YubiKeys found");
                return false;
            }

            // If alwaysAsk is false, and yubiKeyChosen is not null, we probably
            // won't need to choose. But it is possible that since the
            // yubiKeyChosen was first chosen, it was removed and another
            // inserted. Check for that.
            // Check the serial number. If the current yubiKeyChosen does not
            // have a serial number, we can't check, so just return true. This
            // will leave the chosen YubiKey to the current value.
            if (!alwaysAsk && !(yubiKeyChosen is null))
            {
                if (yubiKeyChosen.SerialNumber is null)
                {
                    return true;
                }

                for (int index = 0; index < yubiKeyArray.Length; index++)
                {
                    if (!(yubiKeyArray[index].SerialNumber is null)
                        && (yubiKeyChosen.SerialNumber == yubiKeyArray[index].SerialNumber))
                    {
                        return true;
                    }
                }
            }

            // If we reach this point, either the original yubiKeyChosen has been
            // removed, or else we're finding a new one, but either way, set the
            // return value to null.
            yubiKeyChosen = null;

            if ((yubiKeyArray.Length == 1) && (alwaysAsk == false))
            {
                yubiKeyChosen = yubiKeyArray[0];
                return true;
            }

            // Write out a menu requesting the caller choose one.
            string[] choices = new string[yubiKeyArray.Length];
            for (int index = 0; index < yubiKeyArray.Length; index++)
            {
                string versionNumber = yubiKeyArray[index].FirmwareVersion.ToString();
                string serialNumber = yubiKeyArray[index].SerialNumber.ToString() ?? "No serial number";
                choices[index] = versionNumber + " : " + serialNumber;
            }

            int indexChosen = menuObject.RunMenu("Which YubiKey do you want to use?", choices);
            if ((indexChosen >= 0) && (indexChosen < yubiKeyArray.Length))
            {
                yubiKeyChosen = yubiKeyArray[indexChosen];
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }
    }
}
