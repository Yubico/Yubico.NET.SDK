// Copyright 2023 Yubico AB
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
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    // This file contains the methods to run each of the main menu items.
    // The main menu is displayed, the user selects an option, and the code that
    // receives the choice will call the appropriate method in this file to
    // make the appropriate calls to perform the operation selected.
    public partial class Fido2SampleRun
    {
        public bool RunMenuItem(Fido2MainMenuItem menuItem)
        {
            if (menuItem >= Fido2MainMenuItem.MakeCredential
                && menuItem < Fido2MainMenuItem.Reset)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\n---This sample uses the SDK's automatic authentication (see the User's Manual)---\n");
            }

            return menuItem switch
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
                Fido2MainMenuItem.VerifyUv => RunVerifyUv(),
                Fido2MainMenuItem.MakeCredential => RunMakeCredential(),
                Fido2MainMenuItem.GetAssertion => RunGetAssertions(),
                Fido2MainMenuItem.ListCredentials => RunListCredentials(),
                Fido2MainMenuItem.UpdateCredentialUserInfo => RunUpdateCredentialUserInfo(),
                Fido2MainMenuItem.DeleteCredential => RunDeleteCredential(),
                Fido2MainMenuItem.RetrieveLargeBlobData => RunRetrieveLargeBlobData(),
                Fido2MainMenuItem.StoreLargeBlobData => RunStoreLargeBlobData(),
                Fido2MainMenuItem.DeleteLargeBlobData => RunDeleteLargeBlobData(),
                Fido2MainMenuItem.GetBioInfo => RunGetBioInfo(),
                Fido2MainMenuItem.EnrollFingerprint => RunEnrollFingerprint(),
                Fido2MainMenuItem.SetBioTemplateFriendlyName => RunSetBioTemplateFriendlyName(),
                Fido2MainMenuItem.RemoveBioEnrollment => RunRemoveBioEnrollment(),
                Fido2MainMenuItem.EnableEnterpriseAttestation => RunEnableEnterpriseAttestation(),
                Fido2MainMenuItem.ToggleAlwaysUv => RunToggleAlwaysUv(),
                Fido2MainMenuItem.SetPinConfig => RunSetPinConfig(),
                Fido2MainMenuItem.Reset => RunReset(),
                _ => RunUnimplementedOperation()
            };
        }

        public static bool RunInvalidEntry()
        {
            SampleMenu.WriteMessage(MessageType.Special, numberToWrite: 0, "Invalid entry");
            return true;
        }

        public static bool RunUnimplementedOperation()
        {
            SampleMenu.WriteMessage(MessageType.Special, numberToWrite: 0, "Unimplemented operation");
            return true;
        }

        public bool RunReset()
        {
            string versionNumber = _yubiKeyChosen.FirmwareVersion.ToString();

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "DANGER!!!");
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "Resetting the FIDO2 application will mean losing all FIDO2");
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "credentials on this YubiKey.\n");

            string[] menuItems =
            {
                "Yes",
                "No"
            };

            int response = _menuObject.RunMenu("Do you want to continue?", menuItems);
            if (response != 0)
            {
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "This is the YubiKey for which the FIDO2 application will be reset.\n");

            int? serial = _yubiKeyChosen.SerialNumber;
            if (serial is null)
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "Unknown serial number : version = " + versionNumber);
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, serial + " : version = " + versionNumber);
            }

            response = _menuObject.RunMenu("\nIs this correct?", menuItems);
            if (response != 0)
            {
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "To reset, when prompted, you will need to remove, then re-insert");
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "the YubiKey. Then, when prompted, touch the YubiKey's contact.\n");
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
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "\nFIDO2 application successfully reset.\n");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "\nFIDO2 application NOT reset.\n");
            }

            return true;
        }

        // It is not possible to set the PIN if it is already set.
        // If it is already set, it is possible to change it.
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
            bool isValid =
                Fido2Protocol.RunGetAuthenticatorInfo(_yubiKeyChosen, out AuthenticatorInfo authenticatorInfo);
            if (isValid)
            {
                isValid = GetVerifyArguments(
                    verifyPin: true, authenticatorInfo, out PinUvAuthTokenPermissions? permissions,
                    out string relyingPartyId);

                if (isValid)
                {
                    _keyCollector.Operation = Fido2KeyCollectorOperation.Verify;

                    if (Fido2Pin.VerifyPin(
                            _yubiKeyChosen,
                            _keyCollector.Fido2SampleKeyCollectorDelegate,
                            permissions,
                            relyingPartyId) == false)
                    {
                        SampleMenu.WriteMessage(MessageType.Special, numberToWrite: 0, "PIN collection canceled.");
                    }

                    return true;
                }
            }

            return false;
        }

        public bool RunVerifyUv()
        {
            bool isValid =
                Fido2Protocol.RunGetAuthenticatorInfo(_yubiKeyChosen, out AuthenticatorInfo authenticatorInfo);

            if (isValid)
            {
                isValid = GetVerifyArguments(
                    verifyPin: false, authenticatorInfo, out PinUvAuthTokenPermissions? permissions,
                    out string relyingPartyId);

                if (isValid && !(permissions is null))
                {
                    _keyCollector.Operation = Fido2KeyCollectorOperation.Verify;

                    return Fido2Pin.VerifyUv(
                        _yubiKeyChosen,
                        _keyCollector.Fido2SampleKeyCollectorDelegate,
                        permissions.Value,
                        relyingPartyId);
                }
            }

            return false;
        }

        public bool RunMakeCredential()
        {
            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "In order to make a credential, this sample code will collect relying party and user\n" +
                "info, and will set the \"rk\" option to true. If the hmac-secret extension is supported,\n" +
                "it will make sure one is created. If the credBlob extension is supported, it will ask\n" +
                "if the caller wants to store any data in the credBlob. If the credProtect extension is\n" +
                "supported, it will ask if the caller wants to set the cred protect policy.\n" +
                "No other optional elements will be set.\n" +
                "For expedience, it will generate a value based on the relying party info and use it as\n" +
                "the ClientDataHash, even though what is performed in this sample code is not how the\n" +
                "standard specifies that it be computed.\n" +
                "In addition, the User ID is binary data (a byte array), and this sample simply uses\n" +
                "random numbers (as recommended in W3C).\n" +
                "Furthermore, the standard specifies that the relyingPartyName, user Name, and user\n" +
                "DisplayName are optional. However, this sample requires these elements.\n\n" +
                "Note that the relying party name is available as a convenience, it allows the\n" +
                "application to store a more easily understood description of the relying party. The\n" +
                "relying party ID is the value used in FIDO2 computations. For example, it is often\n" +
                "based on the URL of the entity to which the user is connecting, such as\n" +
                "\"example.login.com\".\n");

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the relyingPartyName");
            _ = SampleMenu.ReadResponse(out string relyingPartyName);
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out string relyingPartyId);

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the user Name");
            _ = SampleMenu.ReadResponse(out string userName);
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the user DisplayName");
            _ = SampleMenu.ReadResponse(out string userDisplayName);

            RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            byte[] randomBytes = new byte[16];
            randomObject.GetBytes(randomBytes);
            var userId = new ReadOnlyMemory<byte>(randomBytes);

            ReadOnlyMemory<byte> clientDataHash = BuildFakeClientDataHash(relyingPartyId);

            if (!Fido2Protocol.RunGetAuthenticatorInfo(_yubiKeyChosen, out AuthenticatorInfo authenticatorInfo))
            {
                return false;
            }

            CredProtectPolicy credProtectPolicy = CredProtectPolicy.None;
            if (authenticatorInfo.Extensions.Contains("credProtect"))
            {
                string[] menuItems =
                {
                    "No (use default)",
                    "Yes - " + Enum.GetName(CredProtectPolicy.UserVerificationOptional),
                    "Yes - " + Enum.GetName(CredProtectPolicy.UserVerificationOptionalWithCredentialIDList),
                    "Yes - " + Enum.GetName(CredProtectPolicy.UserVerificationRequired)
                };
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "This YubiKey allows you to set the credProtectPolicy. If you do not set it,\n" +
                    "the policy will be the YubiKey's default. For most YubiKeys, the default is\n" +
                    "\"UserVerificationOptional\".\n");

                int response = _menuObject.RunMenu("Do you want to set the credProtectPolicy?", menuItems);
                credProtectPolicy = (CredProtectPolicy)response;
            }

            byte[] credBlobData = Array.Empty<byte>();
            int maxCredBlobLength = authenticatorInfo.MaximumCredentialBlobLength ?? 0;
            if (maxCredBlobLength > 0)
            {
                string[] menuItems =
                {
                    "Yes",
                    "No"
                };
                int response =
                    _menuObject.RunMenu("Do you want to store credBlob data? (Maximum " + maxCredBlobLength + " bytes)",
                        menuItems);
                if (response == 0)
                {
                    SampleMenu.WriteMessage(
                        MessageType.Title, numberToWrite: 0,
                        "The credBlob data can be any binary bytes but this sample code will store a string.\n" +
                        "This sample code will expect each character in the string to be UTF-16, so the limit\n" +
                        "is " + (maxCredBlobLength / 2) +
                        " characters. If you use any characters other than UTF-16,\n" +
                        "there is no guarantee this sample will execute properly.\n" +
                        "Note that the SDK will check the length of the credBlob, but not this sample code.");

                    SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the credBlob data");
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
                credProtectPolicy,
                credBlobData,
                out MakeCredentialData makeCredentialData);

            if (!isValid)
            {
                return false;
            }

            // Store whatever information you need.
            _credentialList.Add(makeCredentialData);

            var publicKey = (CoseEcPublicKey)makeCredentialData.AuthenticatorData.CredentialPublicKey;
            string xCoordinate = BitConverter.ToString(publicKey.XCoordinate.ToArray())
                .Replace("-", string.Empty, StringComparison.Ordinal);
            string yCoordinate = BitConverter.ToString(publicKey.YCoordinate.ToArray())
                .Replace("-", string.Empty, StringComparison.Ordinal);
            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "public key credential:\n  x-coordinate = " + xCoordinate + "\n  " + "y-coordinate = " + yCoordinate +
                "\n");

            return true;
        }

        public bool RunGetAssertions()
        {
            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "This will return zero, one, or more assertions.\n" +
                "Because this sample code only has access to public keys of credentials made during\n" +
                "this sample run, it will only be able to verify the signatures of assertions for\n" +
                "those credentials.\n" +
                "For expedience, it will generate a value based on the relying party info and use it\n" +
                "as the ClientDataHash, even though what is performed in this sample code is not how\n" +
                "it is actually computed.\n");

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out string relyingPartyId);

            ReadOnlyMemory<byte> clientDataHash = BuildFakeClientDataHash(relyingPartyId);

            ReadOnlyMemory<byte> salt = ReadOnlyMemory<byte>.Empty;
            bool isValid =
                Fido2Protocol.RunGetAuthenticatorInfo(_yubiKeyChosen, out AuthenticatorInfo authenticatorInfo);
            if (isValid)
            {
                if (authenticatorInfo.Extensions.Contains("hmac-secret"))
                {
                    SampleMenu.WriteMessage(
                        MessageType.Title, numberToWrite: 0,
                        "\nWould you like the hmac-secret returned with the assertions?\n" +
                        "If not, type Enter.\n" +
                        "Otherwise, enter a string that will be used to derive a salt.\n" +
                        "Normally, a salt is 32 random bytes or the digest of some identifying data.\n" +
                        "This sample code will perform SHA-256 on the input you provide and send that\n" +
                        "digest to the YubiKey as the salt.\n");
                    _ = SampleMenu.ReadResponse(out string dataToDigest);
                    byte[] dataBytes = Encoding.Unicode.GetBytes(dataToDigest);
                    SHA256 digester = CryptographyProviders.Sha256Creator();
                    _ = digester.TransformFinalBlock(dataBytes, inputOffset: 0, dataBytes.Length);

                    salt = new ReadOnlyMemory<byte>(digester.Hash);
                }
            }

            _keyCollector.Operation = Fido2KeyCollectorOperation.GetAssertion;

            if (!Fido2Protocol.RunGetAssertions(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    clientDataHash,
                    relyingPartyId,
                    salt,
                    out IReadOnlyList<GetAssertionData> assertions,
                    out IReadOnlyList<byte[]> hmacSecrets))
            {
                return false;
            }

            if (assertions.Count == 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\nThe YubiKey was not able to get any assertions for the specified relying party ID.\n");

                return true;
            }

            for (int index = 0; index < assertions.Count; index++)
            {
                DisplayAssertion(index, assertions[index], hmacSecrets[index], clientDataHash);
            }

            return true;
        }

        public bool RunListCredentials()
        {
            if (!Fido2Protocol.RunGetCredentialData(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    out IReadOnlyList<object> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, fullReport: true, largeBlobReport: true, out int _);

            return true;
        }

        public bool RunUpdateCredentialUserInfo()
        {
            if (!Fido2Protocol.RunGetCredentialData(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    out IReadOnlyList<object> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, fullReport: false, largeBlobReport: false, out int credentialCount);

            CredentialUserInfo userInfo = SelectCredential(credentialData, credentialCount);

            if (userInfo is null)
            {
                return false;
            }

            UserEntity updatedInfo = GetUpdatedInfo(userInfo.User);

            return Fido2Protocol.RunUpdateUserInfo(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                userInfo.CredentialId,
                updatedInfo);
        }

        public bool RunDeleteCredential()
        {
            if (!Fido2Protocol.RunGetCredentialData(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    out IReadOnlyList<object> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, fullReport: false, largeBlobReport: false, out int credentialCount);

            CredentialUserInfo userInfo = SelectCredential(credentialData, credentialCount);

            if (userInfo is null)
            {
                return false;
            }

            if (!Fido2Protocol.RunDeleteCredential(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    userInfo.CredentialId))
            {
                return false;
            }

            // The FIDO2 standard recommends deleting any largeBlob
            // data associated with a credential along with the credential itself.
            // This sample code will still return true even if something goes
            // wrong trying to delete an entry in the largeBlob data.
            if (userInfo.LargeBlobKey is null)
            {
                return true;
            }

            if (!Fido2Protocol.RunGetLargeBlobArray(
                    _yubiKeyChosen,
                    out SerializedLargeBlobArray blobArray))
            {
                return true;
            }

            _ = Fido2Protocol.GetLargeBlobEntry(
                blobArray, userInfo.LargeBlobKey.Value, out int entryIndex);

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
                    MessageType.Title, numberToWrite: 0,
                    "\nThere is no largeBlob data stored on the YubiKey.\n");

                return true;
            }

            if (blobArray.Entries.Count == 1)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\nThere is one largeBlob entry stored on the YubiKey.\n");
            }
            else
            {
                string entryCount = blobArray.Entries.Count.ToString(CultureInfo.InvariantCulture);
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\nThere are " + entryCount + " largeBlob entries stored on the YubiKey.\n");
            }

            string[] menuItems =
            {
                "Yes",
                "No"
            };
            int response = _menuObject.RunMenu("Continue?", menuItems);
            if (response != 0)
            {
                return true;
            }

            if (!Fido2Protocol.RunGetCredentialData(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    out IReadOnlyList<object> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, fullReport: false, largeBlobReport: true, out int credentialCount);

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "LargeBlob data is stored against a credential. Select a credential for which\n" +
                "you want to see the largeBlob data. It is possible to retrieve data only for\n" +
                "credentials that have an available Large Blob Key.\n");
            CredentialUserInfo userInfo = SelectCredential(credentialData, credentialCount);

            if (userInfo is null)
            {
                return false;
            }

            if (userInfo.LargeBlobKey is null)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "There is no large blob key for this credential, therefore it will not be\n" +
                    "possible to retrieve largeBlob data. It is likely there is no largeBlob\n" +
                    "data for this credential.\n");

                return true;
            }

            string currentContents = Fido2Protocol.GetLargeBlobEntry(
                blobArray, userInfo.LargeBlobKey.Value, out int entryIndex);

            if (entryIndex < 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\nThere is no largeBlob data associated with the selected credential.\n");
            }
            else
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
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
                    out IReadOnlyList<object> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, fullReport: false, largeBlobReport: true, out int credentialCount);

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "LargeBlob data is stored against a credential. That is, for each credential, it\n" +
                "is possible to store some largeBlob data. However, the credential must be made\n" +
                "with the largeBlob option. Hence, you must choose a credential against which the\n" +
                "data will be stored, and that credential must have an available Large Blob Key.\n" +
                "Note that this sample code will store only one entry per credential.\n");
            CredentialUserInfo userInfo = SelectCredential(credentialData, credentialCount);

            if (userInfo is null)
            {
                return false;
            }

            if (userInfo.LargeBlobKey is null)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "There is no large blob key for this credential, therefore it will not be\n" +
                    "possible to store largeBlob data.\n");

                return true;
            }

            string currentContents = Fido2Protocol.GetLargeBlobEntry(
                blobArray, userInfo.LargeBlobKey.Value, out int entryIndex);

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "The largeBlob data can be any binary bytes but this sample code will accept only\n" +
                "strings. This sample code will expect each character in the string to be UTF-16.\n" +
                "If you use any characters other than UTF-16, there is no guarantee this sample will\n" +
                "execute properly.\n" +
                "Note also that what you supply as the largeBlob data will replace the current contents.");

            if (entryIndex < 0)
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "\nThere are no current contents.\n");
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "\nThe current contents:\n\n" + currentContents + "\n");
                blobArray.RemoveEntry(entryIndex);
            }

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the largeBlob data to store.");
            _ = SampleMenu.ReadResponse(out string largeBlobDataString);

            byte[] largeBlobData = Encoding.Unicode.GetBytes(largeBlobDataString);
            blobArray.AddEntry(largeBlobData, userInfo.LargeBlobKey.Value);

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
                    out IReadOnlyList<object> credentialData))
            {
                return false;
            }

            ReportCredentials(credentialData, fullReport: false, largeBlobReport: true, out int credentialCount);

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "LargeBlob data is stored against a credential. Select a credential for which\n" +
                "you want to delete the largeBlob data. If there are no credentials on the\n" +
                "YubiKey, or if there is no largeBlob data stored, or nothing stored against\n" +
                "the selected credential, this sample code will do nothing.\n");

            CredentialUserInfo userInfo = SelectCredential(credentialData, credentialCount);

            if (userInfo is null || userInfo.LargeBlobKey is null)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "There is no large blob key for this credential, therefore it will not be\n" +
                    "possible to delete largeBlob data. It is likely there is no largeBlob\n" +
                    "data for this credential.\n");

                return true;
            }

            string currentContents = Fido2Protocol.GetLargeBlobEntry(
                blobArray, userInfo.LargeBlobKey.Value, out int entryIndex);

            if (entryIndex < 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\nThere is no largeBlob data associated with the selected credential.\n");

                return true;
            }

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "\nThe largeBlob data (to be deleted) for the selected credential is the following:\n\n" +
                currentContents + "\n");
            blobArray.RemoveEntry(entryIndex);

            return Fido2Protocol.RunStoreLargeBlobArray(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                blobArray);
        }

        public bool RunGetBioInfo()
        {
            if (!Fido2Protocol.RunGetBioInfo(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    out BioModality modality,
                    out FingerprintSensorInfo sensorInfo,
                    out IReadOnlyList<TemplateInfo> templates))
            {
                return false;
            }

            ReportBioInfo(modality, sensorInfo, templates, reportTemplatesOnly: false);

            return true;
        }

        public bool RunEnrollFingerprint()
        {
            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "Enter the friendlyName (or simply Enter if you do not want one for now).\n" +
                "Note that if there is already a template with the requested name or if the\n" +
                "YubiKey rejects the name (likely because it is too long), then the template\n" +
                "will be created with no friendly name.");
            _ = SampleMenu.ReadResponse(out string friendlyName);
            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "\nEnter a timeout (for each sample, in milliseconds) if you want to override the\n" +
                "default. Note that some YubiKeys do not allow a timeout override and will ignore\n" +
                "your requested timeout.\n" +
                "If you do not want to override the default, just Enter");
            int timeoutMilliseconds = SampleMenu.ReadResponse(out string _);
            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "\nTo enroll a fingerprint, you will be asked to provide several samples.\n" +
                "You will be notified when to provide a sample. Each notification after the\n" +
                "first will indicate whether the previous sample was good or not, along\n" +
                "with the number of good samples still needed to complete the enrollment.\n");

            string[] menuItems =
            {
                "Yes",
                "No"
            };

            int response = _menuObject.RunMenu("Do you want to continue?", menuItems);
            if (response != 0)
            {
                return true;
            }

            try
            {
                TemplateInfo templateInfo = Fido2Protocol.RunEnrollFingerprint(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    friendlyName,
                    timeoutMilliseconds);

                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "friendly name applied: " + (string.IsNullOrEmpty(templateInfo.FriendlyName) ? "-" : friendlyName));
                string idString = BitConverter.ToString(templateInfo.TemplateId.ToArray())
                    .Replace("-", string.Empty, StringComparison.Ordinal);
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "          templateID : " + idString + "\n");
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is Fido2Exception)
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, ex.Message + "\n\n");
            }

            return true;
        }

        public bool RunSetBioTemplateFriendlyName()
        {
            if (!Fido2Protocol.RunGetBioInfo(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    out BioModality modality,
                    out FingerprintSensorInfo sensorInfo,
                    out IReadOnlyList<TemplateInfo> templates))
            {
                return false;
            }

            ReportBioInfo(modality, sensorInfo, templates, reportTemplatesOnly: true);

            if (templates.Count == 0)
            {
                return true;
            }

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0, "For which template do you want to set the friendly name?");
            int response = SampleMenu.ReadResponse(out string _);
            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0, "Enter the friendly name.");
            _ = SampleMenu.ReadResponse(out string friendlyName);

            return Fido2Protocol.RunSetBioTemplateFriendlyName(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                templates[response - 1].TemplateId,
                friendlyName);
        }

        public bool RunRemoveBioEnrollment()
        {
            if (!Fido2Protocol.RunGetBioInfo(
                    _yubiKeyChosen,
                    _keyCollector.Fido2SampleKeyCollectorDelegate,
                    out BioModality modality,
                    out FingerprintSensorInfo sensorInfo,
                    out IReadOnlyList<TemplateInfo> templates))
            {
                return false;
            }

            ReportBioInfo(modality, sensorInfo, templates, reportTemplatesOnly: true);

            if (templates.Count == 0)
            {
                return true;
            }

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0, "Which template do you want to delete?");
            int response = SampleMenu.ReadResponse(out string _);

            return Fido2Protocol.RunRemoveBioEnrollment(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                templates[response - 1].TemplateId);
        }

        public bool RunEnableEnterpriseAttestation()
        {
            bool isEnabled = Fido2Protocol.RunEnableEnterpriseAttestation(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate);

            if (isEnabled)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "Enterprise Attestation is enabled.\n\n");
            }
            else
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "The selected YubiKey does not support AuthenticatorConfig operations.\n\n");
            }

            return true;
        }

        public bool RunToggleAlwaysUv()
        {
            bool isValid =
                Fido2Protocol.RunGetAuthenticatorInfo(_yubiKeyChosen, out AuthenticatorInfo authenticatorInfo);
            if (!isValid)
            {
                return false;
            }

            OptionValue optionValue = authenticatorInfo.GetOptionValue("alwaysUv");

            string[] menuItems =
            {
                "Yes",
                "No"
            };

            int response;
            switch (optionValue)
            {
                case OptionValue.True:
                    SampleMenu.WriteMessage(
                        MessageType.Title, numberToWrite: 0,
                        "The current status of always UV is True, toggling will set it to False.\n");
                    response = _menuObject.RunMenu("Do you want to toggle alwaysUv to False?", menuItems);
                    break;

                case OptionValue.False:
                    SampleMenu.WriteMessage(
                        MessageType.Title, numberToWrite: 0,
                        "The current status of always UV is False, toggling will set it to True.\n");
                    response = _menuObject.RunMenu("Do you want to toggle alwaysUv to True?", menuItems);
                    break;

                default:
                    SampleMenu.WriteMessage(
                        MessageType.Title, numberToWrite: 0,
                        "The selected YubiKey does not support AuthenticatorConfig operations.\n\n");

                    return true;
            }

            if (response != 0)
            {
                return true;
            }

            isValid = Fido2Protocol.RunToggleAlwaysUv(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                out OptionValue newValue);

            if (isValid)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "The Option alwaysUv is now " + Enum.GetName(newValue) + ".\n\n");
            }

            return isValid;
        }

        public bool RunSetPinConfig()
        {
            bool isValid =
                Fido2Protocol.RunGetAuthenticatorInfo(_yubiKeyChosen, out AuthenticatorInfo authenticatorInfo);
            if (!isValid)
            {
                return false;
            }

            OptionValue setMinPinValue = authenticatorInfo.GetOptionValue(AuthenticatorOptions.setMinPINLength);
            if (setMinPinValue != OptionValue.True)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "The selected YubiKey does not support AuthenticatorConfig operations.\n\n");

                return true;
            }

            int minPinLength = 0;
            var rpIdList = new List<string>();
            bool forceChangePin = false;

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "This operation can\n  reset the minimum PIN length (to a greater length only), and/or\n" +
                "  set the list of relying parties that are allowed to see the minimum PIN length, and/or\n" +
                "  force a PIN change.\n");
            int currentLen = authenticatorInfo.MinimumPinLength ?? AuthenticatorInfo.DefaultMinimumPinLength;

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "The current minimum PIN length is " + currentLen + ".\n");

            string[] menuItems =
            {
                "Yes",
                "No"
            };
            int response = _menuObject.RunMenu("Do you want to change the minimum PIN length?", menuItems);
            if (response == 0)
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the new minimum PIN length");
                minPinLength = SampleMenu.ReadResponse(out string _);
            }

            int rpsAddCount = authenticatorInfo.MaximumRpidsForSetMinPinLength ?? 0;
            if (rpsAddCount == 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\nThis YubiKey does not allow creating a list of RP IDs\n" +
                    "that are allowed to see the minimum PIN length.");
            }
            else
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\nThis YubiKey allows creating a list of RP IDs\n" +
                    "that are allowed to see the minimum PIN length.\n" +
                    "The maximum number of RP IDs in the list is " + rpsAddCount + ".\n");
                response = _menuObject.RunMenu("Do you want to create an RP ID list?", menuItems);
                if (response == 0)
                {
                    for (int index = 0; index < rpsAddCount; index++)
                    {
                        SampleMenu.WriteMessage(
                            MessageType.Title, numberToWrite: 0,
                            "Enter the relyingPartyId or simply Enter if there are no more\n" +
                            " RP IDs for the list.");
                        _ = SampleMenu.ReadResponse(out string relyingPartyId);
                        if (string.IsNullOrWhiteSpace(relyingPartyId))
                        {
                            break;
                        }

                        rpIdList.Add(relyingPartyId);
                    }
                }
            }

            response = _menuObject.RunMenu("\nDo you want to force the PIN to be changed?", menuItems);
            forceChangePin = response == 0;

