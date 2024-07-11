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
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    public static class ChooseCredential
    {
        // Choose the credential to use.
        // If alwaysAsk is true, then always list the credentials and ask the user to select,
        // even if there is only one credential stored. If alwaysAsk is false, then don't ask
        // the user to choose if there is only one credential, just use it. If there are more
        // than one ask the user to choose.
        // Return true if the method succeeds, false otherwise. Note that if there are no
        // credentials on the YubiKey, this will report that fact and return false.
        // So, make sure your logic works appropriately when receiving a false from this method.
        public static bool RunChooseCredential(
            IYubiKeyDevice yubiKey,
            bool alwaysAsk,
            SampleMenu menuObject,
            out Credential chosenCredential)
        {
            chosenCredential = null;

            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            using var oathSession = new OathSession(yubiKey);
            {
                IList<Credential> credentials = oathSession.GetCredentials();

                // Are there any?
                if (credentials.Count == 0)
                {
                    SampleMenu.WriteMessage(MessageType.Special, 0, "No credentials found");
                    return false;
                }

                if (credentials.Count == 1 && alwaysAsk == false)
                {
                    chosenCredential = credentials[0];
                    return true;
                }

                // Write out a menu requesting the caller choose one.
                string[] choices = new string[credentials.Count];
                for (int index = 0; index < credentials.Count; index++)
                {
                    string name = credentials[index].Name;
                    choices[index] = name;
                }

                int indexChosen = menuObject.RunMenu("Which Credential do you want to use?", choices);
                if (indexChosen >= 0 && indexChosen < credentials.Count)
                {
                    chosenCredential = credentials[indexChosen];
                    return true;
                }
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }

        // Choose how to add credential.
        // Return true if the method succeeds, false otherwise.
        public static bool RunChooseAddCredentialOption(SampleMenu menuObject, out int? index)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            index = null;
            // Write out a menu requesting the caller choose one.
            string[] choices =
            {
                "Create credential yourself",
                "Add an already configured credential",
            };

            int indexChosen = menuObject.RunMenu("How would you want to add it?", choices);

            if (indexChosen >= 0 && indexChosen < choices.Length)
            {
                index = indexChosen;
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }

        // Choose what kind of credential to add.
        // Return true if the method succeeds, false otherwise.
        public static bool RunChooseCredentialOption(SampleMenu menuObject, out int? index)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            index = null;
            // Write out a menu requesting the caller choose one.
            string[] choices =
            {
                "TOTP Credential (with default parameters)",
                "TOTP Credential (with advanced parameters)",
                "HOTP Credential (with default parameters)",
                "HOTP Credential (with advanced parameters)",
                "Credential from URI"
            };

            int indexChosen = menuObject.RunMenu("What kind of credential do you want to add?", choices);

            if (indexChosen >= 0 && indexChosen < choices.Length)
            {
                index = indexChosen;
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }

        // Choose how to remove/rename/calculate credential.
        // Return true if the method succeeds, false otherwise.
        public static bool RunChooseAction(SampleMenu menuObject, out int? index, string name)
        {
            if (menuObject is null)
            {
                throw new ArgumentNullException(nameof(menuObject));
            }

            index = null;
            // Write out a menu requesting the caller choose one.
            string[] choices =
            {
                "Create existing credential yourself",
                "Pick from credentials",
            };

            int indexChosen = menuObject.RunMenu("How would you want to " + name + " it?", choices);

            if (indexChosen >= 0 && indexChosen < choices.Length)
            {
                index = indexChosen;
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid response");
            return false;
        }
    }
}
