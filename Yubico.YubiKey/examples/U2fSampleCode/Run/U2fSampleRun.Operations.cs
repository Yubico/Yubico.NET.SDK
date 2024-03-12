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
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Sample.SharedCode;
using Yubico.YubiKey.U2f;

namespace Yubico.YubiKey.Sample.U2fSampleCode
{
    // This file contains the methods to run each of the main menu items.
    // The main menu is displayed, the user selects an option, and the code that
    // receives the choice will call the appropriate method in this file to
    // make the appropriate calls to perform the operation selected.
    public partial class U2fSampleRun
    {
        public bool RunMenuItem(U2fMainMenuItem menuItem) => menuItem switch
        {
            U2fMainMenuItem.Exit => false,
            // Find all currently connected YubiKeys that can communicate
            // over the HID FIDO protocol. This is the protocol used to
            // communicate with the U2F application.
            // Using Transport.HidFido finds all YubiKeys connected via USB.
            U2fMainMenuItem.ListYubiKeys => ListYubiKeys.RunListYubiKeys(Transport.HidFido),
            U2fMainMenuItem.ChooseYubiKey => RunChooseYubiKey(),
            U2fMainMenuItem.IsYubiKeyFipsSeries => RunIsYubiKeyFipsSeries(),
            U2fMainMenuItem.SetPin => RunSetPin(),
            U2fMainMenuItem.ChangePin => RunChangePin(),
            U2fMainMenuItem.VerifyPin => RunVerifyPin(),
            U2fMainMenuItem.GetFipsMode => RunGetFipsMode(),
            U2fMainMenuItem.RegisterCredential => RunRegisterCredential(),
            U2fMainMenuItem.ListCredentials => RunListCredentials(),
            U2fMainMenuItem.AuthenticateCredential => RunAuthenticateCredential(),
            U2fMainMenuItem.Reset => RunReset(),
            _ => RunUnimplementedOperation(),
        };

        public static bool RunInvalidEntry()
        {
            SampleMenu.WriteMessage(MessageType.Special, 0, "Invalid entry");
            return true;
        }

        public static bool RunUnimplementedOperation()
        {
            SampleMenu.WriteMessage(MessageType.Special, 0, "Unimplemented operation");
            return true;
        }

        public bool RunIsYubiKeyFipsSeries()
        {
            SampleMenu.WriteMessage(MessageType.Special, 0,
                "The chosen YubiKey is " + (_yubiKeyChosen.IsFipsSeries ? "" : "not ") + "FIPS series");
            return true;
        }

