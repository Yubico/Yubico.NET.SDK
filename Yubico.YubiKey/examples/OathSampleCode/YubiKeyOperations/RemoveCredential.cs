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
    public static class RemoveCredential
    {
        // Remove an existing credential on the YubiKey.
        public static bool RunRemoveCredential(
            IYubiKeyDevice yubiKey,
            Credential credential,
            Func<KeyEntryData, bool> KeyCollectorDelegate)
        {
            if (credential is null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            using var oathSession = new OathSession(yubiKey);
            {
                oathSession.KeyCollector = KeyCollectorDelegate;
                oathSession.RemoveCredential(credential);
                ReportResult(credential);
            }

            return true;
        }

        private static void ReportResult(Credential credential)
        {
            var outputList = new StringBuilder("Removed credential:");
            _ = outputList.AppendLine();
            _ = outputList.AppendLine($"Issuer    : {credential.Issuer}");
            _ = outputList.AppendLine($"Account   : {credential.AccountName}");
            _ = outputList.AppendLine($"Type      : {credential.Type}");
            _ = outputList.AppendLine($"Period    : {(int?)credential.Period}sec");

            SampleMenu.WriteMessage(MessageType.Special, numberToWrite: 0, outputList.ToString());
        }
    }
}
