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
    // This file contains the methods to run each of the main menu items.
    // The main menu is displayed, the user selects an option, and the code that
    // receives the choice will call the appropriate method in this file to
    // make the appropriate calls to perform the operation selected.
    public partial class OathSampleRun
    {
        public OathMainMenuItem RunMenuItem(OathMainMenuItem menuItem)
        {
            bool isValid = true;

            switch (menuItem)
            {
                default:
                case OathMainMenuItem.Exit:
                    break;

                case OathMainMenuItem.ListYubiKeys:
                    // Find all currently connected YubiKeys that can communicate
                    // over the SmartCard (CCID) protocol. This is the protocol
                    // used to communicate with the OATH application.
                    // Using Transport.SmartCard finds all YubiKeys connected via USB and
                    // NFC.
                    // To get only YubiKeys connected via USB, call
                    //   YubiKey.FindByTransport(Transport.UsbSmartCard);
                    // To get only YubiKeys connected via NFC, call
                    //   YubiKey.FindByTransport(Transport.NfcSmartCard);
                    isValid = ListYubiKeys.RunListYubiKeys(Transport.SmartCard);
                    break;

                case OathMainMenuItem.ChooseYubiKey:
                    // If there are no YubiKeys, this will return false. In that
                    // case, we don't want to exit, we just want to report the
                    // result and run through the main menu again.
                    _ = ChooseYubiKey.RunChooseYubiKey(true, _menuObject, Transport.SmartCard, ref _yubiKeyChosen);
                    break;

                case OathMainMenuItem.GetOathCredentials:
                    isValid = GetCredentials.RunGetCredentials(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate);
                    break;

                case OathMainMenuItem.CalculateOathCredentials:
                    isValid = CalculateCredentials.RunCalculateCredentials(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate);
                    break;

                case OathMainMenuItem.CalculateSpecificOathCredential:
                    _ = ChooseCredential.RunChooseAction(_menuObject, out _optionIndex, "calculate");
                    isValid = RunCalculateCredentialMenuItem(_optionIndex);
                    break;

                case OathMainMenuItem.AddOathCredential:
                    _ = ChooseCredential.RunChooseAddCredentialOption(_menuObject, out _optionIndex);
                    _ = ChooseCredential.RunChooseCredentialOption(_menuObject, out _index);
                    isValid = RunAddCredentialMenuItem(_index);
                    break;

                case OathMainMenuItem.RenameOathCredential:
                    _ = ChooseCredential.RunChooseAction(_menuObject, out _optionIndex, "rename");
                    isValid = RunRenameCredentialMenuItem(_optionIndex);
                    break;

                case OathMainMenuItem.RemoveOathCredential:
                    _ = ChooseCredential.RunChooseAction(_menuObject, out _optionIndex, "remove");
                    isValid = RunRemoveCredentialMenuItem(_optionIndex);
                    break;

                case OathMainMenuItem.VerifyOathPassword:
                    isValid = ManagePassword.RunVerifyPassword(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate);
                    break;

                case OathMainMenuItem.SetOathPassword:
                    isValid = ManagePassword.RunSetPassword(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate);
                    break;

                case OathMainMenuItem.UnsetOathPassword:
                    isValid = ManagePassword.RunUnsetPassword(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate);
                    break;

                case OathMainMenuItem.ResetOath:
                    isValid = ResetApplication.RunResetOath(_yubiKeyChosen);
                    break;
            }

            return isValid ? menuItem : OathMainMenuItem.Exit;
        }

        // Collect a credential.
        private void RunDefaultCollectCredential()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter issuer");
            _ = SampleMenu.ReadResponse(out string issuer);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter account name");
            _ = SampleMenu.ReadResponse(out string account);

            _ = ChooseCredentialProperties.RunChooseTypeOption(_menuObject, out CredentialType? type);

            CredentialPeriod period = CredentialPeriod.Undefined;

            if (type == CredentialType.Totp)
            {
                _ = ChooseCredentialProperties.RunChoosePeriodOption(_menuObject,
                    out CredentialPeriod? credentialPeriod);
                period = (CredentialPeriod)credentialPeriod;
            }

            _credentialChosen = new Credential(issuer, account, (CredentialType)type, period);
        }

        private bool RunAddCredentialMenuItem(int? index)
        {
            switch (index)
            {
                case 0:
                    return AddCredential.RunAddDefaultTotpCredential(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate,
                        _optionIndex != 0 ? null : _menuObject);
                case 1:
                    return AddCredential.RunAddTotpCredential(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate,
                        _optionIndex != 0 ? null : _menuObject);
                case 2:
                    return AddCredential.RunAddDefaultHotpCredential(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate,
                        _optionIndex != 0 ? null : _menuObject);
                case 3:
                    return AddCredential.RunAddHotpCredential(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate,
                        _optionIndex != 0 ? null : _menuObject);
                case 4:
                    return AddCredential.RunAddCredentialFromQR(
                        _yubiKeyChosen,
                        SampleKeyCollector.SampleKeyCollectorDelegate,
                        _optionIndex != 0 ? null : _menuObject);
            }

            return false;
        }

        private bool RunRemoveCredentialMenuItem(int? index)
        {
            if (index != 0)
            {
                _ = ChooseCredential.RunChooseCredential(
                    _yubiKeyChosen,
                    true,
                    _menuObject,
                    out _credentialChosen);
            }
            else
            {
                RunDefaultCollectCredential();
            }

            return RemoveCredential.RunRemoveCredential(
                _yubiKeyChosen,
                _credentialChosen,
                SampleKeyCollector.SampleKeyCollectorDelegate);
        }

        private bool RunCalculateCredentialMenuItem(int? index)
        {
            if (index != 0)
            {
                _ = ChooseCredential.RunChooseCredential(
                    _yubiKeyChosen,
                    true,
                    _menuObject,
                    out _credentialChosen);
            }
            else
            {
                RunDefaultCollectCredential();
            }

            return CalculateCredentials.RunCalculateOneCredential(
                _yubiKeyChosen,
                _credentialChosen,
                SampleKeyCollector.SampleKeyCollectorDelegate);
        }

        private bool RunRenameCredentialMenuItem(int? index)
        {
            if (index != 0)
            {
                _ = ChooseCredential.RunChooseCredential(
                    _yubiKeyChosen,
                    true,
                    _menuObject,
                    out _credentialChosen);

                return RenameCredential.RunRenameCredential(
                    _yubiKeyChosen,
                    SampleKeyCollector.SampleKeyCollectorDelegate,
                    _credentialChosen,
                    "Yubico",
                    "testRename@example.com");
            }
            else
            {
                RunCollectCredential(_menuObject,
                    out Credential credential,
                    out string newIssuer,
                    out string newAccount);

                return RenameCredential.RunRenameCredential(
                    _yubiKeyChosen,
                    SampleKeyCollector.SampleKeyCollectorDelegate,
                    credential,
                    newIssuer,
                    newAccount);
            }
        }

        // Collect a credential.
        private static void RunCollectCredential(
            SampleMenu menuObject,
            out Credential credential,
            out string newIssuer,
            out string newAccount)
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter current issuer");
            _ = SampleMenu.ReadResponse(out string currentIssuer);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter current account name");
            _ = SampleMenu.ReadResponse(out string currentAccount);

            _ = ChooseCredentialProperties.RunChooseTypeOption(menuObject, out CredentialType? type);

            CredentialPeriod period = CredentialPeriod.Undefined;

            if (type == CredentialType.Totp)
            {
                _ = ChooseCredentialProperties.RunChoosePeriodOption(menuObject,
                    out CredentialPeriod? credentialPeriod);
                period = credentialPeriod.Value;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter new issuer");
            _ = SampleMenu.ReadResponse(out string issuer);

            SampleMenu.WriteMessage(MessageType.Title, 0, "Enter new account name");
            _ = SampleMenu.ReadResponse(out string account);

            newIssuer = issuer;
            newAccount = account;
            credential = new Credential(currentIssuer, currentAccount, type.Value, period);
        }
    }
}