        public bool RunGetFipsMode()
        {
            bool isFips = U2fFips.GetFipsMode(_yubiKeyChosen, out bool isFipsMode);
            if (!isFips)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "The chosen YubiKey is not FIPS series");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Special, 0,
                    "The chosen YubiKey is FIPS series " + (isFipsMode ? "" : "not ") + "in FIPS mode");
            }

            return true;
        }

        // It is possible to set the PIN only on FIPS Series YubiKeys.
        // Furthermore, it is not possible to set the PIN if it is already set.
        // It is is already set, it is possible to change it.
        public bool RunSetPin()
        {
            bool isFips = U2fFips.GetFipsMode(_yubiKeyChosen, out bool isFipsMode);
            if (!isFips)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "The chosen YubiKey is not FIPS series");
            }
            else
            {
                bool isSet = U2fFips.SetPin(_yubiKeyChosen, _keyCollector.U2fSampleKeyCollectorDelegate);
                WritePinMessage("Set PIN", isSet);
            }

            return true;
        }

        // It is possible to change the PIN only on FIPS Series YubiKeys.
        // Furthermore, it is not possible to set the PIN if it is already set.
        // It is is already set, it is possible to change it.
        public bool RunChangePin()
        {
            bool isFips = U2fFips.GetFipsMode(_yubiKeyChosen, out bool isFipsMode);
            if (!isFips)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "The chosen YubiKey is not FIPS series");
            }
            else
            {
                bool isChanged = U2fFips.ChangePin(_yubiKeyChosen, _keyCollector.U2fSampleKeyCollectorDelegate);
                WritePinMessage("Change PIN", isChanged);
            }

            return true;
        }

        public bool RunVerifyPin()
        {
            bool isFips = U2fFips.GetFipsMode(_yubiKeyChosen, out bool isFipsMode);
            if (!isFips)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "The chosen YubiKey is not FIPS series");
            }
            else
            {
                _keyCollector.Operation = U2fKeyCollectorOperation.Verify;
                bool isVerified = U2fFips.VerifyPin(_yubiKeyChosen, _keyCollector.U2fSampleKeyCollectorDelegate);
                WritePinMessage("Verify PIN", isVerified);
            }

            return true;
        }

        // For the YubiKey to perform operations necessary to register a
        // credential, it will use data provided by the client.
        // This sample will run a "simulation" of the client's operations when
        // computing that data.
        // That is, this sample code will be able to generate client values that
        // look like "real" registration data, but does not adhere to the
        // standard. After all, this sample code demonstrates how to write code
        // to call on the YubiKey to perform its part in the U2F protocol, not
        // the client. Hence, rather than write a full-fledged client application
        // that strictly adheres to the standard, this sample code provides an
        // implementation that simulates what a real client would do, producing
        // output that is indistinguishable from strictly valid data.
        public bool RunRegisterCredential()
        {
            // In order to register a new credential, the client (usually a
            // browser) will compute two values:
            //
            //   originDataHash
            //   clientDataHash
            //
            // The originDataHash is the SHA-256 digest of the origin as computed
            // by the client. That is, it is who the client is actually connected
            // to. If you read different descriptions of U2F, including the
            // standard itself, you will see this value labeled the "application
            // parameter" or "appIdHash" or "hash of the origin" or "origin data"
            // or "applicationId".
            //
            // The clientDataHash is the SHA-256 digest of the "appId" and
            // challenge. The relying party sends to the client its own computed
            // origin, called the appId. The client can compare the relying
            // party's appId with its own computed origin, they should be the
            // same thing. The relying party also sends a random challenge. The
            // clientDataHash is sometimes called the "challenge parameter".
            string relyingPartyName = GetRelyingPartyName();
            string origin = GetClientComputedOrigin(relyingPartyName);
            string challenge = GetRelyingPartyChallenge();

            byte[] clientDataHash = ComputeSampleClientDataHash(relyingPartyName, challenge);
            byte[] applicationId = ComputeSampleOriginDataHash(origin);

            // We now have the data necessary to get the registration data from
            // the YubiKey.
            // The SDK's U2fSession class calls the origin data "applicationId"
            _keyCollector.Operation = U2fKeyCollectorOperation.Register;
            if (!U2fProtocol.Register(
                _yubiKeyChosen, _keyCollector.U2fSampleKeyCollectorDelegate,
                applicationId, clientDataHash, out RegistrationData registrationData))
            {
                return false;
            }

            // Save the RegistrationData.
            _credentials.Add(origin, registrationData);

            return true;
        }

        public bool RunListCredentials()
        {
            string[] nameList = _credentials.Keys.ToArray<string>();

            if (nameList.Length == 0)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "There are currently no U2F credentials");
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "U2F Credentials");
            for (int index = 0; index < nameList.Length; index++)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "    " + nameList[index]);
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "\n");
            return true;
        }

        public bool RunAuthenticateCredential()
        {
            string[] nameList = _credentials.Keys.ToArray<string>();

            if (nameList.Length == 0)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "There are currently no U2F credentials");
                return true;
            }

            int response = _menuObject.RunMenu("Which credential is to be authenticated?", nameList);
            RegistrationData regData = _credentials[nameList[response]];

            // There are a number of ways a credential can be unauthenticated.
            // One way is if the keyHandle is invalid. If that is the case, we
            // want to return true, meaning we were able to determine that the
            // credential did not authenticate. We'll want to display a message
            // indicating what went wrong.
            // So we'll determine if the keyHandle is valid before we try to
            // verify the credential.
            if (!U2fProtocol.VerifyKeyHandle(
                _yubiKeyChosen, regData.ApplicationId, regData.ClientDataHash, regData.KeyHandle, out bool isVerified))
            {
                return false;
            }

            if (!isVerified)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "Credential did not authenticate: key handle did not match.\n");
                return true;
            }

            // Now that we know we have a vaild key handle, call on the YubiKey
            // to compute the signature.
            // Verifying a credential is a two-step process, get the YubiKey to
            // sign the data, and then verify the signature.
            // If the YubiKey is not able to sign, we're going to return false,
            // something went wrong and we can't determine if the the
            // credential authenticates or not.
            // If it is able to compute a signature, we'll verify it below.
            _keyCollector.Operation = U2fKeyCollectorOperation.Authenticate;
            if (!U2fProtocol.Authenticate(
                _yubiKeyChosen, _keyCollector.U2fSampleKeyCollectorDelegate,
                regData.ApplicationId, regData.ClientDataHash, regData.KeyHandle,
                out AuthenticationData authenticationData))
            {
                return false;
            }

            // To verify the credential, verify the signature.
            // It is possible for the signature not to verify and this method to
            // return true. In that case, the method was able to do its job,
            // which is to determine if the credential authenticates. Discovering
            // that a credential does not authenticate is not an error, it is the
            // method successfully completing the task it was designed to do,
            // namely, to determine if the crenential authenticates.
            if (authenticationData.VerifySignature(
                regData.UserPublicKey, regData.ApplicationId, regData.ClientDataHash))
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "Credential authenticates");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "Credential did not authenticate");
            }

            return true;
        }

        public bool RunReset()
        {
            string versionNumber = _yubiKeyChosen.FirmwareVersion.ToString();

            if (_yubiKeyChosen.FirmwareVersion >= new FirmwareVersion(5, 0, 0)
                || _yubiKeyChosen.FirmwareVersion < new FirmwareVersion(4, 0, 0)
                || !_yubiKeyChosen.IsFipsSeries)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "It is possible to reset the U2F application on only version 4");
                SampleMenu.WriteMessage(MessageType.Title, 0, "FIPS series YubiKeys.");
                if (_yubiKeyChosen.IsFipsSeries)
                {
                    SampleMenu.WriteMessage(MessageType.Title, 0, "Although this is a FIPS series YubiKey, the version is");
                    SampleMenu.WriteMessage(MessageType.Title, 0, versionNumber + "\n");
                }
                else
                {
                    SampleMenu.WriteMessage(MessageType.Title, 0, "This YubiKey is version " + _yubiKeyChosen.FirmwareVersion.ToString());
                    SampleMenu.WriteMessage(MessageType.Title, 0, "and is not FIPS series.\n");
                }

                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "DANGER!!!");
            SampleMenu.WriteMessage(MessageType.Title, 0, "Resetting the U2F application will mean losing all U2F credentials");
            SampleMenu.WriteMessage(MessageType.Title, 0, "on this YubiKey, taking the U2F application out of FIPS mode, and");
            SampleMenu.WriteMessage(MessageType.Title, 0, "preventing this YubiKey's U2F application from ever being put into");
            SampleMenu.WriteMessage(MessageType.Title, 0, "FIPS mode again.\n");

            string[] menuItems = new string[] {
                "Yes",
                "No",
            };

            int response = _menuObject.RunMenu("Do you want to continue?", menuItems);
            if (response != 0)
            {
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "This is the YubiKey for which the U2F application will be reset.\n");

            int? serial = _yubiKeyChosen.SerialNumber;
            if (serial is null)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "Unknown serial number : version = " + versionNumber);
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, serial.ToString() + " : version = " + versionNumber);
            }

            response = _menuObject.RunMenu("\nIs this correct?", menuItems);
            if (response != 0)
            {
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "To reset, when prompted, you will need to remove, then re-insert");
            SampleMenu.WriteMessage(MessageType.Title, 0, "the YubiKey, then, when prompted, touch the YubiKey's contact.\n");
            response = _menuObject.RunMenu("Do you want to continue?", menuItems);
            if (response != 0)
            {
                return true;
            }

            _keyCollector.Operation = U2fKeyCollectorOperation.Reset;
            return U2fFips.RunReset(_yubiKeyChosen.SerialNumber, _keyCollector.U2fSampleKeyCollectorDelegate);
        }

        private static string GetRelyingPartyName()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter a value to represent the relying party name");
            SampleMenu.WriteMessage(MessageType.Title, 0, "It is the value the relying party sends to the client");
            SampleMenu.WriteMessage(MessageType.Title, 0, "This will represent the appId\n");
            _ = SampleMenu.ReadResponse(out string rpName);
            SampleMenu.WriteMessage(MessageType.Title, 0, "\n");

            return rpName;
        }

        private string GetClientComputedOrigin(string relyingPartyName)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "The client (browser) determines with whom it is connected");
            SampleMenu.WriteMessage(MessageType.Title, 0, "If everything is correct, it is the same as the appId supplied by the relying party");
            SampleMenu.WriteMessage(MessageType.Title, 0, "This is the listed appId:\n\n" + relyingPartyName);
            SampleMenu.WriteMessage(MessageType.Title, 0, "\nDo you want to say the client computed the same value?");
            SampleMenu.WriteMessage(MessageType.Title, 0, "Or do you want to see what happens when they are different?");
            string[] menuItems = new string[] {
                "The computed origin is the same as the appId",
                "Enter a (possibly) different computed origin",
            };

            int response = _menuObject.RunMenu("Computed Origin", menuItems);
            if (response == 0)
            {
                return relyingPartyName;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the computed origin");
            _ = SampleMenu.ReadResponse(out string computedOrigin);

            return computedOrigin;
        }

        private static string GetRelyingPartyChallenge()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "For this sample code, the challenge is a random value base-64 encoded\n");

            byte[] randomBytes = new byte[9];
            using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            randomObject.GetBytes(randomBytes, 0, randomBytes.Length);
            string challenge = Convert.ToBase64String(randomBytes);
            SampleMenu.WriteMessage(MessageType.Title, 0, "challenge = " + challenge);
            SampleMenu.WriteMessage(MessageType.Title, 0, "\n");

            return challenge;
        }

        private static byte[] ComputeSampleClientDataHash(string relyingParty, string challenge)
        {
            return U2fSession.EncodeAndHashString(relyingParty + challenge);
        }

        private static byte[] ComputeSampleOriginDataHash(string origin)
        {
            return U2fSession.EncodeAndHashString(origin);
        }

        private static void WritePinMessage(string operation, bool result)
        {
            SampleMenu.WriteMessage(MessageType.Special, 0, operation + (result ? ", success" : ", user canceled"));
        }
    }
}
