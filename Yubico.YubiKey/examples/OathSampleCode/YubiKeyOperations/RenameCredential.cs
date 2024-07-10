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
using System.Text;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.OathSampleCode
{
    public static class RenameCredential
    {
        // Rename the existing credential on the YubiKey by setting new issuer and account names.
        // If menuObject is null it means the user picked to choose a credential from the list and
        // use pre-configured issuer and account names to rename the chosen credential to.
        // Otherwise we prompt to enter a credential and new issuer and account names.
        public static bool RunRenameCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            Credential credential,
            string newIssuer,
            string newAccount)
        {
            if (credential is null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;

                Credential renamedCredential = oathSession.RenameCredential(
                    credential.Issuer,
                    credential.AccountName,
                    newIssuer,
                    newAccount,
                    credential.Type.Value,
                    credential.Period.Value);

                ReportResult(renamedCredential);
            }
            return true;
        }

        private static void ReportResult(Credential credential)
        {
            var outputList = new StringBuilder("Renamed credential:");
            _ = outputList.AppendLine();
            _ = outputList.AppendLine($"Issuer    : {credential.Issuer}");
            _ = outputList.AppendLine($"Account   : {credential.AccountName}");
            _ = outputList.AppendLine($"Type      : {credential.Type}");
            _ = outputList.AppendLine($"Period    : {(int?)credential.Period}sec");
            _ = outputList.AppendLine($"Digits    : {credential.Digits}");
            _ = outputList.AppendLine($"Algorithm : {credential.Algorithm}");
            _ = outputList.AppendLine($"Secret    : {credential.Secret}");
            _ = outputList.AppendLine($"Counter   : {credential.Counter}");
            _ = outputList.AppendLine($"Touch     : {credential.RequiresTouch}");

            SampleMenu.WriteMessage(MessageType.Special, numberToWrite: 0, outputList.ToString());
        }

        // Collect a credential.
        private static void RunCollectCredential(
            SampleMenu menuObject,
            out Credential credential,
            out string newIssuer,
            out string newAccount)
        {
            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter current issuer");
            _ = SampleMenu.ReadResponse(out string currentIssuer);

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter current account name");
            _ = SampleMenu.ReadResponse(out string currentAccount);

            _ = ChooseCredentialProperties.RunChooseTypeOption(menuObject, out CredentialType? type);

            CredentialPeriod period = CredentialPeriod.Undefined;

            if (type == CredentialType.Totp)
            {
                _ = ChooseCredentialProperties.RunChoosePeriodOption(menuObject,
                    out CredentialPeriod? credentialPeriod);
                period = credentialPeriod.Value;
            }

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter new issuer");
            _ = SampleMenu.ReadResponse(out string issuer);

            SampleMenu.WriteMessage(MessageType.Title, numberToWrite: 0, "Enter new account name");
            _ = SampleMenu.ReadResponse(out string account);

            newIssuer = issuer;
            newAccount = account;
            credential = new Credential(currentIssuer, currentAccount, type.Value, period);
        }
    }
}