#nullable enable
            int? newMinPinLength = null;
            if (minPinLength != 0)
            {
                newMinPinLength = minPinLength;
            }

            isValid = Fido2Protocol.RunSetPinConfig(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                newMinPinLength,
                rpIdList.Count == 0 ? null : rpIdList,
                forceChangePin);
#nullable restore

            if (isValid)
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "\nSet PIN Config successful.\n");
            }

            return isValid;
        }

        // If the input arg reportTemplatesOnly is true, report only the
        // templates. Otherwise, report all the info.
        private static void ReportBioInfo(
            BioModality modality,
            FingerprintSensorInfo sensorInfo,
            IReadOnlyList<TemplateInfo> templates,
            bool reportTemplatesOnly)
        {
            if (modality == BioModality.None)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "The selected YubiKey does not support Bio operations.");

                return;
            }

            if (!reportTemplatesOnly)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "Bio Modality: " + Enum.GetName(typeof(BioModality), modality));
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "Fingerprint Kind: " + (sensorInfo.FingerprintKind == 1 ? "touch type" : "swipe type"));
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "Maximum Capture Count: " + sensorInfo.MaxCaptureCount);
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "Maximum Friendly Name Bytes: " + sensorInfo.MaxFriendlyNameBytes + "\n");
            }

            if (templates.Count == 0)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "There are no fingerprint templates on the selected YubiKey.\n");

                return;
            }

            for (int index = 0; index < templates.Count; index++)
            {
                int counter = index + 1;
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    counter + "  Template Friendly Name: " + templates[index].FriendlyName);
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "   Template ID: " +
                    BitConverter.ToString(
                        templates[index].TemplateId.ToArray()).Replace("-", string.Empty, StringComparison.Ordinal) +
                    "\n");
            }
        }

        private void DisplayAssertion(
            int index,
            GetAssertionData assertion,
            byte[] hmacSecret,
            ReadOnlyMemory<byte> clientDataHash)
        {
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Assertion number " + index);
            string userIdString = BitConverter.ToString(assertion.User.Id.ToArray())
                .Replace("-", string.Empty, StringComparison.Ordinal);
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "User ID: " + userIdString);
            if (!(assertion.User.DisplayName is null))
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "User DisplayName: " + assertion.User.DisplayName);
            }

            string credentialIdString = BitConverter.ToString(assertion.CredentialId.Id.ToArray())
                .Replace("-", string.Empty, StringComparison.Ordinal);
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Credential ID: " + credentialIdString);

            byte[] credBlobData = assertion.AuthenticatorData.GetCredBlobExtension();
            if (credBlobData.Length > 0)
            {
                string credBlobDataString = Encoding.Unicode.GetString(credBlobData);
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Credential Blob: " + credBlobDataString);
            }

            if (hmacSecret.Length > 0)
            {
                string hmacSecretString = BitConverter.ToString(hmacSecret)
                    .Replace("-", string.Empty, StringComparison.Ordinal);
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "HMAC Secret: " + hmacSecretString);
            }

            int indexC = FindCredential(assertion.CredentialId.Id);
            if (indexC >= 0)
            {
                bool isVerified = assertion.VerifyAssertion(
                    _credentialList[indexC].AuthenticatorData.CredentialPublicKey, clientDataHash);

                string verifyResult = isVerified ? "is verified" : "does not verify";
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "Assertion signature " + verifyResult + "\n");
            }
            else
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
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
                if (_credentialList[index].AuthenticatorData.CredentialId.Id.Span.SequenceEqual(credentialId.Span))
                {
                    break;
                }
            }

            return index < _credentialList.Count ? index : -1;
        }

        // This does not build a real ClientDataHash. It builds something that
        // looks like a real ClientDataHash, but this is not the algorithm used
        // to build the proper value. To see how to build a proper
        // ClientDataHash, see section 5.8.1 of the W3C WebAuthn API
        // recommendations.
        private static ReadOnlyMemory<byte> BuildFakeClientDataHash(string relyingPartyId)
        {
            byte[] idBytes = Encoding.Unicode.GetBytes(relyingPartyId);

            // Generate a random value to represent the challenge.
            RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            byte[] randomBytes = new byte[16];
            randomObject.GetBytes(randomBytes);

            SHA256 digester = CryptographyProviders.Sha256Creator();
            _ = digester.TransformBlock(randomBytes, inputOffset: 0, randomBytes.Length, outputBuffer: null,
                outputOffset: 0);
            _ = digester.TransformFinalBlock(idBytes, inputOffset: 0, idBytes.Length);

            return new ReadOnlyMemory<byte>(digester.Hash);
        }

        private static void WritePinMessage(string operation, bool result)
        {
            SampleMenu.WriteMessage(MessageType.Special, numberToWrite: 0,
                operation + (result ? ", success" : ", user canceled"));
        }

        // Get arguments to verifyPin or verifyUv. If verifyPin is true, get
        // arguments to verify the PIN. If false, get args for verifyUv.
        private bool GetVerifyArguments(
            bool verifyPin,
            AuthenticatorInfo authenticatorInfo,
            out PinUvAuthTokenPermissions? permissions,
            out string relyingPartyId)
        {
            permissions = null;
            relyingPartyId = "";

            bool isPinUvAuthTokenOption = CheckPinUvAuthTokenOption(authenticatorInfo, out bool uvReady);

            string verifyInstructions;
            if (verifyPin)
            {
                string supportString = isPinUvAuthTokenOption ? "supports" : "does not support";
                verifyInstructions =
                    "In order to verify a PIN, the caller has the option to specify the permissions\n" +
                    "and possibly a relying party ID.\n" +
                    "However, that is possible only if the YubiKey supports the pinUvAuthToken Option\n" +
                    "(see the AuthenticatorInfo returned by Fido2Session.GetAuthenticatorInfo).\n" +
                    "The YubiKey chosen " + supportString + " that Option.\n\n";
            }
            else
            {
                string supportString = uvReady ? "can" : "cannot";
                verifyInstructions =
                    "In order to perform the verifyUv operation, the YubiKey must support BioEnroll\n" +
                    "and have a fingerprint enrolled.\n" +
                    "Furthermore, the caller must specify the permissions and possibly a\n" +
                    "relying party ID.\n" +
                    "The YubiKey chosen " + supportString + " perform fingerprint verification.\n\n";
            }

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, verifyInstructions);

            if (!verifyPin && !uvReady)
            {
                return false;
            }

            if (!isPinUvAuthTokenOption)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
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
        // If the YubiKey can perform fingerprint verification, set uvReady to
        // true.
        private static bool CheckPinUvAuthTokenOption(AuthenticatorInfo authenticatorInfo, out bool uvReady)
        {
            uvReady = false;
            if (authenticatorInfo.GetOptionValue(AuthenticatorOptions.pinUvAuthToken) == OptionValue.True)
            {
                uvReady = authenticatorInfo.GetOptionValue(AuthenticatorOptions.uv) == OptionValue.True;
                return true;
            }

            return false;
        }

        private bool CollectPermissions(out PinUvAuthTokenPermissions? permissions)
        {
            permissions = null;
            PinUvAuthTokenPermissions current = PinUvAuthTokenPermissions.None;

            SampleMenu.WriteMessage(
                MessageType.Title, numberToWrite: 0,
                "It is possible to specify more than one permission. In the following,\n" +
                "select one permission to add to your total. The menu will then repeat,\n" +
                "allowing you to add more. When there are no more permissions to add,\n" +
                "select No More\n");
            string[] menuItems =
            {
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
                response = _menuObject.RunMenu(
                    "Which permission would you like to add? (Choose No More when complete.)", menuItems);
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
                    MessageType.Title, numberToWrite: 0,
                    "\nBased on the permissions, a relyingPartyId is required\n");
            }
            else if (current.HasFlag(PinUvAuthTokenPermissions.CredentialManagement))
            {
                SampleMenu.WriteMessage(
                    MessageType.Title, numberToWrite: 0,
                    "\nBased on the permissions, a relyingPartyId is optional.\n");
                string[] menuItems =
                {
                    "Yes",
                    "No"
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
                    MessageType.Title, numberToWrite: 0,
                    "\nBased on the permissions, a relyingPartyId will be ignored, so\n" +
                    "this sample will not collect one.");
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out relyingPartyId);

            return true;
        }

        private static void ReportCredentials(
            IReadOnlyList<object> credentialData,
            bool fullReport,
            bool largeBlobReport,
            out int credentialCount)
        {
            credentialCount = 0;
            foreach (object current in credentialData)
            {
                // If the current object is really a Tuple<int,int>, then this
                // entry is metadata.
                if (current is Tuple<int, int> metadata)
                {
                    ReportMetadata(metadata, fullReport);
                    continue;
                }

                if (current is RelyingParty relyingParty)
                {
                    ReportRelyingParty(relyingParty, fullReport);
                    continue;
                }

                if (current is CredentialUserInfo userInfo)
                {
                    credentialCount++;
                    ReportCredential(userInfo, fullReport, largeBlobReport, credentialCount);
                }
            }

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "\n");
        }

        private static void ReportMetadata(Tuple<int, int> metadata, bool fullReport)
        {
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "Discoverable credentials:  " + metadata.Item1);

            if (fullReport)
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "Remaining available slots: " + metadata.Item2);
            }
        }

        private static void ReportRelyingParty(RelyingParty relyingParty, bool fullReport)
        {
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "\n  Relying party ID:       " + relyingParty.Id);
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "  Relying party Name:     " + (relyingParty.Name ?? "-"));
            if (fullReport)
            {
                byte[] idHash = relyingParty.RelyingPartyIdHash.ToArray();
                string rpIdHash = BitConverter.ToString(idHash);
                rpIdHash = rpIdHash.Replace("-", "", StringComparison.Ordinal);
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "  Relying party ID Hash:  " + rpIdHash);
            }

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "-----------");
        }

        private static void ReportCredential(
            CredentialUserInfo userInfo, bool fullReport, bool largeBlobReport, int credentialIndex)
        {
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Credential index = " + credentialIndex);
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "        User Name:          " + userInfo.User.Name);
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "        User Display Name:  " + (userInfo.User.DisplayName ?? "-"));
            if (fullReport)
            {
                byte[] id = userInfo.User.Id.ToArray();
                string userId = BitConverter.ToString(id);
                userId = userId.Replace("-", "", StringComparison.Ordinal);
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "        User ID:            " + userId);
                id = userInfo.CredentialId.Id.ToArray();
                string credId = BitConverter.ToString(id);
                credId = credId.Replace("-", "", StringComparison.Ordinal);
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "        Credential ID:      " + credId);
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "        CredProtect Policy: " + userInfo.CredProtectPolicy);
            }

            if (largeBlobReport)
            {
                string lbKeyStatus = userInfo.LargeBlobKey is null ? "not available" : "available";
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "        Large Blob Key:     " + lbKeyStatus);
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "-----------");
            }
        }

        // Return the CredentialUserInfo for the credential of interest in the
        // credentialData List.
        // If none is selected, return null.
        private CredentialUserInfo SelectCredential(
            IReadOnlyList<object> credentialData, int credentialCount)
        {
            if (credentialCount > 0)
            {
                string[] menuItems = new string[credentialCount];
                int index = 1;
                for (; index <= credentialCount; index++)
                {
                    menuItems[index - 1] = "Credential index " + index.ToString(CultureInfo.InvariantCulture);
                }

                ;

                int response = _menuObject.RunMenu("On which credential do you want to operate?", menuItems);

                int credIndex = 0;
                foreach (object current in credentialData)
                {
                    if (current is CredentialUserInfo userInfo)
                    {
                        if (credIndex == response)
                        {
                            return userInfo;
                        }

                        credIndex++;
                    }
                }
            }

            // Error case
            return null;
        }

        private UserEntity GetUpdatedInfo(UserEntity original)
        {
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "Current Name:         " + (original.Name ?? "-"));
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "Current Display Name: " + (original.DisplayName ?? "-"));
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                "It is not possible to change the User.Id component.");

            var returnValue = new UserEntity(original.Id);

            string[] menuItems =
            {
                "Yes",
                "No"
            };

            int response = _menuObject.RunMenu("Do you want to change the Name?", menuItems);
            if (response == 0)
            {
                SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0,
                    "Enter the new Name (simply press Enter to remove the Name).");
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
                    MessageType.Title, numberToWrite: 0,
                    "Enter the new DisplayName (simply press Enter to remove the DisplayName).");
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
