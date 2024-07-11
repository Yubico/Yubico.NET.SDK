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

using System.Collections.Generic;
using Yubico.YubiKey.Sample.SharedCode;
using Yubico.YubiKey.U2f;

namespace Yubico.YubiKey.Sample.U2fSampleCode
{
    // This class simply runs the main menu ("What do you want to do?") and calls
    // on the classes that perform each of the sample operations.
    public partial class U2fSampleRun
    {
        private readonly SampleMenu _menuObject;
        private bool _chosenByUser;
        private IYubiKeyDevice _yubiKeyChosen;
        private readonly U2fSampleKeyCollector _keyCollector;

        private readonly Dictionary<string, RegistrationData> _credentials;

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
        public U2fSampleRun(int maxInvalidCount)
        {
            _menuObject = new SampleMenu(maxInvalidCount, typeof(U2fMainMenuItem), (int)U2fMainMenuItem.Exit);
            _keyCollector = new U2fSampleKeyCollector();
            _credentials = new Dictionary<string, RegistrationData>();
        }

        // Run the sample.
        // Run the main menu, then based on the item chosen, run the appropriate
        // operation.
        // After running the operation, return to the main menu. Keep doing this
        // until the user calls for Exit or enters too many invalid responses in
        // a row.
        public void RunSample()
        {
            U2fMainMenuItem menuItem;

            do
            {
                menuItem = (U2fMainMenuItem)_menuObject.RunMainMenu("What do you want to do?");

                // If whatever the user wants to do requires a YubiKey, make sure
                // we have one chosen. If the caller has chosen one specifically
                // (they ran ChooseYubiKey), use it. If not, pick a default.
                // If we have already picked a default, it is possible the user
                // removed it and inserted another, so verify it is still
                // inserted. If so, keep using it. If not, find another default.
                // does not require a chosen YubiKey, this method will do nothing
                // and return true.
                if (DefaultChooseYubiKey(menuItem))
                {
                    if (!RunMenuItem(menuItem))
                    {
                        menuItem = U2fMainMenuItem.Exit;
                    }
                }
            } while (menuItem != U2fMainMenuItem.Exit);
        }

        // Make sure a YubiKey is chosen.
        // If the user has already chosen a YubiKey, don't do anything, just
        // return true.
        // If the menuItem is Exit or NoItem or ListYubiKeys (or something
        // similar), don't choose, just return true.
        private bool DefaultChooseYubiKey(U2fMainMenuItem menuItem)
        {
            switch (menuItem)
            {
                case U2fMainMenuItem.ListYubiKeys:
                case U2fMainMenuItem.ChooseYubiKey:
                case U2fMainMenuItem.Exit:
                    return true;

                default:
                    if (_chosenByUser)
                    {
                        return true;
                    }

                    return ChooseYubiKey.RunChooseYubiKey(
                        false,
                        _menuObject,
                        Transport.HidFido,
                        ref _yubiKeyChosen);
            }
        }

        // Run this method if the caller has chosen the menu item ChooseYubiKey.
        private bool RunChooseYubiKey()
        {
            _chosenByUser = ChooseYubiKey.RunChooseYubiKey(
                true,
                _menuObject,
                Transport.HidFido,
                ref _yubiKeyChosen);

            return _chosenByUser;
        }
    }
}
