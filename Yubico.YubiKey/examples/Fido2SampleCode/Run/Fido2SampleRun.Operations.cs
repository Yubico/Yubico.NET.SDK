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
using System.Collections.Generic;
using System.Security.Cryptography;
using Yubico.YubiKey.Sample.SharedCode;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;

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
                "info, and will set the \"rk\" option to true. No other optional elements will be set.\n" +
                "For expedience, it will generate a value based on the relying party info and use it as\n" +
                "the ClientDataHash, even though that is not how it is actually computed.\n" +
                "In addition, the User ID is binary data (a byte array), and this sample simply uses\n" +
                "random numbers (as recommended in W3C).\n");

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the relyingPartyName (optional)");
            _ = SampleMenu.ReadResponse(out string relyingPartyName);
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out string relyingPartyId);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the user Name (optional)");
            _ = SampleMenu.ReadResponse(out string userName);
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the user DisplayName (optional)");
            _ = SampleMenu.ReadResponse(out string userDisplayName);

            RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            byte[] randomBytes = new byte[16];
            randomObject.GetBytes(randomBytes);
            var userId = new ReadOnlyMemory<byte>(randomBytes);

            ReadOnlyMemory<byte> clientDataHash = BuildFakeClientDataHash(relyingPartyId);

            bool isValid = Fido2Protocol.RunMakeCredential(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                clientDataHash,
                relyingPartyName, relyingPartyId,
                userName, userDisplayName, userId,
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
                "as the ClientDataHash, even though that is not how it is actually computed.\n");

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out string relyingPartyId);

            ReadOnlyMemory<byte> clientDataHash = BuildFakeClientDataHash(relyingPartyId);

            bool isValid = Fido2Protocol.RunGetAssertions(
                _yubiKeyChosen,
                _keyCollector.Fido2SampleKeyCollectorDelegate,
                clientDataHash,
                relyingPartyId,
                out IList<GetAssertionData> assertions);

            if (!isValid)
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
                    "yes",
                    "no",
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
                    "\nBased on the permissions, a relyingPartyId will be ignored,.\n" +
                    "this sample will not collect one.");
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter the relyingPartyId");
            _ = SampleMenu.ReadResponse(out relyingPartyId);

            return true;
        }
    }
}
