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

using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    // This class simply runs the main menu ("What do you want to do?") and calls
    // on the classes that perform each of the sample operations.
    public partial class OathSampleRun
    {
        private readonly SampleMenu _menuObject;
        private Credential _credentialChosen;
        private int? _index;
        private int? _optionIndex;
        private IYubiKeyDevice _yubiKeyChosen;

        // Provide the max invalid count, this is the number of times in a row
        // the a user can type an invalid response to a menu request before
        // giving up.
        // For example, if the maxInvalidCount is 2, and in response to the main
        // menu the user types "5000", the class will indicate invalid response
        // and ask again. If on the second try, the user types "11111", the class
        // will indicate invalid response and exit.
        // The maxInvalidCount can be 1, 2, 3, 4, or 5. Any other input and the
        // class will set the max to 3. For example, if you instantiate this
        // class
        //   var sampleRun = new SampleRun(maxInvalidCount: 21).
        // the constructor will build an object and the maxInvalidCount will be 3.
        public OathSampleRun(int maxInvalidCount)
        {
            _menuObject = new SampleMenu(maxInvalidCount, typeof(OathMainMenuItem), (int)OathMainMenuItem.Exit);
        }

        // Run the sample.
        // Run the main menu, then based on the item chosen, run the appropriate
        // operation.
        // After running the operation, return to the main menu. Keep doing this
        // until the user calls for Exit or enters too many invalid responses in
        // a row.
        public void RunSample()
        {
            OathMainMenuItem menuItem;

            do
            {
                menuItem = (OathMainMenuItem)_menuObject.RunMainMenu("What do you want to do?");

                // If whatever the user wants to do requires a YubiKey, make sure
                // we have one chosen. If one is already chosen or the menuItem
                // does not require a chosen YubiKey, this method will do nothing
                // and return true.
                if (DefaultChooseYubiKey(menuItem))
                {
                    menuItem = RunMenuItem(menuItem);
                }
            } while (menuItem != OathMainMenuItem.Exit);
        }

        // Make sure a YubiKey is chosen.
        // If there is a chosen YubiKey already, don't do anything, just return
        // true.
        // If the menuItem is Exit or NoItem or ChooseYubiKey (or something
        // similar), don't choose, just return true.
        private bool DefaultChooseYubiKey(OathMainMenuItem menuItem)
        {
            if (!(_yubiKeyChosen is null))
            {
                return true;
            }

            switch (menuItem)
            {
                case OathMainMenuItem.ListYubiKeys:
                case OathMainMenuItem.ChooseYubiKey:
                case OathMainMenuItem.Exit:
                    return true;

                default:
                    return ChooseYubiKey.RunChooseYubiKey(alwaysAsk: false, _menuObject, Transport.UsbSmartCard,
                        ref _yubiKeyChosen);
            }
        }
    }
}
