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
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Yubico.YubiKey.Sample.SharedCode;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using System.Globalization;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    // This file contains the methods to run each of the main menu items.
    // The main menu is displayed, the user selects an option, and the code that
    // receives the choice will call the appropriate method in this file to
    // make the appropriate calls to perform the operation selected.
    public partial class Fido2SampleRun
    {
        public bool RunMenuItem(Fido2MainMenuItem menuItem) => menuItem switch
        {
            Fido2MainMenuItem.Exit => false,
            // Find all currently connected YubiKeys that can communicate
            // over the HID FIDO protocol. This is the protocol used to
            // communicate with the Fido2 application.
            // Using Transport.HidFido finds all YubiKeys connected via USB.
            Fido2MainMenuItem.ListYubiKeys => ListYubiKeys.RunListYubiKeys(Transport.HidFido),
            Fido2MainMenuItem.ChooseYubiKey => RunChooseYubiKey(),
            Fido2MainMenuItem.SetPin => RunSetPin(),
            Fido2MainMenuItem.ChangePin => RunChangePin(),
            Fido2MainMenuItem.VerifyPin => RunVerifyPin(),
            Fido2MainMenuItem.MakeCredential => RunMakeCredential(),
            Fido2MainMenuItem.GetAssertion => RunGetAssertions(),
            Fido2MainMenuItem.ListCredentials => RunListCredentials(),
            Fido2MainMenuItem.UpdateCredentialUserInfo => RunUpdateCredentialUserInfo(),
            Fido2MainMenuItem.DeleteCredential => RunDeleteCredential(),
            Fido2MainMenuItem.RetrieveLargeBlobData => RunRetrieveLargeBlobData(),
            Fido2MainMenuItem.StoreLargeBlobData => RunStoreLargeBlobData(),
            Fido2MainMenuItem.DeleteLargeBlobData => RunDeleteLargeBlobData(),
            Fido2MainMenuItem.Reset => RunReset(),
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

        public bool RunReset()
        {
            string versionNumber = _yubiKeyChosen.FirmwareVersion.ToString();

            SampleMenu.WriteMessage(MessageType.Title, 0, "DANGER!!!");
            SampleMenu.WriteMessage(MessageType.Title, 0, "Resetting the FIDO2 application will mean losing all FIDO2");
            SampleMenu.WriteMessage(MessageType.Title, 0, "credentials on this YubiKey.\n");

            string[] menuItems = new string[] {
                "Yes",
                "No",
            };

            int response = _menuObject.RunMenu("Do you want to continue?", menuItems);
            if (response != 0)
            {
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "This is the YubiKey for which the FIDO2 application will be reset.\n");

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
            SampleMenu.WriteMessage(MessageType.Title, 0, "the YubiKey. Then, when prompted, touch the YubiKey's contact.\n");
            response = _menuObject.RunMenu("Do you want to continue?", menuItems);
            if (response != 0)
            {
                return true;
            }

            _keyCollector.Operation = Fido2KeyCollectorOperation.Reset;

            // To reset the FIDO2 application, one must call the reset command
            // within a short time limit after the YubiKey has been "rebooted".
            // In order to obtain an IYubiKeyDevice quickly after reinserting
            // we're going to listen for the event.
            // This means we need to worry about asynchronous operations, and the
            // EventHandler delegates must have access to the serial number.
            // So we're going to use a separate class to handle this.
            var fido2Reset = new Fido2Reset(_yubiKeyChosen.SerialNumber);
            ResponseStatus status = fido2Reset.RunFido2Reset(_keyCollector.Fido2SampleKeyCollectorDelegate);
            if (status == ResponseStatus.Success)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "\nFIDO2 application successfully reset.\n");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "\nFIDO2 application NOT reset.\n");
            }

            return true;
        }

        // It is not possible to set the PIN if it is already set.
        // It is is already set, it is possible to change it.
        public bool RunSetPin()
        {
            bool isSet = Fido2Pin.SetPin(_yubiKeyChosen, _keyCollector.Fido2SampleKeyCollectorDelegate);
            WritePinMessage("Set PIN", isSet);

            return true;
        }

        public bool RunChangePin()
        {
            bool isChanged = Fido2Pin.ChangePin(_yubiKeyChosen, _keyCollector.Fido2SampleKeyCollectorDelegate);
            WritePinMessage("Change PIN", isChanged);

            return true;
        }

        public bool RunVerifyPin()
        {
            bool isValid = Fido2Protocol.RunGetAuthenticatorInfo(_yubiKeyChosen, out AuthenticatorInfo authenticatorInfo);
            if (isValid)
            {
                isValid = GetVerifyPinArguments(
                    authenticatorInfo, out PinUvAuthTokenPermissions? permissions, out string relyingPartyId);

                if (isValid)
                {
                    _keyCollector.Operation = Fido2KeyCollectorOperation.Verify;

                    return Fido2Pin.VerifyPin(
                        _yubiKeyChosen,
                        _keyCollector.Fido2SampleKeyCollectorDelegate,
                        permissions,
                        relyingPartyId);
                }
            }

            return false;
        }

        public bool RunMakeCredential()
        {
            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "In order to make a credential, this sample code will collect relying party and user\n" +
                "info, and will set the \"rk\" option to true. If the credBlob extension is supported,\n" +
                "it will ask if the caller wants to store any data in the credBlob.\n" +
                "No other optional elements will be set.\n" +
                "For expedience, it will generate a value based on the relying party info and use it as\n" +
                "the ClientDataHash, even though what is performed in this sample code is not how it is\n" +
                "actually computed.\n" +
                "In addition, the User ID is binary data (a byte array), and this sample simply uses\n" +
                "random numbers (as recommended in W3C).\n" +
                "Furthermore, the standard specifies that the relyingPartyName, user Name, and user\n" +
                "DisplayName are optional. However, this sample requires these elements.\n");

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the relyingPartyName");
            _ = SampleMenu.ReadResponse(out string relyingPartyName);
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out string relyingPartyId);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the user Name");
            _ = SampleMenu.ReadResponse(out string userName);
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the user DisplayName");
            _ = SampleMenu.ReadResponse(out string userDisplayName);

            RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            byte[] randomBytes = new byte[16];
            randomObject.GetBytes(randomBytes);
            var userId = new ReadOnlyMemory<byte>(randomBytes);

            ReadOnlyMemory<byte> clientDataHash = BuildFakeClientDataHash(relyingPartyId);

            byte[] credBlobData = Array.Empty<byte>();
            if (!Fido2Protocol.RunGetAuthenticatorInfo(_yubiKeyChosen, out AuthenticatorInfo authenticatorInfo))
            {
                return false;
            }
            int maxCredBlobLength = authenticatorInfo.MaximumCredentialBlobLength ?? 0;
            if (maxCredBlobLength > 0)
            {
                string[] menuItems = new string[] {
                    "Yes",
                    "No",
                };

                int response = _menuObject.RunMenu("Do you want to store credBlob data? (Maximum " + maxCredBlobLength + " bytes)", menuItems);
                if (response == 0)
                {
                    SampleMenu.WriteMessage(
                        MessageType.Title, 0,
                        "The credBlob data can be any binary bytes but this sample code will store a string.\n" +
                        "This sample code will expect each character in the string to be UTF-16, so the limit\n" +
                        "is " + (maxCredBlobLength / 2) + " characters. If you use any characters other than UTF-16,\n" +
                        "there is no guarantee this sample will execute properly.\n" +
                        "Note that the SDK will check the length of the credBlob, but not this sample code.");

                    SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the credBlob data");
                    _ = SampleMenu.ReadResponse(out string credBlobDataString);

                    credBlobData = Encoding.Unicode.GetBytes(credBlobDataString);
                }
            }

            _keyCollector.Operation = Fido2KeyCollectorOperation.MakeCredential;

            bool isValid = Fido2Protocol.RunMakeCredential(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                clientDataHash,
                relyingPartyName, relyingPartyId,
                userName, userDisplayName, userId,
                credBlobData,
                out MakeCredentialData makeCredentialData);

            if (!isValid)
            {
                return false;
            }

            // Store whatever information you need.
            _credentialList.Add(makeCredentialData);

            var publicKey = (CoseEcPublicKey)makeCredentialData.AuthenticatorData.CredentialPublicKey;
            string xCoordinate = BitConverter.ToString(publicKey.XCoordinate.ToArray()).Replace("-", string.Empty, StringComparison.Ordinal);
            string yCoordinate = BitConverter.ToString(publicKey.YCoordinate.ToArray()).Replace("-", string.Empty, StringComparison.Ordinal);
            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "public key credential:\n  x-coordinate = " + xCoordinate + "\n  " + "y-coordinate = " + yCoordinate + "\n");

            return true;
        }

        public bool RunGetAssertions()
        {
            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "This will return zero, one, or more assertions.\n" +
                "Because this sample code only has access to public keys of credentials made during\n" +
                "this sample run, it will only be able to verify the signatures of assertions for\n" +
                "those credentials.\n" +
                "For expedience, it will generate a value based on the relying party info and use it\n" +
                "as the ClientDataHash, even though what is performed in this sample code is not how\n" +
                "it is actually computed.\n");

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out string relyingPartyId);

            ReadOnlyMemory<byte> clientDataHash = BuildFakeClientDataHash(relyingPartyId);

            _keyCollector.Operation = Fido2KeyCollectorOperation.GetAssertion;

            if (!Fido2Protocol.RunGetAssertions(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                clientDataHash,
                relyingPartyId,
                out IReadOnlyList<GetAssertionData> assertions))
            {
                return false;
            }

            if (assertions.Count == 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nThe YubiKey was not able to get any assertions for the specified relying party ID.\n");

                return true;
            }

            for (int index = 0; index < assertions.Count; index++)
            {
                DisplayAssertion(index, assertions[index], clientDataHash);
            }

            return true;
        }

        public bool RunListCredentials()
        {
            if (!Fido2Protocol.RunGetCredentialData(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                out IReadOnlyList<CredentialManagementData> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, true, true, out int _);

            return true;
        }

        public bool RunUpdateCredentialUserInfo()
        {
            if (!Fido2Protocol.RunGetCredentialData(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                out IReadOnlyList<CredentialManagementData> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, false, false, out int credentialCount);

            int index = SelectCredential(credentialData, credentialCount);
            if ((index < 0) || (credentialData[index].User is null))
            {
                return false;
            }

            UserEntity updatedInfo = GetUpdatedInfo(credentialData[index].User);

            return Fido2Protocol.RunUpdateUserInfo(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                credentialData[index].CredentialId,
                updatedInfo);
        }

        public bool RunDeleteCredential()
        {
            if (!Fido2Protocol.RunGetCredentialData(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                out IReadOnlyList<CredentialManagementData> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, false, false, out int credentialCount);

            int index = SelectCredential(credentialData, credentialCount);
            // If there are no credentials, there's nothing to do.
            if ((index < 0) || (credentialData[index].CredentialId is null))
            {
                return false;
            }

            if (!Fido2Protocol.RunDeleteCredential(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                credentialData[index].CredentialId))
            {
                return false;
            }

            // The FIDO2 standard recommends deleting any largeBlob
            // data associated with a credential along with the credential itself.
            // This sample code will still return true even if something goes
            // wrong trying to delete an entry in the largeBlob data.
            if (!Fido2Protocol.RunGetLargeBlobArray(
                _yubiKeyChosen,
                out SerializedLargeBlobArray blobArray))
            {
                return true;
            }

            _ = Fido2Protocol.GetLargeBlobEntry(
                blobArray, credentialData[index].LargeBlobKey.Value, out int entryIndex);

            if (entryIndex < 0)
            {
                return true;
            }

            blobArray.RemoveEntry(entryIndex);

            _ = Fido2Protocol.RunStoreLargeBlobArray(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                blobArray);

            return true;
        }

        public bool RunRetrieveLargeBlobData()
        {
            if (!Fido2Protocol.RunGetLargeBlobArray(
                _yubiKeyChosen,
                out SerializedLargeBlobArray blobArray))
            {
                return false;
            }

            if (blobArray.Entries.Count == 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nThere is no largeBlob data stored on the YubiKey.\n");

                return true;
            }

            if (blobArray.Entries.Count == 1)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nThere is one largeBlob entry stored on the YubiKey.\n");
            }
            else
            {
                string entryCount = blobArray.Entries.Count.ToString(CultureInfo.InvariantCulture);
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nThere are " + entryCount + " largeBlob entries stored on the YubiKey.\n");
            }

            string[] menuItems = new string[] {
                "Yes",
                "No",
            };
            int response = _menuObject.RunMenu("Continue?", menuItems);
            if (response != 0)
            {
                return true;
            }

            if (!Fido2Protocol.RunGetCredentialData(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                out IReadOnlyList<CredentialManagementData> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, false, true, out int credentialCount);

            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "LargeBlob data is stored against a credential. Select a credential for which\n" +
                "you want to see the largeBlob data. It is possible to retrieve data only for\n" +
                "credentials that have an available Large Blob Key.\n");
            int index = SelectCredential(credentialData, credentialCount);
            if ((index < 0) || (credentialData[index].LargeBlobKey is null))
            {
                return false;
            }

            string currentContents = Fido2Protocol.GetLargeBlobEntry(
                blobArray, credentialData[index].LargeBlobKey.Value, out int entryIndex);

            if (entryIndex < 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nThere is no largeBlob data associated with the selected credential.\n");
            }
            else
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nThe largeBlob data for the selected credential is the following:\n\n" + currentContents + "\n");
            }

            return true;
        }

        public bool RunStoreLargeBlobData()
        {
            // The way to store large blob data is to get the current large blob
            // data and "edit" it.
            if (!Fido2Protocol.RunGetLargeBlobArray(
                _yubiKeyChosen,
                out SerializedLargeBlobArray blobArray))
            {
                return false;
            }

            if (!Fido2Protocol.RunGetCredentialData(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                out IReadOnlyList<CredentialManagementData> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, false, true, out int credentialCount);

            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "LargeBlob data is stored against a credential. That is, for each credential, it\n" +
                "is possible to store some largeBlob data. However, the credential must be made\n" +
                "with the largeBlob option. Hence, you must choose a credential against which the\n" +
                "data will be stored, and that credential must have an available Large Blob Key.\n" +
                "Note that this sample code will store only one entry per credential.\n");
            int index = SelectCredential(credentialData, credentialCount);
            if ((index < 0) || (credentialData[index].LargeBlobKey is null))
            {
                return false;
            }

            string currentContents = Fido2Protocol.GetLargeBlobEntry(
                blobArray, credentialData[index].LargeBlobKey.Value, out int entryIndex);

            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "The largeBlob data can be any binary bytes but this sample code will accept only\n" +
                "strings. This sample code will expect each character in the string to be UTF-16.\n" +
                "If you use any characters other than UTF-16, there is no guarantee this sample will\n" +
                "execute properly.\n" +
                "Note also that what you supply as the largeBlob data will replace the current contents.");

            if (entryIndex < 0)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "\nThere are no current contents.\n");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "\nThe current contents:\n\n" + currentContents + "\n");
                blobArray.RemoveEntry(entryIndex);
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the largeBlob data to store.");
            _ = SampleMenu.ReadResponse(out string largeBlobDataString);

            byte[] largeBlobData = Encoding.Unicode.GetBytes(largeBlobDataString);
            blobArray.AddEntry(largeBlobData, credentialData[index].LargeBlobKey.Value);

            return Fido2Protocol.RunStoreLargeBlobArray(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                blobArray);
        }

        public bool RunDeleteLargeBlobData()
        {
            if (!Fido2Protocol.RunGetLargeBlobArray(
                _yubiKeyChosen,
                out SerializedLargeBlobArray blobArray))
            {
                return false;
            }

            if (!Fido2Protocol.RunGetCredentialData(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                out IReadOnlyList<CredentialManagementData> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, false, true, out int credentialCount);

            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "LargeBlob data is stored against a credential. Select a credential for which\n" +
                "you want to delete the largeBlob data. If there are no credentials on the\n" +
                "YubiKey, or if there is no largeBlob data stored, or nothing stored against\n" +
                "the selected credential, this sample code will do nothing.\n");

            int index = SelectCredential(credentialData, credentialCount);

            // If there's nothing to delete, there's nothing to do.
            if ((index < 0) || (credentialData[index].LargeBlobKey is null))
            {
                return true;
            }

            string currentContents = Fido2Protocol.GetLargeBlobEntry(
                blobArray, credentialData[index].LargeBlobKey.Value, out int entryIndex);

            if (entryIndex < 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nThere is no largeBlob data associated with the selected credential.\n");

                return true;
            }

            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "\nThe largeBlob data (to be deleted) for the selected credential is the following:\n\n" + currentContents + "\n");
            blobArray.RemoveEntry(entryIndex);

            return Fido2Protocol.RunStoreLargeBlobArray(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                blobArray);
        }

        private void DisplayAssertion(int index, GetAssertionData assertion, ReadOnlyMemory<byte> clientDataHash)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Assertion number " + index);
            string userIdString = BitConverter.ToString(assertion.User.Id.ToArray()).Replace("-", string.Empty, StringComparison.Ordinal);
            SampleMenu.WriteMessage(MessageType.Title, 0, "User ID: " + userIdString);
            if (!(assertion.User.DisplayName is null))
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "User DisplayName: " + assertion.User.DisplayName);
            }
            string credentialIdString = BitConverter.ToString(assertion.CredentialId.Id.ToArray()).Replace("-", string.Empty, StringComparison.Ordinal);
            SampleMenu.WriteMessage(MessageType.Title, 0, "Credential ID: " + credentialIdString);

            byte[] credBlobData = assertion.AuthenticatorData.GetCredBlobExtension();
            if (credBlobData.Length > 0)
            {
                string credBlobDataString = Encoding.Unicode.GetString(credBlobData);
                SampleMenu.WriteMessage(MessageType.Title, 0, "Credential Blob: " + credBlobDataString);
            }

            int indexC = FindCredential(assertion.CredentialId.Id);
            if (indexC >= 0)
            {
                bool isVerified = assertion.VerifyAssertion(
                    _credentialList[indexC].AuthenticatorData.CredentialPublicKey, clientDataHash);

                string verifyResult = isVerified ? "is verified" : "does not verify";
                SampleMenu.WriteMessage(MessageType.Title, 0, "Assertion signature " + verifyResult + "\n");
            }
            else
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "No stored credential in sample run, Assertion signature cannot be verified\n");
            }
        }

        // Find the credential in _credentialList that has the given
        // credentialId. Return its index in the List.
        // In no credential has that ID, return -1.
        private int FindCredential(ReadOnlyMemory<byte> credentialId)
        {
            int index = 0;
            for (; index < _credentialList.Count; index++)
            {
                if (MemoryExtensions.SequenceEqual(
                    _credentialList[index].AuthenticatorData.CredentialId.Id.Span, credentialId.Span))
                {
                    break;
                }
            }

            return (index < _credentialList.Count) ? index : -1;
        }

        // This does not build a real ClientDataHash. It builds something that
        // looks like a real ClientDataHash, but this is not the algorithm used
        // to build the proper value. To see how to build a proper
        // ClientDataHash, see section 5.8.1 of the W3C WebAuthn API
        // recommendations.
        private static ReadOnlyMemory<byte> BuildFakeClientDataHash(string relyingPartyId)
        {
            byte[] idBytes = System.Text.Encoding.Unicode.GetBytes(relyingPartyId);

            // Generate a random value to represent the challenge.
            RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            byte[] randomBytes = new byte[16];
            randomObject.GetBytes(randomBytes);

            SHA256 digester = CryptographyProviders.Sha256Creator();
            _ = digester.TransformBlock(randomBytes, 0, randomBytes.Length, null, 0);
            _ = digester.TransformFinalBlock(idBytes, 0, idBytes.Length);

            return new ReadOnlyMemory<byte>(digester.Hash);
        }

        private static void WritePinMessage(string operation, bool result)
        {
            SampleMenu.WriteMessage(MessageType.Special, 0, operation + (result ? ", success" : ", user canceled"));
        }

        private bool GetVerifyPinArguments(
            AuthenticatorInfo authenticatorInfo, out PinUvAuthTokenPermissions? permissions, out string relyingPartyId)
        {
            permissions = null;
            relyingPartyId = "";

            bool isPinUvAuthTokenOption = CheckPinUvAuthTokenOption(authenticatorInfo);

            string supportString = isPinUvAuthTokenOption ? "supports" : "does not support";
            string verifyPinInstructions =
                "In order to verify a PIN, the caller has the option to specify the permissions\n" +
                "and possibly a relying party ID.\n" +
                "However, that is possible only if the YubiKey supports the pinUvAuthToken Option\n" +
                "(see the AuthenticatorInfo returned by Fido2Session.GetAuthenticatorInfo).\n\n" +
                "The YubiKey chosen " + supportString + " that Option.";
            SampleMenu.WriteMessage(MessageType.Title, 0, verifyPinInstructions);

            if (!isPinUvAuthTokenOption)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nHence, this sample will not ask for permissions and relying party ID, and\n" +
                    "will use the legacy GetPinTokenCommand in order to verify the PIN.\n");
                return true;
            }

            bool isValid = CollectPermissions(out permissions);
            if (isValid)
            {
                isValid = CollectRelyingPartyId(permissions, out relyingPartyId);
            }

            return isValid;
        }

        // Look for the "pinUvAuthToken" Option in the authInfo. If it is not
        // there, return false. If it is there and false, return false.
        // If it is there and true, return true.
        private static bool CheckPinUvAuthTokenOption(AuthenticatorInfo authenticatorInfo)
        {
            if (authenticatorInfo.Options.ContainsKey("pinUvAuthToken"))
            {
                if (authenticatorInfo.Options["pinUvAuthToken"])
                {
                    return true;
                }
            }

            return false;
        }

        private bool CollectPermissions(out PinUvAuthTokenPermissions? permissions)
        {
            permissions = null;
            PinUvAuthTokenPermissions current = PinUvAuthTokenPermissions.None;

            SampleMenu.WriteMessage(
                MessageType.Title, 0,
                "It is possible to specify more than one permission. In the following,\n" +
                "select one permission to add to your total. The menu will then repeat,\n" +
                "allowing you to add more. When there are no more permissions to add,\n" +
                "select No More\n");
            string[] menuItems = new string[] {
                "No More",
                "MakeCredential",
                "GetAssertion",
                "CredentialManagement",
                "BioEnrollment",
                "LargeBlobWrite",
                "AuthenticatorConfiguration"
            };

            int response;
            do
            {
                response = _menuObject.RunMenu("Which permission would you like to add? (Choose No More when complete.)", menuItems);
                switch (response)
                {
                    default:
                        response = 0;
                        break;

                    case 1:
                        current |= PinUvAuthTokenPermissions.MakeCredential;
                        break;

                    case 2:
                        current |= PinUvAuthTokenPermissions.GetAssertion;
                        break;

                    case 3:
                        current |= PinUvAuthTokenPermissions.CredentialManagement;
                        break;

                    case 4:
                        current |= PinUvAuthTokenPermissions.BioEnrollment;
                        break;

                    case 5:
                        current |= PinUvAuthTokenPermissions.LargeBlobWrite;
                        break;

                    case 6:
                        current |= PinUvAuthTokenPermissions.AuthenticatorConfiguration;
                        break;
                }

            } while (response != 0);

            if (current != PinUvAuthTokenPermissions.None)
            {
                permissions = current;
            }

            return true;
        }

        private bool CollectRelyingPartyId(PinUvAuthTokenPermissions? permissions, out string relyingPartyId)
        {
            relyingPartyId = "";
            PinUvAuthTokenPermissions current = PinUvAuthTokenPermissions.None;
            if (!(permissions is null))
            {
                current = permissions.Value;
            }

            if (current.HasFlag(PinUvAuthTokenPermissions.GetAssertion)
                || current.HasFlag(PinUvAuthTokenPermissions.MakeCredential))
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nBased on the permissions, a relyingPartyId is required\n");
            }
            else if (current.HasFlag(PinUvAuthTokenPermissions.CredentialManagement))
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nBased on the permissions, a relyingPartyId is optional.\n");
                string[] menuItems = new string[] {
                    "Yes",
                    "No",
                };
                int response = _menuObject.RunMenu("Do you want to enter one?", menuItems);
                if (response != 0)
                {
                    return true;
                }
            }
            else
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\nBased on the permissions, a relyingPartyId will be ignored, so\n" +
                    "this sample will not collect one.");
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out relyingPartyId);

            return true;
        }

        private static void ReportCredentials(
            IReadOnlyList<CredentialManagementData> credentialData,
            bool fullReport,
            bool largeBlobReport,
            out int credentialCount)
        {
            credentialCount = 0;
            foreach (CredentialManagementData current in credentialData)
            {
                // If NumberOfDiscoverableCredentials is not null, then this
                // entry is metadata, the ReportMetadata will report the
                // meatadata and return true. If it is null, this Report call
                // will do nothing and return false.
                if (ReportMetadata(current, fullReport))
                {
                    continue;
                }

                // NumberOfDiscoverableCredentials is null, so this is not
                // metadata. Is it a relying party? If the RelyingParty property
                // is not null, it is.
                if (ReportRelyingParty(current, fullReport))
                {
                    continue;
                }

                // If not metadata nor RP, it must be a credential.
                credentialCount++;
                ReportCredential(current, fullReport, largeBlobReport, credentialCount);
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "\n");
        }

        private static bool ReportMetadata(CredentialManagementData current, bool fullReport)
        {
            if (current.NumberOfDiscoverableCredentials is null)
            {
                return false;
            }

            if (current.NumberOfDiscoverableCredentials == 0)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "There are no credentials on the selected YubiKey.\n");
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0,
                "Discoverable credentials:  " + current.NumberOfDiscoverableCredentials);
            if (fullReport)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0,
                    "Remaining available slots: " + (current.RemainingCredentialCount.ToString() ?? "unknown"));
            }

            return true;
        }

        private static bool ReportRelyingParty(CredentialManagementData current, bool fullReport)
        {
            if (current.RelyingParty is null)
            {
                return false;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "\n  Relying party ID:       " + current.RelyingParty.Id);
            SampleMenu.WriteMessage(MessageType.Title, 0, "  Relying party Name:     " + (current.RelyingParty.Name ?? "-"));
            if (fullReport)
            {
                byte[] idHash = current.RelyingPartyIdHash?.ToArray();
                string rpIdHash = BitConverter.ToString(idHash);
                rpIdHash = rpIdHash.Replace("-", "", StringComparison.Ordinal);
                SampleMenu.WriteMessage(MessageType.Title, 0, "  Relying party ID Hash:  " + rpIdHash);
            }
            SampleMenu.WriteMessage(MessageType.Title, 0, "-----------");

            return true;
        }

        private static void ReportCredential(
            CredentialManagementData current, bool fullReport, bool largeBlobReport, int credentialIndex)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Credential index = " + credentialIndex);
            SampleMenu.WriteMessage(MessageType.Title, 0, "        User Name:          " + (current.User.Name ?? "unknown"));
            SampleMenu.WriteMessage(MessageType.Title, 0, "        User Display Name:  " + (current.User.DisplayName ?? "-"));
            if (fullReport)
            {
                byte[] id = current.User?.Id.ToArray();
                string userId = BitConverter.ToString(id);
                userId = userId.Replace("-", "", StringComparison.Ordinal);
                SampleMenu.WriteMessage(MessageType.Title, 0, "        User ID:            " + userId);
                id = current.CredentialId?.Id.ToArray();
                string credId = BitConverter.ToString(id);
                credId = credId.Replace("-", "", StringComparison.Ordinal);
                SampleMenu.WriteMessage(MessageType.Title, 0, "        Credential ID:      " + credId);
                SampleMenu.WriteMessage(MessageType.Title, 0, "        CredProtect Policy: " + GetCredProtectPolicy(current));
            }
            if (largeBlobReport)
            {
                string lbKeyStatus = (current.LargeBlobKey is null) ? "not available" : "available";
                SampleMenu.WriteMessage(MessageType.Title, 0, "        Large Blob Key:     " + lbKeyStatus);
                SampleMenu.WriteMessage(MessageType.Title, 0, "-----------");
            }
        }

        private static string GetCredProtectPolicy(CredentialManagementData current)
        {
            int cpValue = current.CredProtectPolicy ?? -1;

            return cpValue switch
            {
                1 => "userVerificationOptional",
                2 => "userVerificationOptionalWithCredentialIDList",
                3 => "userVerificationRequired",
                _ => "unknown",
            };
        }

        // Return the index in the credentialData List of the credential of
        // interest.
        private int SelectCredential(
            IReadOnlyList<CredentialManagementData> credentialData, int credentialCount)
        {
            if (credentialCount > 0)
            {
                string[] menuItems = new string[credentialCount];
                int index = 1;
                for (; index <= credentialCount; index++)
                {
                    menuItems[index - 1] = "Credential index " + index.ToString(CultureInfo.InvariantCulture);
                };

                int response = _menuObject.RunMenu("On which credential do you want to operate?", menuItems);

                index = -1;
                int credIndex = 0;
                foreach (CredentialManagementData current in credentialData)
                {
                    index++;
                    if (current.User is null)
                    {
                        continue;
                    }
                    if (credIndex == response)
                    {
                        return index;
                    }
                    credIndex++;
                }
            }

            // Error case
            return -1;
        }

        private UserEntity GetUpdatedInfo(UserEntity original)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Current Name:         " + (original.Name ?? "-"));
            SampleMenu.WriteMessage(MessageType.Title, 0, "Current Display Name: " + (original.DisplayName ?? "-"));
            SampleMenu.WriteMessage(MessageType.Title, 0, "It is not possible to change the User.Id component.");

            var returnValue = new UserEntity(original.Id);

            string[] menuItems = new string[] {
                "Yes",
                "No",
            };

            int response = _menuObject.RunMenu("Do you want to change the Name?", menuItems);
            if (response == 0)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the new Name (simply press Enter to remove the Name).");
                _ = SampleMenu.ReadResponse(out string newName);

                if (!string.IsNullOrWhiteSpace(newName))
                {
                    returnValue.Name = newName;
                }
            }
            else
            {
                returnValue.Name = original.Name;
            }

            response = _menuObject.RunMenu("Do you want to change the DisplayName?", menuItems);
            if (response == 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, 0, "Enter the new DisplayName (simply press Enter to remove the DisplayName).");
                _ = SampleMenu.ReadResponse(out string newName);

                if (!string.IsNullOrWhiteSpace(newName))
                {
                    returnValue.DisplayName = newName;
                }
            }
            else
            {
                returnValue.DisplayName = original.DisplayName;
            }

            return returnValue;
        }
    }
}
